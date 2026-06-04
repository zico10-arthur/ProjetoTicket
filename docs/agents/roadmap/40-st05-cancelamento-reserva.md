# 40 — ST-05: Comprador Cancela Reserva com Reembolso

> **Origem:** [`storytelling.md#st-05-comprador-cancela-reserva`](../../storytelling.md#st-05-comprador-cancela-reserva)
>
> **Problema:** [`visao.md#2`](../../visao.md#2-problema) — Processar cancelamentos e reembolsos de forma organizada e transparente.

---

## 40.1 História

**Como** um comprador (ou Admin, ou Vendedor),
**Quero** cancelar minha reserva antes do evento começar e receber reembolso,
**Para** não perder dinheiro se não puder mais comparecer.

---

## 40.2 Endpoint

```
DELETE /api/reserva/{id}
Auth: JWT (dono da reserva, Vendedor dono do evento, ou Admin)
```

### Autorização

| Perfil | Condição |
|--------|----------|
| Comprador | `Reserva.UsuarioCpf == cpf do JWT` |
| Vendedor | `Reserva.VendedorId == cpf do JWT` |
| Admin | Sem restrições |

---

## 40.3 Lógica de Cancelamento

```csharp
public void CancelarReserva(Guid reservaId, string usuarioCpf, string perfil)
{
    var reserva = _reservaRepo.BuscarPorId(reservaId);
    var evento = _eventoRepo.BuscarPorId(reserva.EventoId);

    // 50.3.1 Verificar se já foi cancelada
    if (reserva.Reembolsada)
        throw new ReservaJaCanceladaException();

    // 50.3.2 Verificar se evento já começou
    if (evento.DataEvento <= DateTime.Now)
        throw new EventoJaComecouException("Não é possível cancelar. O evento já começou.");

    // 50.3.3 Verificar autorização
    if (perfil == "Comprador" && reserva.UsuarioCpf != usuarioCpf)
        throw new UnauthorizedException();
    if (perfil == "Vendedor" && reserva.VendedorId != usuarioCpf)
        throw new UnauthorizedException();

    // 50.3.4 Transação atômica
    using var transaction = _connection.BeginTransaction();

    if (evento.Gratuito)
    {
        // Evento gratuito: cancela sem reembolso
        _reservaRepo.MarcarReembolsada(reservaId, transaction);
        _itemReservaRepo.MarcarTodosReembolsados(reservaId, transaction);
        if (evento.Tipo == TipoEvento.Teatro)
            _ingressoRepo.LiberarIngressosDaReserva(reservaId, transaction);
    }
    else
    {
        // Evento pago: cancela com reembolso
        _reservaRepo.MarcarReembolsada(reservaId, transaction);
        _itemReservaRepo.MarcarTodosReembolsados(reservaId, transaction);
        if (evento.Tipo == TipoEvento.Teatro)
            _ingressoRepo.LiberarIngressosDaReserva(reservaId, transaction);
    }

    transaction.Commit();
}
```

---

## 40.4 SQL da Transação

```sql
BEGIN TRANSACTION;

    -- Marca reserva como reembolsada
    UPDATE Reservas SET Reembolsada = 1 WHERE Id = @reservaId;

    -- Marca todos os itens como reembolsados
    UPDATE ItensReserva SET Reembolsado = 1 WHERE ReservaId = @reservaId;

    -- Libera ingressos (apenas Teatro)
    UPDATE Ingressos
    SET Status = 0, DataBloqueio = NULL
    WHERE Id IN (SELECT IngressoId FROM ItensReserva WHERE ReservaId = @reservaId);

COMMIT;
```

## 40.5 Respostas

| Código | Caso |
|--------|------|
| `200 OK` | Reserva cancelada com sucesso |
| `400 Bad Request` | Evento já começou: "Não é possível cancelar. O evento já começou." |
| `403 Forbidden` | Usuário não é dono da reserva nem Admin |
| `404 Not Found` | Reserva não encontrada |
| `409 Conflict` | Reserva já foi cancelada anteriormente |
