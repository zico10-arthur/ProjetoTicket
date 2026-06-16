# Spec 170 — Design: Pagamento Simulado (Checkout Interno)

> **Requirements:** [`requirements.md`](./requirements.md)
> **Contexto:** Clean Architecture (.NET 9 + Dapper + SQL Server + DbUp)

---

## 1. Modelo de Domínio

### 1.1 Enum `StatusPagamento`

```csharp
// Domain/Enums/StatusPagamento.cs
namespace Domain.Enums;

public enum StatusPagamento
{
    Pendente   = 0,   // Reservado para futuro uso com gateway real
    Confirmado = 1,   // Pagamento simulado confirmado
    Reembolsado = 2,  // Marcado quando ST-05/ST-06 processam reembolso
    Falhou     = 3    // Reservado para futuro uso com gateway real
}
```

### 1.2 Entidade `Pagamento` (atualizada)

```csharp
// Domain/Entities/Pagamento.cs
namespace Domain.Entities;

public class Pagamento
{
    public Guid Id { get; private set; }
    public Guid ReservaId { get; private set; }
    public decimal ValorPago { get; private set; }
    public StatusPagamento Status { get; private set; }
    public string Metodo { get; private set; }
    public DateTime DataPagamento { get; private set; }
    public DateTime DataCriacao { get; private set; }

    // Navegação
    public Reserva Reserva { get; private set; }

    private Pagamento() { }

    public Pagamento(Guid reservaId, decimal valorPago, string metodo)
    {
        Id = Guid.NewGuid();
        ReservaId = reservaId;
        ValorPago = valorPago;
        Metodo = metodo;
        Status = StatusPagamento.Confirmado;
        DataPagamento = DateTime.UtcNow;
        DataCriacao = DateTime.UtcNow;
    }

    public void MarcarReembolsado()
    {
        Status = StatusPagamento.Reembolsado;
    }
}
```

### 1.3 Campo adicional em `Reserva`

```csharp
// Adicionar à entidade Domain/Entities/Reserva.cs:
public bool Pago { get; private set; }

// Adicionar método:
public void MarcarPago()
{
    Pago = true;
}

// Adicionar método:
public void MarcarReembolsado()
{
    Pago = false;
    // ou manter Pago = true e usar campo Reembolsada separado
}
```

> **Nota:** `Reserva` já possui o campo `Reembolsada` (via ST-05/ST-06). O campo `Pago` é adicional e independente. Uma reserva pode estar `Pago = true` e depois `Reembolsada = true` após cancelamento.

---

## 2. Banco de Dados

### 2.1 Migration: `Script0011_CriarTabelaPagamentos.sql`

```sql
-- Script0011: Criar tabela Pagamentos e adicionar campo Pago em Reservas

-- 1. Criar tabela Pagamentos
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Pagamentos')
BEGIN
    CREATE TABLE dbo.Pagamentos (
        Id              UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        ReservaId       UNIQUEIDENTIFIER NOT NULL,
        ValorPago       DECIMAL(10,2)    NOT NULL,
        Status          INT              NOT NULL DEFAULT 0,
        Metodo          NVARCHAR(50)     NOT NULL,
        DataPagamento   DATETIME2        NOT NULL,
        DataCriacao     DATETIME2        NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_Pagamentos_Reservas FOREIGN KEY (ReservaId)
            REFERENCES Reservas(Id)
    );
END

-- 2. Adicionar campo Pago em Reservas
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Reservas' AND COLUMN_NAME = 'Pago'
)
BEGIN
    ALTER TABLE dbo.Reservas ADD Pago BIT NOT NULL DEFAULT 0;
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
│ Pago         │ NEW   │ Status         │       │ ...           │
│ Reembolsada  │       │ Metodo         │       └───────────────┘
│ ...          │       │ DataPagamento  │              │
└──────────────┘       │ DataCriacao    │              │
       │               └────────────────┘              │
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

### 3.1 `POST /api/pagamento/checkout/{reservaId}`

```
POST /api/pagamento/checkout/a1b2c3d4-e5f6-7890-abcd-ef1234567890
Auth: JWT (qualquer perfil autenticado)
Content-Type: application/json
```

#### Request Body

```json
{
    "metodo": "Simulado"
}
```

#### Response `200 OK`

```json
{
    "pagamentoId": "f1e2d3c4-b5a6-9780-cdef-1234567890ab",
    "reservaId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "valorPago": 180.00,
    "status": "Confirmado",
    "dataPagamento": "2026-06-16T14:30:00Z",
    "message": "Pagamento confirmado com sucesso!"
}
```

#### Response `400 Bad Request`

```json
{
    "message": "Não é possível pagar. O evento já começou."
}
```

#### Response `403 Forbidden`

```json
{
    "message": "Esta reserva não pertence ao usuário logado."
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
    "message": "Reserva já foi paga."
}
```

### 3.2 `GET /api/pagamento/admin/todos`

```
GET /api/pagamento/admin/todos
Auth: JWT (role=Admin)
```

#### Response `200 OK`

```json
[
    {
        "pagamentoId": "f1e2d3c4-...",
        "reservaId": "a1b2c3d4-...",
        "usuarioCpf": "529.885.310-09",
        "eventoNome": "Workshop .NET 9",
        "valorPago": 180.00,
        "status": "Confirmado",
        "metodo": "Simulado",
        "dataPagamento": "2026-06-16T14:30:00Z"
    }
]
```

### 3.3 DTOs

```csharp
// Application/DTOs/CheckoutRequestDTO.cs
public record CheckoutRequestDTO(string Metodo);

// Application/DTOs/CheckoutResponseDTO.cs
public record CheckoutResponseDTO(
    Guid PagamentoId,
    Guid ReservaId,
    decimal ValorPago,
    string Status,
    DateTime DataPagamento,
    string Message
);

// Application/DTOs/PagamentoAdminDTO.cs
public record PagamentoAdminDTO(
    Guid PagamentoId,
    Guid ReservaId,
    string UsuarioCpf,
    string EventoNome,
    decimal ValorPago,
    string Status,
    string Metodo,
    DateTime DataPagamento
);
```

---

## 4. Camada de Aplicação

### 4.1 Interface `IPagamentoService`

```csharp
// Application/Interfaces/IPagamentoService.cs
namespace Application.Interfaces;

public interface IPagamentoService
{
    Task<CheckoutResponseDTO> ConfirmarCheckout(Guid reservaId, string usuarioCpf, string metodo, CancellationToken ct);
    Task<IEnumerable<PagamentoAdminDTO>> ListarTodosAdmin(CancellationToken ct);
}
```

### 4.2 Lógica de Checkout (`PagamentoService`)

```csharp
// Application/Service/PagamentoService.cs
public async Task<CheckoutResponseDTO> ConfirmarCheckout(
    Guid reservaId, string usuarioCpf, string metodo, CancellationToken ct)
{
    // 1. Buscar reserva
    var reserva = await _reservaRepo.BuscarPorId(reservaId, ct);
    if (reserva == null)
        throw new DomainException("Reserva não encontrada.");

    // 2. Verificar propriedade
    if (reserva.UsuarioCpf != usuarioCpf)
        throw new UnauthorizedAccessException("Esta reserva não pertence ao usuário logado.");

    // 3. Verificar se já foi paga
    if (reserva.Pago)
        throw new DomainException("Reserva já foi paga.");

    // 4. Verificar se já foi reembolsada/cancelada
    if (reserva.Reembolsada)
        throw new DomainException("Reserva foi cancelada.");

    // 5. Verificar se evento já começou
    var evento = await _eventoRepo.BuscarPorId(reserva.EventoId, ct);
    if (evento.DataEvento <= DateTime.UtcNow)
        throw new DomainException("Não é possível pagar. O evento já começou.");

    // 6. Criar entidade Pagamento
    var pagamento = new Pagamento(reservaId, reserva.ValorFinalPago, metodo);

    // 7. Transação atômica
    await _pagamentoRepo.CriarComTransacao(pagamento, reserva, evento, ct);

    return new CheckoutResponseDTO(
        pagamento.Id,
        reservaId,
        pagamento.ValorPago,
        pagamento.Status.ToString(),
        pagamento.DataPagamento,
        "Pagamento confirmado com sucesso!"
    );
}
```

---

## 5. Camada de Infraestrutura

### 5.1 Interface `IPagamentoRepository`

```csharp
// Domain/Interface/IPagamentoRepository.cs
namespace Domain.Interface;

public interface IPagamentoRepository
{
    Task CriarComTransacao(Pagamento pagamento, Reserva reserva, Evento evento, CancellationToken ct);
    Task<IEnumerable<PagamentoAdminDTO>> ListarTodosAdmin(CancellationToken ct);
}
```

### 5.2 Transação SQL (Repository)

```sql
-- Executado dentro de uma transação no PagamentoRepository:

BEGIN TRANSACTION;

    -- 1. Inserir pagamento
    INSERT INTO Pagamentos (Id, ReservaId, ValorPago, Status, Metodo, DataPagamento, DataCriacao)
    VALUES (@Id, @ReservaId, @ValorPago, @Status, @Metodo, @DataPagamento, @DataCriacao);

    -- 2. Marcar reserva como paga
    UPDATE Reservas SET Pago = 1 WHERE Id = @ReservaId;

    -- 3. Atualizar status dos ingressos para Vendido (Status=2)
    --    Apenas para eventos tipo Teatro (que possuem Ingressos com IngressoId)
    UPDATE Ingressos
    SET Status = 2
    WHERE Id IN (
        SELECT IngressoId FROM ItensReserva WHERE ReservaId = @ReservaId
    );

COMMIT;
```

> **Branch por TipoEvento:** Se o evento for `Palestra`, não há registros em `ItensReserva.IngressoId` — o UPDATE em `Ingressos` afeta 0 linhas, o que é seguro. A lógica permanece a mesma.

---

## 6. Integração com Fluxos Existentes

### 6.1 Criação de Reserva (existente)

```
POST /api/reserva/criar
  → Cria Reserva (Pago = 0 por padrão)
  → Ingressos Status = 1 (Reservado)
  → Comprador precisa confirmar pagamento em seguida
```

### 6.2 Checkout de Pagamento (nova)

```
POST /api/pagamento/checkout/{reservaId}
  → Cria Pagamento
  → Reserva.Pago = 1
  → Ingressos Status = 2 (Vendido)
```

### 6.3 Cancelamento com Reembolso (ST-05/ST-06)

```
DELETE /api/reserva/{id}  (quando ST-05 implementado)
  → Reserva.Reembolsada = true
  → Ingressos Status = 0 (Livre)
  → SE existir Pagamento associado:
       Pagamento.Status = Reembolsado
       Pagamento.MarcarReembolsado()
```

> **Acoplamento mínimo:** O fluxo de reembolso (ST-05/ST-06) deve ser atualizado para chamar `_pagamentoRepo.MarcarReembolsado(reservaId)` se houver pagamento. Essa dependência está documentada em `tasks.md`.

### 6.4 Verificação de Status de Pagamento

O campo `Reserva.Pago` é incluído automaticamente nas queries de reserva existentes:
- `GET /api/reserva/minhas` → response inclui `"pago": true/false`
- `GET /api/reserva/Admin/Todas` → response inclui `"pago": true/false`

---

## 7. Fluxo de Dados (Sequência)

```
Comprador               API                    Service              Repository           SQL Server
   │                     │                        │                     │                    │
   │  POST /checkout/{id}│                        │                     │                    │
   │────────────────────>│                        │                     │                    │
   │                     │  ConfirmarCheckout()   │                     │                    │
   │                     │───────────────────────>│                     │                    │
   │                     │                        │  BuscarPorId()      │                    │
   │                     │                        │────────────────────>│  SELECT Reserva    │
   │                     │                        │<────────────────────│                    │
   │                     │                        │                     │                    │
   │                     │                        │  Validações:        │                    │
   │                     │                        │  - dono?            │                    │
   │                     │                        │  - já pago?         │                    │
   │                     │                        │  - evento começou?  │                    │
   │                     │                        │                     │                    │
   │                     │                        │  new Pagamento()    │                    │
   │                     │                        │                     │                    │
   │                     │                        │  CriarComTransacao()│                    │
   │                     │                        │────────────────────>│  BEGIN TRAN        │
   │                     │                        │                     │  INSERT Pagamento  │
   │                     │                        │                     │  UPDATE Reservas   │
   │                     │                        │                     │  UPDATE Ingressos  │
   │                     │                        │                     │  COMMIT            │
   │                     │                        │<────────────────────│                    │
   │                     │                        │                     │                    │
   │                     │  CheckoutResponseDTO   │                     │                    │
   │                     │<───────────────────────│                     │                    │
   │  200 OK             │                        │                     │                    │
   │<────────────────────│                        │                     │                    │
```

---

## 8. Decisões de Design

| Decisão | Justificativa |
|---------|---------------|
| `Metodo` como `string` (não enum) | Permite adicionar novos métodos de pagamento sem mudar o schema (ex: "Cartao", "Pix", "Boleto" no futuro) |
| `Status` como `enum` com `Pendente` e `Falhou` mesmo sem uso imediato | Facilita a migração para gateway real na Fase 3 sem alterar schema |
| `Pago` como `BIT` na tabela `Reservas` (não deduzido do Pagamento) | Evita JOIN desnecessário em todas as queries que precisam saber status de pagamento |
| Transação no Repository, não no Service | Segue o padrão existente do projeto (ex: ST-05, ST-06 usam transaction no repositório) |
| Endpoint dedicado `PagamentoController` (não no `ReservaController`) | Separação de responsabilidades; o `ReservaController` já tem responsabilidades de CRUD de reserva |
| `ConfirmarPagamento` existente será removido | O endpoint atual em `ReservaController` (`POST /api/reserva/ConfirmarPagamento/{ingressoId}`) opera por ingresso individual e será substituído pelo novo fluxo |
