---
name: "Resiliência e Tratamento de Erros"
status: "audited"
references:
  - "requirements.md (este diretório)"
  - "design.md (este diretório)"
---

# Spec 150: Resiliência e Tratamento de Erros — Implementation Tasks

## Task Order

Tasks MUST be executed sequentially in numerical order. Each task depends on the completion of all preceding tasks.

```
Task 1 (Sanitizar GlobalExceptionHandlerMiddleware)
  │
  └──→ Task 2 ([Authorize] IngressoController) — independente, mas faz após Task 1
  │
Task 3 (Data annotations CadastrarUsuarioDTO)
  ├──→ Task 4 (Data annotations ReservarDTO)
  ├──→ Task 5 (Data annotations CadastrarCupomDTO)
  ├──→ Task 6 (Data annotations ItemReservaRequestDTO)
  └──→ Task 7 (Data annotations Alterar*DTOs)
        │
        └──→ Task 8 (Trim nos DTOs) — depende de Tasks 3-7 para saber campos exatos
              │
              └──→ Task 9 (Build + verificação) — compile check
                    │
                    └──→ Task 10 (Testes) — deve ser a ÚLTIMA task
```

---

## Tasks

### Task 1: Sanitizar GlobalExceptionHandlerMiddleware

**Objective:** Substituir `ex.Message` por mensagens fixas e padronizadas no handler de exceções.

**Requirements Covered:** FR-001, FR-005, FR-006

**Design References:** design.md — Component 1

**Actions:**
1. Open file `Api/Middlewares/GlobalExceptionHandlerMiddleware.cs`
2. Locate the `HandleExceptionAsync` method
3. Find the switch expression that maps exceptions → (statusCode, message)
4. Replace the switch expression with the sanitized version:

```csharp
var (statusCode, message) = ex switch
{
    // Spec 120: LoginErro retorna 401 com mensagem padronizada
    Application.Exceptions.LoginErro
        => (StatusCodes.Status401Unauthorized, "Email ou senha inválidos."),

    // Spec 150: Conflitos (409) — mensagens específicas do negócio
    Application.Exceptions.CnpjJaCadastrado
        => (StatusCodes.Status409Conflict, "CNPJ já cadastrado."),
    Application.Exceptions.EmailJaCadastrado
        => (StatusCodes.Status409Conflict, "E-mail já cadastrado."),
    Application.Exceptions.UsuarioCadastrado
        => (StatusCodes.Status409Conflict, "Usuário já cadastrado."),

    // Spec 150: DomainException (400) — mensagem genérica
    DomainException => (StatusCodes.Status400BadRequest, 
        "Dados inválidos. Verifique as informações enviadas."),

    // Spec 150: Não encontrado (404)
    KeyNotFoundException => (StatusCodes.Status404NotFound, "Recurso não encontrado."),

    // Spec 150: Não autorizado (401) — mensagem fixa
    UnauthorizedAccessException
        => (StatusCodes.Status401Unauthorized, "Acesso não autorizado."),

    // Spec 150: Fallback genérico (500)
    _ => (StatusCodes.Status500InternalServerError, "Erro interno do servidor.")
};
```

5. **Verify** the Content-Type line is `"application/json; charset=utf-8"` (not just `"application/json"`)
6. If currently `"application/json"`, change to `"application/json; charset=utf-8"`
7. Save the file
8. Run: `dotnet build Api/Api.csproj`

**Validation:**
- [ ] `dotnet build` succeeds with 0 errors
- [ ] Code review: NENHUM `ex.Message` exposto na resposta (exceto log)
- [ ] Code review: switch expression cobre LoginErro, conflitos (CnpjJaCadastrado/EmailJaCadastrado/UsuarioCadastrado), DomainException, KeyNotFoundException, UnauthorizedAccessException, fallback
- [ ] Code review: Content-Type é `"application/json; charset=utf-8"`

**Status:** [x] done

---

### Task 2: Adicionar [Authorize] ao IngressoController

**Objective:** Proteger endpoints de ingressos com autenticação JWT.

**Requirements Covered:** FR-002

**Design References:** design.md — Component 2

**Actions:**
1. Open file `Api/Controllers/IngressoController.cs`
2. Add `[Authorize]` attribute on the class, between `[Route("api/[controller]")]` and `public class IngressoController`
3. Ensure the file has `using Microsoft.AspNetCore.Authorization;` at the top (check if already present)
4. The final class declaration should look like:

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IngressoController : ControllerBase
```

5. Save the file
6. Run: `dotnet build Api/Api.csproj`

**Validation:**
- [ ] `dotnet build` succeeds with 0 errors
- [ ] Code review: `[Authorize]` attribute present on the class
- [ ] Code review: `using Microsoft.AspNetCore.Authorization;` present

**Status:** [x] done

---

### Task 3: Data Annotations em CadastrarUsuarioDTO

**Objective:** Adicionar `[Required]`, `[EmailAddress]`, `[MaxLength]` no DTO de cadastro de comprador.

**Requirements Covered:** FR-003

**Design References:** design.md — Component 3a

**Actions:**
1. Open file `Application/DTOs/CadastrarUsuarioDTO.cs`
2. Add `using System.ComponentModel.DataAnnotations;` at the top
3. Add data annotations to each property:
   - `Nome`: `[Required(ErrorMessage = "Nome é obrigatório.")]` + `[MaxLength(200)]`
   - `Email`: `[Required(ErrorMessage = "Email é obrigatório.")]` + `[EmailAddress(ErrorMessage = "Email inválido.")]` + `[MaxLength(200)]`
   - `Cpf`: `[Required(ErrorMessage = "CPF é obrigatório.")]` + `[MaxLength(14)]`
   - `Senha`: `[Required(ErrorMessage = "Senha é obrigatória.")]` + `[MaxLength(100)]`
4. Save the file
5. Run: `dotnet build Application/Application.csproj`

**Validation:**
- [ ] `dotnet build` succeeds with 0 errors
- [ ] Code review: all 4 properties have `[Required]`
- [ ] Code review: `Email` has `[EmailAddress]`
- [ ] Code review: `Nome`, `Email`, `Cpf`, `Senha` have `[MaxLength]`

**Status:** [x] done

---

### Task 4: Data Annotations em ReservarDTO

**Objective:** Adicionar `[Required]` e `[MinLength]` no DTO de reserva.

**Requirements Covered:** FR-003

**Design References:** design.md — Component 3b

**Actions:**
1. Open file `Application/DTOs/ReservarDTO.cs`
2. Add `using System.ComponentModel.DataAnnotations;` at the top
3. Add data annotations:
   - `EventoId`: `[Required(ErrorMessage = "EventoId é obrigatório.")]`
   - `Itens`: `[Required(ErrorMessage = "Itens é obrigatório.")]` + `[MinLength(1, ErrorMessage = "Pelo menos um item é obrigatório.")]`
4. Save the file
5. Run: `dotnet build Application/Application.csproj`

**Validation:**
- [ ] `dotnet build` succeeds with 0 errors
- [ ] Code review: `EventoId` has `[Required]`
- [ ] Code review: `Itens` has `[Required]` + `[MinLength(1)]`

**Status:** [x] done

---

### Task 5: Data Annotations em CadastrarCupomDTO

**Objective:** Adicionar `[Required]`, `[Range]`, `[MaxLength]` no DTO de cadastro de cupom.

**Requirements Covered:** FR-003

**Design References:** design.md — Component 3c

**Actions:**
1. Open file `Application/DTOs/CadastrarCupomDTO.cs`
2. Add `using System.ComponentModel.DataAnnotations;` at the top
3. Add data annotations:
   - `Codigo`: `[Required(ErrorMessage = "Código é obrigatório.")]` + `[MaxLength(50)]`
   - `PorcentagemDesconto`: `[Required]` + `[Range(1, 100, ErrorMessage = "Desconto deve ser entre 1 e 100.")]`
   - `ValorMinimo`: `[Required]` + `[Range(0, double.MaxValue, ErrorMessage = "Valor mínimo não pode ser negativo.")]`
4. Save the file
5. Run: `dotnet build Application/Application.csproj`

**Validation:**
- [ ] `dotnet build` succeeds with 0 errors
- [ ] Code review: `Codigo` has `[Required]` + `[MaxLength(50)]`
- [ ] Code review: `PorcentagemDesconto` has `[Required]` + `[Range(1, 100)]`
- [ ] Code review: `ValorMinimo` has `[Required]` + `[Range(0, ...)]`

**Status:** [x] done

---

### Task 6: Data Annotations em ItemReservaRequestDTO

**Objective:** Adicionar `[Required]` e `[MaxLength]` no DTO de item de reserva.

**Requirements Covered:** FR-003

**Design References:** design.md — Component 3d

**Actions:**
1. Open file `Application/DTOs/ItemReservaRequestDTO.cs`
2. Add `using System.ComponentModel.DataAnnotations;` at the top
3. Add data annotations:
   - `CpfParticipante`: `[Required(ErrorMessage = "CPF do participante é obrigatório.")]` + `[MaxLength(14)]`
   - `IngressoId`: `[Required(ErrorMessage = "IngressoId é obrigatório.")]`
4. Save the file
5. Run: `dotnet build Application/Application.csproj`

**Validation:**
- [ ] `dotnet build` succeeds with 0 errors
- [ ] Code review: both properties have `[Required]`
- [ ] Code review: `CpfParticipante` has `[MaxLength(14)]`

**Status:** [x] done

---

### Task 7: Data Annotations em DTOs Alterar*

**Objective:** Adicionar validações nos DTOs de alteração de dados do usuário.

**Requirements Covered:** FR-003

**Design References:** design.md — Components 3e, 3f, 3g

**Actions:**
1. **AlterarNomeDTO.cs:**
   - Add `using System.ComponentModel.DataAnnotations;`
   - Add `[Required(ErrorMessage = "Novo nome é obrigatório.")]` + `[MaxLength(200)]` on `NovoNome`

2. **AlterarEmailDTO.cs:**
   - Add `using System.ComponentModel.DataAnnotations;`
   - Add `[Required(ErrorMessage = "Novo email é obrigatório.")]` + `[EmailAddress(ErrorMessage = "Email inválido.")]` + `[MaxLength(200)]` on `NovoEmail`

3. **AlterarSenhaDTO.cs:**
   - Add `using System.ComponentModel.DataAnnotations;`
   - Add `[Required(ErrorMessage = "Nova senha é obrigatória.")]` + `[MaxLength(100)]` on `NovaSenha`

4. Save all files
5. Run: `dotnet build Application/Application.csproj`

**Validation:**
- [ ] `dotnet build` succeeds with 0 errors
- [ ] Code review: `AlterarNomeDTO.NovoNome` has `[Required]` + `[MaxLength(200)]`
- [ ] Code review: `AlterarEmailDTO.NovoEmail` has `[Required]` + `[EmailAddress]` + `[MaxLength(200)]`
- [ ] Code review: `AlterarSenhaDTO.NovaSenha` has `[Required]` + `[MaxLength(100)]`

**Status:** [x] done

---

### Task 8: Aplicar Trim nos DTOs de Entrada

**Objective:** Adicionar sanitização `.Trim()` em todos os campos string dos DTOs de entrada.

**Requirements Covered:** FR-004

**Design References:** design.md — Component 4

**Actions:**
For each DTO listed below, convert plain auto-properties to full properties with backing fields that apply `.Trim()` in the setter:

**8a. `CadastrarUsuarioDTO.cs`** — Campos: Nome, Email, Cpf
**8b. `CadastrarVendedorDTO.cs`** — Campos: RazaoSocial, NomeFantasia, Email, Cnpj
**8c. `LoginDTO.cs`** — Campo: Email (Trim + ToLowerInvariant)
**8d. `EventoRequestDTO.cs`** — Campos: Nome (Descricao e Local já são nullable string)
**8e. `CadastrarCupomDTO.cs`** — Campo: Codigo
**8f. `ItemReservaRequestDTO.cs`** — Campo: CpfParticipante
**8g. `AlterarNomeDTO.cs`** — Campo: NovoNome
**8h. `AlterarEmailDTO.cs`** — Campo: NovoEmail (Trim + ToLowerInvariant)

**Pattern:**
```csharp
private string _nome = string.Empty;
public string Nome
{
    get => _nome;
    set => _nome = value?.Trim() ?? string.Empty;
}
```

**Pattern for Email (Trim + ToLowerInvariant):**
```csharp
private string _email = string.Empty;
public string Email
{
    get => _email;
    set => _email = value?.Trim().ToLowerInvariant() ?? string.Empty;
}
```

5. Save all files
6. Run: `dotnet build Application/Application.csproj`

**Validation:**
- [ ] `dotnet build` succeeds with 0 errors
- [ ] Code review: `CadastrarUsuarioDTO` Nome/Email/Cpf aplicam Trim
- [ ] Code review: `CadastrarVendedorDTO` RazaoSocial/NomeFantasia/Email/Cnpj aplicam Trim
- [ ] Code review: `LoginDTO.Email` aplica Trim + ToLowerInvariant
- [ ] Code review: `EventoRequestDTO.Nome` aplica Trim
- [ ] Code review: `CadastrarCupomDTO.Codigo` aplica Trim
- [ ] Code review: `ItemReservaRequestDTO.CpfParticipante` aplica Trim
- [ ] Code review: `AlterarNomeDTO.NovoNome` aplica Trim
- [ ] Code review: `AlterarEmailDTO.NovoEmail` aplica Trim + ToLowerInvariant

**Status:** [x] done

---

### Task 9: Build e Verificação de Consistência

**Objective:** Garantir que o projeto compila e que os 106 testes existentes continuam passando.

**Requirements Covered:** FR-001~006 (verificação geral)

**Design References:** —

**Actions:**
1. Run: `dotnet build`
2. Fix any compilation errors
3. Run: `dotnet test`
4. Verify that all previously passing tests still pass (104 pass, 2 fail pré-existentes do `AutoCadastroVendedorTests`)
5. Check for new test failures introduced by this spec

**Validation:**
- [ ] `dotnet build` succeeds with 0 errors
- [ ] `dotnet test` — 104 pass, 2 fail (mesmas falhas pré-existentes)
- [ ] Nenhuma nova falha de teste introduzida
- [ ] Warnings count não aumentou significativamente

**Status:** [ ] pending

---

### Task 10: Escrever Testes Automatizados

**Objective:** Criar testes para validação de sanitização de erros e validação de input.

**Requirements Covered:** FR-001, FR-002, FR-003, FR-004, FR-005, FR-006 (automated); FR-001~006 (manual verification)

**Design References:** design.md — Testing Strategy

**Actions:**
1. Create file `tests/ResilicienciaTests.cs`
2. Implement the following test methods:

**Unit Tests (using xUnit + Moq):**

| # | Test Method | FR |
|---|------------|-----|
| T1 | `DomainException_Retorna400_ComMensagemFixa` — Lança `DomainException("msg interna")`, verifica 400 + body `{"message":"Dados inválidos. Verifique as informações enviadas."}` | FR-001 |
| T2 | `ExceptionGenerica_Retorna500_ComMensagemGenerica` — Lança `Exception("detalhes")`, verifica 500 + body `{"message":"Erro interno do servidor."}` | FR-006 |
| T3 | `UnauthorizedAccessException_Retorna401_Fixo` — Lança `UnauthorizedAccessException("msg interna")`, verifica 401 + "Acesso não autorizado." | FR-001 |
| T4 | `LoginErro_Retorna401_MensagemLogin` — Lança `LoginErro`, verifica 401 + "Email ou senha inválidos." | FR-001 |

**Integration Tests (using WebApplicationFactory<Program>):**

| # | Test Method | FR |
|---|------------|-----|
| T5 | `IngressoController_SemToken_Retorna401` — GET /api/ingresso/eventos/{id}/ingressos sem token → 401 | FR-002 |
| T6 | `IngressoController_ComToken_Retorna200` — GET com token válido → 200 | FR-002 |
| T7 | `CadastrarUsuario_SemNome_Retorna400` — POST /api/usuario/cadastrar-comprador sem Nome → 400 | FR-003 |
| T8 | `CadastrarUsuario_EmailInvalido_Retorna400` — POST com email "nao-arroba" → 400 | FR-003 |

**DTO Sanitization Tests (Unit):**

| # | Test Method | FR |
|---|------------|-----|
| T9 | `DTO_Trim_NomeComEspacos` — `CadastrarUsuarioDTO { Nome = "  João  " }` → Nome == "João" | FR-004 |
| T10 | `DTO_Trim_EmailComEspacos` — `LoginDTO { Email = "  TESTE@EMAIL.COM  " }` → Email == "teste@email.com" | FR-004 |

3. Run: `dotnet test --filter "ResilicienciaTests"`

**Validation:**
- [ ] All 10 tests pass (green)
- [ ] `dotnet test` output shows "10 Passed, 0 Failed, 0 Skipped" for ResilicienciaTests
- [ ] No tests are flaky (run twice, both pass)
- [ ] Full test suite: 114 pass, 2 fail (104 + 10 novos, 2 pré-existentes)

**Status:** [x] done

---

## Summary

| # | Layer | File(s) | Action | FRs Covered | Est. Time |
|---|-------|---------|--------|-------------|-----------|
| 1 | Api | `GlobalExceptionHandlerMiddleware.cs` | Sanitizar mapeamento de exceções | FR-001, FR-005, FR-006 | 15 min |
| 2 | Api | `IngressoController.cs` | Adicionar `[Authorize]` | FR-002 | 5 min |
| 3 | Application | `CadastrarUsuarioDTO.cs` | Data annotations | FR-003 | 10 min |
| 4 | Application | `ReservarDTO.cs` | Data annotations | FR-003 | 5 min |
| 5 | Application | `CadastrarCupomDTO.cs` | Data annotations | FR-003 | 5 min |
| 6 | Application | `ItemReservaRequestDTO.cs` | Data annotations | FR-003 | 5 min |
| 7 | Application | `Alterar*DTO.cs` (3 files) | Data annotations | FR-003 | 10 min |
| 8 | Application | 8 DTOs | Trim nos setters | FR-004 | 25 min |
| 9 | All | — | Build + rodar testes existentes | FR-001~006 | 10 min |
| 10 | tests | `ResilicienciaTests.cs` (NEW) | 10 automated tests | FR-001~006 | 1h 00min |

**Total estimated time:** ~2h 30min

**Total files created:** 1 (`ResilicienciaTests.cs`)

**Total files modified:** 12 (`GlobalExceptionHandlerMiddleware.cs`, `IngressoController.cs`, `CadastrarUsuarioDTO.cs`, `CadastrarVendedorDTO.cs`, `LoginDTO.cs`, `ReservarDTO.cs`, `CadastrarCupomDTO.cs`, `ItemReservaRequestDTO.cs`, `EventoRequestDTO.cs`, `AlterarNomeDTO.cs`, `AlterarEmailDTO.cs`, `AlterarSenhaDTO.cs`)
