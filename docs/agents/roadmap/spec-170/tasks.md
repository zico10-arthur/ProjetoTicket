# Spec 170 — Tasks: Pagamento Simulado (Checkout Interno)

> **Requirements:** [`requirements.md`](./requirements.md)
> **Design:** [`design.md`](./design.md)
> **Ordem:** Cada task depende das anteriores. Seguir a sequência numérica.

---

## Task 1 — Enum `StatusPagamento` (Domain)

**Objetivo:** Criar o enum que define os estados possíveis de um pagamento.

| Campo | Valor |
|-------|-------|
| Arquivo novo | `Domain/Enums/StatusPagamento.cs` |
| Dependências | Nenhuma |

**Conteúdo:**
```csharp
namespace Domain.Enums;

public enum StatusPagamento
{
    Pendente   = 0,
    Confirmado = 1,
    Reembolsado = 2,
    Falhou     = 3
}
```

**Verificação:** Projeto `Domain` compila sem erros.

---

## Task 2 — Atualizar Entidade `Pagamento` (Domain)

**Objetivo:** Substituir a entidade `Pagamento` básica existente pela versão completa com status, método e data de criação.

| Campo | Valor |
|-------|-------|
| Arquivo | `Domain/Entities/Pagamento.cs` (sobrescrever) |
| Dependências | Task 1 (`StatusPagamento`) |

**O que muda em relação à entidade atual:**
- Adicionar `using Domain.Enums;`
- Adicionar propriedades: `Status`, `Metodo`, `DataCriacao`
- Modificar construtor para receber `metodo` (string)
- Definir `Status = StatusPagamento.Confirmado` no construtor
- Adicionar `DataCriacao = DateTime.UtcNow`
- Adicionar método `MarcarReembolsado()`

**Verificação:**
- Projeto `Domain` compila sem erros
- `new Pagamento(guid, 100m, "Simulado")` cria objeto com `Status = Confirmado` e `Metodo = "Simulado"`

---

## Task 3 — Adicionar Campo `Pago` em `Reserva` (Domain)

**Objetivo:** Adicionar flag de pagamento na entidade `Reserva` para consulta rápida sem JOIN.

| Campo | Valor |
|-------|-------|
| Arquivo | `Domain/Entities/Reserva.cs` (editar) |
| Dependências | Nenhuma |

**Mudanças:**
- Adicionar propriedade: `public bool Pago { get; private set; }`
- Inicializar `Pago = false` no construtor privado
- Adicionar método: `public void MarcarPago() => Pago = true;`

> **Cuidado:** Não alterar lógica existente de `Criar()`, `ValidarItens()`, `ValidarUsoDoCupom()`. Apenas adicionar o novo campo.

**Verificação:**
- Projeto `Domain` compila sem erros
- `Reserva.Criar(...)` produz reserva com `Pago = false`

---

## Task 4 — Interface `IPagamentoRepository` (Domain)

**Objetivo:** Definir o contrato do repositório de pagamento.

| Campo | Valor |
|-------|-------|
| Arquivo novo | `Domain/Interface/IPagamentoRepository.cs` |
| Dependências | Task 2, Task 3 |

**Assinaturas:**
```csharp
namespace Domain.Interface;

public interface IPagamentoRepository
{
    Task CriarComTransacao(Pagamento pagamento, Reserva reserva, Evento evento, CancellationToken ct);
    Task<List<Pagamento>> ListarTodosAdmin(CancellationToken ct);
    Task<Pagamento?> BuscarPorReservaId(Guid reservaId, CancellationToken ct);
}
```

**Verificação:** Projeto `Domain` compila sem erros.

---

## Task 5 — Migration SQL (Database)

**Objetivo:** Criar script de migração para a tabela `Pagamentos` e campo `Pago` em `Reservas`.

| Campo | Valor |
|-------|-------|
| Arquivo novo | `db/Script0011_CriarTabelaPagamentos.sql` |
| Dependências | Task 3 (schema de Reserva) |

**Conteúdo:** Conforme especificado em [`design.md §2.1`](./design.md#21-migration-script0011_criartabelapagamentossql).

**Verificação:**
- Ao rodar a API, o DbUp executa o script sem erros
- Tabela `Pagamentos` existe no banco com as colunas corretas
- Coluna `Pago` existe na tabela `Reservas` com `DEFAULT 0`

---

## Task 6 — DTOs (Application)

**Objetivo:** Criar os objetos de transferência de dados para o fluxo de pagamento.

| Campo | Valor |
|-------|-------|
| Arquivos novos | `Application/DTOs/CheckoutRequestDTO.cs` |
|                | `Application/DTOs/CheckoutResponseDTO.cs` |
|                | `Application/DTOs/PagamentoAdminDTO.cs` |
| Dependências | Nenhuma |

**Estruturas:** Conforme especificado em [`design.md §3.3`](./design.md#33-dtos).

**Verificação:** Projeto `Application` compila sem erros.

---

## Task 7 — Interface `IPagamentoService` (Application)

**Objetivo:** Definir o contrato do serviço de pagamento.

| Campo | Valor |
|-------|-------|
| Arquivo novo | `Application/Interfaces/IPagamentoService.cs` |
| Dependências | Task 6 (DTOs) |

**Assinaturas:**
```csharp
namespace Application.Interfaces;

public interface IPagamentoService
{
    Task<CheckoutResponseDTO> ConfirmarCheckout(Guid reservaId, string usuarioCpf, string metodo, CancellationToken ct);
    Task<IEnumerable<PagamentoAdminDTO>> ListarTodosAdmin(CancellationToken ct);
}
```

**Verificação:** Projeto `Application` compila sem erros.

---

## Task 8 — `PagamentoService` (Application)

**Objetivo:** Implementar a lógica de checkout de pagamento.

| Campo | Valor |
|-------|-------|
| Arquivo novo | `Application/Service/PagamentoService.cs` |
| Dependências | Task 4 (IPagamentoRepository), Task 7 (IPagamentoService), Task 6 (DTOs) |

**Dependências injetadas:**
- `IPagamentoRepository`
- `IReservaRepository`
- `IEventoRepository`

**Lógica de `ConfirmarCheckout`:** Conforme especificado em [`design.md §4.2`](./design.md#42-lógica-de-checkout-pagamentoservice).

**Validações na ordem correta:**
1. `reserva == null` → `"Reserva não encontrada."`
2. `reserva.UsuarioCpf != usuarioCpf` → `"Esta reserva não pertence ao usuário logado."`
3. `reserva.Pago` → `"Reserva já foi paga."`
4. `reserva.Reembolsada` → `"Reserva foi cancelada."`
5. `evento.DataEvento <= DateTime.UtcNow` → `"Não é possível pagar. O evento já começou."`

**Verificação:**
- Projeto `Application` compila sem erros
- Serviço lança exceção para cada condição de erro
- Serviço cria `Pagamento` e chama `_pagamentoRepo.CriarComTransacao()` no caminho feliz

---

## Task 9 — `PagamentoRepository` (Infrastructure)

**Objetivo:** Implementar o repositório de pagamento com Dapper e transação atômica.

| Campo | Valor |
|-------|-------|
| Arquivo novo | `Infraestructure/Repository/PagamentoRepository.cs` |
| Dependências | Task 4 (interface), Task 5 (migration) |

**Método `CriarComTransacao`:**
```csharp
public async Task CriarComTransacao(Pagamento pagamento, Reserva reserva, Evento evento, CancellationToken ct)
{
    using var connection = _connectionFactory.CreateConnection();
    connection.Open();
    using var transaction = connection.BeginTransaction();

    try
    {
        // 1. INSERT Pagamento
        await connection.ExecuteAsync(@"
            INSERT INTO Pagamentos (Id, ReservaId, ValorPago, Status, Metodo, DataPagamento, DataCriacao)
            VALUES (@Id, @ReservaId, @ValorPago, @Status, @Metodo, @DataPagamento, @DataCriacao)",
            new {
                pagamento.Id, pagamento.ReservaId, pagamento.ValorPago,
                Status = (int)pagamento.Status, pagamento.Metodo,
                pagamento.DataPagamento, pagamento.DataCriacao
            }, transaction);

        // 2. UPDATE Reservas SET Pago = 1
        await connection.ExecuteAsync(
            "UPDATE Reservas SET Pago = 1 WHERE Id = @ReservaId",
            new { ReservaId = reserva.Id }, transaction);

        // 3. UPDATE Ingressos SET Status = 2
        await connection.ExecuteAsync(@"
            UPDATE Ingressos SET Status = 2
            WHERE Id IN (SELECT IngressoId FROM ItensReserva WHERE ReservaId = @ReservaId)",
            new { ReservaId = reserva.Id }, transaction);

        transaction.Commit();
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}
```

> **Nota:** Para eventos tipo `Palestra`, `ItensReserva.IngressoId` pode ser `NULL`. Nesse caso, o sub-SELECT não retorna linhas e o UPDATE em `Ingressos` afeta 0 registros — comportamento seguro.

**Verificação:**
- Projeto `Infraestructure` compila sem erros
- Teste manual: criar reserva → chamar repositório → verificar que Pagamento foi inserido, Reserva.Pago = 1, Ingressos.Status = 2

---

## Task 10 — `PagamentoController` (API)

**Objetivo:** Expor os endpoints de checkout e listagem admin.

| Campo | Valor |
|-------|-------|
| Arquivo novo | `Api/Controllers/PagamentoController.cs` |
| Dependências | Task 8 (service), Task 9 (repository) |

**Endpoints:**
```
POST   /api/pagamento/checkout/{reservaId}   → ConfirmarCheckout
GET    /api/pagamento/admin/todos             → ListarTodosAdmin  [Authorize(Roles = "Admin")]
```

**Estrutura do Controller:**
```csharp
[ApiController]
[Route("api/[controller]")]
public class PagamentoController : ControllerBase
{
    private readonly IPagamentoService _service;

    public PagamentoController(IPagamentoService service)
    {
        _service = service;
    }

    [HttpPost("checkout/{reservaId:guid}")]
    [Authorize]
    public async Task<IActionResult> Checkout(Guid reservaId, [FromBody] CheckoutRequestDTO dto, CancellationToken ct)
    {
        var cpf = User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value;
        if (string.IsNullOrEmpty(cpf))
            return Unauthorized(new { message = "CPF não encontrado no token." });

        try
        {
            var result = await _service.ConfirmarCheckout(reservaId, cpf, dto.Metodo, ct);
            return Ok(result);
        }
        catch (DomainException ex)
        {
            return ex.Message switch
            {
                "Reserva não encontrada." => NotFound(new { message = ex.Message }),
                "Esta reserva não pertence ao usuário logado." => Forbid(),
                _ => BadRequest(new { message = ex.Message })
            };
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("admin/todos")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ListarTodosAdmin(CancellationToken ct)
    {
        var pagamentos = await _service.ListarTodosAdmin(ct);
        return Ok(pagamentos);
    }
}
```

> **Respostas HTTP mapeadas:** `400` (evento começou, já paga, cancelada) | `403` (não é dono) | `404` (reserva não encontrada) | `200` (sucesso)

**Verificação:**
- Projeto `Api` compila sem erros
- Swagger exibe os 2 novos endpoints

---

## Task 11 — Registro de DI (API)

**Objetivo:** Registrar as novas dependências no contêiner de injeção.

| Campo | Valor |
|-------|-------|
| Arquivo | `Api/Program.cs` (editar) |
| Dependências | Task 8, Task 9 |

**Linhas a adicionar:**
```csharp
// Services
builder.Services.AddScoped<IPagamentoService, PagamentoService>();

// Repositories
builder.Services.AddScoped<IPagamentoRepository, PagamentoRepository>();
```

**Verificação:** API sobe sem erros de resolução de dependência.

---

## Task 12 — Remover Endpoint Antigo (API)

**Objetivo:** Remover o endpoint `ConfirmarPagamento` obsoleto do `ReservaController`.

| Campo | Valor |
|-------|-------|
| Arquivo | `Api/Controllers/ReservaController.cs` (editar) |
| Dependências | Task 10 |

**Remover:**
- Método `ConfirmarPagamento(Guid ingressoId, CancellationToken ct)` (linhas 70-76)
- Atributo `[HttpPost("ConfirmarPagamento/{ingressoId}")]`

> **Não remover** a injeção `IIngressoRepository` se ela for usada por outros métodos do controller.

**Verificação:**
- Projeto `Api` compila sem erros
- Swagger não exibe mais `POST /api/reserva/ConfirmarPagamento/{ingressoId}`

---

## Task 13 — Atualizar DTOs de Reserva (Application)

**Objetivo:** Incluir o campo `Pago` nos responses de consulta de reserva para que o frontend saiba o status de pagamento.

| Campo | Valor |
|-------|-------|
| Arquivos | `Application/DTOs/ItemReservaResponseDTO.cs` (editar) |
|          | `Domain/DTOs/ReservaDetalhadaDTO.cs` (editar) |
|          | `Domain/DTOs/ReservaAdminDTO.cs` (editar) |
| Dependências | Task 3 |

**Adicionar campo em cada DTO:**
```csharp
public bool Pago { get; set; }
```

**Atualizar queries SQL nos repositórios:**
- `Infraestructure/Repository/ReservaRepository.cs` — incluir `r.Pago` nas queries de SELECT

**Verificação:**
- `GET /api/reserva/minhas` retorna `"pago": true` ou `"pago": false` para cada reserva
- Projeto compila sem erros

---

## Task 14 — Testes Automatizados

**Objetivo:** Garantir cobertura de testes para o fluxo de pagamento.

| Campo | Valor |
|-------|-------|
| Arquivo novo | `tests/PagamentoTests.cs` |
| Dependências | Tasks 1-13 |

**Testes a implementar:**

| # | Teste | Cenário |
|---|-------|---------|
| T1 | `ConfirmarCheckout_Sucesso` | Reserva válida → Pagamento criado, `Pago = true`, `Ingressos.Status = 2` |
| T2 | `ConfirmarCheckout_JaPago_409` | Reserva com `Pago = true` → `DomainException` "Reserva já foi paga." |
| T3 | `ConfirmarCheckout_OutroUsuario_403` | `UsuarioCpf != cpf do token` → `UnauthorizedAccessException` |
| T4 | `ConfirmarCheckout_EventoJaComecou_400` | `DataEvento <= DateTime.UtcNow` → `DomainException` "Não é possível pagar. O evento já começou." |
| T5 | `ConfirmarCheckout_ReservaNaoEncontrada_404` | `reservaId` inexistente → `DomainException` "Reserva não encontrada." |
| T6 | `ConfirmarCheckout_EventoGratuito_Sucesso` | `ValorFinalPago = 0` → pagamento criado com `ValorPago = 0` |
| T7 | `ConfirmarCheckout_ReservaCancelada_400` | `Reembolsada = true` → `DomainException` "Reserva foi cancelada." |

**Estrutura dos testes (xUnit + Moq):**
```csharp
[Fact]
public async Task ConfirmarCheckout_Sucesso()
{
    // Arrange
    var reservaId = Guid.NewGuid();
    var cpf = "529.885.310-09";
    var reserva = new Reserva(...); // Pago = false, Reembolsada = false
    var evento = new Evento(...);   // DataEvento futuro

    _reservaRepoMock.Setup(r => r.BuscarPorId(reservaId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(reserva);
    _eventoRepoMock.Setup(r => r.BuscarPorId(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(evento);

    // Act
    var result = await _service.ConfirmarCheckout(reservaId, cpf, "Simulado", CancellationToken.None);

    // Assert
    Assert.Equal("Confirmado", result.Status);
    _pagamentoRepoMock.Verify(r => r.CriarComTransacao(
        It.IsAny<Pagamento>(), reserva, evento, It.IsAny<CancellationToken>()), Times.Once);
}
```

**Verificação:**
- `dotnet test` passa com todos os 7 testes verdes
- Cobertura do fluxo de pagamento ≥ 80%

---

## Resumo de Tarefas

| # | Camada | Arquivo(s) | Ação |
|---|--------|-----------|------|
| 1 | Domain | `Enums/StatusPagamento.cs` | **Criar** |
| 2 | Domain | `Entities/Pagamento.cs` | **Sobrescrever** |
| 3 | Domain | `Entities/Reserva.cs` | **Editar** (+campo Pago) |
| 4 | Domain | `Interface/IPagamentoRepository.cs` | **Criar** |
| 5 | db | `Script0011_CriarTabelaPagamentos.sql` | **Criar** |
| 6 | Application | `DTOs/CheckoutRequestDTO.cs`, `CheckoutResponseDTO.cs`, `PagamentoAdminDTO.cs` | **Criar** |
| 7 | Application | `Interfaces/IPagamentoService.cs` | **Criar** |
| 8 | Application | `Service/PagamentoService.cs` | **Criar** |
| 9 | Infraestructure | `Repository/PagamentoRepository.cs` | **Criar** |
| 10 | Api | `Controllers/PagamentoController.cs` | **Criar** |
| 11 | Api | `Program.cs` | **Editar** (DI) |
| 12 | Api | `Controllers/ReservaController.cs` | **Editar** (remover endpoint antigo) |
| 13 | Application/Infra | DTOs + Repository queries | **Editar** (campo Pago) |
| 14 | tests | `PagamentoTests.cs` | **Criar** |

---

## Dependências entre Tasks

```
Task 1 (enum)
  └→ Task 2 (entidade Pagamento)
       └→ Task 4 (interface repo)
            └→ Task 8 (service) ──┐
                 └→ Task 10 (controller) ──→ Task 12 (remover antigo)
                      └→ Task 11 (DI)
            └→ Task 9 (repo)
                 └→ Task 14 (testes)

Task 3 (Reserva.Pago) ──→ Task 5 (migration) ──→ Task 9 (repo)
                                              ──→ Task 13 (DTOs + queries)

Task 6 (DTOs) ──→ Task 7 (interface service) ──→ Task 8 (service)
```

---

## Estimativa de Esforço

| Bloco | Tasks | Esforço |
|-------|-------|---------|
| Domain | 1, 2, 3, 4 | 1h |
| Database | 5 | 30min |
| Application | 6, 7, 8 | 1h30 |
| Infrastructure | 9 | 45min |
| API | 10, 11, 12 | 45min |
| Integration | 13 | 30min |
| Tests | 14 | 1h30 |
| **Total** | **14 tasks** | **~6h** |
