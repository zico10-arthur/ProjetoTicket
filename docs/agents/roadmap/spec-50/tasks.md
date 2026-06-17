# Spec 50 — Tasks: Vendedor Cancela Evento com Reembolso Obrigatório

> **Requirements:** [`requirements.md`](./requirements.md)
> **Design:** [`design.md`](./design.md)
> **Ordem:** Cada task depende das anteriores. Seguir a sequência numérica.
> **Pré-requisito:** Spec 40 implementada (campo `Reembolsada` em `Reservas`).

---

## Task 1 — Criar DTO `StatusCancelamentoDTO` (Application)

**Objetivo:** Criar o DTO para o response de consulta de status de cancelamento.

| Campo | Valor |
|-------|-------|
| Arquivo novo | `Application/DTOs/StatusCancelamentoDTO.cs` |
| Dependências | Nenhuma |

**Conteúdo:**
```csharp
namespace Application.DTOs;

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

**Verificação:** Projeto `Application` compila sem erros.

---

## Task 2 — Atualizar `IEventoRepository` (Domain)

**Objetivo:** Adicionar métodos de cancelamento e status na interface do repositório.

| Campo | Valor |
|-------|-------|
| Arquivo | `Domain/Interface/IEventoRepository.cs` (editar) |
| Dependências | Task 1 (DTO) |

**Mudanças:**
- Adicionar: `Task<StatusCancelamentoDTO> ObterStatusCancelamento(Guid eventoId, CancellationToken ct);`
- Adicionar: `Task CancelarComTransacao(Guid eventoId, bool gratuito, CancellationToken ct);`
- **Remover:** `Task DeleteAsync(Guid id);` (cancelamento é lógico, não físico)

**Verificação:**
- Projeto `Domain` compila sem erros
- Projetos que referenciam `DeleteAsync` quebram a compilação — serão corrigidos nas tasks seguintes

---

## Task 3 — Implementar `ObterStatusCancelamento` no Repository (Infrastructure)

**Objetivo:** Implementar a consulta agregada de status de cancelamento.

| Campo | Valor |
|-------|-------|
| Arquivo | `Infraestructure/Repository/EventoRepository.cs` (editar) |
| Dependências | Task 2 (interface) |

**Query SQL:** Conforme especificado em [`design.md §5.2`](./design.md#52-consulta-de-status-de-cancelamento-repository).

**Verificação:**
- Projeto `Infraestructure` compila sem erros
- Teste manual: chamar com um eventoId que tenha ingressos vendidos → retorna dados agregados corretos

---

## Task 4 — Implementar `CancelarComTransacao` no Repository (Infrastructure)

**Objetivo:** Implementar a transação atômica de cancelamento de evento.

| Campo | Valor |
|-------|-------|
| Arquivo | `Infraestructure/Repository/EventoRepository.cs` (editar) |
| Dependências | Task 2 (interface), Task 3 |

**Lógica da transação:** Conforme especificado em [`design.md §5.4`](./design.md#54-implementação-do-repositório).

**Ordem das operações SQL:**
1. `UPDATE Eventos SET Cancelado = 1 WHERE Id = @eventoId`
2. Se `!gratuito`: `UPDATE Ingressos SET Status = 3 WHERE EventoId = @eventoId AND Status = 2`
3. `UPDATE Reservas SET Reembolsada = 1 WHERE EventoId = @eventoId AND Reembolsada = 0`
4. `UPDATE ItensReserva SET Reembolsado = 1 WHERE ReservaId IN (SELECT Id FROM Reservas WHERE EventoId = @eventoId)`
5. Se `!gratuito`: `UPDATE Pagamentos SET Status = 3 WHERE ReservaId IN (SELECT Id FROM Reservas WHERE EventoId = @eventoId) AND Status = 1`

**Verificação:**
- Projeto `Infraestructure` compila sem erros
- Teste manual: cancelar evento pago → verificar todas as 5 tabelas atualizadas

---

## Task 5 — Atualizar `IEventoService` (Application)

**Objetivo:** Substituir `DeleteAsync` por `CancelarEvento` e adicionar `ObterStatusCancelamento`.

| Campo | Valor |
|-------|-------|
| Arquivo | `Application/Interfaces/IEventoService.cs` (editar) |
| Dependências | Task 1 (DTO) |

**Mudanças:**
- Adicionar: `Task<StatusCancelamentoDTO> ObterStatusCancelamento(Guid eventoId, string vendedorCpf, bool isAdmin, CancellationToken ct);`
- Adicionar: `Task CancelarEvento(Guid eventoId, string vendedorCpf, bool isAdmin, CancellationToken ct);`
- **Remover:** `Task DeleteAsync(Guid id, string vendedorCpf, bool isAdmin = false);`

**Verificação:** Projeto `Application` compila com erros esperados em `EventoService` e `EventoController` (a corrigir nas próximas tasks).

---

## Task 6 — Implementar `ObterStatusCancelamento` e `CancelarEvento` no Service (Application)

**Objetivo:** Substituir `DeleteAsync` pela nova lógica de cancelamento com reembolso.

| Campo | Valor |
|-------|-------|
| Arquivo | `Application/Service/EventoService.cs` (editar) |
| Dependências | Task 4 (repository), Task 5 (interface) |

**Método `ObterStatusCancelamento`:** Conforme especificado em [`design.md §4.2`](./design.md#42-lógica-de-obterstatuscancelamento-eventoservice).

**Método `CancelarEvento`:** Conforme especificado em [`design.md §4.3`](./design.md#43-lógica-de-cancelarevento-eventoservice).

**Validações na ordem correta (`CancelarEvento`):**
1. `evento == null` → `KeyNotFoundException`
2. `!isAdmin && evento.VendedorCpf != vendedorCpf` → `UnauthorizedAccessException`
3. `evento.Cancelado` → `DomainException("Evento já foi cancelado.")`

**Ação:** Remover método `DeleteAsync` existente.

**Verificação:**
- Projeto `Application` compila sem erros
- Serviço lança exceção correta para cada condição de erro

---

## Task 7 — Atualizar `EventoController` (API)

**Objetivo:** Adicionar endpoint de status-cancelamento e modificar DELETE para cancelamento lógico.

| Campo | Valor |
|-------|-------|
| Arquivo | `Api/Controllers/EventoController.cs` (editar) |
| Dependências | Task 6 (service) |

**Endpoint novo — `GET /api/evento/{id}/status-cancelamento`:**
```csharp
[HttpGet("{id}/status-cancelamento")]
[Authorize(Roles = "Vendedor,Admin")]
public async Task<IActionResult> StatusCancelamento(Guid id, CancellationToken ct)
{
    var cpf = User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value;
    if (string.IsNullOrEmpty(cpf)) return Unauthorized();

    try
    {
        var isAdmin = User.IsInRole("Admin");
        var status = await _eventoService.ObterStatusCancelamento(id, cpf, isAdmin, ct);
        return Ok(status);
    }
    catch (KeyNotFoundException e) { return NotFound(new { message = e.Message }); }
    catch (UnauthorizedAccessException) { return Forbid(); }
}
```

**Endpoint modificado — `DELETE /api/evento/{id}`:**
- Alterar `DeleteAsync` → `CancelarEvento`
- Manter `[Authorize(Roles = "Vendedor,Admin")]`
- Atualizar mensagens de erro para contexto de cancelamento
- Adicionar tratamento de `409 Conflict` para evento já cancelado

**Verificação:**
- Projeto `Api` compila sem erros
- Swagger exibe `GET /api/evento/{id}/status-cancelamento`
- Swagger exibe `DELETE /api/evento/{id}` com nova semântica

---

## Task 8 — Filtrar Eventos Cancelados na Listagem Pública (Infrastructure)

**Objetivo:** Impedir que eventos cancelados apareçam na listagem pública.

| Campo | Valor |
|-------|-------|
| Arquivo | `Infraestructure/Repository/EventoRepository.cs` (editar) |
| Dependências | Task 4 |

**Mudança na query `GetAllAsync`:**
```sql
-- Adicionar cláusula WHERE:
SELECT * FROM Eventos WHERE Cancelado = 0
```

**Verificação:**
- `GET /api/evento` não retorna eventos cancelados
- `GET /api/evento/meus` continua retornando todos os eventos do vendedor (incluindo cancelados)

---

## Task 9 — Testes Automatizados

**Objetivo:** Garantir cobertura de testes para o fluxo de cancelamento de evento.

| Campo | Valor |
|-------|-------|
| Arquivo | `tests/EventoTests.cs` (editar — adicionar testes) |
| Dependências | Tasks 1-8 |

**Testes a implementar:**

| # | Teste | Cenário |
|---|-------|---------|
| T1 | `CancelarEvento_Sucesso_EventoPagoComVendas` | Evento pago com ingressos vendidos → todos os status atualizados |
| T2 | `CancelarEvento_Sucesso_EventoGratuito` | Evento gratuito → `Cancelado = true`, sem alterar pagamentos |
| T3 | `CancelarEvento_Sucesso_SemVendas` | Evento sem ingressos vendidos → apenas `Cancelado = true` |
| T4 | `CancelarEvento_JaCancelado_409` | `Cancelado = true` → `DomainException` |
| T5 | `CancelarEvento_NaoDono_403` | `VendedorCpf != cpf do token` → `UnauthorizedAccessException` |
| T6 | `CancelarEvento_AdminCancelando` | Admin cancela evento de outro vendedor → sucesso |
| T7 | `CancelarEvento_NaoEncontrado_404` | `eventoId` inexistente → `KeyNotFoundException` |
| T8 | `ObterStatusCancelamento_Sucesso` | Retorna dados agregados corretos |
| T9 | `ObterStatusCancelamento_NaoDono_403` | Vendedor consulta evento de outro → `UnauthorizedAccessException` |

**Verificação:**
- `dotnet test` passa com todos os 9 testes verdes
- Cobertura do fluxo de cancelamento de evento ≥ 80%

---

## Resumo de Tarefas

| # | Camada | Arquivo(s) | Ação |
|---|--------|-----------|------|
| 1 | Application | `DTOs/StatusCancelamentoDTO.cs` | **Criar** |
| 2 | Domain | `Interface/IEventoRepository.cs` | **Editar** (+2 métodos, -DeleteAsync) |
| 3 | Infraestructure | `Repository/EventoRepository.cs` | **Editar** (+ObterStatusCancelamento) |
| 4 | Infraestructure | `Repository/EventoRepository.cs` | **Editar** (+CancelarComTransacao) |
| 5 | Application | `Interfaces/IEventoService.cs` | **Editar** (+2 métodos, -DeleteAsync) |
| 6 | Application | `Service/EventoService.cs` | **Editar** (+2 métodos, -DeleteAsync) |
| 7 | Api | `Controllers/EventoController.cs` | **Editar** (+status, mod DELETE) |
| 8 | Infraestructure | `Repository/EventoRepository.cs` | **Editar** (filtro Cancelado=0) |
| 9 | tests | `EventoTests.cs` | **Editar** (+9 testes) |

---

## Dependências entre Tasks

```
Task 1 (DTO)
  └→ Task 2 (interface repo) ──→ Task 3 (repo status) ──┐
                                └→ Task 4 (repo cancel) ─┼→ Task 5 (interface svc) ──→ Task 6 (service) ──→ Task 7 (controller) ──→ Task 9 (testes)
                                                          │
                                                          └→ Task 8 (filtro eventos) ──→ Task 9 (testes)
```

---

## Estimativa de Esforço

| Bloco | Tasks | Esforço |
|-------|-------|---------|
| Domain | 2 | 20min |
| Application | 1, 5, 6 | 1h |
| Infrastructure | 3, 4, 8 | 1h15 |
| API | 7 | 30min |
| Tests | 9 | 1h30 |
| **Total** | **9 tasks** | **~4h35** |
