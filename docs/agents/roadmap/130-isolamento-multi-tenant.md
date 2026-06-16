# 130 — Isolamento Multi-Tenant (VendedorId)

> **Origem:** Pontos críticos [#9](../../sprints.md) e regras de isolamento do [`especificacoes.md`](../../especificacoes.md#42-regras-de-isolamento)
>
> **Problema:** [`visao.md#2`](../../visao.md#2-problema) — Ter autonomia com privacidade: um vendedor não pode ver dados de outro vendedor.

---

## 130.1 O que resolve

| Item | Risco | Solução |
|------|-------|---------|
| Queries sem `VendedorId` | Vendedor X vê eventos/cupons/reservas do Vendedor Y | Toda query inclui `WHERE VendedorId = @vendedorId` |
| `VendedorId` via rota | Falsificação de identidade manipulando a URL | `VendedorId` SEMPRE extraído do JWT (`User.FindFirst("sub")`) |
| Background Worker sem filtro | Worker opera fora do contexto de isolamento | Worker respeita `VendedorId` quando interage com dados de vendedor |

---

## 130.2 Tabelas com VendedorId

| Tabela | Coluna | Índice |
|--------|--------|--------|
| `Eventos` | `VendedorId` | `CREATE INDEX IX_Eventos_VendedorId ON Eventos(VendedorId)` |
| `Cupons` | *(global)* | Cupons são globais — sem `VendedorId`, gerenciados pelo Admin |
| `Reservas` | `VendedorId` | `CREATE INDEX IX_Reservas_VendedorId ON Reservas(VendedorId)` |

---

## 130.3 Padrão de Query

```csharp
// ❌ NUNCA: VendedorId como parâmetro de rota ou corpo
[HttpGet("eventos/{vendedorId}")]
public IActionResult Listar(string vendedorId) { ... }

// ✅ SEMPRE: VendedorId do JWT
[HttpGet("meus-eventos")]
[Authorize(Roles = "Vendedor")]
public IActionResult MeusEventos()
{
    var vendedorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var eventos = _eventoRepo.ListarPorVendedor(vendedorId);
    return Ok(eventos);
}
```

```sql
-- Toda query de vendedor inclui este filtro
SELECT * FROM Eventos WHERE VendedorId = @vendedorId;
SELECT * FROM Cupons WHERE VendedorId = @vendedorId;
SELECT * FROM Reservas WHERE VendedorId = @vendedorId;
```

---

## 130.4 Background Worker

```csharp
public class LiberacaoAssentosWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(60), ct);

            // Worker é processo de sistema — opera globalmente
            // Mas consultas que precisam de contexto de vendedor incluem VendedorId
            await _ingressoRepo.LiberarIngressosExpirados();
        }
    }
}
```

```sql
-- Worker: libera ingressos bloqueados há mais de 15 minutos (global)
UPDATE Ingressos
SET Status = 0, DataBloqueio = NULL
WHERE Status = 1
  AND DATEDIFF(MINUTE, DataBloqueio, GETDATE()) >= 15;
```

---

## 130.5 SQL Migration

```sql
-- Adicionar VendedorId nas tabelas (exceto Cupons — globais)
ALTER TABLE Eventos ADD VendedorId VARCHAR(14) NOT NULL DEFAULT '';
ALTER TABLE Reservas ADD VendedorId VARCHAR(14) NOT NULL DEFAULT '';

-- Foreign Keys
ALTER TABLE Eventos ADD CONSTRAINT FK_Eventos_Usuarios
    FOREIGN KEY (VendedorId) REFERENCES Usuarios(Cpf);
ALTER TABLE Reservas ADD CONSTRAINT FK_Reservas_Usuarios
    FOREIGN KEY (VendedorId) REFERENCES Usuarios(Cpf);

-- Índices
CREATE INDEX IX_Eventos_VendedorId ON Eventos(VendedorId);
CREATE INDEX IX_Reservas_VendedorId ON Reservas(VendedorId);
```

---

## 130.6 Regra de Negócio

```
SE perfil == Admin:
    → Query SEM filtro VendedorId (visão global)

SE perfil == Vendedor:
    → Toda query tem WHERE VendedorId = @vendedorId do JWT
    → Vendedor NUNCA acessa dados de outro vendedor

SE perfil == Comprador:
    → Vê apenas suas reservas: WHERE UsuarioCpf = @cpf do JWT
    → Vê todos eventos públicos (sem filtro VendedorId na listagem)
```
