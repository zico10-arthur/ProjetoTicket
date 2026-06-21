# Spec 50 — Design: Vendedor Cancela Evento com Reembolso Obrigatório

> **Requirements:** [`requirements.md`](./requirements.md)
> **Contexto:** Clean Architecture (.NET 9 + Dapper + SQL Server + DbUp)

---

## 1. Modelo de Domínio

### 1.1 Entidades existentes (sem alterações)

```csharp
// Domain/Entities/Evento.cs — já existe:
public bool Cancelado { get; private set; }
public void Cancelar() { Cancelado = true; }
public bool Gratuito => PrecoPadrao == 0;

// Domain/Entities/Reserva.cs — campo Reembolsada adicionado pela spec 40
// Domain/Entities/ItemReserva.cs — campo Reembolsado já existe
// Domain/Entities/Pagamento.cs — método MarcarReembolsado() já existe (spec 170)
// Domain/Entities/Ingresso.cs — Status (int): 0=Livre, 1=Reservado, 2=Vendido
```

> **Nenhuma entidade nova é necessária.** As entidades existentes já possuem os campos e métodos requeridos. O campo `Reembolsada` em `Reserva` será adicionado pela spec 40 (dependência cross-spec).

---

## 2. Banco de Dados

### 2.1 Migration (apenas se `Reembolsada` ainda não existir)

A spec 50 depende da migration da spec 40 (`Script0012_AdicionarReembolsadaReservas.sql`). Nenhuma migration adicional é necessária.

### 2.2 Diagrama de Impacto do Cancelamento

```
┌──────────────────┐
│     Eventos      │
│  Cancelado = 1   │ ← UPDATE
└────────┬─────────┘
         │
    ┌────┴────┐
    │         │
    ▼         ▼
┌──────────┐ ┌──────────────┐
│Ingressos │ │   Reservas   │
│Status = 3│ │Reembolsada=1 │ ← UPDATE
└──────────┘ └──────┬───────┘
                    │
              ┌─────┴─────┐
              ▼           ▼
       ┌────────────┐ ┌────────────┐
       │ItensReserva│ │ Pagamentos │
       │Reembolsado │ │Status = 3  │ ← UPDATE
       │    = 1     │ │            │
       └────────────┘ └────────────┘
```

---

## 3. API

### 3.1 `GET /api/evento/{id}/status-cancelamento`

```
GET /api/evento/a1b2c3d4-e5f6-7890-abcd-ef1234567890/status-cancelamento
Auth: JWT (Vendedor dono do evento ou Admin)
```

#### Response `200 OK`

```json
{
    "eventoId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "nome": "Workshop .NET 9",
    "gratuito": false,
    "totalIngressosVendidos": 23,
    "totalReservasAtivas": 18,
    "reembolsoNecessario": true,
    "valorTotalReembolso": 1147.70,
    "mensagem": "23 ingressos vendidos. O cancelamento exigirá reembolso de R$ 1.147,70. Deseja continuar?"
}
```

#### Response `403 Forbidden`

```json
{
    "message": "Você não tem permissão para acessar este evento."
}
```

#### Response `404 Not Found`

```json
{
    "message": "Evento não encontrado."
}
```

### 3.2 `DELETE /api/evento/{id}` (modificado)

```
DELETE /api/evento/a1b2c3d4-e5f6-7890-abcd-ef1234567890
Auth: JWT (Vendedor dono do evento ou Admin)
```

> **Mudança em relação ao comportamento atual:** Antes este endpoint excluía fisicamente o evento. Agora ele cancela com reembolso (marcação lógica).

#### Response `200 OK`

```json
{
    "message": "Evento cancelado com sucesso. 23 ingressos reembolsados."
}
```

#### Response `403 Forbidden`

```json
{
    "message": "Você não tem permissão para cancelar este evento."
}
```

#### Response `404 Not Found`

```json
{
    "message": "Evento não encontrado."
}
```

#### Response `409 Conflict`

```json
{
    "message": "Evento já foi cancelado."
}
```

### 3.3 Atualização em `GET /api/evento` (listagem pública)

A query de listagem pública deve filtrar eventos cancelados:

```sql
-- Antes:
SELECT * FROM Eventos

-- Depois:
SELECT * FROM Eventos WHERE Cancelado = 0
```

### 3.4 DTOs

```csharp
// Application/DTOs/StatusCancelamentoDTO.cs (novo)
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

---

## 4. Camada de Aplicação

### 4.1 Interface `IEventoService` (atualizada)

```csharp
// Application/Interfaces/IEventoService.cs — adicionar:
namespace Application.Interfaces;

public interface IEventoService
{
    // ... métodos existentes (GetAllAsync, GetByIdAsync, CriarEventoAsync, UpdateAsync) ...

    /// <summary>
    /// Consulta o impacto do cancelamento antes de executá-lo.
    /// </summary>
    Task<StatusCancelamentoDTO> ObterStatusCancelamento(Guid eventoId, string vendedorCpf, bool isAdmin, CancellationToken ct);

    /// <summary>
    /// Cancela o evento com reembolso obrigatório dos ingressos vendidos.
    /// Substitui o DeleteAsync anterior.
    /// </summary>
    Task CancelarEvento(Guid eventoId, string vendedorCpf, bool isAdmin, CancellationToken ct);
}
```

### 4.2 Lógica de `ObterStatusCancelamento` (EventoService)

```csharp
// Application/Service/EventoService.cs — adicionar método:
public async Task<StatusCancelamentoDTO> ObterStatusCancelamento(
    Guid eventoId, string vendedorCpf, bool isAdmin, CancellationToken ct)
{
    var evento = await _eventoRepository.GetByIdAsync(eventoId);
    if (evento == null)
        throw new KeyNotFoundException($"Evento {eventoId} não encontrado.");

    if (!isAdmin && evento.VendedorCpf != vendedorCpf)
        throw new UnauthorizedAccessException("Você não tem permissão para acessar este evento.");

    var status = await _eventoRepository.ObterStatusCancelamento(eventoId, ct);

    var mensagem = status.ReembolsoNecessario
        ? $"{status.TotalIngressosVendidos} ingressos vendidos. O cancelamento exigirá reembolso de R$ {status.ValorTotalReembolso:F2}. Deseja continuar?"
        : "Nenhum ingresso vendido. O cancelamento não exige reembolso.";

    return status with { Mensagem = mensagem };
}
```

### 4.3 Lógica de `CancelarEvento` (EventoService)

```csharp
// Application/Service/EventoService.cs — adicionar método (substitui DeleteAsync):
public async Task CancelarEvento(Guid eventoId, string vendedorCpf, bool isAdmin, CancellationToken ct)
{
    // 1. Buscar evento
    var evento = await _eventoRepository.GetByIdAsync(eventoId);
    if (evento == null)
        throw new KeyNotFoundException($"Evento {eventoId} não encontrado.");

    // 2. Verificar autorização
    if (!isAdmin && evento.VendedorCpf != vendedorCpf)
        throw new UnauthorizedAccessException("Você não tem permissão para cancelar este evento.");

    // 3. Verificar se já cancelado
    if (evento.Cancelado)
        throw new DomainException("Evento já foi cancelado.");

    // 4. Executar cancelamento com transação atômica
    await _eventoRepository.CancelarComTransacao(eventoId, evento.Gratuito, ct);
}
```

> **Nota:** O método `DeleteAsync` existente deve ser **substituído** por `CancelarEvento`. O endpoint `DELETE` não será mais de exclusão física.

---

## 5. Camada de Infraestrutura

### 5.1 Interface `IEventoRepository` (atualizada)

```csharp
// Domain/Interface/IEventoRepository.cs — adicionar:
namespace Domain.Interface;

public interface IEventoRepository
{
    // ... métodos existentes (GetAllAsync, GetByIdAsync, CriarEventoCompletoAsync, UpdateAsync) ...

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

    /// <summary>
    /// Remove o DeleteAsync antigo — cancelamento é lógico, não físico.
    /// </summary>
    // Task DeleteAsync(Guid id); ← REMOVER da interface
}
```

### 5.2 Consulta de Status de Cancelamento (Repository)

```csharp
// Infraestructure/Repository/EventoRepository.cs — adicionar método:
public async Task<StatusCancelamentoDTO> ObterStatusCancelamento(Guid eventoId, CancellationToken ct)
{
    using var connection = _factory.CreateConnection();

    const string sql = @"
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
        GROUP BY e.Id, e.Nome, e.PrecoPadrao";

    return await connection.QuerySingleOrDefaultAsync<StatusCancelamentoDTO>(
        new CommandDefinition(sql, new { eventoId }, cancellationToken: ct));
}
```

### 5.3 Transação SQL de Cancelamento de Evento (Repository)

```sql
-- Executado dentro de transação no EventoRepository.CancelarComTransacao:

BEGIN TRANSACTION;

    -- 1. Marcar evento como cancelado
    UPDATE Eventos SET Cancelado = 1 WHERE Id = @eventoId;

    -- 2. Se evento pago: marcar ingressos como reembolsados (Status=3)
    IF (@gratuito = 0)
    BEGIN
        UPDATE Ingressos
        SET Status = 3
        WHERE EventoId = @eventoId AND Status = 2;
    END

    -- 3. Marcar todas as reservas como reembolsadas
    UPDATE Reservas
    SET Reembolsada = 1
    WHERE EventoId = @eventoId AND Reembolsada = 0;

    -- 4. Marcar todos os itens como reembolsados
    UPDATE ItensReserva
    SET Reembolsado = 1
    WHERE ReservaId IN (
        SELECT Id FROM Reservas WHERE EventoId = @eventoId
    );

    -- 5. Marcar pagamentos como reembolsados (se evento pago)
    IF (@gratuito = 0)
    BEGIN
        UPDATE Pagamentos
        SET Status = 3
        WHERE ReservaId IN (
            SELECT Id FROM Reservas WHERE EventoId = @eventoId
        ) AND Status = 1;
    END

COMMIT;
```

### 5.4 Implementação do Repositório

```csharp
// Infraestructure/Repository/EventoRepository.cs — adicionar método:
public async Task CancelarComTransacao(Guid eventoId, bool gratuito, CancellationToken ct)
{
    using var connection = _factory.CreateConnection();
    await connection.OpenAsync(ct);
    using var transaction = connection.BeginTransaction();

    try
    {
        // 1. Marcar evento
        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE Eventos SET Cancelado = 1 WHERE Id = @eventoId",
            new { eventoId }, transaction, cancellationToken: ct));

        // 2. Se pago: reembolsar ingressos
        if (!gratuito)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                "UPDATE Ingressos SET Status = 3 WHERE EventoId = @eventoId AND Status = 2",
                new { eventoId }, transaction, cancellationToken: ct));
        }

        // 3. Marcar reservas
        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE Reservas SET Reembolsada = 1 WHERE EventoId = @eventoId AND Reembolsada = 0",
            new { eventoId }, transaction, cancellationToken: ct));

        // 4. Marcar itens
        await connection.ExecuteAsync(new CommandDefinition(@"
            UPDATE ItensReserva SET Reembolsado = 1
            WHERE ReservaId IN (SELECT Id FROM Reservas WHERE EventoId = @eventoId)",
            new { eventoId }, transaction, cancellationToken: ct));

        // 5. Marcar pagamentos (se pago)
        if (!gratuito)
        {
            await connection.ExecuteAsync(new CommandDefinition(@"
                UPDATE Pagamentos SET Status = 3
                WHERE ReservaId IN (SELECT Id FROM Reservas WHERE EventoId = @eventoId)
                AND Status = 1",
                new { eventoId }, transaction, cancellationToken: ct));
        }

        await transaction.CommitAsync(ct);
    }
    catch
    {
        await transaction.RollbackAsync(ct);
        throw;
    }
}
```

---

## 6. Integração com Fluxos Existentes

### 6.1 Listagem pública de eventos (afetada)

```
GET /api/evento
  → Query atualizada para filtrar WHERE Cancelado = 0
  → Eventos cancelados não aparecem mais para compradores
```

### 6.2 Listagem de eventos do vendedor (afetada)

```
GET /api/evento/meus
  → Incluir campo "cancelado" no response
  → Eventos cancelados aparecem com destaque visual
```

### 6.3 Reservas de eventos cancelados

```
GET /api/reserva/minhas
  → Reservas de eventos cancelados continuam visíveis
  → Campo "reembolsada": true indica que o comprador foi reembolsado
```

### 6.4 Dependência da spec 40

```
Spec 40 (Reembolsada em Reservas) deve ser implementada antes ou em conjunto.
O campo Reservas.Reembolsada é usado na transação de cancelamento de evento.
```

---

## 7. Fluxo de Dados (Sequência)

```
Vendedor              Controller              Service                  Repository            SQL Server
   │                      │                      │                         │                     │
   │  GET /status-cancelamento/{id}              │                         │                     │
   │─────────────────────>│                      │                         │                     │
   │                      │  ObterStatus()       │                         │                     │
   │                      │─────────────────────>│                         │                     │
   │                      │                      │  BuscarPorId()          │                     │
   │                      │                      │────────────────────────>│  SELECT Evento      │
   │                      │                      │<────────────────────────│                     │
   │                      │                      │  ObterStatusCancelamento│                     │
   │                      │                      │────────────────────────>│  SELECT agregado    │
   │                      │                      │<────────────────────────│                     │
   │                      │  StatusCancelamentoDTO                        │                     │
   │                      │<─────────────────────│                         │                     │
   │  200 OK (dados)      │                      │                         │                     │
   │<─────────────────────│                      │                         │                     │
   │                      │                      │                         │                     │
   │  DELETE /api/evento/{id}                    │                         │                     │
   │─────────────────────>│                      │                         │                     │
   │                      │  CancelarEvento()    │                         │                     │
   │                      │─────────────────────>│                         │                     │
   │                      │                      │  Validações             │                     │
   │                      │                      │  CancelarComTransacao() │                     │
   │                      │                      │────────────────────────>│  BEGIN TRAN         │
   │                      │                      │                         │  UPDATE Eventos     │
   │                      │                      │                         │  UPDATE Ingressos   │
   │                      │                      │                         │  UPDATE Reservas    │
   │                      │                      │                         │  UPDATE ItensReserva│
   │                      │                      │                         │  UPDATE Pagamentos  │
   │                      │                      │                         │  COMMIT             │
   │                      │                      │<────────────────────────│                     │
   │  200 OK              │                      │                         │                     │
   │<─────────────────────│                      │                         │                     │
```

---

## 8. Decisões de Design

| Decisão | Justificativa |
|---------|---------------|
| Cancelamento lógico (`Cancelado = true`) em vez de exclusão física | Preserva histórico para auditoria e consultas de compradores; reservas reembolsadas precisam continuar visíveis em "Minhas Reservas" |
| `GET /api/evento/{id}/status-cancelamento` como endpoint separado | Separação de responsabilidades — o frontend consulta o impacto antes de confirmar; evita sobrecarregar o DELETE com lógica de pré-visualização |
| `Ingressos.Status = 3` para reembolsado | Diferencia de `Status = 0` (Livre) para auditoria; consistente com spec 170 (`StatusPagamento.Reembolsado = 3`) |
| Transação condicional por `@gratuito` | Eventos gratuitos não têm pagamentos — evita UPDATEs desnecessários em `Pagamentos` e `Ingressos` |
| Admin pode cancelar qualquer evento | Consistente com o comportamento atual do `DeleteAsync` (que já permitia Admin) |
| `DeleteAsync` removido da interface | Cancelamento é lógico, não físico; o método de exclusão física não é mais necessário |
| Endpoint `DELETE` mantido com semântica alterada | O verbo HTTP `DELETE` permanece, mas agora executa cancelamento lógico em vez de exclusão física — semântica REST de remover o recurso do estado ativo |
