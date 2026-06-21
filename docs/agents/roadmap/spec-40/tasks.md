# Spec 40 — Tasks: Comprador Cancela Reserva com Reembolso

> **Requirements:** [`requirements.md`](./requirements.md)
> **Design:** [`design.md`](./design.md)
> **Ordem:** Cada task depende das anteriores. Seguir a sequência numérica.

---

## Task 1 — Adicionar Campo `Reembolsada` em `Reserva` (Domain)

**Objetivo:** Adicionar flag de reembolso na entidade `Reserva`.

| Campo | Valor |
|-------|-------|
| Arquivo | `Domain/Entities/Reserva.cs` (editar) |
| Dependências | Nenhuma |

**Mudanças:**
- Adicionar propriedade: `public bool Reembolsada { get; private set; }`
- Inicializar `Reembolsada = false` no construtor privado
- Adicionar método: `public void MarcarReembolsada() => Reembolsada = true;`

> **Cuidado:** Não alterar lógica existente de `Criar()`, `ValidarItens()`, `ValidarUsoDoCupom()`. Apenas adicionar o novo campo.

**Verificação:**
- Projeto `Domain` compila sem erros
- `Reserva.Criar(...)` produz reserva com `Reembolsada = false`

---

## Task 2 — Migration SQL (Database)

**Objetivo:** Criar script de migração para a coluna `Reembolsada` em `Reservas`.

| Campo | Valor |
|-------|-------|
| Arquivo novo | `db/Script0012_AdicionarReembolsadaReservas.sql` |
| Dependências | Task 1 |

**Conteúdo:** Conforme especificado em [`design.md §2.1`](./design.md#21-migration-script0012_adicionarreembolsadareservassql).

**Verificação:**
- Ao rodar a API, o DbUp executa o script sem erros
- Coluna `Reembolsada` existe na tabela `Reservas` com `DEFAULT 0`

---

## Task 3 — Atualizar `IReservaRepository` (Domain)

**Objetivo:** Adicionar o método `CancelarComTransacao` na interface do repositório.

| Campo | Valor |
|-------|-------|
| Arquivo | `Domain/Interface/IReservaRepository.cs` (editar) |
| Dependências | Task 1 |

**Assinatura a adicionar:**
```csharp
Task CancelarComTransacao(Guid reservaId, Evento evento, CancellationToken ct);
```

**Verificação:** Projeto `Domain` compila sem erros.

---

## Task 4 — Implementar `CancelarComTransacao` no Repository (Infrastructure)

**Objetivo:** Implementar a transação atômica de cancelamento no `ReservaRepository`.

| Campo | Valor |
|-------|-------|
| Arquivo | `Infraestructure/Repository/ReservaRepository.cs` (editar) |
| Dependências | Task 2 (migration), Task 3 (interface) |

**Lógica da transação:** Conforme especificado em [`design.md §5.3`](./design.md#53-implementação-do-repositório).

**Ordem das operações SQL:**
1. `UPDATE Reservas SET Reembolsada = 1 WHERE Id = @reservaId`
2. `UPDATE ItensReserva SET Reembolsado = 1 WHERE ReservaId = @reservaId`
3. `UPDATE Ingressos SET Status = 0, DataBloqueio = NULL WHERE Id IN (SELECT IngressoId FROM ItensReserva WHERE ReservaId = @reservaId)`
4. `UPDATE Pagamentos SET Status = 3 WHERE ReservaId = @reservaId AND Status = 1`

**Verificação:**
- Projeto `Infraestructure` compila sem erros
- Teste manual: cancelar uma reserva paga → verificar todas as 4 tabelas atualizadas

---

## Task 5 — Atualizar `IReservaService` (Application)

**Objetivo:** Adicionar o método `CancelarReserva` na interface do serviço.

| Campo | Valor |
|-------|-------|
| Arquivo | `Application/Interfaces/IReservaService.cs` (editar) |
| Dependências | Nenhuma |

**Assinatura a adicionar:**
```csharp
Task CancelarReserva(Guid reservaId, string usuarioCpf, CancellationToken ct);
```

**Verificação:** Projeto `Application` compila sem erros.

---

## Task 6 — Implementar `CancelarReserva` no Service (Application)

**Objetivo:** Implementar a lógica de cancelamento no `ReservaService`.

| Campo | Valor |
|-------|-------|
| Arquivo | `Application/Service/ReservaService.cs` (editar) |
| Dependências | Task 4 (repository), Task 5 (interface) |

**Lógica:** Conforme especificado em [`design.md §4.2`](./design.md#42-lógica-de-cancelarreserva-reservaservice).

**Validações na ordem correta:**
1. `reserva == null` → `"Reserva não encontrada."`
2. `reserva.UsuarioCpf != usuarioCpf` → `"Esta reserva não pertence a você."`
3. `reserva.Reembolsada` → `"Reserva já foi cancelada."`
4. `evento == null` → `"Evento não encontrado."`
5. `evento.DataEvento <= DateTime.UtcNow` → `"Não é possível cancelar. O evento já começou."`

**Verificação:**
- Projeto `Application` compila sem erros
- Serviço lança exceção correta para cada condição de erro
- Serviço chama `_repositoryReserva.CancelarComTransacao()` no caminho feliz

---

## Task 7 — Adicionar `DELETE /api/reserva/{id}` no Controller (API)

**Objetivo:** Expor o endpoint de cancelamento no `ReservaController`.

| Campo | Valor |
|-------|-------|
| Arquivo | `Api/Controllers/ReservaController.cs` (editar) |
| Dependências | Task 6 (service) |

**Endpoint:**
```csharp
[HttpDelete("{id:guid}")]
[Authorize]
public async Task<IActionResult> CancelarReserva(Guid id, CancellationToken ct)
{
    var cpf = User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value;
    if (string.IsNullOrEmpty(cpf))
        return Unauthorized(new { message = "CPF não encontrado no token." });

    try
    {
        await _service.CancelarReserva(id, cpf, ct);
        return Ok(new { message = "Reserva cancelada com sucesso. Reembolso registrado." });
    }
    catch (DomainException ex)
    {
        return ex.Message switch
        {
            "Reserva não encontrada." => NotFound(new { message = ex.Message }),
            "Esta reserva não pertence a você." => StatusCode(403, new { message = ex.Message }),
            "Reserva já foi cancelada." => Conflict(new { message = ex.Message }),
            _ => BadRequest(new { message = ex.Message })
        };
    }
    catch (UnauthorizedAccessException ex)
    {
        return StatusCode(403, new { message = ex.Message });
    }
}
```

**Mapeamento de erros HTTP:** `404` (não encontrada) | `403` (não é dono) | `409` (já cancelada) | `400` (evento começou)

**Verificação:**
- Projeto `Api` compila sem erros
- Swagger exibe `DELETE /api/reserva/{id}`

---

## Task 8 — Atualizar DTOs e Queries de Minhas Reservas (Application + Infra)

**Objetivo:** Incluir os campos `Reembolsada` e `Reembolsado` nos responses de consulta de reservas.

| Campo | Valor |
|-------|-------|
| Arquivos | `Domain/DTOs/ReservaDetalhadaDTO.cs` (editar) |
|          | `Infraestructure/Repository/ReservaRepository.cs` (editar queries) |
| Dependências | Task 1, Task 2 |

**Adicionar campo no DTO:**
```csharp
// Domain/DTOs/ReservaDetalhadaDTO.cs — adicionar:
public bool Reembolsada { get; set; }
```

**Atualizar query SQL em `ListarReservasDetalhadasPorCpf`:**
```sql
-- Adicionar no SELECT:
r.Reembolsada,
```

**Atualizar query SQL em `ListarTodasDetalhadasAdmin`:**
```sql
-- Adicionar no SELECT:
r.Reembolsada,
```

**Adicionar campo `podeCancelar` no response (calculado em memória ou via SQL):**
```csharp
// No serviço, após obter as reservas, mapear:
podeCancelar = !r.Reembolsada && r.DataEvento > DateTime.UtcNow
```

> **Nota:** O campo `Reembolsado` dos itens já é retornado via query — verificar se o DTO atual o inclui.

**Verificação:**
- `GET /api/reserva/minhas` retorna `"reembolsada": true/false` e `"podeCancelar": true/false`
- Projeto compila sem erros

---

## Task 9 — Testes Automatizados

**Objetivo:** Garantir cobertura de testes para o fluxo de cancelamento.

| Campo | Valor |
|-------|-------|
| Arquivo | `tests/ReservaTests.cs` (editar — adicionar testes) |
| Dependências | Tasks 1-8 |

**Testes a implementar:**

| # | Teste | Cenário |
|---|-------|---------|
| T1 | `CancelarReserva_Sucesso_EventoPago` | Reserva paga → `Reembolsada = true`, itens reembolsados, ingressos livres, pagamento reembolsado |
| T2 | `CancelarReserva_Sucesso_EventoGratuito` | Reserva gratuita → `Reembolsada = true`, itens reembolsados, sem alterar pagamento |
| T3 | `CancelarReserva_Sucesso_NaoPaga` | Reserva não paga → `Reembolsada = true`, ingressos liberados |
| T4 | `CancelarReserva_JaReembolsada_409` | `Reembolsada = true` → `DomainException` "Reserva já foi cancelada." |
| T5 | `CancelarReserva_OutroUsuario_403` | `UsuarioCpf != cpf do token` → `UnauthorizedAccessException` |
| T6 | `CancelarReserva_EventoJaComecou_400` | `DataEvento <= DateTime.UtcNow` → `DomainException` "Não é possível cancelar..." |
| T7 | `CancelarReserva_NaoEncontrada_404` | `reservaId` inexistente → `DomainException` "Reserva não encontrada." |
| T8 | `CancelarReserva_MultiplosItens` | Reserva com 4 itens → todos marcados `Reembolsado = true`, todos ingressos liberados |

**Estrutura dos testes (xUnit + Moq):**
```csharp
[Fact]
public async Task CancelarReserva_Sucesso_EventoPago()
{
    // Arrange
    var reservaId = Guid.NewGuid();
    var cpf = "529.885.310-09";
    var reserva = /* Reserva com Pago = true, Reembolsada = false */;
    var evento = /* Evento com DataEvento futuro */;

    _reservaRepoMock.Setup(r => r.BuscarPorId(reservaId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(reserva);
    _eventoRepoMock.Setup(r => r.GetByIdAsync(reserva.EventoId))
        .ReturnsAsync(evento);

    // Act
    await _service.CancelarReserva(reservaId, cpf, CancellationToken.None);

    // Assert
    _reservaRepoMock.Verify(r => r.CancelarComTransacao(
        reservaId, evento, It.IsAny<CancellationToken>()), Times.Once);
}
```

**Verificação:**
- `dotnet test` passa com todos os 8 testes verdes
- Cobertura do fluxo de cancelamento ≥ 80%

---

## Resumo de Tarefas

| # | Camada | Arquivo(s) | Ação |
|---|--------|-----------|------|
| 1 | Domain | `Entities/Reserva.cs` | **Editar** (+campo Reembolsada) |
| 2 | db | `Script0012_AdicionarReembolsadaReservas.sql` | **Criar** |
| 3 | Domain | `Interface/IReservaRepository.cs` | **Editar** (+CancelarComTransacao) |
| 4 | Infraestructure | `Repository/ReservaRepository.cs` | **Editar** (+método) |
| 5 | Application | `Interfaces/IReservaService.cs` | **Editar** (+CancelarReserva) |
| 6 | Application | `Service/ReservaService.cs` | **Editar** (+método) |
| 7 | Api | `Controllers/ReservaController.cs` | **Editar** (+DELETE endpoint) |
| 8 | Domain/Infra | DTOs + Repository queries | **Editar** (campos reembolso) |
| 9 | tests | `ReservaTests.cs` | **Editar** (+8 testes) |

---

## Dependências entre Tasks

```
Task 1 (Reserva.Reembolsada)
  ├→ Task 2 (migration)
  │    └→ Task 4 (repository) ──→ Task 6 (service) ──→ Task 7 (controller) ──→ Task 9 (testes)
  └→ Task 3 (interface repo) ──┘
       
Task 5 (interface service) ──→ Task 6 (service)

Task 8 (DTOs + queries) ──→ Task 9 (testes)
```

---

## Estimativa de Esforço

| Bloco | Tasks | Esforço |
|-------|-------|---------|
| Domain | 1, 3 | 30min |
| Database | 2 | 15min |
| Application | 5, 6 | 45min |
| Infrastructure | 4, 8 | 1h |
| API | 7 | 30min |
| Tests | 9 | 1h30 |
| **Total** | **9 tasks** | **~4h30** |
