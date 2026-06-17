# Spec 110 — Tasks: Cancelamento de Reserva — Visão Unificada

> **Requirements:** [`requirements.md`](./requirements.md)
> **Design:** [`design.md`](./design.md)
> **Ordem:** Cada task depende das anteriores. Seguir a sequência numérica.
> **Pré-requisito:** Spec 40 implementada (campo `Reembolsada` e `CancelarReserva`).

---

## Task 1 — Atualizar DTO `ReservaDetalhadaDTO` (Domain)

**Objetivo:** Adicionar campos de reembolso e itens no DTO de resposta de Minhas Reservas.

| Campo | Valor |
|-------|-------|
| Arquivo | `Domain/DTOs/ReservaDetalhadaDTO.cs` (editar) |
| Dependências | Spec 40 (campo `Reembolsada`) |

**Mudanças:**
- Adicionar propriedade: `public bool Reembolsada { get; set; }`
- Adicionar propriedade: `public List<ItemReservaResponseDTO> Itens { get; set; }`
- Adicionar propriedade: `public bool PodeCancelar { get; set; }`

> **Nota:** Os campos antigos de agregação (`PosicaoIngresso`, `SetorIngresso`, `CupomUtilizado`) podem ser removidos se não forem mais usados pelo frontend, ou mantidos para compatibilidade.

**Verificação:** Projeto `Domain` compila sem erros.

---

## Task 2 — Garantir `ItemReservaResponseDTO` com Campo `Reembolsado` (Application)

**Objetivo:** Verificar e, se necessário, adicionar o campo `Reembolsado` no DTO de item.

| Campo | Valor |
|-------|-------|
| Arquivo | `Application/DTOs/ItemReservaResponseDTO.cs` (verificar/editar) |
| Dependências | Nenhuma |

**Conteúdo esperado:**
```csharp
public class ItemReservaResponseDTO
{
    public Guid Id { get; set; }
    public string CpfParticipante { get; set; }
    public decimal PrecoUnitario { get; set; }
    public bool Reembolsado { get; set; }
}
```

> Se o arquivo já existir e já tiver o campo `Reembolsado`, esta task é apenas uma verificação.

**Verificação:** Projeto `Application` compila sem erros.

---

## Task 3 — Refatorar Query de Minhas Reservas no Repository (Infrastructure)

**Objetivo:** Substituir a query atual (que usa `STRING_AGG`) por uma query com `splitOn` que retorna itens individuais com flags.

| Campo | Valor |
|-------|-------|
| Arquivo | `Infraestructure/Repository/ReservaRepository.cs` (editar) |
| Dependências | Task 1 (DTO), Task 2 (ItemReservaResponseDTO) |

**Query SQL e lógica de agrupamento:** Conforme especificado em [`design.md §5.1`](./design.md#51-query-de-minhas-reservas-com-itens-atualizada).

**Mudanças em relação à query atual:**
- Remover `STRING_AGG(i.Posicao, ', ')` e `STRING_AGG(i.Setor, ', ')`
- Adicionar colunas de item: `ir.Id AS ItemId`, `ir.CpfParticipante`, `ir.PrecoUnitario`, `ir.Reembolsado`
- Adicionar `r.Reembolsada` no SELECT
- Usar `splitOn: "ItemId"` no `QueryAsync`
- Agrupar itens em dicionário em memória

**Verificação:**
- Projeto `Infraestructure` compila sem erros
- Teste manual: `GET /api/reserva/minhas` retorna itens individuais com `reembolsado: true/false`

---

## Task 4 — Adicionar Cálculo de `PodeCancelar` no Service (Application)

**Objetivo:** Calcular a flag `PodeCancelar` no `ReservaService.ListarMinhasReservas`.

| Campo | Valor |
|-------|-------|
| Arquivo | `Application/Service/ReservaService.cs` (editar) |
| Dependências | Task 3 (repository query) |

**Mudança no método `ListarMinhasReservas`:**
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

**Verificação:**
- Projeto `Application` compila sem erros
- Reserva reembolsada → `podeCancelar: false`
- Reserva ativa com evento futuro → `podeCancelar: true`
- Reserva ativa com evento passado → `podeCancelar: false`

---

## Task 5 — Verificar `[Authorize]` no Controller (API)

**Objetivo:** Confirmar que os endpoints de Minhas Reservas e Cancelamento aceitam qualquer perfil autenticado.

| Campo | Valor |
|-------|-------|
| Arquivo | `Api/Controllers/ReservaController.cs` (verificar) |
| Dependências | Spec 40 (DELETE endpoint) |

**Verificações:**
- `GET /api/reserva/minhas` → `[Authorize]` (sem restrição de role) ✅ — já está correto
- `DELETE /api/reserva/{id}` → `[Authorize]` (sem restrição de role) ✅ — spec 40 já define assim

> Se o código atual tiver restrição de role (ex: `[Authorize(Roles = "Comprador")]`), **remover a restrição**.

**Verificação:**
- Comprador, Vendedor e Admin conseguem acessar ambos os endpoints
- Swagger reflete `[Authorize]` sem roles específicas

---

## Task 6 — Testes Automatizados

**Objetivo:** Garantir cobertura de testes para o cancelamento unificado e visibilidade granular.

| Campo | Valor |
|-------|-------|
| Arquivo | `tests/ReservaTests.cs` (editar — adicionar testes) |
| Dependências | Tasks 1-5 |

**Testes a implementar:**

| # | Teste | Cenário |
|---|-------|---------|
| T1 | `ListarMinhasReservas_Comprador` | Comprador vê apenas suas reservas |
| T2 | `ListarMinhasReservas_Vendedor` | Vendedor vê apenas suas reservas (não vê reservas dos compradores dos seus eventos) |
| T3 | `ListarMinhasReservas_Admin` | Admin vê apenas suas reservas próprias |
| T4 | `ListarMinhasReservas_ComItensReembolsados` | Reserva cancelada → itens com `reembolsado: true` |
| T5 | `ListarMinhasReservas_PodeCancelar_EventoFuturo` | Evento futuro, não reembolsada → `podeCancelar: true` |
| T6 | `ListarMinhasReservas_PodeCancelar_JaReembolsada` | Reembolsada → `podeCancelar: false` |
| T7 | `ListarMinhasReservas_PodeCancelar_EventoPassado` | Evento já começou → `podeCancelar: false` |
| T8 | `CancelarReserva_Vendedor_CancelaPropria` | Vendedor cancela sua própria reserva → sucesso |
| T9 | `CancelarReserva_Admin_CancelaPropria` | Admin cancela sua própria reserva → sucesso |

**Verificação:**
- `dotnet test` passa com todos os 9 testes verdes
- Cobertura dos cenários unificados ≥ 80%

---

## Resumo de Tarefas

| # | Camada | Arquivo(s) | Ação |
|---|--------|-----------|------|
| 1 | Domain | `DTOs/ReservaDetalhadaDTO.cs` | **Editar** (+campos) |
| 2 | Application | `DTOs/ItemReservaResponseDTO.cs` | **Verificar/Editar** (+Reembolsado) |
| 3 | Infraestructure | `Repository/ReservaRepository.cs` | **Refatorar** (query splitOn) |
| 4 | Application | `Service/ReservaService.cs` | **Editar** (+PodeCancelar) |
| 5 | Api | `Controllers/ReservaController.cs` | **Verificar** ([Authorize]) |
| 6 | tests | `ReservaTests.cs` | **Editar** (+9 testes) |

---

## Dependências entre Tasks

```
Spec 40 (pré-requisito)
  ├→ Task 1 (DTO ReservaDetalhada)
  │    └→ Task 3 (query refactor) ──→ Task 4 (PodeCancelar) ──→ Task 6 (testes)
  └→ Task 5 (verificar [Authorize]) ──→ Task 6 (testes)

Task 2 (ItemReservaResponseDTO) ──→ Task 3 (query refactor)
```

---

## Estimativa de Esforço

| Bloco | Tasks | Esforço |
|-------|-------|---------|
| Domain | 1 | 15min |
| Application | 2, 4 | 30min |
| Infrastructure | 3 | 45min |
| API | 5 | 15min |
| Tests | 6 | 1h15 |
| **Total** | **6 tasks** | **~3h** |
