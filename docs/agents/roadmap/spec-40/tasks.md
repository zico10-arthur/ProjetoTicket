# Spec 40: Comprador Cancela Reserva com Reembolso — Implementation Tasks

> **Requirements:** [`requirements.md`](./requirements.md)
> **Design:** [`design.md`](./design.md)
> **Ordem:** Tasks MUST be executed sequentially. Each task depends on all preceding tasks.

---

## Task Order

```
Task 1 (Reserva.Reembolsada)
  ├→ Task 2 (migration Script0013)
  │    └→ Task 3 (IReservaRepository interface)
  │         └→ Task 4 (CancelarComTransacao repository)
  │              └→ Task 5 (IReservaService interface)
  │                   └→ Task 6 (CancelarReserva service)
  │                        └→ Task 7 (DELETE endpoint controller)
  └→ Task 8 (DTOs + queries)
       └→ Task 9 (tests)
```

---

## Tasks

### Task 1: Adicionar Campo `Reembolsada` em `Reserva` (Domain)

**Objective:** Adicionar flag de reembolso na entidade `Reserva`.

**Requirements Covered:** FR-010

**Design References:** Component 1

**File:** `Domain/Entities/Reserva.cs` (EDITAR)

**Actions:**
1. Ler o arquivo `Domain/Entities/Reserva.cs` para obter o estado atual.
2. Adicionar propriedade após `public bool Pago { get; private set; }` (linha 16 do arquivo atual):
   ```csharp
   public bool Reembolsada { get; private set; }
   ```
3. No construtor privado (linha 23-30), adicionar após `Pago = false;` (linha 29):
   ```csharp
   Reembolsada = false;
   ```
4. Adicionar método após `MarcarPago()` (linha 35):
   ```csharp
   public void MarcarReembolsada()
   {
       Reembolsada = true;
   }
   ```
5. NÃO alterar `Criar()`, `ValidarItens()`, `ValidarUsoDoCupom()`, `MarcarPago()`, propriedades existentes.

**Validation:**
- [ ] `dotnet build Domain/Domain.csproj` compila sem erros.
- [ ] Propriedade `Reembolsada` acessível com `{ get; private set; }`.
- [ ] `Reserva.Criar(...)` produz reserva com `Reembolsada == false`.

**Status:** [x] done

---

### Task 2: Criar Migration `Script0013` (Database)

**Objective:** Adicionar coluna `Reembolsada` à tabela `Reservas` via DbUp.

**Requirements Covered:** FR-011

**Design References:** Component 2

**File:** `db/Script0013_AdicionarReembolsadaReservas.sql` (CRIAR)

**Actions:**
1. Criar arquivo `db/Script0013_AdicionarReembolsadaReservas.sql`.
2. Conteúdo completo:
   ```sql
   -- Script0013: Adicionar coluna Reembolsada na tabela Reservas
   -- Spec 40: Comprador Cancela Reserva com Reembolso

   IF NOT EXISTS (
       SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
       WHERE TABLE_NAME = 'Reservas' AND COLUMN_NAME = 'Reembolsada'
   )
   BEGIN
       ALTER TABLE dbo.Reservas ADD Reembolsada BIT NOT NULL DEFAULT 0;
   END
   ```

**Validation:**
- [ ] Arquivo existe em `db/Script0013_AdicionarReembolsadaReservas.sql`.
- [ ] Script usa `IF NOT EXISTS` (idempotente).
- [ ] `DEFAULT 0` está presente.
- [ ] Ao rodar a API, DbUp executa o script sem erros (verificar logs).

**Status:** [x] done

---

### Task 3: Atualizar `IReservaRepository` (Domain Interface)

**Objective:** Adicionar método `CancelarComTransacao` na interface do repositório.

**Requirements Covered:** FR-015

**Design References:** Component 3

**File:** `Domain/Interface/IReservaRepository.cs` (EDITAR)

**Actions:**
1. Ler `Domain/Interface/IReservaRepository.cs`.
2. Adicionar declaração de método antes do `}` final da interface (após linha 15):
   ```csharp
   /// <summary>
   /// Spec 40: Executa cancelamento em transação atômica.
   /// UPDATE Reservas.Reembolsada = 1,
   /// UPDATE ItensReserva.Reembolsado = 1,
   /// UPDATE Ingressos.Status = 0, DataBloqueio = NULL,
   /// UPDATE Pagamentos.Status = 2 (Reembolsado) se Status = 1 (Confirmado).
   /// </summary>
   Task CancelarComTransacao(Guid reservaId, CancellationToken ct);
   ```

**Validation:**
- [ ] `dotnet build Domain/Domain.csproj` compila sem erros.

**Status:** [x] done

---

### Task 4: Implementar `CancelarComTransacao` no `ReservaRepository` (Infrastructure)

**Objective:** Implementar a transação atômica de cancelamento.

**Requirements Covered:** FR-005, FR-006, FR-007, FR-008, FR-009, FR-019, NFR-001

**Design References:** Component 4

**File:** `Infraestructure/Repository/ReservaRepository.cs` (EDITAR)

**Actions:**
1. Ler `Infraestructure/Repository/ReservaRepository.cs` para obter o estado atual.
2. Adicionar método antes do `}` final da classe (após o método `ListarReservasDetalhadasPorVendedor`, linha 214):
   ```csharp
   public async Task CancelarComTransacao(Guid reservaId, CancellationToken ct)
   {
       using var connection = _factory.CreateConnection();
       await connection.OpenAsync(ct);
       using var transaction = connection.BeginTransaction();

       try
       {
           // 1. Marcar reserva como reembolsada
           await connection.ExecuteAsync(new CommandDefinition(
               "UPDATE Reservas SET Reembolsada = 1 WHERE Id = @reservaId",
               new { reservaId }, transaction, cancellationToken: ct));

           // 2. Marcar todos os itens da reserva como reembolsados
           await connection.ExecuteAsync(new CommandDefinition(
               "UPDATE ItensReserva SET Reembolsado = 1 WHERE ReservaId = @reservaId",
               new { reservaId }, transaction, cancellationToken: ct));

           // 3. Liberar ingressos (Status = 0) e limpar DataBloqueio
           await connection.ExecuteAsync(new CommandDefinition(@"
               UPDATE Ingressos
               SET Status = 0, DataBloqueio = NULL
               WHERE Id IN (
                   SELECT IngressoId FROM ItensReserva WHERE ReservaId = @reservaId
               )",
               new { reservaId }, transaction, cancellationToken: ct));

           // 4. Marcar pagamento como reembolsado (StatusPagamento.Reembolsado = 2)
           //    Apenas se Status = 1 (Confirmado)
           await connection.ExecuteAsync(new CommandDefinition(
               "UPDATE Pagamentos SET Status = 2 WHERE ReservaId = @reservaId AND Status = 1",
               new { reservaId }, transaction, cancellationToken: ct));

           await transaction.CommitAsync(ct);
       }
       catch
       {
           await transaction.RollbackAsync(ct);
           throw;
       }
   }
   ```

**IMPORTANTE:** O valor `Status = 2` no UPDATE de Pagamentos corresponde a `(int)StatusPagamento.Reembolsado`. Verificar `Domain/Enums/StatusPagamento.cs`: `Pendente=0, Confirmado=1, Reembolsado=2, Falhou=3`.

**Validation:**
- [ ] `dotnet build Infraestructure/Infraestructure.csproj` compila sem erros.
- [ ] Método usa `BeginTransaction()` / `CommitAsync()` / `RollbackAsync()`.
- [ ] Todos os 4 UPDATEs usam `new CommandDefinition(..., transaction, ...)`.
- [ ] Valor de Status no 4º UPDATE é `2`, não `3`.

**Status:** [x] done

---

### Task 5: Atualizar `IReservaService` (Application Interface)

**Objective:** Adicionar método `CancelarReserva` na interface do serviço.

**Requirements Covered:** FR-016

**Design References:** Component 5

**File:** `Application/Interfaces/IReservaService.cs` (EDITAR)

**Actions:**
1. Ler `Application/Interfaces/IReservaService.cs`.
2. Adicionar declaração de método antes do `}` final da interface (após linha 12):
   ```csharp
   /// <summary>
   /// Spec 40: Cancela uma reserva própria. Valida propriedade, data do evento,
   /// e reembolso prévio. Executa transação atômica e enfileira e-mail de reembolso.
   /// </summary>
   Task CancelarReserva(Guid reservaId, string usuarioCpf, CancellationToken ct);
   ```

**Validation:**
- [ ] `dotnet build Application/Application.csproj` compila sem erros.

**Status:** [x] done

---

### Task 6: Implementar `CancelarReserva` no `ReservaService` (Application)

**Objective:** Implementar a lógica de cancelamento com validações e e-mail.

**Requirements Covered:** FR-002, FR-003, FR-004, FR-017, FR-018

**Design References:** Component 6

**File:** `Application/Service/ReservaService.cs` (EDITAR)

**Actions:**
1. Ler `Application/Service/ReservaService.cs` para obter o estado atual completo.
2. Verificar que o construtor já possui `_emailSender`, `_repositoryUsuario`, `_repositoryEvento`, `_repositoryReserva` (injetados pela spec 180).
3. Verificar que `using Infraestructure.Email;` já existe (linha 8).
4. Adicionar método `CancelarReserva` antes do `}` final da classe (após `ListarVendasDoVendedor`, linha 119 do arquivo atual):
   ```csharp
   public async Task CancelarReserva(Guid reservaId, string usuarioCpf, CancellationToken ct)
   {
       // 1. Buscar reserva
       var reserva = await _repositoryReserva.BuscarPorId(reservaId, ct);
       if (reserva == null)
           throw new Domain.Exceptions.DomainException("Reserva não encontrada.");

       // 2. Verificar propriedade (apenas o dono cancela)
       if (reserva.UsuarioCpf != usuarioCpf)
           throw new UnauthorizedAccessException("Esta reserva não pertence a você.");

       // 3. Verificar se já foi reembolsada
       if (reserva.Reembolsada)
           throw new Domain.Exceptions.DomainException("Reserva já foi cancelada.");

       // 4. Buscar evento para validação de data e para o e-mail
       var evento = await _repositoryEvento.GetByIdAsync(reserva.EventoId);
       if (evento == null)
           throw new Domain.Exceptions.DomainException("Evento não encontrado.");

       // 5. Verificar se o evento já começou
       if (evento.DataEvento <= DateTime.UtcNow)
           throw new Domain.Exceptions.DomainException("Não é possível cancelar. O evento já começou.");

       // 6. Executar transação atômica (4 UPDATEs)
       await _repositoryReserva.CancelarComTransacao(reservaId, ct);

       // 7. Enfileirar e-mail de reembolso (fire-and-forget)
       var usuario = await _repositoryUsuario.BuscarCpf(usuarioCpf, ct);
       var destinatario = usuario?.Email ?? usuarioCpf;
       var email = EmailTemplates.ReembolsoConfirmado(
           destinatario,
           evento.Nome,
           reserva.ValorFinalPago);
       await _emailSender.EnfileirarAsync(email, ct);
   }
   ```
5. **NÃO alterar o construtor.** Todas as dependências já estão injetadas.
6. Adicionar `using System;` no topo se `UnauthorizedAccessException` não resolver (pode estar em `System` ou já implícito).

**Validation:**
- [ ] `dotnet build Application/Application.csproj` compila sem erros.
- [ ] Construtor NÃO foi alterado (mesmo número de parâmetros).
- [ ] Ordem das validações: null → propriedade → reembolsada → evento null → data evento.
- [ ] `EnfileirarAsync` é chamado após `CancelarComTransacao`.
- [ ] Se `CancelarComTransacao` lançar exceção, `EnfileirarAsync` NÃO é chamado (a exceção propaga antes).
- [ ] `UnauthorizedAccessException` é lançada para erro de propriedade (não `DomainException`).

**Status:** [x] done

---

### Task 7: Adicionar `DELETE /api/reserva/{id}` no `ReservaController` (API)

**Objective:** Expor o endpoint de cancelamento.

**Requirements Covered:** FR-001, FR-002 (mapeamento HTTP)

**Design References:** Component 7

**File:** `Api/Controllers/ReservaController.cs` (EDITAR)

**Actions:**
1. Ler `Api/Controllers/ReservaController.cs` para obter o estado atual.
2. Adicionar método antes do `}` final da classe (após `ListarMinhasVendas`, linha 78):
   ```csharp
   /// <summary>
   /// Spec 40: Cancelar reserva própria. Qualquer perfil autenticado pode cancelar
   /// suas próprias reservas antes do início do evento.
   /// </summary>
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
       catch (Domain.Exceptions.DomainException ex)
       {
           return ex.Message switch
           {
               "Reserva não encontrada." => NotFound(new { message = ex.Message }),
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
3. `_service` já está disponível (tipo `IReservaService`, injetado no construtor existente).

**Validation:**
- [ ] `dotnet build Api/Api.csproj` compila sem erros.
- [ ] Swagger exibe `DELETE /api/reserva/{id}`.
- [ ] Endpoint tem `[Authorize]` sem restrição de role.
- [ ] CPF extraído de `User.Claims`, não de parâmetro do método.
- [ ] Mapeamento de exceções: `DomainException` → `404`/`409`/`400`, `UnauthorizedAccessException` → `403`, sucesso → `200`.

**Status:** [x] done

---

### Task 8: Atualizar DTOs e Queries SQL com Campo `Reembolsada` (Domain + Infrastructure)

**Objective:** Incluir `Reembolsada` nos DTOs e nas queries de listagem.

**Requirements Covered:** FR-012, FR-013, FR-014

**Design References:** Components 8, 9, 10

**Actions:**

**Arquivo 1: `Domain/DTOs/ReservaDetalhadaDTO.cs` (EDITAR)**
1. Ler o arquivo.
2. Adicionar após `public bool Pago { get; set; }` (linha 12):
   ```csharp
   public bool Reembolsada { get; set; }
   ```

**Arquivo 2: `Domain/DTOs/ReservaVendedorDTO.cs` (EDITAR)**
1. Ler o arquivo.
2. Adicionar após `public bool Pago { get; set; }` (linha 9):
   ```csharp
   public bool Reembolsada { get; set; }
   ```

**Arquivo 3: `Infraestructure/Repository/ReservaRepository.cs` (EDITAR — 3 queries)**

**Query A — `ListarReservasDetalhadasPorCpf` (linhas 143-158 do arquivo atual):**
- Adicionar `r.Reembolsada` no SELECT (após `r.Pago`).
- Adicionar `r.Reembolsada` no GROUP BY (após `r.Pago`).

Mudança exata no SELECT (entre `r.Pago` e `FROM`):
```sql
r.Pago,
r.Reembolsada
FROM Reservas r
```

Mudança exata no GROUP BY (após `r.Pago`):
```sql
r.Pago,
r.Reembolsada
```

**Query B — `ListarTodasDetalhadasAdmin` (linhas 169-187 do arquivo atual):**
- Mesmo padrão: adicionar `r.Reembolsada` no SELECT (após `r.Pago`) e no GROUP BY (após `r.Pago`).

**Query C — `ListarReservasDetalhadasPorVendedor` (linhas 197-210 do arquivo atual):**
- Adicionar `r.Reembolsada` no SELECT (após `r.Pago`). Esta query NÃO usa GROUP BY — apenas adicionar a coluna.

**Validation:**
- [ ] `dotnet build Domain/Domain.csproj` compila sem erros.
- [ ] `dotnet build Infraestructure/Infraestructure.csproj` compila sem erros.
- [ ] Query A: `r.Reembolsada` está no SELECT e no GROUP BY.
- [ ] Query B: `r.Reembolsada` está no SELECT e no GROUP BY.
- [ ] Query C: `r.Reembolsada` está no SELECT.
- [ ] `GET /api/reserva/minhas` retorna `"reembolsada": true` para reservas canceladas.
- [ ] `GET /api/reserva/minhas` retorna `"reembolsada": false` para reservas ativas.

**Status:** [x] done

---

### Task 9: Testes Automatizados

**Objective:** Garantir cobertura de testes para o fluxo de cancelamento.

**Requirements Covered:** FR-002, FR-003, FR-004, FR-005, FR-006, FR-007, FR-008, FR-009, FR-010, FR-017, FR-018, NFR-001

**Design References:** Testing Strategy section

**File:** `tests/ReservaTests.cs` (EDITAR — adicionar novos testes)

**Actions:**
1. Ler `tests/ReservaTests.cs` para obter o estado atual.
2. Adicionar os seguintes testes na classe `ReservaTests`:

**Testes de Domínio (Reserva entity):**

```csharp
[Fact]
public void Reserva_Reembolsada_InicializaFalse()
{
    var itens = new List<ItemReserva>
    {
        CriarItem(CpfValido, Guid.NewGuid(), 100m)
    };
    var reserva = Reserva.Criar(CpfValido, EventoId, itens);

    Assert.False(reserva.Reembolsada);
}

[Fact]
public void Reserva_MarcarReembolsada_DeveAlterarFlag()
{
    var itens = new List<ItemReserva>
    {
        CriarItem(CpfValido, Guid.NewGuid(), 100m)
    };
    var reserva = Reserva.Criar(CpfValido, EventoId, itens);
    Assert.False(reserva.Reembolsada);

    reserva.MarcarReembolsada();

    Assert.True(reserva.Reembolsada);
}
```

**Testes de Service (ReservaService.CancelarReserva):**

> **Nota:** Os testes de service exigem mocks de `IReservaRepository`, `IUsuarioRepository`, `IEventoRepository`, `IEmailSender`, `ICupomRepository`, `IIngressoRepository`. O `ReservaService` já recebe essas 6 dependências no construtor (spec 180). Usar `Mock.Of<T>()` para dependências não usadas no teste.

**Setup do mock (helper para os testes):**
```csharp
private Mock<IReservaRepository> _reservaRepoMock = null!;
private Mock<IUsuarioRepository> _usuarioRepoMock = null!;
private Mock<IEventoRepository> _eventoRepoMock = null!;
private Mock<IEmailSender> _emailSenderMock = null!;
private ReservaService _service = null!;

private void SetupService()
{
    _reservaRepoMock = new Mock<IReservaRepository>();
    _usuarioRepoMock = new Mock<IUsuarioRepository>();
    _eventoRepoMock = new Mock<IEventoRepository>();
    _emailSenderMock = new Mock<IEmailSender>();

    _service = new ReservaService(
        _reservaRepoMock.Object,
        _usuarioRepoMock.Object,
        _eventoRepoMock.Object,
        Mock.Of<ICupomRepository>(),
        Mock.Of<IIngressoRepository>(),
        _emailSenderMock.Object);
}
```

**T3 — Sucesso (reserva paga):**
```csharp
[Fact]
public async Task CancelarReserva_Sucesso_ReservaPaga()
{
    SetupService();
    var reservaId = Guid.NewGuid();
    var eventoId = Guid.NewGuid();
    var cpf = "52998224725";

    var itens = new List<ItemReserva> { CriarItem(cpf, Guid.NewGuid(), 100m) };
    var reserva = Reserva.Criar(cpf, eventoId, itens);
    typeof(Reserva).GetProperty("Id")!.SetValue(reserva, reservaId);

    var evento = new Evento("Workshop .NET", 50,
        DateTime.UtcNow.AddDays(7), 100m, "12345678000199");

    _reservaRepoMock.Setup(r => r.BuscarPorId(reservaId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(reserva);
    _eventoRepoMock.Setup(r => r.GetByIdAsync(eventoId))
        .ReturnsAsync(evento);
    _usuarioRepoMock.Setup(r => r.BuscarCpf(cpf, It.IsAny<CancellationToken>()))
        .ReturnsAsync((Usuario?)null); // email não encontrado → fallback

    await _service.CancelarReserva(reservaId, cpf, CancellationToken.None);

    _reservaRepoMock.Verify(r => r.CancelarComTransacao(
        reservaId, It.IsAny<CancellationToken>()), Times.Once);
    _emailSenderMock.Verify(e => e.EnfileirarAsync(
        It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Once);
}
```

**T4 — Reserva não encontrada (404):**
```csharp
[Fact]
public async Task CancelarReserva_NaoEncontrada_LancaDomainException()
{
    SetupService();
    var reservaId = Guid.NewGuid();
    var cpf = "52998224725";

    _reservaRepoMock.Setup(r => r.BuscarPorId(reservaId, It.IsAny<CancellationToken>()))
        .ReturnsAsync((Reserva?)null);

    var ex = await Assert.ThrowsAsync<Domain.Exceptions.DomainException>(() =>
        _service.CancelarReserva(reservaId, cpf, CancellationToken.None));

    Assert.Equal("Reserva não encontrada.", ex.Message);
    _reservaRepoMock.Verify(r => r.CancelarComTransacao(
        It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
}
```

**T5 — Outro usuário (403):**
```csharp
[Fact]
public async Task CancelarReserva_OutroUsuario_LancaUnauthorizedAccessException()
{
    SetupService();
    var reservaId = Guid.NewGuid();
    var cpfDono = "52998224725";
    var cpfOutro = "98765432100";

    var itens = new List<ItemReserva> { CriarItem(cpfDono, Guid.NewGuid(), 100m) };
    var reserva = Reserva.Criar(cpfDono, Guid.NewGuid(), itens);
    typeof(Reserva).GetProperty("Id")!.SetValue(reserva, reservaId);

    _reservaRepoMock.Setup(r => r.BuscarPorId(reservaId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(reserva);

    var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
        _service.CancelarReserva(reservaId, cpfOutro, CancellationToken.None));

    Assert.Equal("Esta reserva não pertence a você.", ex.Message);
}
```

**T6 — Já reembolsada (409):**
```csharp
[Fact]
public async Task CancelarReserva_JaReembolsada_LancaDomainException()
{
    SetupService();
    var reservaId = Guid.NewGuid();
    var cpf = "52998224725";

    var itens = new List<ItemReserva> { CriarItem(cpf, Guid.NewGuid(), 100m) };
    var reserva = Reserva.Criar(cpf, Guid.NewGuid(), itens);
    typeof(Reserva).GetProperty("Id")!.SetValue(reserva, reservaId);
    reserva.MarcarReembolsada(); // Já reembolsada

    _reservaRepoMock.Setup(r => r.BuscarPorId(reservaId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(reserva);

    var ex = await Assert.ThrowsAsync<Domain.Exceptions.DomainException>(() =>
        _service.CancelarReserva(reservaId, cpf, CancellationToken.None));

    Assert.Equal("Reserva já foi cancelada.", ex.Message);
}
```

**T7 — Evento já começou (400):**
```csharp
[Fact]
public async Task CancelarReserva_EventoJaComecou_LancaDomainException()
{
    SetupService();
    var reservaId = Guid.NewGuid();
    var eventoId = Guid.NewGuid();
    var cpf = "52998224725";

    var itens = new List<ItemReserva> { CriarItem(cpf, Guid.NewGuid(), 100m) };
    var reserva = Reserva.Criar(cpf, eventoId, itens);
    typeof(Reserva).GetProperty("Id")!.SetValue(reserva, reservaId);

    var evento = new Evento("Workshop .NET", 50,
        DateTime.UtcNow.AddDays(-1), 100m, "12345678000199"); // Já passou

    _reservaRepoMock.Setup(r => r.BuscarPorId(reservaId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(reserva);
    _eventoRepoMock.Setup(r => r.GetByIdAsync(eventoId))
        .ReturnsAsync(evento);

    var ex = await Assert.ThrowsAsync<Domain.Exceptions.DomainException>(() =>
        _service.CancelarReserva(reservaId, cpf, CancellationToken.None));

    Assert.Equal("Não é possível cancelar. O evento já começou.", ex.Message);
}
```

**T8 — Transação falha, e-mail NÃO enfileirado:**
```csharp
[Fact]
public async Task CancelarReserva_TransacaoFalha_NaoEnfileiraEmail()
{
    SetupService();
    var reservaId = Guid.NewGuid();
    var eventoId = Guid.NewGuid();
    var cpf = "52998224725";

    var itens = new List<ItemReserva> { CriarItem(cpf, Guid.NewGuid(), 100m) };
    var reserva = Reserva.Criar(cpf, eventoId, itens);
    typeof(Reserva).GetProperty("Id")!.SetValue(reserva, reservaId);

    var evento = new Evento("Workshop .NET", 50,
        DateTime.UtcNow.AddDays(7), 100m, "12345678000199");

    _reservaRepoMock.Setup(r => r.BuscarPorId(reservaId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(reserva);
    _eventoRepoMock.Setup(r => r.GetByIdAsync(eventoId))
        .ReturnsAsync(evento);
    _reservaRepoMock.Setup(r => r.CancelarComTransacao(reservaId, It.IsAny<CancellationToken>()))
        .ThrowsAsync(new Exception("SQL deadlock"));

    await Assert.ThrowsAsync<Exception>(() =>
        _service.CancelarReserva(reservaId, cpf, CancellationToken.None));

    _emailSenderMock.Verify(e => e.EnfileirarAsync(
        It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
}
```

3. Adicionar os imports necessários no topo de `tests/ReservaTests.cs`:
   ```csharp
   using Moq;
   using Application.Service;
   using Application.Interfaces;
   using Domain.Interface;
   using Domain.DTOs;
   ```

**Validation:**
- [ ] `dotnet test` — todos os 16 testes existentes + 8 novos testes passam (24 total).
- [ ] Cobertura dos novos testes cobre 100% das branches de `CancelarReserva`.
- [ ] Testes de domínio (T1, T2) verificam `Reembolsada` corretamente.
- [ ] Testes de service (T3-T8) verificam validações e chamadas a mocks.

**Status:** [x] done

---

## Resumo de Tarefas

| # | Camada | Arquivo(s) | Ação | FRs |
|---|--------|-----------|------|-----|
| 1 | Domain | `Entities/Reserva.cs` | **Editar** (+propriedade +método) | FR-010 |
| 2 | db | `Script0013_AdicionarReembolsadaReservas.sql` | **Criar** | FR-011 |
| 3 | Domain | `Interface/IReservaRepository.cs` | **Editar** (+método) | FR-015 |
| 4 | Infraestructure | `Repository/ReservaRepository.cs` | **Editar** (+CancelarComTransacao) | FR-005 a FR-009, FR-019 |
| 5 | Application | `Interfaces/IReservaService.cs` | **Editar** (+método) | FR-016 |
| 6 | Application | `Service/ReservaService.cs` | **Editar** (+CancelarReserva) | FR-002, FR-003, FR-004, FR-017, FR-018 |
| 7 | Api | `Controllers/ReservaController.cs` | **Editar** (+DELETE endpoint) | FR-001 |
| 8 | Domain + Infra | `DTOs/ReservaDetalhadaDTO.cs`, `DTOs/ReservaVendedorDTO.cs`, `Repository/ReservaRepository.cs` (3 queries) | **Editar** (+campos +colunas SQL) | FR-012, FR-013, FR-014 |
| 9 | tests | `ReservaTests.cs` | **Editar** (+8 testes) | Todos os FRs |

---

## Dependencies Between Tasks

```
Task 1 (Reserva.Reembolsada)
  ├→ Task 2 (migration Script0013)
  │    └→ Task 3 (IReservaRepository interface)
  │         └→ Task 4 (CancelarComTransacao repository)
  │              └→ Task 5 (IReservaService interface) ────┐
  │                   └→ Task 6 (CancelarReserva service) ──┤
  │                        └→ Task 7 (DELETE endpoint)       │
  └→ Task 8 (DTOs + queries) ───────────────────────────────┤
       └→ Task 9 (tests) ←──────────────────────────────────┘
```

---

## Estimativa de Esforço

| Bloco | Tasks | Esforço |
|-------|-------|---------|
| Domain | 1, 3, 8 (DTOs) | 30min |
| Database | 2 | 15min |
| Application | 5, 6 | 45min |
| Infrastructure | 4, 8 (queries) | 1h |
| API | 7 | 30min |
| Tests | 9 | 1h30 |
| **Total** | **9 tasks** | **~4h30** |
