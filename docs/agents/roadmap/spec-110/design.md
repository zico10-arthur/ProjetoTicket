# Spec 110 — Design: Cancelamento de Reserva — Visão Unificada

> **Requirements:** [`requirements.md`](./requirements.md)
> **Contexto:** Clean Architecture (.NET 9 + Dapper + SQL Server + DbUp)
> **Pré-requisitos:** Spec 40 (cancelamento de reserva), Spec 70/80/90 (login unificado, vendedor na tabela Usuarios, perfis simplificados)

---

## 1. Modelo de Domínio

### 1.1 Entidades existentes (sem alterações)

```csharp
// Domain/Entities/Reserva.cs — já possui:
public string UsuarioCpf { get; private set; }    // CPF do dono (qualquer perfil)
public bool Pago { get; private set; }
public bool Reembolsada { get; private set; }      // Adicionado pela spec 40

// Domain/Entities/ItemReserva.cs — já possui:
public bool Reembolsado { get; private set; }
```

> **Nenhuma entidade nova ou alteração de domínio é necessária.** As specs 40, 70, 80 e 90 já fornecem a base: login unificado com `UsuarioCpf` no JWT, e `Reembolsada`/`Reembolsado` nas entidades.

---

## 2. Banco de Dados

### 2.1 Nenhuma migration necessária

As tabelas `Reservas` e `ItensReserva` já possuem as colunas necessárias:
- `Reservas.Reembolsada` — adicionada pela spec 40 (`Script0012`)
- `ItensReserva.Reembolsado` — já existente (spec 30/ST-04)

### 2.2 Query atualizada de Minhas Reservas

```sql
-- Query consolidada que retorna reservas com itens e flags de reembolso:
SELECT
    r.Id,
    e.Nome AS EventoNome,
    e.DataEvento,
    r.ValorFinalPago,
    r.Pago,
    r.Reembolsada,
    r.DataReserva,
    ir.Id AS ItemId,
    ir.CpfParticipante,
    ir.PrecoUnitario,
    ir.Reembolsado,
    STRING_AGG(i.Posicao, ', ') AS PosicaoIngresso,
    STRING_AGG(i.Setor, ', ') AS SetorIngresso
FROM Reservas r
INNER JOIN Eventos e ON r.EventoId = e.Id
INNER JOIN ItensReserva ir ON ir.ReservaId = r.Id
INNER JOIN Ingressos i ON ir.IngressoId = i.Id
WHERE r.UsuarioCpf = @cpf
GROUP BY r.Id, e.Nome, e.DataEvento, r.ValorFinalPago, r.Pago,
         r.Reembolsada, r.DataReserva, ir.Id, ir.CpfParticipante,
         ir.PrecoUnitario, ir.Reembolsado
ORDER BY r.DataReserva DESC;
```

> **Mudança chave:** A query filtra por `r.UsuarioCpf = @cpf` (extraído do JWT) — sem restrição de role. Qualquer perfil autenticado vê apenas suas próprias reservas.

---

## 3. API

### 3.1 `GET /api/reserva/minhas` (sem alterações de rota)

```
GET /api/reserva/minhas
Auth: JWT (qualquer perfil — sem restrição de role)
```

> **Antes:** O endpoint já existia com `[Authorize]` sem restrição, mas o design thinking era "Comprador". **Agora:** Explicitamente unificado para todos os perfis.

#### Response `200 OK`

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
        "podeCancelar": true,
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
        ]
    }
]
```

### 3.2 `DELETE /api/reserva/{id}` (sem alterações de rota)

```
DELETE /api/reserva/a1b2c3d4-e5f6-7890-abcd-ef1234567890
Auth: JWT (qualquer perfil — sem restrição de role)
```

> **Antes:** Não existia. **Spec 40:** Criado com `[Authorize]` sem restrição. **Esta spec:** Confirma a unificação — a autorização é por `UsuarioCpf`, não por role.

#### Regras de Cancelamento (visão unificada)

| Perfil | Pode cancelar se | Bloqueado se |
|--------|-----------------|-------------|
| **Comprador** | `Reserva.UsuarioCpf == cpf do JWT` | Não é dono |
| **Vendedor** | `Reserva.UsuarioCpf == cpf do JWT` | Não é dono |
| **Admin** | `Reserva.UsuarioCpf == cpf do JWT` (suas próprias) | — |

> **Nota:** Admin também pode cancelar QUALQUER reserva via endpoints admin (`GET /api/reserva/Admin/Todas`), mas em "Minhas Reservas" aparecem apenas as dele.

### 3.3 DTOs atualizados

```csharp
// Domain/DTOs/ReservaDetalhadaDTO.cs — atualizar:
public class ReservaDetalhadaDTO
{
    public Guid Id { get; set; }
    public string EventoNome { get; set; }
    public DateTime DataEvento { get; set; }
    public string PosicaoIngresso { get; set; }
    public string SetorIngresso { get; set; }
    public string CupomUtilizado { get; set; }
    public decimal ValorFinalPago { get; set; }
    public bool Pago { get; set; }
    public bool Reembolsada { get; set; }          // NOVO (spec 40)
    public List<ItemReservaResponseDTO> Itens { get; set; }  // NOVO (substitui agregação)
    public bool PodeCancelar { get; set; }          // NOVO
}

// Application/DTOs/ItemReservaResponseDTO.cs — garantir que existe:
public class ItemReservaResponseDTO
{
    public Guid Id { get; set; }
    public string CpfParticipante { get; set; }
    public decimal PrecoUnitario { get; set; }
    public bool Reembolsado { get; set; }
}
```

---

## 4. Camada de Aplicação

### 4.1 Interface `IReservaService` (já definida pela spec 40)

```csharp
// Application/Interfaces/IReservaService.cs — já contém:
Task CancelarReserva(Guid reservaId, string usuarioCpf, CancellationToken ct);
Task<IEnumerable<ReservaDetalhadaDTO>> ListarMinhasReservas(string cpf, CancellationToken ct);
```

### 4.2 Lógica de `ListarMinhasReservas` (atualizada)

```csharp
// Application/Service/ReservaService.cs — método existente, lógica atualizada:
public async Task<IEnumerable<ReservaDetalhadaDTO>> ListarMinhasReservas(string cpf, CancellationToken ct)
{
    var reservas = await _repositoryReserva.ListarReservasDetalhadasPorCpf(cpf, ct);

    // Adicionar flag podeCancelar (calculada em memória)
    foreach (var reserva in reservas)
    {
        reserva.PodeCancelar = !reserva.Reembolsada && reserva.DataEvento > DateTime.UtcNow;
    }

    return reservas;
}
```

> **Mudança:** Antes o método não calculava `PodeCancelar`. Agora ele adiciona essa informação com base em `Reembolsada` e `DataEvento`.

---

## 5. Camada de Infraestrutura

### 5.1 Query de Minhas Reservas com Itens (atualizada)

O `ReservaRepository.ListarReservasDetalhadasPorCpf` deve ser refatorado para retornar itens individuais com flags:

```csharp
// Infraestructure/Repository/ReservaRepository.cs — substituir query existente:
public async Task<IEnumerable<ReservaDetalhadaDTO>> ListarReservasDetalhadasPorCpf(string cpf, CancellationToken ct)
{
    using var connection = _factory.CreateConnection();

    // Dicionário para agrupar itens por reserva
    var reservaDict = new Dictionary<Guid, ReservaDetalhadaDTO>();

    const string sql = @"
        SELECT
            r.Id,
            e.Nome AS EventoNome,
            e.DataEvento,
            r.ValorFinalPago,
            r.Pago,
            r.Reembolsada,
            r.DataReserva,
            ir.Id AS ItemId,
            ir.CpfParticipante,
            ir.PrecoUnitario,
            ir.Reembolsado
        FROM Reservas r
        INNER JOIN Eventos e ON r.EventoId = e.Id
        INNER JOIN ItensReserva ir ON ir.ReservaId = r.Id
        WHERE r.UsuarioCpf = @cpf
        ORDER BY r.DataReserva DESC, ir.CpfParticipante";

    await connection.QueryAsync<ReservaDetalhadaDTO, ItemReservaResponseDTO, ReservaDetalhadaDTO>(
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

> **Mudança estrutural:** A query anterior usava `STRING_AGG` para agregar posições de ingressos em string. A nova query retorna itens individuais com `splitOn: "ItemId"`, permitindo visibilidade granular de `Reembolsado`.

---

## 6. Integração com Fluxos Existentes

### 6.1 Relação com spec 40 (Cancelamento de Reserva)

```
Spec 40 implementa:
  - DELETE /api/reserva/{id} com [Authorize] (qualquer perfil)
  - CancelarReserva no Service (unificado por UsuarioCpf)

Spec 110 garante:
  - GET /api/reserva/minhas funciona para todos os perfis
  - Response inclui flags de reembolso por item
  - Lógica unificada é validada para todos os cenários de perfil
```

### 6.2 Relação com spec 50 (Cancelamento de Evento)

```
Quando um evento é cancelado (spec 50):
  - Reservas.Reembolsada = true
  - ItensReserva.Reembolsado = true

GET /api/reserva/minhas reflete automaticamente esses status.
```

### 6.3 Relação com specs 70/80/90 (Login Unificado)

```
O login unificado garante que Admin, Vendedor e Comprador recebem JWT com claim "cpf".
O campo Reserva.UsuarioCpf é o mesmo para todos os perfis.
A autorização "UsuarioCpf == cpf do JWT" funciona identicamente para todos.
```

---

## 7. Fluxo de Dados (Sequência)

```
Usuário (qualquer perfil)      Controller              Service                  Repository
        │                         │                       │                         │
        │  GET /api/reserva/minhas│                       │                         │
        │────────────────────────>│                       │                         │
        │                         │  ListarMinhasReservas │                         │
        │                         │──────────────────────>│                         │
        │                         │                       │  ListarDetalhadas(cpf)  │
        │                         │                       │────────────────────────>│
        │                         │                       │<────────────────────────│
        │                         │                       │                         │
        │                         │                       │  Calcula PodeCancelar:  │
        │                         │                       │  !Reembolsada &&        │
        │                         │                       │  DataEvento > agora     │
        │                         │                       │                         │
        │                         │  List<ReservaDetalhada>                         │
        │                         │<──────────────────────│                         │
        │  200 OK (com itens)     │                       │                         │
        │<────────────────────────│                       │                         │
```

---

## 8. Decisões de Design

| Decisão | Justificativa |
|---------|---------------|
| `[Authorize]` sem restrição de role nos endpoints | Todos os perfis compartilham a mesma tabela `Usuarios` e o mesmo campo `UsuarioCpf` — a role é irrelevante para posse de reserva |
| `PodeCancelar` calculado em memória (não no SQL) | Lógica simples (`!Reembolsada && DataEvento > agora`); não justifica complexidade no SQL ou coluna adicional |
| Query com `splitOn` para itens (substitui `STRING_AGG`) | Permite expor flags individuais de `Reembolsado` por item; `STRING_AGG` perdia essa granularidade |
| Agrupamento de itens em memória (dicionário) | Dapper não suporta `splitOn` com agrupamento nativo; dicionário em memória é performático para o volume típico de reservas por usuário |
| Sem endpoint separado por perfil | Um único `GET /api/reserva/minhas` serve todos — evita duplicação e mantém a experiência unificada |
