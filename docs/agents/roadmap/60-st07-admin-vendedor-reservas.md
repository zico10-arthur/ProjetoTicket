# 60 — ST-07: Admin e Vendedor Podem Fazer Reservas

> **Origem:** [`storytelling.md#st-07-admin-e-vendedor-podem-fazer-reservas`](../../storytelling.md#st-07-admin-e-vendedor-podem-fazer-reservas)
>
> **Problema:** [`visao.md#2`](../../visao.md#2-problema) — Ter autonomia: todos os perfis do sistema podem participar de eventos como compradores.

---

## 60.1 História

**Como** um Admin ou Vendedor,
**Quero** poder comprar ingressos para eventos usando meu próprio perfil,
**Para** participar de eventos como qualquer outro comprador.

---

## 60.2 Mudança nos Controllers

### ReservaController

```csharp
// ❌ ANTES
[Authorize(Roles = "Comprador")]
public class ReservaController : ControllerBase { }

// ✅ DEPOIS
[Authorize]  // Qualquer perfil autenticado
public class ReservaController : ControllerBase { }
```

### Método FazerReserva

```csharp
[HttpPost("criar")]
public IActionResult CriarReserva(ReservarDTO dto)
{
    // Identidade vem do JWT — funciona para Admin, Vendedor e Comprador
    var usuarioCpf = User.FindFirst("sub")?.Value;
    var role = User.FindFirst(ClaimTypes.Role)?.Value;

    var reserva = _reservaService.FazerReserva(usuarioCpf, dto);
    return Created($"/api/reserva/{reserva.Id}", reserva);
}
```

### Método MinhasReservas

```csharp
[HttpGet("minhas")]
public IActionResult MinhasReservas()
{
    var usuarioCpf = User.FindFirst("sub")?.Value;

    // Funciona para Admin, Vendedor e Comprador
    var reservas = _reservaService.ListarPorUsuario(usuarioCpf);
    return Ok(reservas);
}
```

---

## 60.3 Query

```sql
-- Funciona para qualquer perfil (Admin, Vendedor, Comprador)
SELECT r.*, e.Nome as EventoNome, e.DataEvento
FROM Reservas r
JOIN Eventos e ON r.EventoId = e.Id
WHERE r.UsuarioCpf = @usuarioCpf
ORDER BY r.DataReserva DESC;
```

---

## 60.4 Regras Mantidas

```
✅ Anti-cambista: um CPF só pode ter uma reserva ativa por evento
   → Independente do perfil (Admin, Vendedor ou Comprador)

✅ Cancelamento: as mesmas regras para todos
   → DataEvento > agora, transação atômica

✅ Isolamento na listagem:
   → Minhas Reservas: filtra por UsuarioCpf (seu próprio CPF)
   → Admin NÃO vê reservas de outros automaticamente em "Minhas Reservas"
```

## 60.5 Impacto

| Antes | Depois |
|-------|--------|
| `[Authorize(Roles = "Comprador")]` no ReservaController | `[Authorize]` sem restrição de role |
| Admin e Vendedor não podiam comprar ingressos | Ambos acessam Home, compram e veem Minhas Reservas |
| Apenas Comprador aparecia na lista de eventos com botão "Comprar" | Todos os perfis veem o botão "Comprar" |
