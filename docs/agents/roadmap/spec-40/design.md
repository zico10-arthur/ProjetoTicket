# Spec 40: Comprador Cancela Reserva com Reembolso — Design

> **Requirements:** [`requirements.md`](./requirements.md)
> **Arquitetura:** [Clean Architecture](../../../arquitetura.md) | [ADR-012](../../../ADR.md#adr-012-cancelamento-lógico-com-reembolso-atômico-specs-40-50-110)
> **Projeto Base:** .NET 9 + Dapper + SQL Server + DbUp + JWT Bearer + IEmailSender (spec 180)

---

## Design Approach

**High-level strategy:** Implementar cancelamento lógico com transação atômica (ADR-012). A coluna `Reembolsada` é adicionada à tabela `Reservas` via migration DbUp. O fluxo de cancelamento segue o padrão existente do projeto: validações no Service, transação SQL no Repository, endpoint REST no Controller. O e-mail de reembolso reutiliza a infraestrutura da spec 180 (`IEmailSender` + `EmailTemplates.ReembolsoConfirmado`), já injetada em `ReservaService`.

**Princípios aplicados:**
- **Cancelamento lógico:** Nunca exclusão física — flags `Reembolsada = 1`, `Reembolsado = 1`, `Status = 0` (ADR-012).
- **Transação atômica:** BEGIN/COMMIT/ROLLBACK cobre 4 tabelas (ADR-012).
- **Fail-fast:** Validações no Service em ordem de custo — propriedade, reembolso prévio, data do evento.
- **Fire-and-forget:** E-mail enfileirado via `IEmailSender.EnfileirarAsync` após transação bem-sucedida (ADR-013).
- **Reuso:** Nenhuma nova dependência no `ReservaService` — `IEmailSender`, `IUsuarioRepository`, `IEventoRepository` já estão injetados (spec 180).

---

## Architecture Decisions

| # | Decisão | Justificativa |
|---|---------|---------------|
| **D1** | `Reembolsada` como `BIT NOT NULL DEFAULT 0` na tabela `Reservas` | Evita JOIN para verificar status; consulta rápida em `GET /api/reserva/minhas`. Consistente com `Pago` (também BIT). |
| **D2** | Transação no Repository, não no Service | Segue padrão existente: `CadastrarReservaComItens`, `CriarComTransacao` (spec 170). Service não conhece detalhes de conexão/transação. |
| **D3** | `CancelarComTransacao(Guid reservaId, CancellationToken ct)` — sem parâmetro `Evento` | A transação usa apenas `reservaId`. Todas as tabelas referenciam `ReservaId` via FK. `Evento` não é necessário no SQL. |
| **D4** | `Pagamento.Status = 2` (Reembolsado) | Valor consistente com `Domain.Enums.StatusPagamento`: `Pendente=0, Confirmado=1, Reembolsado=2, Falhou=3`. |
| **D5** | `ReservaService` não adiciona novos parâmetros ao construtor | `IEmailSender`, `IUsuarioRepository`, `IEventoRepository`, `IReservaRepository` já estão injetados (spec 180). KISS — zero mudanças no `Program.cs`. |
| **D6** | Validações no Service, não no Repository | Repository é responsável apenas pela transação SQL. Service contém lógica de negócio (propriedade, data, reembolso prévio). Separação clara de responsabilidades. |
| **D7** | CPF extraído do JWT, nunca do body | Segurança (ADR-004). Controller extrai, Service recebe como parâmetro tipado `string usuarioCpf`. |
| **D8** | `ReservaDetalhadaDTO.Reembolsada` como `bool` (não calculado) | Valor direto do banco. `podeCancelar` (calculado) pertence à spec 110. Spec 40 apenas expõe o dado bruto. |

---

## Component / Module Breakdown

### Component 1: Campo `Reembolsada` em `Reserva` (Domain Entity)

**Purpose:** Adicionar flag de reembolso à entidade `Reserva`.

**File:** `Domain/Entities/Reserva.cs` (EDITAR — adicionar 1 propriedade e 1 método)

**Interface — propriedade existente (linha 17 do arquivo atual):**
```csharp
public bool Pago { get; private set; }
```

**Nova propriedade (adicionar na linha 18, após `Pago`):**
```csharp
public bool Reembolsada { get; private set; }
```

**Construtor privado existente (linhas 23-30):**
```csharp
private Reserva(string usuarioCpf, Guid eventoId, string? cupomUtilizado, decimal valorFinalPago)
{
    UsuarioCpf = usuarioCpf;
    EventoId = eventoId;
    CupomUtilizado = cupomUtilizado;
    ValorFinalPago = valorFinalPago;
    Pago = false;
}
```

**Adicionar após `Pago = false;` (linha 29):**
```csharp
    Reembolsada = false;
```

**Novo método (adicionar após `MarcarPago`, linha 35):**
```csharp
public void MarcarReembolsada()
{
    Reembolsada = true;
}
```

**Internal Logic:**
- `Reembolsada` é inicializado como `false` no construtor privado — toda reserva nova não é reembolsada.
- `MarcarReembolsada()` define `Reembolsada = true`. Chamado apenas pelo método `Criar`? Não — o cancelamento altera uma reserva existente, então `MarcarReembolsada` é chamado... na verdade, a transação SQL no Repository define `Reembolsada = 1` diretamente. O método `MarcarReembolsada()` existe para consistência de domínio e para testes unitários, mas o Repository opera via SQL direto.
- `Criar()` (método estático, linha 40) NÃO precisa ser alterado — já chama o construtor privado que agora define `Reembolsada = false`.

**Dependencies:** Nenhuma nova. Namespace `Domain.Entities`.

**Error Handling:** Nenhum. `MarcarReembolsada()` é idempotente (definir `true` múltiplas vezes é inofensivo).

---

### Component 2: Migration `Script0013` (Database)

**Purpose:** Adicionar coluna `Reembolsada` à tabela `Reservas`.

**File:** `db/Script0013_AdicionarReembolsadaReservas.sql` (CRIAR)

**Interface (conteúdo completo do arquivo):**
```sql
-- Script0013: Adicionar coluna Reembolsada na tabela Reservas
-- Spec 40: Comprador Cancela Reserva com Reembolso

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Reservas' AND COLUMN_NAME = 'Reembolsada'
)
BEGIN
    ALTER TABLE dbo.Reservas ADD Reembolsada BIT NOT NULL DEFAULT 0;
END
```

**Internal Logic:**
- Idempotente via `IF NOT EXISTS`. Se a coluna já existir, o script não faz nada.
- `BIT NOT NULL DEFAULT 0` — todas as reservas existentes recebem `Reembolsada = 0` automaticamente.
- Executado pelo DbUp na inicialização da API (`DatabaseMigration.cs` já configurado).

**Dependencies:** DbUp (já configurado no projeto).

**Error Handling:** Se a tabela `Reservas` não existir, DbUp reporta erro na inicialização (esperado — a tabela é criada em scripts anteriores).

---

### Component 3: Atualização de `IReservaRepository` (Domain Interface)

**Purpose:** Adicionar método `CancelarComTransacao` na interface do repositório.

**File:** `Domain/Interface/IReservaRepository.cs` (EDITAR — adicionar 1 método antes do `}` final)

**Método a adicionar (após a linha 15, antes da `}` final):**
```csharp
/// <summary>
/// Spec 40: Executa cancelamento em transação atômica.
/// UPDATE Reservas.Reembolsada = 1,
/// UPDATE ItensReserva.Reembolsado = 1,
/// UPDATE Ingressos.Status = 0, DataBloqueio = NULL,
/// UPDATE Pagamentos.Status = 2 (Reembolsado) se Status = 1 (Confirmado).
/// </summary>
Task CancelarComTransacao(Guid reservaId, CancellationToken ct);
```

**Dependencies:** `System.Threading.Tasks` (já importado). Nenhuma nova.

---

### Component 4: Implementação de `CancelarComTransacao` (Infrastructure Repository)

**Purpose:** Executar a transação atômica de cancelamento no `ReservaRepository`.

**File:** `Infraestructure/Repository/ReservaRepository.cs` (EDITAR — adicionar 1 método após `ListarReservasDetalhadasPorVendedor`)

**Novo método (adicionar antes da `}` final da classe, após linha 214):**
```csharp
public async Task CancelarComTransacao(Guid reservaId, CancellationToken ct)
{
    using var connection = _factory.CreateConnection();
    await connection.OpenAsync(ct);
    using var transaction = connection.BeginTransaction();

    try
    {
        // 1. Marcar reserva como reembolsada
        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE Reservas SET Reembolsada = 1 WHERE Id = @reservaId",
            new { reservaId }, transaction, cancellationToken: ct));

        // 2. Marcar todos os itens da reserva como reembolsados
        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE ItensReserva SET Reembolsado = 1 WHERE ReservaId = @reservaId",
            new { reservaId }, transaction, cancellationToken: ct));

        // 3. Liberar ingressos (Status = 0) e limpar DataBloqueio
        await connection.ExecuteAsync(new CommandDefinition(@"
            UPDATE Ingressos
            SET Status = 0, DataBloqueio = NULL
            WHERE Id IN (
                SELECT IngressoId FROM ItensReserva WHERE ReservaId = @reservaId
            )",
            new { reservaId }, transaction, cancellationToken: ct));

        // 4. Marcar pagamento como reembolsado (StatusPagamento.Reembolsado = 2)
        //    Apenas se Status = 1 (Confirmado)
        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE Pagamentos SET Status = 2 WHERE ReservaId = @reservaId AND Status = 1",
            new { reservaId }, transaction, cancellationToken: ct));

        await transaction.CommitAsync(ct);
    }
    catch
    {
        await transaction.RollbackAsync(ct);
        throw;
    }
}
```

**Internal Logic:**
1. Abre conexão e transação.
2. Executa 4 UPDATEs sequencialmente (ordem não afeta atomicidade, mas segue ordem lógica: Reserva → Itens → Ingressos → Pagamento).
3. Comita transação.
4. Em caso de falha em qualquer UPDATE, faz rollback e relança exceção.

**IMPORTANTE:** O valor `2` no UPDATE de Pagamentos corresponde a `(int)StatusPagamento.Reembolsado`. O enum `Domain.Enums.StatusPagamento` define: `Pendente=0, Confirmado=1, Reembolsado=2, Falhou=3`.

**Dependencies:**
- `ConnectionFactory` (já injetado no construtor do `ReservaRepository`)
- `Dapper` (já referenciado)
- `Microsoft.Data.SqlClient` (já referenciado — `BeginTransaction`)

**Error Handling:**
- Qualquer exceção nos UPDATEs causa rollback da transação completa.
- A exceção é relançada para o Service caller.
- Conexão é fechada automaticamente pelo `using`.

---

### Component 5: Atualização de `IReservaService` (Application Interface)

**Purpose:** Adicionar método `CancelarReserva` na interface do serviço.

**File:** `Application/Interfaces/IReservaService.cs` (EDITAR — adicionar 1 método antes do `}` final)

**Método a adicionar (após linha 12):**
```csharp
/// <summary>
/// Spec 40: Cancela uma reserva própria. Valida propriedade, data do evento,
/// e reembolso prévio. Executa transação atômica e enfileira e-mail de reembolso.
/// </summary>
Task CancelarReserva(Guid reservaId, string usuarioCpf, CancellationToken ct);
```

**Dependencies:** Nenhuma nova.

---

### Component 6: Implementação de `CancelarReserva` (Application Service)

**Purpose:** Implementar lógica de cancelamento no `ReservaService`.

**File:** `Application/Service/ReservaService.cs` (EDITAR — adicionar 1 método antes da `}` final da classe, após linha 119)

**Novo método:**
```csharp
public async Task CancelarReserva(Guid reservaId, string usuarioCpf, CancellationToken ct)
{
    // 1. Buscar reserva
    var reserva = await _repositoryReserva.BuscarPorId(reservaId, ct);
    if (reserva == null)
        throw new Domain.Exceptions.DomainException("Reserva não encontrada.");

    // 2. Verificar propriedade (apenas o dono cancela)
    if (reserva.UsuarioCpf != usuarioCpf)
        throw new UnauthorizedAccessException("Esta reserva não pertence a você.");

    // 3. Verificar se já foi reembolsada
    if (reserva.Reembolsada)
        throw new Domain.Exceptions.DomainException("Reserva já foi cancelada.");

    // 4. Buscar evento para validação de data e para o e-mail
    var evento = await _repositoryEvento.GetByIdAsync(reserva.EventoId);
    if (evento == null)
        throw new Domain.Exceptions.DomainException("Evento não encontrado.");

    // 5. Verificar se o evento já começou
    if (evento.DataEvento <= DateTime.UtcNow)
        throw new Domain.Exceptions.DomainException("Não é possível cancelar. O evento já começou.");

    // 6. Executar transação atômica (4 UPDATEs)
    await _repositoryReserva.CancelarComTransacao(reservaId, ct);

    // 7. Enfileirar e-mail de reembolso (fire-and-forget)
    var usuario = await _repositoryUsuario.BuscarCpf(usuarioCpf, ct);
    var destinatario = usuario?.Email ?? usuarioCpf;
    var email = EmailTemplates.ReembolsoConfirmado(
        destinatario,
        evento.Nome,
        reserva.ValorFinalPago);
    await _emailSender.EnfileirarAsync(email, ct);
}
```

**Internal Logic — ordem das validações é crítica:**
1. `BuscarPorId` → barato (1 query por PK). Se null, falha rápido.
2. `UsuarioCpf != usuarioCpf` → comparação de strings em memória. Se falso, falha antes de buscar evento.
3. `Reembolsada == true` → leitura de propriedade em memória. Se true, falha antes de buscar evento.
4. `GetByIdAsync` → só busca evento se as validações anteriores passaram.
5. `DataEvento <= DateTime.UtcNow` → comparação de DateTime. Se passou, falha.
6. `CancelarComTransacao` → operação pesada (transação SQL), só executada se todas as validações passaram.
7. `EnfileirarAsync` → fire-and-forget, após transação bem-sucedida.

**Integração com spec 180 (e-mail):**
- `_emailSender` já injetado (linha 19 do arquivo atual).
- `_repositoryUsuario` já injetado (linha 15).
- `_repositoryEvento` já injetado (linha 17).
- `using Infraestructure.Email;` já existe (linha 8).
- `EmailTemplates.ReembolsoConfirmado` já existe (spec 180, Component 6).

**Dependencies:**
- `IReservaRepository` — `BuscarPorId`, `CancelarComTransacao`
- `IUsuarioRepository` — `BuscarCpf` (para e-mail)
- `IEventoRepository` — `GetByIdAsync`
- `IEmailSender` — `EnfileirarAsync`
- `EmailTemplates` (static) — `ReembolsoConfirmado`

**Error Handling:**
- Cada validação lança exceção tipada com mensagem em português.
- `UnauthorizedAccessException` é lançada para erro de propriedade (capturada como 403 no controller).
- `DomainException` é lançada para demais erros de negócio (capturadas com mapeamento específico no controller).
- Se `CancelarComTransacao` lançar exceção (ex: deadlock SQL), a exceção propaga para o controller → `GlobalExceptionHandlerMiddleware` retorna 500.

**IMPORTANTE:** O construtor do `ReservaService` NÃO é alterado. As dependências `IEmailSender`, `IUsuarioRepository`, `IEventoRepository`, `IReservaRepository` já estão injetadas (spec 180).

---

### Component 7: Endpoint `DELETE /api/reserva/{id}` (API Controller)

**Purpose:** Expor o endpoint de cancelamento no `ReservaController`.

**File:** `Api/Controllers/ReservaController.cs` (EDITAR — adicionar 1 método antes da `}` final da classe, após linha 78)

**Novo método:**
```csharp
/// <summary>
/// Spec 40: Cancelar reserva própria. Qualquer perfil autenticado pode cancelar
/// suas próprias reservas antes do início do evento.
/// </summary>
[HttpDelete("{id:guid}")]
[Authorize]
public async Task<IActionResult> CancelarReserva(Guid id, CancellationToken ct)
{
    var cpf = User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value;
    if (string.IsNullOrEmpty(cpf))
        return Unauthorized(new { message = "CPF não encontrado no token." });

    try
    {
        await _service.CancelarReserva(id, cpf, ct);
        return Ok(new { message = "Reserva cancelada com sucesso. Reembolso registrado." });
    }
    catch (Domain.Exceptions.DomainException ex)
    {
        return ex.Message switch
        {
            "Reserva não encontrada." => NotFound(new { message = ex.Message }),
            "Reserva já foi cancelada." => Conflict(new { message = ex.Message }),
            _ => BadRequest(new { message = ex.Message })
        };
    }
    catch (UnauthorizedAccessException ex)
    {
        return StatusCode(403, new { message = ex.Message });
    }
}
```

**Mapeamento de HTTP status codes:**

| Exceção | HTTP Status | Mensagem |
|---------|-------------|----------|
| CPF ausente no JWT | `401 Unauthorized` | "CPF não encontrado no token." |
| `UnauthorizedAccessException` | `403 Forbidden` | "Esta reserva não pertence a você." |
| `DomainException("Reserva não encontrada.")` | `404 Not Found` | "Reserva não encontrada." |
| `DomainException("Reserva já foi cancelada.")` | `409 Conflict` | "Reserva já foi cancelada." |
| `DomainException` (demais) | `400 Bad Request` | Mensagem da exceção |
| Sucesso | `200 OK` | "Reserva cancelada com sucesso. Reembolso registrado." |

**Dependencies:**
- `_service` (tipo `IReservaService`) — já injetado no construtor existente.
- `User.Claims` — claims do JWT, disponíveis em qualquer método com `[Authorize]`.
- Nenhum novo using necessário.

**Error Handling:**
- `DomainException` com mensagem específica → mapeamento por switch expression.
- `UnauthorizedAccessException` → 403 Forbidden (capturado separadamente porque não é `DomainException`).
- Exceções não capturadas (ex: SQL deadlock em `CancelarComTransacao`) → propagam para `GlobalExceptionHandlerMiddleware` (spec 150) que retorna 500.

---

### Component 8: Atualização de `ReservaDetalhadaDTO` (Domain DTO)

**Purpose:** Incluir campo `Reembolsada` no DTO de listagem de reservas.

**File:** `Domain/DTOs/ReservaDetalhadaDTO.cs` (EDITAR — adicionar 1 propriedade)

**Nova propriedade (adicionar após `Pago`, linha 12 do arquivo atual):**
```csharp
public bool Reembolsada { get; set; }
```

**Internal Logic:**
- `ReservaAdminDTO` herda de `ReservaDetalhadaDTO` → automaticamente inclui `Reembolsada`.
- Dapper mapeia por nome de coluna → a query SQL deve incluir `r.Reembolsada` no SELECT.

**Dependencies:** Nenhuma.

---

### Component 9: Atualização de `ReservaVendedorDTO` (Domain DTO)

**Purpose:** Incluir campo `Reembolsada` no DTO de vendas do vendedor.

**File:** `Domain/DTOs/ReservaVendedorDTO.cs` (EDITAR — adicionar 1 propriedade)

**Nova propriedade (adicionar após `Pago`, linha 9 do arquivo atual):**
```csharp
public bool Reembolsada { get; set; }
```

**Dependencies:** Nenhuma.

---

### Component 10: Atualização das Queries SQL (Infrastructure Repository)

**Purpose:** Incluir coluna `r.Reembolsada` nas queries de listagem.

**File:** `Infraestructure/Repository/ReservaRepository.cs` (EDITAR — 3 queries)

**Query 1: `ListarReservasDetalhadasPorCpf` (linhas 143-158 do arquivo atual)**

Antes:
```sql
SELECT 
    r.Id,
    e.Nome AS NomeEvento,
    e.DataEvento,
    STRING_AGG(i.Posicao, ', ') AS PosicaoIngresso,
    STRING_AGG(i.Setor, ', ') AS SetorIngresso,
    r.CupomUtilizado,
    r.ValorFinalPago,
    r.Pago
FROM Reservas r
...
GROUP BY r.Id, e.Nome, e.DataEvento, r.CupomUtilizado, r.ValorFinalPago, r.Pago
```

Depois:
```sql
SELECT 
    r.Id,
    e.Nome AS NomeEvento,
    e.DataEvento,
    STRING_AGG(i.Posicao, ', ') AS PosicaoIngresso,
    STRING_AGG(i.Setor, ', ') AS SetorIngresso,
    r.CupomUtilizado,
    r.ValorFinalPago,
    r.Pago,
    r.Reembolsada
FROM Reservas r
...
GROUP BY r.Id, e.Nome, e.DataEvento, r.CupomUtilizado, r.ValorFinalPago, r.Pago, r.Reembolsada
```

**Query 2: `ListarTodasDetalhadasAdmin` (linhas 169-187 do arquivo atual)**

Adicionar `r.Reembolsada` no SELECT e `r.Reembolsada` no GROUP BY (mesmo padrão da Query 1).

**Query 3: `ListarReservasDetalhadasPorVendedor` (linhas 197-210 do arquivo atual)**

Adicionar `r.Reembolsada` no SELECT (esta query não usa GROUP BY, então apenas adicionar a coluna no SELECT).

**Internal Logic:**
- Dapper faz mapeamento por nome de coluna → se `Reembolsada` não estiver no SELECT, a propriedade fica com `default(bool) = false`.
- Para queries com `GROUP BY`, a coluna precisa estar no GROUP BY também (regra SQL).

**Dependencies:** Nenhuma nova.

---

## Data Flow

### Flow: Cancelamento de Reserva (Sequência Completa)

```
Comprador               Controller              Service                   Repository              SQL Server
   │                         │                      │                          │                        │
   │  DELETE /api/reserva/{id}                      │                          │                        │
   │────────────────────────>│                      │                          │                        │
   │                         │  Extrair CPF do JWT  │                          │                        │
   │                         │  CancelarReserva()   │                          │                        │
   │                         │─────────────────────>│                          │                        │
   │                         │                      │  BuscarPorId()           │                        │
   │                         │                      │─────────────────────────>│  SELECT * FROM         │
   │                         │                      │<─────────────────────────│  Reservas WHERE Id=@Id │
   │                         │                      │                          │                        │
   │                         │                      │  [null? → throw 404]     │                        │
   │                         │                      │  [dono? → throw 403]     │                        │
   │                         │                      │  [já reembolsada? → 409] │                        │
   │                         │                      │                          │                        │
   │                         │                      │  GetByIdAsync()          │                        │
   │                         │                      │─────────────────────────>│  SELECT * FROM         │
   │                         │                      │<─────────────────────────│  Eventos WHERE Id=@Id  │
   │                         │                      │                          │                        │
   │                         │                      │  [null? → throw 400]     │                        │
   │                         │                      │  [já começou? → 400]     │                        │
   │                         │                      │                          │                        │
   │                         │                      │  CancelarComTransacao()  │                        │
   │                         │                      │─────────────────────────>│  BEGIN TRANSACTION     │
   │                         │                      │                          │  UPDATE Reservas       │
   │                         │                      │                          │  UPDATE ItensReserva   │
   │                         │                      │                          │  UPDATE Ingressos      │
   │                         │                      │                          │  UPDATE Pagamentos     │
   │                         │                      │                          │  COMMIT                │
   │                         │                      │<─────────────────────────│                        │
   │                         │                      │                          │                        │
   │                         │                      │  BuscarCpf (para e-mail) │                        │
   │                         │                      │─────────────────────────>│  SELECT * FROM         │
   │                         │                      │<─────────────────────────│  Usuarios WHERE Cpf=@Cpf│
   │                         │                      │                          │                        │
   │                         │                      │  EnfileirarAsync()      │                        │
   │                         │                      │  ─→ Channel.WriteAsync  │                        │
   │                         │                      │                          │                        │
   │  200 OK {"message":...}│                      │                          │                        │
   │<────────────────────────│                      │                          │                        │

   [BACKGROUND] EmailBackgroundWorker → ProcessarComRetentativa → SmtpEmailSender → SMTP
```

---

## Data Structures

### `Reservas` Table (após migration Script0013)

```
+----------------------------+
| Reservas                   |
+----------------------------+
| Id: UNIQUEIDENTIFIER (PK)  |
| UsuarioCpf: NVARCHAR (FK)  |
| EventoId: UNIQUEIDENTIFIER (FK) |
| VendedorCpf: NVARCHAR (FK) |
| CupomUtilizado: NVARCHAR   | NULLABLE
| ValorFinalPago: DECIMAL    |
| Pago: BIT                  | DEFAULT 0
| Reembolsada: BIT           | DEFAULT 0  ← NOVO (Script0013)
| DataBloqueio: DATETIME2    | NULLABLE
+----------------------------+
```

### `ItensReserva` Table (já existente — sem alterações)

```
+----------------------------+
| ItensReserva               |
+----------------------------+
| Id: UNIQUEIDENTIFIER (PK)  |
| ReservaId: UNIQUEIDENTIFIER (FK) |
| CpfParticipante: NVARCHAR  |
| IngressoId: UNIQUEIDENTIFIER (FK) |
| PrecoUnitario: DECIMAL     |
| Reembolsado: BIT           | DEFAULT 0  (já existente)
+----------------------------+
```

### `Pagamentos` Table — Coluna `Status` (já existente — spec 170)

```
Status: INT
  0 = Pendente (StatusPagamento.Pendente)
  1 = Confirmado (StatusPagamento.Confirmado)
  2 = Reembolsado (StatusPagamento.Reembolsado)
  3 = Falhou (StatusPagamento.Falhou)
```

### `ReservaDetalhadaDTO` (após atualização)

```
+-------------------------------+
| ReservaDetalhadaDTO           |
+-------------------------------+
| + Id: Guid                    |
| + NomeEvento: string          |
| + DataEvento: DateTime        |
| + PosicaoIngresso: string     |
| + SetorIngresso: string       |
| + CupomUtilizado: string?     |
| + ValorFinalPago: decimal     |
| + Pago: bool                  |
| + Reembolsada: bool           | ← NOVO
+-------------------------------+
```

### `ReservaVendedorDTO` (após atualização)

```
+-------------------------------+
| ReservaVendedorDTO            |
+-------------------------------+
| + Id: Guid                    |
| + NomeEvento: string          |
| + DataEvento: DateTime        |
| + ValorFinalPago: decimal     |
| + Pago: bool                  |
| + Reembolsada: bool           | ← NOVO
| + NomeComprador: string       |
| + CpfComprador: string        |
+-------------------------------+
```

---

## File / Module Layout

### Novos Arquivos (1 arquivo)
```
db/
  Script0013_AdicionarReembolsadaReservas.sql     — Migration
```

### Arquivos Editados (7 arquivos)
```
Domain/
  Entities/
    Reserva.cs                                    — +propriedade Reembolsada + método MarcarReembolsada
  Interface/
    IReservaRepository.cs                         — +método CancelarComTransacao
  DTOs/
    ReservaDetalhadaDTO.cs                        — +campo Reembolsada
    ReservaVendedorDTO.cs                         — +campo Reembolsada

Application/
  Interfaces/
    IReservaService.cs                            — +método CancelarReserva
  Service/
    ReservaService.cs                             — +método CancelarReserva (sem alterar construtor)

Api/
  Controllers/
    ReservaController.cs                          — +endpoint DELETE /api/reserva/{id}

Infraestructure/
  Repository/
    ReservaRepository.cs                          — +método CancelarComTransacao + atualizar 3 queries SQL
```

### Arquivos NÃO Alterados
```
Api/Program.cs                                   — NENHUMA alteração (DI já existente)
Api/appsettings.json                              — NENHUMA alteração
Infraestructure/Infraestructure.csproj            — NENHUMA alteração (sem novos pacotes)
```

---

## Testing Strategy

### Testes Unitários (xUnit + Moq)

| # | Teste | O que verifica | FR |
|---|-------|---------------|-----|
| T1 | `CancelarReserva_Sucesso_ReservaPaga` | Reserva paga → `CancelarComTransacao` chamado 1x, `EnfileirarAsync` chamado 1x com template `ReembolsoConfirmado` | FR-005 a FR-009, FR-017, FR-018 |
| T2 | `CancelarReserva_Sucesso_ReservaGratuita` | Reserva com `ValorFinalPago = 0` → transação executada, e-mail enviado com `valorReembolsado = 0` | FR-007, FR-008, FR-018 |
| T3 | `CancelarReserva_Sucesso_ReservaNaoPaga` | `Pago = false`, sem registro em `Pagamentos` → transação executada, UPDATE de Pagamentos afeta 0 linhas | FR-008 |
| T4 | `CancelarReserva_Sucesso_MultiplosItens` | Reserva com 4 itens → verificar que `CancelarComTransacao` é chamado (transação cobre todos os itens) | FR-006 |
| T5 | `CancelarReserva_NaoEncontrada_404` | `BuscarPorId` retorna null → `DomainException("Reserva não encontrada.")` → `CancelarComTransacao` NUNCA chamado | FR-017 |
| T6 | `CancelarReserva_OutroUsuario_403` | `UsuarioCpf != cpfDoJWT` → `UnauthorizedAccessException` → `CancelarComTransacao` NUNCA chamado | FR-002 |
| T7 | `CancelarReserva_JaReembolsada_409` | `Reembolsada = true` → `DomainException("Reserva já foi cancelada.")` → `CancelarComTransacao` NUNCA chamado | FR-004 |
| T8 | `CancelarReserva_EventoJaComecou_400` | `DataEvento <= DateTime.UtcNow` → `DomainException("Não é possível cancelar...")` → `CancelarComTransacao` NUNCA chamado | FR-003 |
| T9 | `CancelarReserva_EventoNaoEncontrado_400` | `GetByIdAsync` retorna null → `DomainException("Evento não encontrado.")` | FR-017 |
| T10 | `CancelarReserva_TransacaoFalha_Rollback` | `CancelarComTransacao` lança exceção → `EnfileirarAsync` NUNCA chamado, exceção propagada | FR-009, NFR-001 |
| T11 | `CancelarReserva_EmailFallback` | `BuscarCpf` retorna null → destinatário = `usuarioCpf` (não quebra) | FR-018 |
| T12 | `Reserva_Reembolsada_InicializaFalse` | `Reserva.Criar(...)` produz reserva com `Reembolsada == false` | FR-010 |
| T13 | `Reserva_MarcarReembolsada` | `reserva.MarcarReembolsada()` → `Reembolsada == true` | FR-010 |

**Boilerplate de teste (exemplo):**
```csharp
[Fact]
public async Task CancelarReserva_Sucesso_ReservaPaga()
{
    // Arrange
    var reservaId = Guid.NewGuid();
    var cpf = "52998224725";
    var eventoId = Guid.NewGuid();

    var reserva = Reserva.Criar(cpf, eventoId, new List<ItemReserva>
    {
        new ItemReserva(cpf, Guid.NewGuid(), 100m)
    });
    // Usar reflexão ou construtor interno para definir Id
    typeof(Reserva).GetProperty("Id")!.SetValue(reserva, reservaId);

    var evento = new Evento("Workshop .NET", 50, DateTime.UtcNow.AddDays(7), 100m, "12345678000199");

    _reservaRepoMock.Setup(r => r.BuscarPorId(reservaId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(reserva);
    _eventoRepoMock.Setup(r => r.GetByIdAsync(eventoId))
        .ReturnsAsync(evento);
    _usuarioRepoMock.Setup(r => r.BuscarCpf(cpf, It.IsAny<CancellationToken>()))
        .ReturnsAsync(new Usuario { Email = "teste@email.com" });

    // Act
    await _service.CancelarReserva(reservaId, cpf, CancellationToken.None);

    // Assert
    _reservaRepoMock.Verify(r => r.CancelarComTransacao(
        reservaId, It.IsAny<CancellationToken>()), Times.Once);
    _emailSenderMock.Verify(e => e.EnfileirarAsync(
        It.Is<EmailMessage>(m => m.Assunto.Contains("Reembolso processado")),
        It.IsAny<CancellationToken>()), Times.Once);
}
```

---

## Migration / Rollback

### Migration
- `Script0013_AdicionarReembolsadaReservas.sql` é executado automaticamente pelo DbUp na inicialização da API.
- A migration é idempotente (`IF NOT EXISTS`) — pode ser executada múltiplas vezes sem erro.
- A coluna `Reembolsada` é adicionada com `DEFAULT 0` — reservas existentes recebem automaticamente `Reembolsada = 0`.

### Rollback
Para reverter esta spec:
1. Remover coluna (se necessário): `ALTER TABLE Reservas DROP COLUMN Reembolsada` (apenas em desenvolvimento — NÃO executar em produção sem backup).
2. Reverter edições nos 7 arquivos via `git revert`.
3. Remover `db/Script0013_AdicionarReembolsadaReservas.sql`.
4. Re-compilar e testar.

Não há migração de dados para reverter — a coluna é adicionada com valor padrão, não modifica dados existentes.
