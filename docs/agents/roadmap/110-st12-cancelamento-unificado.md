# 110 — ST-12: Cancelamento de Reserva — Visão Unificada

> **Origem:** [`storytelling.md#st-12-cancelamento-de-reserva`](../../storytelling.md#st-12-cancelamento-de-reserva)
>
> **Problema:** [`visao.md#2`](../../visao.md#2-problema) — Processar cancelamentos e reembolsos: qualquer perfil pode cancelar sua reserva com as mesmas regras.

---

## 110.1 História

**Como** qualquer usuário logado (Comprador, Admin ou Vendedor),
**Quero** acessar Minhas Reservas e cancelar qualquer reserva minha,
**Para** ter controle total sobre minhas compras.

---

## 110.2 Endpoint — Minhas Reservas

```
GET /api/reserva/minhas
Auth: JWT (qualquer perfil)
```

### Query

```sql
SELECT r.Id, r.EventoId, e.Nome as EventoNome, e.DataEvento,
       r.ValorFinalPago, r.Reembolsada, r.DataReserva,
       ir.Id as ItemId, ir.CpfParticipante, ir.PrecoUnitario, ir.Reembolsado
FROM Reservas r
JOIN Eventos e ON r.EventoId = e.Id
LEFT JOIN ItensReserva ir ON ir.ReservaId = r.Id
WHERE r.UsuarioCpf = @usuarioCpf   -- CPF do JWT
ORDER BY r.DataReserva DESC;
```

### Response

```json
[
    {
        "id": "r1r2r3r4-...",
        "eventoNome": "Workshop .NET 9",
        "dataEvento": "2026-07-15T19:00:00",
        "valorFinalPago": 90.00,
        "reembolsada": false,
        "dataReserva": "2026-06-04T15:30:00",
        "itens": [
            { "cpfParticipante": "11122233344", "precoUnitario": 50.00, "reembolsado": false },
            { "cpfParticipante": "55566677788", "precoUnitario": 50.00, "reembolsado": false }
        ],
        "podeCancelar": true
    }
]
```

---

## 110.3 Endpoint — Cancelar Reserva

```
DELETE /api/reserva/{id}
Auth: JWT (dono da reserva)
```

### Regras de Cancelamento (visão unificada)

| Perfil | Pode cancelar se | Bloqueado se |
|--------|-----------------|-------------|
| **Comprador** | `Reserva.UsuarioCpf == cpf do JWT` | Não é dono |
| **Vendedor** | `Reserva.UsuarioCpf == cpf do JWT` | Não é dono |
| **Admin** | `Reserva.UsuarioCpf == cpf do JWT` (suas próprias) | — |

> **Nota:** Admin também pode cancelar QUALQUER reserva via endpoints de admin, mas em "Minhas Reservas" aparecem apenas as dele.

---

## 110.4 Lógica Unificada

```csharp
public void CancelarMinhaReserva(Guid reservaId, string usuarioCpf)
{
    var reserva = _repo.BuscarPorId(reservaId);

    // 120.4.1 Verificar propriedade (mesma lógica para todos os perfis)
    if (reserva.UsuarioCpf != usuarioCpf)
        throw new UnauthorizedException("Esta reserva não pertence a você.");

    // 120.4.2 Verificar se já foi cancelada
    if (reserva.Reembolsada)
        throw new ReservaJaCanceladaException();

    // 120.4.3 Verificar se evento já começou
    var evento = _eventoRepo.BuscarPorId(reserva.EventoId);
    if (evento.DataEvento <= DateTime.Now)
        throw new EventoJaComecouException("Não é possível cancelar. O evento já começou.");

    // 120.4.4 Transação
    using var tx = _connection.BeginTransaction();
    _repo.MarcarReembolsada(reservaId, tx);
    _itemRepo.MarcarTodosReembolsados(reservaId, tx);
    if (evento.Tipo == TipoEvento.Teatro)
        _ingressoRepo.LiberarDaReserva(reservaId, tx);
    tx.Commit();
}
```

---

## 110.5 Mudança da ST-12

| Antes | Agora |
|-------|-------|
| Cancelamento restrito ao perfil Comprador | Qualquer perfil (Admin, Vendedor, Comprador) cancela suas próprias reservas |
| Cancelamento sem visibilidade de itens | Cada ItemReserva tem seu flag `Reembolsado` visível |
| Lógica duplicada por controller | Método único `CancelarMinhaReserva` no Service |
