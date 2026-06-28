---
name: Vendedor Cancela Evento com Reembolso Obrigatório
status: verified
---

# Spec 50: Vendedor Cancela Evento com Reembolso Obrigatório — Design

## Design Approach

**Estratégia:** Cancelamento lógico com transação atômica Dapper. O endpoint `DELETE /api/evento/{id}` mantém o verbo HTTP mas muda a semântica: de exclusão física para cancelamento lógico com reembolso. Um novo endpoint `GET /api/evento/{id}/status-cancelamento` permite consultar o impacto antes de cancelar.

**Padrões:** Clean Architecture (ADR-001), Repository Pattern via Dapper (ADR-002), DbUp para migrations (ADR-008), Cancelamento Lógico (ADR-012).

**Princípios:** KISS — o cancelamento é implementado como uma transação SQL direta, sem orquestração distribuída. DRY — a lógica de autorização (dono do evento ou Admin) é reutilizada entre os dois endpoints.

---

## Architecture Decisions

| Decisão | Justificativa |
|---------|---------------|
| Cancelamento lógico (`Cancelado = true`) em vez de exclusão física | ADR-012: preserva histórico para auditoria e consultas de compradores |
| `GET /api/evento/{id}/status-cancelamento` como endpoint separado | Separação de responsabilidades — o frontend consulta o impacto antes de confirmar |
| `Ingressos.Status = 3` para reembolsado | Consistente com spec 170 (StatusPagamento usa inteiros; Ingressos usa 3 para diferenciar de 0/1/2) |
| Transação condicional por `gratuito` | Eventos gratuitos não têm pagamentos — evita UPDATEs desnecessários |
| Admin pode cancelar qualquer evento | Consistente com o comportamento atual do `DeleteAsync` |
| `DeleteAsync` removido da interface | Cancelamento é lógico, não físico |
| Endpoint `DELETE` mantido com semântica alterada | REST: remover o recurso do estado ativo |
| Dapper com transação explícita (`BeginTransaction`/`Commit`/`Rollback`) | ADR-002: SQL explícito, sem ORM magic |

---

## Component / Module Breakdown

### Component 1: `StatusCancelamentoDTO` (Application Layer — NOVO)

**Purpose:** DTO imutável para o response do endpoint de consulta de status de cancelamento.

**Interface:**
```csharp
// Arquivo NOVO: Application/DTOs/StatusCancelamentoDTO.cs
namespace Application.DTOs;

public record StatusCancelamentoDTO(
    Guid EventoId,
    string Nome,
    bool Gratuito,
    int TotalIngressosVendidos,
    int TotalReservasAtivas,
    bool ReembolsoNecessario,
    decimal ValorTotalReembolso,
    string Mensagem
);
```

**Internal Logic:** Nenhuma — é um DTO puro (record imutável). O campo `Mensagem` é preenchido pelo `EventoService` com base nos dados agregados.

**Dependencies:** Nenhuma. Referenciado por `Domain/Interface/IEventoRepository.cs` e `Application/Interfaces/IEventoService.cs`.

**Error Handling:** N/A — DTO não contém lógica.

---

### Component 2: `IEventoRepository` (Domain Layer — MODIFICADO)

**Purpose:** Interface do repositório de eventos. Adiciona 2 métodos e remove 1.

**Interface (exata — após modificação):**
```csharp
// Arquivo: Domain/Interface/IEventoRepository.cs
// MÉTODOS EXISTENTES (não modificar):
//   Task<IEnumerable<Evento>> GetAllAsync();
//   Task<Evento?> GetByIdAsync(Guid id);
//   Task<Guid> CriarEventoCompletoAsync(Evento evento);
//   Task UpdateAsync(Evento evento);

// NOVOS MÉTODOS (adicionar):
/// <summary>
/// Retorna dados agregados sobre o impacto do cancelamento.
/// </summary>
Task<StatusCancelamentoDTO> ObterStatusCancelamento(Guid eventoId, CancellationToken ct);

/// <summary>
/// Executa cancelamento do evento em transação atômica.
/// Marca Evento.Cancelado, Ingressos.Status=3, Reservas.Reembolsada,
/// ItensReserva.Reembolsado, Pagamentos.Status=3.
/// </summary>
Task CancelarComTransacao(Guid eventoId, bool gratuito, CancellationToken ct);

// MÉTODO A REMOVER:
// Task DeleteAsync(Guid id);  ← REMOVER da interface
```

**Dependencies:** `Application/DTOs/StatusCancelamentoDTO.cs`

**Error Handling:** N/A — interface não implementa, apenas declara.

---

### Component 3: `EventoRepository` (Infrastructure Layer — MODIFICADO)

**Purpose:** Implementação Dapper do repositório de eventos. Adiciona 2 métodos implementados e modifica 1 query existente.

#### 3.1 Método: `ObterStatusCancelamento`

**Interface:**
```csharp
public async Task<StatusCancelamentoDTO> ObterStatusCancelamento(Guid eventoId, CancellationToken ct)
```

**Internal Logic:**
1. Abre conexão via `_factory.CreateConnection()`
2. Executa query SQL agregada (ver seção Data Flow §1)
3. Mapeia resultado para `StatusCancelamentoDTO` via `QuerySingleOrDefaultAsync`
4. Se nenhum resultado, retorna `null` (tratado no Service)

**SQL Query:**
```sql
SELECT
    e.Id              AS EventoId,
    e.Nome            AS Nome,
    CAST(CASE WHEN e.PrecoPadrao = 0 THEN 1 ELSE 0 END AS BIT) AS Gratuito,
    COUNT(i.Id)       AS TotalIngressosVendidos,
    COUNT(DISTINCT r.Id) AS TotalReservasAtivas,
    CAST(CASE WHEN e.PrecoPadrao > 0 AND COUNT(i.Id) > 0 THEN 1 ELSE 0 END AS BIT) AS ReembolsoNecessario,
    ISNULL(SUM(ir.PrecoUnitario), 0) AS ValorTotalReembolso
FROM Eventos e
LEFT JOIN Ingressos i ON i.EventoId = e.Id AND i.Status = 2
LEFT JOIN ItensReserva ir ON ir.IngressoId = i.Id
LEFT JOIN Reservas r ON r.EventoId = e.Id AND r.Reembolsada = 0
WHERE e.Id = @eventoId
GROUP BY e.Id, e.Nome, e.PrecoPadrao
```

**Dependencies:** `_factory` (`ConnectionFactory` injetado), Dapper `QuerySingleOrDefaultAsync`

**Error Handling:** Se a query falhar (ex: timeout), a exceção do Dapper é propagada para o Service.

#### 3.2 Método: `CancelarComTransacao`

**Interface:**
```csharp
public async Task CancelarComTransacao(Guid eventoId, bool gratuito, CancellationToken ct)
```

**Internal Logic (passo a passo):**
1. Abre conexão via `_factory.CreateConnection()`
2. `await connection.OpenAsync(ct)`
3. `using var transaction = connection.BeginTransaction()`
4. Executa UPDATEs em ordem (ver Data Flow §2)
5. `await transaction.CommitAsync(ct)`
6. Se qualquer passo falhar → `catch { transaction.RollbackAsync(ct); throw; }`

**Ordem exata dos UPDATEs dentro da transação:**

```
1. UPDATE Eventos SET Cancelado = 1 WHERE Id = @eventoId

2. IF gratuito == false:
     UPDATE Ingressos SET Status = 3 WHERE EventoId = @eventoId AND Status = 2

3. UPDATE Reservas SET Reembolsada = 1 WHERE EventoId = @eventoId AND Reembolsada = 0

4. UPDATE ItensReserva SET Reembolsado = 1
   WHERE ReservaId IN (SELECT Id FROM Reservas WHERE EventoId = @eventoId)

5. IF gratuito == false:
     UPDATE Pagamentos SET Status = 3
     WHERE ReservaId IN (SELECT Id FROM Reservas WHERE EventoId = @eventoId)
     AND Status = 1
```

**Dependencies:** `_factory` (`ConnectionFactory` injetado), Dapper `ExecuteAsync` com `CommandDefinition`

**Error Handling:**
- Qualquer exceção durante os UPDATEs → `transaction.RollbackAsync(ct)` + `throw` (propaga a exceção original)
- `RollbackAsync` é chamado em bloco `catch`, sem try-catch aninhado — se o rollback falhar, a exceção do rollback substitui a original

#### 3.3 Query modificada: `GetAllAsync`

**Mudança:** Adicionar cláusula `WHERE Cancelado = 0` na query SQL de listagem pública.

```sql
-- Antes:
SELECT * FROM Eventos

-- Depois:
SELECT * FROM Eventos WHERE Cancelado = 0
```

**Atenção:** Esta mudança afeta apenas `GetAllAsync()`. `GetByIdAsync()`, `GetByVendedorCpfAsync()` e outras queries continuam sem filtro de `Cancelado`.

**Dependencies:** Nenhuma nova.

**Error Handling:** N/A — modificação de query existente.

---

### Component 4: `IEventoService` (Application Layer — MODIFICADO)

**Purpose:** Interface do serviço de eventos. Adiciona 2 métodos e remove 1.

**Interface (exata — após modificação):**
```csharp
// Arquivo: Application/Interfaces/IEventoService.cs
// MÉTODOS EXISTENTES (não modificar):
//   Task<IEnumerable<EventoDTO>> GetAllAsync();
//   Task<EventoDTO?> GetByIdAsync(Guid id);
//   Task<EventoDTO> CriarEventoAsync(CriarEventoDTO dto, string vendedorCpf);
//   Task UpdateAsync(Guid id, AtualizarEventoDTO dto, string vendedorCpf, bool isAdmin);

// NOVOS MÉTODOS (adicionar):
Task<StatusCancelamentoDTO> ObterStatusCancelamento(Guid eventoId, string vendedorCpf, bool isAdmin, CancellationToken ct);
Task CancelarEvento(Guid eventoId, string vendedorCpf, bool isAdmin, CancellationToken ct);

// MÉTODO A REMOVER:
// Task DeleteAsync(Guid id, string vendedorCpf, bool isAdmin = false);  ← REMOVER
```

**Dependencies:** `Application/DTOs/StatusCancelamentoDTO.cs`

**Error Handling:** N/A — interface.

---

### Component 5: `EventoService` (Application Layer — MODIFICADO)

**Purpose:** Implementação do serviço de eventos. Adiciona 2 métodos e remove `DeleteAsync`.

#### 5.1 Método: `ObterStatusCancelamento`

**Interface:**
```csharp
public async Task<StatusCancelamentoDTO> ObterStatusCancelamento(
    Guid eventoId, string vendedorCpf, bool isAdmin, CancellationToken ct)
```

**Internal Logic (passo a passo):**
1. `var evento = await _eventoRepository.GetByIdAsync(eventoId);`
2. `if (evento == null)` → `throw new KeyNotFoundException($"Evento {eventoId} não encontrado.");`
3. `if (!isAdmin && evento.VendedorCpf != vendedorCpf)` → `throw new UnauthorizedAccessException("Você não tem permissão para acessar este evento.");`
4. `var status = await _eventoRepository.ObterStatusCancelamento(eventoId, ct);`
5. Constrói `mensagem`:
   - Se `status.ReembolsoNecessario`: `"{status.TotalIngressosVendidos} ingressos vendidos. O cancelamento exigirá reembolso de R$ {status.ValorTotalReembolso:F2}. Deseja continuar?"`
   - Senão: `"Nenhum ingresso vendido. O cancelamento não exige reembolso."`
6. Retorna `status with { Mensagem = mensagem };`

**Dependencies:** `_eventoRepository` (injeta `IEventoRepository`)

**Error Handling:**
- `KeyNotFoundException` → propagada para o Controller (mapeada para 404)
- `UnauthorizedAccessException` → propagada para o Controller (mapeada para 403)
- Outras exceções → propagadas para o middleware global

#### 5.2 Método: `CancelarEvento`

**Interface:**
```csharp
public async Task CancelarEvento(Guid eventoId, string vendedorCpf, bool isAdmin, CancellationToken ct)
```

**Internal Logic (passo a passo — validações na ordem exata):**
1. `var evento = await _eventoRepository.GetByIdAsync(eventoId);`
2. `if (evento == null)` → `throw new KeyNotFoundException($"Evento {eventoId} não encontrado.");`
3. `if (!isAdmin && evento.VendedorCpf != vendedorCpf)` → `throw new UnauthorizedAccessException("Você não tem permissão para cancelar este evento.");`
4. `if (evento.Cancelado)` → `throw new DomainException("Evento já foi cancelado.");`
5. `await _eventoRepository.CancelarComTransacao(eventoId, evento.Gratuito, ct);`

**Dependencies:** `_eventoRepository` (injeta `IEventoRepository`)

**Error Handling:**
- `KeyNotFoundException` → propagada (Controller mapeia para 404)
- `UnauthorizedAccessException` → propagada (Controller mapeia para 403)
- `DomainException` → propagada (Controller mapeia para 409)
- Exceção SQL da transação → propagada após rollback no Repository

#### 5.3 Método removido: `DeleteAsync`

Remover completamente o método `DeleteAsync` da classe. Se houver referências em outras partes do código, elas quebrarão a compilação e serão corrigidas na Task 7 (Controller).

---

### Component 6: `EventoController` (API Layer — MODIFICADO)

**Purpose:** Controller REST de eventos. Adiciona 1 endpoint e modifica 1 endpoint existente.

#### 6.1 Novo Endpoint: `GET /api/evento/{id}/status-cancelamento`

```csharp
// Arquivo: Api/Controllers/EventoController.cs
[HttpGet("{id:guid}/status-cancelamento")]
[Authorize(Roles = "Vendedor,Admin")]
public async Task<IActionResult> StatusCancelamento(Guid id, CancellationToken ct)
{
    var cpf = User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value;
    if (string.IsNullOrEmpty(cpf))
        return Unauthorized(new { message = "Token inválido: claim 'cpf' não encontrada." });

    try
    {
        var isAdmin = User.IsInRole("Admin");
        var status = await _eventoService.ObterStatusCancelamento(id, cpf, isAdmin, ct);
        return Ok(status);
    }
    catch (KeyNotFoundException e)
    {
        return NotFound(new { message = e.Message });
    }
    catch (UnauthorizedAccessException e)
    {
        return StatusCode(403, new { message = e.Message });
    }
}
```

**Response codes:** `200 OK` (StatusCancelamentoDTO), `401 Unauthorized`, `403 Forbidden`, `404 Not Found`

#### 6.2 Endpoint Modificado: `DELETE /api/evento/{id}`

**Antes:**
```csharp
[HttpDelete("{id:guid}")]
[Authorize(Roles = "Vendedor,Admin")]
public async Task<IActionResult> Delete(Guid id)
{
    var cpf = User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value;
    if (string.IsNullOrEmpty(cpf)) return Unauthorized();
    try
    {
        var isAdmin = User.IsInRole("Admin");
        await _eventoService.DeleteAsync(id, cpf, isAdmin);  // ← REMOVER esta chamada
        return Ok(new { message = "Evento excluído com sucesso." });
    }
    catch (KeyNotFoundException e) { return NotFound(new { message = e.Message }); }
    catch (UnauthorizedAccessException) { return Forbid(); }
}
```

**Depois:**
```csharp
[HttpDelete("{id:guid}")]
[Authorize(Roles = "Vendedor,Admin")]
public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
{
    var cpf = User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value;
    if (string.IsNullOrEmpty(cpf))
        return Unauthorized(new { message = "Token inválido: claim 'cpf' não encontrada." });

    try
    {
        var isAdmin = User.IsInRole("Admin");
        await _eventoService.CancelarEvento(id, cpf, isAdmin, ct);
        return Ok(new { message = "Evento cancelado com sucesso." });
    }
    catch (KeyNotFoundException e)
    {
        return NotFound(new { message = e.Message });
    }
    catch (UnauthorizedAccessException e)
    {
        return StatusCode(403, new { message = e.Message });
    }
    catch (DomainException e)
    {
        return Conflict(new { message = e.Message });
    }
}
```

**Response codes:** `200 OK`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`, `409 Conflict`

**Dependencies:** `_eventoService` (injeta `IEventoService`), ASP.NET Core `[Authorize]`, `User.Claims`

**Error Handling:** Try-catch com mapeamento explícito de cada tipo de exceção para HTTP status code. A mensagem da exceção é repassada no corpo do response.

---

## Data Flow

### Fluxo 1: Consulta de Status de Cancelamento

```
Vendedor → GET /api/evento/{id}/status-cancelamento
  → EventoController.StatusCancelamento()
    → Extrai CPF do JWT (User.Claims["cpf"])
    → Extrai isAdmin (User.IsInRole("Admin"))
    → EventoService.ObterStatusCancelamento(eventoId, cpf, isAdmin, ct)
      → EventoRepository.GetByIdAsync(eventoId)  // busca evento
      → Valida: evento existe? é dono ou Admin?
      → EventoRepository.ObterStatusCancelamento(eventoId, ct)  // query agregada
        → SQL: SELECT e.*, COUNT(i.Id), COUNT(DISTINCT r.Id), SUM(ir.PrecoUnitario)
          FROM Eventos e
          LEFT JOIN Ingressos i ON i.EventoId = e.Id AND i.Status = 2
          LEFT JOIN ItensReserva ir ON ir.IngressoId = i.Id
          LEFT JOIN Reservas r ON r.EventoId = e.Id AND r.Reembolsada = 0
          WHERE e.Id = @eventoId
          GROUP BY e.Id, e.Nome, e.PrecoPadrao
        → Retorna StatusCancelamentoDTO (sem Mensagem)
      → Constrói Mensagem baseado em ReembolsoNecessario
      → Retorna StatusCancelamentoDTO com Mensagem
    → Retorna 200 OK + JSON
```

### Fluxo 2: Cancelamento de Evento com Reembolso

```
Vendedor → DELETE /api/evento/{id}
  → EventoController.Delete()
    → Extrai CPF do JWT (User.Claims["cpf"])
    → Extrai isAdmin (User.IsInRole("Admin"))
    → EventoService.CancelarEvento(eventoId, cpf, isAdmin, ct)
      → EventoRepository.GetByIdAsync(eventoId)
      → Valida: evento existe?
      → Valida: é dono ou Admin?
      → Valida: evento já cancelado? → DomainException
      → EventoRepository.CancelarComTransacao(eventoId, gratuito, ct)
        → connection.OpenAsync()
        → BEGIN TRANSACTION
          → UPDATE Eventos SET Cancelado = 1 WHERE Id = @eventoId
          → IF !gratuito: UPDATE Ingressos SET Status = 3 WHERE EventoId = @eventoId AND Status = 2
          → UPDATE Reservas SET Reembolsada = 1 WHERE EventoId = @eventoId AND Reembolsada = 0
          → UPDATE ItensReserva SET Reembolsado = 1 WHERE ReservaId IN (SELECT Id FROM Reservas WHERE EventoId = @eventoId)
          → IF !gratuito: UPDATE Pagamentos SET Status = 3 WHERE ReservaId IN (SELECT Id FROM Reservas WHERE EventoId = @eventoId) AND Status = 1
        → COMMIT
        → Se qualquer erro: ROLLBACK + throw
    → Retorna 200 OK
```

---

## Data Structures

### StatusCancelamentoDTO (novo)
```csharp
public record StatusCancelamentoDTO(
    Guid EventoId,              // PK do evento
    string Nome,                // Nome do evento
    bool Gratuito,              // true se PrecoPadrao == 0
    int TotalIngressosVendidos, // COUNT de Ingressos WHERE Status = 2
    int TotalReservasAtivas,    // COUNT DISTINCT de Reservas WHERE Reembolsada = 0
    bool ReembolsoNecessario,   // true se !Gratuito AND TotalIngressosVendidos > 0
    decimal ValorTotalReembolso,// SUM de ItensReserva.PrecoUnitario dos ingressos vendidos
    string Mensagem             // Texto descritivo para o frontend (preenchido pelo Service)
);
```

### Entidades existentes (sem alterações — referência)

```csharp
// Domain/Entities/Evento.cs
public Guid Id { get; private set; }
public string Nome { get; private set; }
public decimal PrecoPadrao { get; private set; }
public string VendedorCpf { get; private set; }    // NÃO MODIFICAR — extraído do JWT
public bool Cancelado { get; private set; }
public bool Gratuito => PrecoPadrao == 0;
public void Cancelar() { Cancelado = true; }

// Domain/Entities/Ingresso.cs
public int Status { get; private set; } = 0;  // 0=Livre, 1=Reservado, 2=Vendido, 3=Reembolsado

// Domain/Entities/Reserva.cs (spec 40)
public bool Reembolsada { get; private set; }
public void MarcarReembolsada() { Reembolsada = true; }

// Domain/Entities/ItemReserva.cs
public bool Reembolsado { get; private set; }

// Domain/Entities/Pagamento.cs (spec 170)
public StatusPagamento Status { get; private set; }  // StatusPagamento.Reembolsado = 2
```

### Estados de Status (int)

| Entidade | Campo | 0 | 1 | 2 | 3 |
|----------|-------|---|---|---|---|
| `Ingresso` | `Status` | Livre | Reservado | Vendido | Reembolsado |
| `Pagamento` | `Status` | Pendente | Confirmado | — | Reembolsado |

---

## File / Module Layout

```
ProjetoTicket/
├── Application/
│   ├── DTOs/
│   │   └── StatusCancelamentoDTO.cs          ← NOVO (Task 1)
│   ├── Interfaces/
│   │   └── IEventoService.cs                 ← MODIFICAR (Task 5: +2 métodos, -DeleteAsync)
│   └── Service/
│       └── EventoService.cs                  ← MODIFICAR (Task 6: +2 métodos, -DeleteAsync)
├── Domain/
│   └── Interface/
│       └── IEventoRepository.cs              ← MODIFICAR (Task 2: +2 métodos, -DeleteAsync)
├── Infraestructure/
│   └── Repository/
│       └── EventoRepository.cs               ← MODIFICAR (Tasks 3, 4, 8)
├── Api/
│   └── Controllers/
│       └── EventoController.cs               ← MODIFICAR (Task 7)
└── tests/
    └── EventoTests.cs                         ← MODIFICAR (Task 9)
```

---

## Testing Strategy

### Testes unitários (Service)

| # | Método | Cenário | Entrada | Saída esperada |
|---|--------|---------|---------|----------------|
| T1 | `ObterStatusCancelamento` | Evento pago com vendas | eventoId válido, vendedor dono | `StatusCancelamentoDTO` com `ReembolsoNecessario = true` |
| T2 | `ObterStatusCancelamento` | Evento gratuito | eventoId válido, vendedor dono | `StatusCancelamentoDTO` com `ReembolsoNecessario = false` |
| T3 | `ObterStatusCancelamento` | Vendedor não dono | eventoId de outro vendedor | `UnauthorizedAccessException` |
| T4 | `ObterStatusCancelamento` | Admin acessa evento alheio | eventoId, isAdmin = true | Sucesso |
| T5 | `ObterStatusCancelamento` | Evento inexistente | GUID aleatório | `KeyNotFoundException` |
| T6 | `CancelarEvento` | Evento pago com vendas | eventoId, cpf dono | Sucesso (sem exceção) |
| T7 | `CancelarEvento` | Evento gratuito | eventoId, cpf dono | Sucesso (sem exceção) |
| T8 | `CancelarEvento` | Evento sem vendas | eventoId, cpf dono | Sucesso (sem exceção) |
| T9 | `CancelarEvento` | Evento já cancelado | eventoId com `Cancelado = true` | `DomainException("Evento já foi cancelado.")` |
| T10 | `CancelarEvento` | Vendedor não dono | eventoId de outro vendedor | `UnauthorizedAccessException` |
| T11 | `CancelarEvento` | Admin cancela evento alheio | eventoId, isAdmin = true | Sucesso |
| T12 | `CancelarEvento` | Evento inexistente | GUID aleatório | `KeyNotFoundException` |

### Testes de integração (Repository)

| # | Cenário | Verificação |
|---|---------|-------------|
| T13 | `ObterStatusCancelamento` com dados reais | Query retorna contagens corretas |
| T14 | `CancelarComTransacao` evento pago | Todas as 5 tabelas atualizadas |
| T15 | `CancelarComTransacao` evento gratuito | Apenas Eventos + Reservas + ItensReserva atualizados |
| T16 | `CancelarComTransacao` rollback em erro | Simular falha no 3º UPDATE → todas as tabelas inalteradas |
| T17 | `GetAllAsync` filtra cancelados | Eventos com `Cancelado = 1` não aparecem |

### Testes de API (Controller)

| # | Cenário | HTTP | Verificação |
|---|---------|------|-------------|
| T18 | Status cancelamento válido | `GET .../status-cancelamento` | `200 OK` + JSON com 8 campos |
| T19 | Status cancelamento sem claim cpf | `GET .../status-cancelamento` | `401 Unauthorized` |
| T20 | Status cancelamento vendedor não dono | `GET .../status-cancelamento` | `403 Forbidden` |
| T21 | Cancelamento válido | `DELETE .../{id}` | `200 OK` |
| T22 | Cancelamento evento já cancelado | `DELETE .../{id}` | `409 Conflict` |
| T23 | Cancelamento vendedor não dono | `DELETE .../{id}` | `403 Forbidden` |
| T24 | Cancelamento Admin | `DELETE .../{id}` | `200 OK` |
| T25 | Listagem pública filtra cancelados | `GET /api/evento` | Não contém eventos com `Cancelado = 1` |

---

## Migration / Rollback

**Nenhuma migration necessária** — o campo `Reservas.Reembolsada` já foi adicionado pela spec 40 (`db/Script0013_AdicionarReembolsadaReservas.sql`).

**Rollback de código:** Reverter os commits da spec 50. O endpoint `DELETE` volta a chamar `DeleteAsync` (exclusão física). O endpoint `GET .../status-cancelamento` é removido. O filtro `WHERE Cancelado = 0` é removido do `GetAllAsync`.

**Rollback de dados:** N/A — a spec não altera schema, apenas dados. Eventos cancelados permanecem com `Cancelado = 1` mesmo após rollback de código.
