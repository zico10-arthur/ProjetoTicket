# 50 — ST-06: Vendedor Cancela Evento com Reembolso Obrigatório

> **Origem:** [`storytelling.md#st-06-vendedor-cancela-evento`](../../storytelling.md#st-06-vendedor-cancela-evento)
>
> **Problema:** [`visao.md#2`](../../visao.md#2-problema) — Processar cancelamentos e reembolsos de forma organizada e transparente.

---

## 50.1 História

**Como** um vendedor,
**Quero** cancelar um evento que não será mais realizado,
**Para** liberar a grade de eventos e reembolsar os compradores quando necessário.

---

## 50.2 Endpoint

```
DELETE /api/evento/{id}
Auth: JWT (Vendedor dono do evento ou Admin)
```

### Verificação Prévia (antes de confirmar)

```
GET /api/evento/{id}/status-cancelamento
Auth: JWT (Vendedor ou Admin)
```

### Response do status-cancelamento

```json
{
    "eventoId": "a1b2c3d4-...",
    "nome": "Workshop .NET 9",
    "gratuito": false,
    "totalIngressosVendidos": 23,
    "totalReservasAtivas": 18,
    "reembolsoNecessario": true,
    "valorTotalReembolso": 1147.70,
    "mensagem": "23 ingressos vendidos. O cancelamento exigirá reembolso. Deseja continuar?"
}
```

---

## 50.3 Lógica de Cancelamento

```csharp
public void CancelarEvento(Guid eventoId, string vendedorId)
{
    var evento = _eventoRepo.BuscarPorId(eventoId);

    // 60.3.1 Verificar propriedade
    if (evento.VendedorId != vendedorId)
        throw new UnauthorizedException();

    // 60.3.2 Verificar se já cancelado
    if (evento.Cancelado)
        throw new EventoJaCanceladoException();

    // 60.3.3 Contar ingressos vendidos
    var ingressosVendidos = _ingressoRepo.ContarVendidosPorEvento(eventoId);

    // 60.3.4 Transação atômica
    using var transaction = _connection.BeginTransaction();

    // Marca evento como cancelado
    _eventoRepo.MarcarCancelado(eventoId, transaction);

    if (!evento.Gratuito && ingressosVendidos > 0)
    {
        // Reembolsa ingressos
        _ingressoRepo.ReembolsarTodosDoEvento(eventoId, transaction);

        // Marca reservas como reembolsadas
        _reservaRepo.MarcarReembolsadasPorEvento(eventoId, transaction);

        // Marca itens como reembolsados
        _itemReservaRepo.MarcarReembolsadosPorEvento(eventoId, transaction);
    }
    else if (evento.Gratuito)
    {
        // Evento gratuito: apenas cancela, sem reembolso
        _reservaRepo.MarcarReembolsadasPorEvento(eventoId, transaction);
        _itemReservaRepo.MarcarReembolsadosPorEvento(eventoId, transaction);
    }

    transaction.Commit();
}
```

---

## 50.4 SQL da Transação

```sql
BEGIN TRANSACTION;

    -- 1. Marca evento
    UPDATE Eventos SET Cancelado = 1 WHERE Id = @eventoId;

    -- 2. Reembolsa ingressos (Status=3)
    UPDATE Ingressos SET Status = 3 WHERE EventoId = @eventoId AND Status = 2;

    -- 3. Marca reservas
    UPDATE Reservas SET Reembolsada = 1
    WHERE EventoId = @eventoId AND Reembolsada = 0;

    -- 4. Marca itens
    UPDATE ItensReserva SET Reembolsado = 1
    WHERE ReservaId IN (SELECT Id FROM Reservas WHERE EventoId = @eventoId);

COMMIT;
```

## 50.5 Respostas

| Código | Caso |
|--------|------|
| `200 OK` | Evento cancelado com sucesso |
| `403 Forbidden` | Vendedor não é dono do evento |
| `404 Not Found` | Evento não encontrado |
| `409 Conflict` | Evento já cancelado anteriormente |
