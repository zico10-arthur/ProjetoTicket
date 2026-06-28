---
name: Cancelamento de Reserva — Visão Unificada
status: verified
---

# Spec 110: Cancelamento de Reserva — Visão Unificada — Design

## Design Approach

**Estratégia:** Refatorar a query de Minhas Reservas para usar `splitOn` do Dapper, expondo itens individuais com flag `Reembolsado`. Adicionar cálculo em memória de `PodeCancelar`. Nenhuma entidade nova — apenas DTOs e queries são afetados.

**Padrões:** Clean Architecture (ADR-001), Repository Pattern via Dapper (ADR-002), Cancelamento Lógico (ADR-012).

**Princípios:** KISS — o `PodeCancelar` é calculado em memória com uma condição simples, sem complexidade no SQL. DRY — um único endpoint `GET /api/reserva/minhas` serve todos os perfis.

---

## Architecture Decisions

| Decisão | Justificativa |
|---------|---------------|
| `[Authorize]` sem restrição de role | Todos os perfis compartilham `UsuarioCpf` — a role é irrelevante para posse de reserva |
| `PodeCancelar` calculado em memória | Lógica simples (`!Reembolsada && DataEvento > agora`); não justifica coluna extra ou complexidade SQL |
| Query com `splitOn` (substitui `STRING_AGG`) | Permite expor flags individuais de `Reembolsado` por item |
| Agrupamento em `Dictionary<Guid, T>` | Dapper não suporta agrupamento nativo com `splitOn`; dicionário é performático para volume típico |
| Único endpoint para todos os perfis | Evita duplicação e mantém experiência unificada |

---

## Component / Module Breakdown

### Component 1: `ReservaDetalhadaDTO` (Domain Layer — MODIFICADO)

**Purpose:** DTO de resposta para listagem de Minhas Reservas. Adiciona campos `Itens` e `PodeCancelar`.

**Interface (exata — após modificação):**
```csharp
// Arquivo: Domain/DTOs/ReservaDetalhadaDTO.cs
namespace Domain.DTOs;

public class ReservaDetalhadaDTO
{
    public Guid Id { get; set; }
    public string NomeEvento { get; set; } = "";
    public DateTime DataEvento { get; set; }
    public string PosicaoIngresso { get; set; } = "";
    public string SetorIngresso { get; set; } = "";
    public string? CupomUtilizado { get; set; }
    public decimal ValorFinalPago { get; set; }
    public bool Pago { get; set; }
    public bool Reembolsada { get; set; }
    public List<ItemReservaResponseDTO> Itens { get; set; } = new();  // NOVO
    public bool PodeCancelar { get; set; }                             // NOVO
}
```

**Dependencies:** `Domain/DTOs/ItemReservaResponseDTO` — referência cross-layer necessária porque o DTO de item vive em Application. Alternativa: mover `ItemReservaResponseDTO` para Domain/DTOs (mas mantemos em Application por consistência com padrão existente).

**Error Handling:** N/A — DTO.

---

### Component 2: `ItemReservaResponseDTO` (Domain Layer — MOVIDO)

**Purpose:** DTO de resposta para cada item de uma reserva. Adiciona campos `Id` e `Reembolsado`.

**Interface (exata — após modificação):**
```csharp
// Arquivo: Domain/DTOs/ItemReservaResponseDTO.cs
namespace Domain.DTOs;

public class ItemReservaResponseDTO
{
    public Guid Id { get; set; }                      // NOVO
    public string CpfParticipante { get; set; } = string.Empty;
    public Guid IngressoId { get; set; }
    public decimal PrecoUnitario { get; set; }
    public bool Reembolsado { get; set; }              // NOVO
}
```

**Dependencies:** Nenhuma.

**Error Handling:** N/A — DTO.

---

### Component 3: `ReservaRepository.ListarReservasDetalhadasPorCpf` (Infrastructure — MODIFICADO)

**Purpose:** Refatorar a query SQL para usar `splitOn` com agrupamento em memória, substituindo `STRING_AGG`.

**Interface (não altera assinatura):**
```csharp
public async Task<IEnumerable<ReservaDetalhadaDTO>> ListarReservasDetalhadasPorCpf(string cpf, CancellationToken ct)
```

**Nova implementação completa:**
```csharp
public async Task<IEnumerable<ReservaDetalhadaDTO>> ListarReservasDetalhadasPorCpf(string cpf, CancellationToken ct)
{
    using var conn = _connectionFactory.CreateConnection();

    var reservaDict = new Dictionary<Guid, ReservaDetalhadaDTO>();

    const string sql = @"
        SELECT
            r.Id,
            e.Nome AS NomeEvento,
            e.DataEvento,
            r.ValorFinalPago,
            r.Pago,
            r.Reembolsada,
            ir.Id AS ItemId,
            ir.CpfParticipante,
            ir.PrecoUnitario,
            ir.Reembolsado
        FROM dbo.Reservas r
        INNER JOIN dbo.Eventos e ON r.EventoId = e.Id
        INNER JOIN dbo.ItensReserva ir ON ir.ReservaId = r.Id
        WHERE r.UsuarioCpf = @cpf
        ORDER BY r.DataReserva DESC, ir.CpfParticipante";

    await conn.QueryAsync<ReservaDetalhadaDTO, ItemReservaResponseDTO, ReservaDetalhadaDTO>(
        new CommandDefinition(sql, new { cpf }, cancellationToken: ct),
        (reserva, item) =>
        {
            if (!reservaDict.TryGetValue(reserva.Id, out var existing))
            {
                existing = reserva;
                existing.Itens = new List<ItemReservaResponseDTO>();
                reservaDict[reserva.Id] = existing;
            }
            existing.Itens.Add(item);
            return existing;
        },
        splitOn: "ItemId"
    );

    return reservaDict.Values;
}
```

**Lógica de agrupamento (passo a passo):**
1. Abre conexão via `_connectionFactory.CreateConnection()`
2. Cria `Dictionary<Guid, ReservaDetalhadaDTO>` vazio
3. Executa query SQL com `INNER JOIN` em `ItensReserva` (sem `STRING_AGG`)
4. Para cada linha retornada:
   a. Tenta obter reserva do dicionário por `Id`
   b. Se não existe: cria entrada com `Itens = new List<>()` e adiciona ao dicionário
   c. Adiciona o `ItemReservaResponseDTO` à lista `Itens` da reserva
5. Retorna `reservaDict.Values`

**Dependencies:** `_connectionFactory` (ConnectionFactory), Dapper `QueryAsync` com `splitOn`, `Domain/DTOs/ReservaDetalhadaDTO`, `Domain/DTOs/ItemReservaResponseDTO`

**Error Handling:** Se a query falhar (timeout, conexão), a exceção do Dapper é propagada.

---

### Component 4: `ReservaService.ListarMinhasReservas` (Application — MODIFICADO)

**Purpose:** Adicionar cálculo de `PodeCancelar` após obter as reservas do Repository.

**Interface (não altera assinatura):**
```csharp
public async Task<IEnumerable<ReservaDetalhadaDTO>> ListarMinhasReservas(string cpf, CancellationToken ct)
```

**Nova implementação:**
```csharp
public async Task<IEnumerable<ReservaDetalhadaDTO>> ListarMinhasReservas(string cpf, CancellationToken ct)
{
    var reservas = await _repositoryReserva.ListarReservasDetalhadasPorCpf(cpf, ct);

    foreach (var reserva in reservas)
    {
        reserva.PodeCancelar = !reserva.Reembolsada && reserva.DataEvento > DateTime.UtcNow;
    }

    return reservas;
}
```

**Lógica (passo a passo):**
1. Chama `_repositoryReserva.ListarReservasDetalhadasPorCpf(cpf, ct)` — obtém lista com itens
2. Itera sobre cada reserva
3. Define `PodeCancelar = !Reembolsada && DataEvento > DateTime.UtcNow`
4. Retorna a coleção modificada

**Dependencies:** `_repositoryReserva` (injeta `IReservaRepository`)

**Error Handling:** Exceções do Repository são propagadas.

---

### Component 5: `ReservaController` (API — VERIFICAR, sem alterações)

**Purpose:** Confirmar que `[Authorize]` não tem restrição de role nos endpoints relevantes.

**Estado esperado (já implementado pela spec 40):**
```csharp
// GET /api/reserva/minhas
[HttpGet("minhas")]
[Authorize]  // sem Roles — aceita qualquer perfil
public async Task<IActionResult> ListarMinhasReservas(CancellationToken ct) { ... }

// DELETE /api/reserva/{id}
[HttpDelete("{id:guid}")]
[Authorize]  // sem Roles — aceita qualquer perfil
public async Task<IActionResult> CancelarReserva(Guid id, CancellationToken ct) { ... }
```

**Ação:** Apenas verificar. Se houver `[Authorize(Roles = "...")]`, remover a restrição.

---

## Data Flow

### Fluxo: Listagem de Minhas Reservas (unificado)

```
Usuário (qualquer perfil) → GET /api/reserva/minhas
  → ReservaController.ListarMinhasReservas()
    → Extrai CPF do JWT (User.Claims["cpf"])
    → ReservaService.ListarMinhasReservas(cpf, ct)
      → ReservaRepository.ListarReservasDetalhadasPorCpf(cpf, ct)
        → SQL: SELECT r.*, e.*, ir.* FROM Reservas r
               INNER JOIN Eventos e ON r.EventoId = e.Id
               INNER JOIN ItensReserva ir ON ir.ReservaId = r.Id
               WHERE r.UsuarioCpf = @cpf
               ORDER BY r.DataReserva DESC
        → Dapper splitOn: "ItemId" com lambda de agrupamento
        → Retorna IEnumerable<ReservaDetalhadaDTO> (com Itens populados)
      → foreach: PodeCancelar = !Reembolsada && DataEvento > DateTime.UtcNow
      → Retorna IEnumerable<ReservaDetalhadaDTO> (com PodeCancelar calculado)
    → Retorna 200 OK + JSON array
```

---

## Data Structures

### ReservaDetalhadaDTO (após modificação)
```csharp
public class ReservaDetalhadaDTO
{
    public Guid Id { get; set; }                        // PK Reserva
    public string NomeEvento { get; set; } = "";        // Nome do evento (JOIN Eventos)
    public DateTime DataEvento { get; set; }             // Data do evento (usado em PodeCancelar)
    public string PosicaoIngresso { get; set; } = "";   // Mantido para compatibilidade (STRING_AGG antigo)
    public string SetorIngresso { get; set; } = "";     // Mantido para compatibilidade
    public string? CupomUtilizado { get; set; }          // Cupom aplicado
    public decimal ValorFinalPago { get; set; }          // Valor após desconto
    public bool Pago { get; set; }                       // Pagamento confirmado
    public bool Reembolsada { get; set; }                // spec 40
    public List<ItemReservaResponseDTO> Itens { get; set; } = new();  // NOVO: itens individuais
    public bool PodeCancelar { get; set; }               // NOVO: calculado em memória
}
```

### ItemReservaResponseDTO (após modificação)
```csharp
public class ItemReservaResponseDTO
{
    public Guid Id { get; set; }                         // NOVO: PK do item
    public string CpfParticipante { get; set; } = string.Empty;
    public Guid IngressoId { get; set; }
    public decimal PrecoUnitario { get; set; }
    public bool Reembolsado { get; set; }                // NOVO: flag de reembolso individual
}
```

---

## File / Module Layout

```
ProjetoTicket/
├── Domain/
│   └── DTOs/
│       └── ReservaDetalhadaDTO.cs              ← MODIFICAR (Task 1: +Itens, +PodeCancelar)
├── Application/
│   └── DTOs/
│       └── ItemReservaResponseDTO.cs           ← MODIFICAR (Task 2: +Id, +Reembolsado)
├── Infraestructure/
│   └── Repository/
│       └── ReservaRepository.cs                ← MODIFICAR (Task 3: refatorar query)
├── Application/
│   └── Service/
│       └── ReservaService.cs                   ← MODIFICAR (Task 4: +PodeCancelar)
├── Api/
│   └── Controllers/
│       └── ReservaController.cs                ← VERIFICAR (Task 5: [Authorize])
└── tests/
    └── ReservaTests.cs                          ← MODIFICAR (Task 6: +9 testes)
```

---

## Testing Strategy

### Testes unitários (Service)

| # | Método | Cenário | Entrada | Saída esperada |
|---|--------|---------|---------|----------------|
| T1 | `ListarMinhasReservas` | Comprador vê apenas suas reservas | cpf comprador | Lista contém apenas reservas do cpf |
| T2 | `ListarMinhasReservas` | Vendedor vê apenas suas reservas | cpf vendedor | Lista contém apenas reservas do cpf |
| T3 | `ListarMinhasReservas` | Admin vê apenas suas reservas | cpf admin | Lista contém apenas reservas do cpf |
| T4 | `ListarMinhasReservas` | Reserva cancelada → itens reembolsados | Reserva com Reembolsada=true | Todos os itens com Reembolsado=true |
| T5 | `ListarMinhasReservas` | Evento futuro, não reembolsada | DataEvento > agora, Reembolsada=false | PodeCancelar=true |
| T6 | `ListarMinhasReservas` | Já reembolsada | Reembolsada=true | PodeCancelar=false |
| T7 | `ListarMinhasReservas` | Evento já passou | DataEvento <= agora, Reembolsada=false | PodeCancelar=false |
| T8 | `CancelarReserva` | Vendedor cancela própria reserva | cpf vendedor = UsuarioCpf | Sucesso |
| T9 | `CancelarReserva` | Admin cancela própria reserva | cpf admin = UsuarioCpf | Sucesso |

---

## Migration / Rollback

**Nenhuma migration necessária** — colunas já existem:
- `Reservas.Reembolsada` — spec 40 (`Script0013_AdicionarReembolsadaReservas.sql`)
- `ItensReserva.Reembolsado` — ST-04
- `Reservas.UsuarioCpf` — spec 40

**Rollback de código:** Reverter query do Repository para versão com `STRING_AGG`. Remover campos `Itens` e `PodeCancelar` do DTO. Remover cálculo de `PodeCancelar` do Service.
