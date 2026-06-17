# Spec 40 — Design: Comprador Cancela Reserva com Reembolso

> **Requirements:** [`requirements.md`](./requirements.md)
> **Contexto:** Clean Architecture (.NET 9 + Dapper + SQL Server + DbUp)

---

## 1. Modelo de Domínio

### 1.1 Campo adicional em `Reserva`

```csharp
// Domain/Entities/Reserva.cs — adicionar:
public bool Reembolsada { get; private set; }

// Adicionar método:
public void MarcarReembolsada()
{
    Reembolsada = true;
}
```

> **Nota:** A entidade `Reserva` já possui `Pago` (spec 170). O campo `Reembolsada` é independente — uma reserva pode estar `Pago = true` e depois `Reembolsada = true` após cancelamento.

### 1.2 Atualização em `ItemReserva`

```csharp
// Domain/Entities/ItemReserva.cs — já existe:
public bool Reembolsado { get; private set; }

public void MarcarReembolsado()
{
    Reembolsado = true;
}
```

> **Status:** `ItemReserva` já possui os campos e métodos necessários. Nenhuma alteração necessária.

### 1.3 Atualização em `Pagamento`

```csharp
// Domain/Entities/Pagamento.cs — já existe (spec 170):
public void MarcarReembolsado()
{
    Status = StatusPagamento.Reembolsado;
}
```

> **Status:** `Pagamento` já possui o método. Nenhuma alteração necessária.

### 1.4 Atualização em `Ingresso`

```csharp
// Domain/Entities/Ingresso.cs — já existe:
public void Liberar() => Status = 0;
```

> **Status:** `Ingresso` já possui o método. Nenhuma alteração necessária.

---

## 2. Banco de Dados

### 2.1 Migration: `Script0012_AdicionarReembolsadaReservas.sql`

```sql
-- Script0012: Adicionar coluna Reembolsada na tabela Reservas

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Reservas' AND COLUMN_NAME = 'Reembolsada'
)
BEGIN
    ALTER TABLE dbo.Reservas ADD Reembolsada BIT NOT NULL DEFAULT 0;
END
```

### 2.2 Diagrama de Relacionamentos

```
┌──────────────┐       ┌────────────────┐       ┌───────────────┐
│   Reservas   │       │   Pagamentos   │       │   Ingressos   │
├──────────────┤       ├────────────────┤       ├───────────────┤
│ Id (PK)      │───<   │ Id (PK)        │       │ Id (PK)       │
│ UsuarioCpf   │       │ ReservaId (FK) │       │ EventoId (FK) │
│ EventoId (FK)│       │ ValorPago      │       │ Status        │
│ Pago         │       │ Status         │       │ DataBloqueio  │
│ Reembolsada  │ NEW   │ Metodo         │       │ ...           │
│ ...          │       │ ...            │       └───────────────┘
└──────────────┘       └────────────────┘              │
       │                                               │
       │   ┌──────────────────┐                        │
       │   │   ItensReserva   │                        │
       │   ├──────────────────┤                        │
       └──<│ ReservaId (FK)   │                        │
           │ IngressoId (FK)  │────────────────────────┘
           │ CpfParticipante  │
           │ PrecoUnitario    │
           │ Reembolsado      │
           └──────────────────┘
```

---

## 3. API

### 3.1 `DELETE /api/reserva/{id}`

```
DELETE /api/reserva/a1b2c3d4-e5f6-7890-abcd-ef1234567890
Auth: JWT (qualquer perfil autenticado)
```

#### Autorização

| Perfil | Condição |
|--------|----------|
| Comprador | `Reserva.UsuarioCpf == cpf do JWT` |
| Vendedor | `Reserva.UsuarioCpf == cpf do JWT` |
| Admin | `Reserva.UsuarioCpf == cpf do JWT` (suas próprias reservas) |

> Admin também pode cancelar qualquer reserva via endpoints admin — mas o `DELETE /api/reserva/{id}` opera apenas sobre as reservas do próprio usuário.

#### Response `200 OK`

```json
{
    "message": "Reserva cancelada com sucesso. Reembolso registrado."
}
```

#### Response `400 Bad Request`

```json
{
    "message": "Não é possível cancelar. O evento já começou."
}
```

#### Response `403 Forbidden`

```json
{
    "message": "Esta reserva não pertence a você."
}
```

#### Response `404 Not Found`

```json
{
    "message": "Reserva não encontrada."
}
```

#### Response `409 Conflict`

```json
{
    "message": "Reserva já foi cancelada."
}
```

### 3.2 Atualização em `GET /api/reserva/minhas` (existente)

O response deve incluir os campos de reembolso:

```json
[
    {
        "id": "r1r2r3r4-...",
        "eventoNome": "Workshop .NET 9",
        "dataEvento": "2026-07-15T19:00:00",
        "valorFinalPago": 90.00,
        "pago": true,
        "reembolsada": false,
        "dataReserva": "2026-06-04T15:30:00",
        "itens": [
            {
                "cpfParticipante": "529.885.310-09",
                "precoUnitario": 50.00,
                "reembolsado": false
            },
            {
                "cpfParticipante": "555.666.777-88",
                "precoUnitario": 50.00,
                "reembolsado": false
            }
        ],
        "podeCancelar": true
    }
]
```

---

## 4. Camada de Aplicação

### 4.1 Interface `IReservaService` (atualizada)

```csharp
// Application/Interfaces/IReservaService.cs — adicionar:
namespace Application.Interfaces;

public interface IReservaService
{
    // ... métodos existentes (FazerReserva, ListarMinhasReservas) ...

    /// <summary>
    /// Cancela uma reserva própria. Marca Reembolsada, libera ingressos,
    /// e transiciona Pagamento.Status para Reembolsado se aplicável.
    /// </summary>
    Task CancelarReserva(Guid reservaId, string usuarioCpf, CancellationToken ct);
}
```

### 4.2 Lógica de `CancelarReserva` (ReservaService)

```csharp
// Application/Service/ReservaService.cs — adicionar método:
public async Task CancelarReserva(Guid reservaId, string usuarioCpf, CancellationToken ct)
{
    // 1. Buscar reserva
    var reserva = await _repositoryReserva.BuscarPorId(reservaId, ct);
    if (reserva == null)
        throw new DomainException("Reserva não encontrada.");

    // 2. Verificar propriedade
    if (reserva.UsuarioCpf != usuarioCpf)
        throw new UnauthorizedAccessException("Esta reserva não pertence a você.");

    // 3. Verificar se já foi cancelada
    if (reserva.Reembolsada)
        throw new DomainException("Reserva já foi cancelada.");

    // 4. Verificar se evento já começou
    var evento = await _repositoryEvento.GetByIdAsync(reserva.EventoId);
    if (evento == null)
        throw new DomainException("Evento não encontrado.");

    if (evento.DataEvento <= DateTime.UtcNow)
        throw new DomainException("Não é possível cancelar. O evento já começou.");

    // 5. Delegar transação atômica ao repositório
    await _repositoryReserva.CancelarComTransacao(reservaId, evento, ct);
}
```

---

## 5. Camada de Infraestrutura

### 5.1 Interface `IReservaRepository` (atualizada)

```csharp
// Domain/Interface/IReservaRepository.cs — adicionar:
namespace Domain.Interface;

public interface IReservaRepository
{
    // ... métodos existentes ...

    /// <summary>
    /// Executa o cancelamento em transação atômica:
    /// Reserva.Reembolsada = 1, ItensReserva.Reembolsado = 1,
    /// Ingressos.Status = 0, Pagamento.Status = 3 (Reembolsado).
    /// </summary>
    Task CancelarComTransacao(Guid reservaId, Evento evento, CancellationToken ct);
}
```

### 5.2 Transação SQL (Repository)

```sql
-- Executado dentro de transação no ReservaRepository.CancelarComTransacao:

BEGIN TRANSACTION;

    -- 1. Marcar reserva como reembolsada
    UPDATE Reservas SET Reembolsada = 1 WHERE Id = @reservaId;

    -- 2. Marcar todos os itens da reserva como reembolsados
    UPDATE ItensReserva SET Reembolsado = 1 WHERE ReservaId = @reservaId;

    -- 3. Liberar ingressos (Status = 0) e limpar DataBloqueio
    UPDATE Ingressos
    SET Status = 0, DataBloqueio = NULL
    WHERE Id IN (
        SELECT IngressoId FROM ItensReserva WHERE ReservaId = @reservaId
    );

    -- 4. Marcar pagamento como reembolsado (se existir)
    UPDATE Pagamentos
    SET Status = 3   -- StatusPagamento.Reembolsado = 3 (código do enum)
    WHERE ReservaId = @reservaId AND Status = 1;  -- apenas Confirmados

COMMIT;
```

> **Nota:** O valor do enum `StatusPagamento.Reembolsado` é `3` no código atual. Verificar consistência com o enum existente — se o valor for diferente, ajustar.

### 5.3 Implementação do Repositório

```csharp
// Infraestructure/Repository/ReservaRepository.cs — adicionar método:
public async Task CancelarComTransacao(Guid reservaId, Evento evento, CancellationToken ct)
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

        // 2. Marcar itens como reembolsados
        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE ItensReserva SET Reembolsado = 1 WHERE ReservaId = @reservaId",
            new { reservaId }, transaction, cancellationToken: ct));

        // 3. Liberar ingressos (apenas se for Teatro — Palestra não tem IngressoId)
        await connection.ExecuteAsync(new CommandDefinition(@"
            UPDATE Ingressos
            SET Status = 0, DataBloqueio = NULL
            WHERE Id IN (
                SELECT IngressoId FROM ItensReserva WHERE ReservaId = @reservaId
            )",
            new { reservaId }, transaction, cancellationToken: ct));

        // 4. Marcar pagamento como reembolsado (se existir)
        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE Pagamentos SET Status = 3 WHERE ReservaId = @reservaId AND Status = 1",
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

> **Branch por TipoEvento:** Para eventos `Palestra`, `ItensReserva.IngressoId` pode ser `NULL`. O sub-SELECT não retorna linhas e o UPDATE em `Ingressos` afeta 0 registros — comportamento seguro.

---

## 6. Integração com Fluxos Existentes

### 6.1 Criação de Reserva (existente)

```
POST /api/reserva/criar
  → Cria Reserva (Reembolsada = 0 por padrão)
  → Ingressos Status = 1 (Reservado)
```

### 6.2 Checkout de Pagamento (spec 170, existente)

```
POST /api/pagamento/checkout/{reservaId}
  → Pagamento criado com Status = Confirmado
  → Reserva.Pago = 1
  → Ingressos Status = 2 (Vendido)
```

### 6.3 Cancelamento com Reembolso (esta spec)

```
DELETE /api/reserva/{id}
  → Reserva.Reembolsada = 1
  → ItensReserva.Reembolsado = 1
  → Ingressos Status = 0 (Livre), DataBloqueio = NULL
  → Pagamento.Status = 3 (Reembolsado) — se existir
```

### 6.4 Consulta de Minhas Reservas (atualizada)

```
GET /api/reserva/minhas
  → Response inclui "reembolsada": true/false por reserva
  → Response inclui "reembolsado": true/false por item
  → Response inclui "podeCancelar": true se !Reembolsada && DataEvento > agora
```

---

## 7. Fluxo de Dados (Sequência)

```
Usuário                  Controller             Service                  Repository            SQL Server
   │                         │                     │                         │                     │
   │  DELETE /api/reserva/{id}                     │                         │                     │
   │────────────────────────>│                     │                         │                     │
   │                         │  CancelarReserva()  │                         │                     │
   │                         │────────────────────>│                         │                     │
   │                         │                     │  BuscarPorId()          │                     │
   │                         │                     │────────────────────────>│  SELECT Reserva     │
   │                         │                     │<────────────────────────│                     │
   │                         │                     │                         │                     │
   │                         │                     │  Validações:            │                     │
   │                         │                     │  - dono?                │                     │
   │                         │                     │  - já reembolsada?      │                     │
   │                         │                     │  - evento começou?      │                     │
   │                         │                     │                         │                     │
   │                         │                     │  CancelarComTransacao() │                     │
   │                         │                     │────────────────────────>│  BEGIN TRAN         │
   │                         │                     │                         │  UPDATE Reservas    │
   │                         │                     │                         │  UPDATE ItensReserva│
   │                         │                     │                         │  UPDATE Ingressos   │
   │                         │                     │                         │  UPDATE Pagamentos  │
   │                         │                     │                         │  COMMIT             │
   │                         │                     │<────────────────────────│                     │
   │                         │                     │                         │                     │
   │                         │  200 OK             │                         │                     │
   │<────────────────────────│                     │                         │                     │
```

---

## 8. Decisões de Design

| Decisão | Justificativa |
|---------|---------------|
| `Reembolsada` como `BIT` na tabela `Reservas` | Evita JOIN desnecessário para verificar status de cancelamento; consulta rápida em `GET /api/reserva/minhas` |
| Transação no Repository, não no Service | Segue o padrão existente do projeto (`CadastrarReservaComItens`, `CriarComTransacao` do spec 170) |
| `Pagamento.Status = 3` (Reembolsado) | Valor do enum `StatusPagamento.Reembolsado` já definido na spec 170; consistência cross-spec |
| Cancelamento apenas de reservas próprias | Evita que um usuário cancele reservas de terceiros; Admin tem endpoints separados para gestão |
| CPF extraído do JWT, nunca do body | Segurança — impossibilita falsificação de identidade; padrão já adotado em todo o projeto |
| Sem verificação de pagamento para cancelar | O comprador pode cancelar mesmo sem ter pago; a reserva é desfeita e os ingressos são liberados |
| UPDATE de `Ingressos` incondicional (sem verificar TipoEvento) | Para `Palestra` o sub-SELECT não retorna linhas — segura e evita branch desnecessária no código |
