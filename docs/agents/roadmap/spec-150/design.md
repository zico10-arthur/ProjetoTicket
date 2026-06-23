---
name: "Resiliência e Tratamento de Erros"
status: "audited"
references:
  - "requirements.md (este diretório)"
  - "ADR-011 (Global Exception Handler como middleware)"
  - "Spec 120 — Security middleware pipeline"
---

# Spec 150: Resiliência e Tratamento de Erros — Design

## Design Approach

**Estratégia:** Modificação cirúrgica do `GlobalExceptionHandlerMiddleware` existente + adição de data annotations em DTOs + `[Authorize]` no IngressoController. Nenhum novo arquivo criado. Apenas modificações em arquivos existentes.

**Princípios:**
1. **KISS:** Aproveitar o middleware existente — apenas refinar o mapeamento de exceções, sem reescrever.
2. **DRY:** Um único ponto de mapeamento (switch expression no middleware) cobre todas as exceções.
3. **Separation of Concerns:** DTOs validam input (capa Api/Application), middleware trata exceções (capa Api).
4. **Fail-Safe:** Exceções não mapeadas são capturadas pelo fallback genérico com log detalhado.

---

## Architecture Decisions

| Decisão | Justificativa | Referência |
|---------|---------------|------------|
| Substituir `ex.Message` por mensagens fixas no middleware | Mensagens fixas são consistentes, localizadas e não vazam detalhes de implementação | ADR-011 |
| Catch-all para `DomainException` com mensagem genérica | Evita mapear cada subclasse individualmente (30+ exceções). Mensagem "Dados inválidos. Verifique as informações enviadas." | KISS |
| Trim no setter dos DTOs | Executado automaticamente no model binding, sem alterar serviços | — |
| `[Authorize]` na classe IngressoController | Aplica a ambos os endpoints. Sem granularidade por método necessária | — |
| Manter `ApiBehaviorOptions` padrão | O formato `{"errors": {...}}` do ASP.NET para erros de validação é aceitável. Não trocar para middleware customizado | KISS |

---

## Component / Module Breakdown

### Component 1: GlobalExceptionHandlerMiddleware (EDIT)

**Purpose:** Sanitizar respostas de erro — substituir `ex.Message` por mensagens fixas para todas as exceções conhecidas.

**File:** `Api/Middlewares/GlobalExceptionHandlerMiddleware.cs` (EDIT)

**Current Code (antes):**
```csharp
var (statusCode, message) = ex switch
{
    Application.Exceptions.LoginErro
        => (StatusCodes.Status401Unauthorized, "Email ou senha inválidos."),

    Application.Exceptions.CnpjJaCadastrado or
    Application.Exceptions.EmailJaCadastrado or
    Application.Exceptions.UsuarioCadastrado
        => (StatusCodes.Status409Conflict, ex.Message),
    DomainException => (StatusCodes.Status400BadRequest, ex.Message),
    KeyNotFoundException => (StatusCodes.Status404NotFound, "Recurso não encontrado."),
    UnauthorizedAccessException
        => (StatusCodes.Status401Unauthorized, ex.Message),
    _ => (StatusCodes.Status500InternalServerError, "Ocorreu um erro interno no servidor.")
};
```

**New Code (depois):**
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

    // Spec 150: DomainException (400) — mensagem genérica, não expõe detalhes
    DomainException => (StatusCodes.Status400BadRequest, 
        "Dados inválidos. Verifique as informações enviadas."),

    // Spec 150: Não encontrado (404) — já era fixo
    KeyNotFoundException => (StatusCodes.Status404NotFound, "Recurso não encontrado."),

    // Spec 150: Não autorizado (401) — mensagem fixa
    UnauthorizedAccessException
        => (StatusCodes.Status401Unauthorized, "Acesso não autorizado."),

    // Spec 150: Fallback genérico (500) — nunca expõe detalhes
    _ => (StatusCodes.Status500InternalServerError, "Erro interno do servidor.")
};
```

**Key Changes:**
1. `CnpjJaCadastrado`, `EmailJaCadastrado`, `UsuarioCadastrado` — cada um tem sua própria mensagem fixa (antes: `ex.Message` comum)
2. `DomainException` — mensagem genérica "Dados inválidos. Verifique as informações enviadas." (antes: `ex.Message`)
3. `UnauthorizedAccessException` — mensagem fixa "Acesso não autorizado." (antes: `ex.Message`)
4. Fallback `_` — "Erro interno do servidor." com ponto final (antes: "Ocorreu um erro interno no servidor.")

**Dependencies:**
- `Domain.Exceptions.DomainException` (já importado)
- `Application.Exceptions` (já importado)
- `ILogger<GlobalExceptionHandlerMiddleware>` — já injetado para log detalhado

**Error Handling:**
- O `ILogger` já registra `ex` completo (incluindo stack trace) antes de chamar `HandleExceptionAsync`

---

### Component 2: IngressoController [Authorize] (EDIT)

**Purpose:** Proteger endpoints de ingressos com autenticação JWT.

**File:** `Api/Controllers/IngressoController.cs` (EDIT)

**Change:** Adicionar `[Authorize]` na classe:

```csharp
namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]  // ← Spec 150: adicionado
public class IngressoController : ControllerBase
```

**Key:** O atributo na classe aplica-se a ambos os métodos (`ListarIngressos`, `ObterPorId`).

---

### Component 3: Data Annotations em DTOs (EDIT)

**Purpose:** Adicionar `[Required]` e validações em DTOs de entrada.

**Files to modify:**

#### 3a. `Application/DTOs/CadastrarUsuarioDTO.cs`
```csharp
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class CadastrarUsuarioDTO
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [MaxLength(200)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email é obrigatório.")]
    [EmailAddress(ErrorMessage = "Email inválido.")]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "CPF é obrigatório.")]
    [MaxLength(14)]
    public string Cpf { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória.")]
    [MaxLength(100)]
    public string Senha { get; set; } = string.Empty;
}
```

#### 3b. `Application/DTOs/ReservarDTO.cs`
```csharp
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class ReservarDTO
{
    [Required(ErrorMessage = "EventoId é obrigatório.")]
    public Guid EventoId { get; set; }

    [Required(ErrorMessage = "Itens é obrigatório.")]
    [MinLength(1, ErrorMessage = "Pelo menos um item é obrigatório.")]
    public List<ItemReservaRequestDTO> Itens { get; set; } = new();

    public string? CupomCodigo { get; set; }
}
```

#### 3c. `Application/DTOs/CadastrarCupomDTO.cs`
```csharp
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class CadastrarCupomDTO
{
    [Required(ErrorMessage = "Código é obrigatório.")]
    [MaxLength(50)]
    public string Codigo { get; set; } = string.Empty;

    [Required]
    [Range(1, 100, ErrorMessage = "Desconto deve ser entre 1 e 100.")]
    public int PorcentagemDesconto { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Valor mínimo não pode ser negativo.")]
    public decimal ValorMinimo { get; set; }

    public DateTime? DataExpiracao { get; set; }
}
```

#### 3d. `Application/DTOs/ItemReservaRequestDTO.cs`
```csharp
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class ItemReservaRequestDTO
{
    [Required(ErrorMessage = "CPF do participante é obrigatório.")]
    [MaxLength(14)]
    public string CpfParticipante { get; set; } = string.Empty;

    [Required(ErrorMessage = "IngressoId é obrigatório.")]
    public Guid IngressoId { get; set; }
}
```

#### 3e. `Application/DTOs/AlterarNomeDTO.cs`
```csharp
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class AlterarNomeDTO
{
    [Required(ErrorMessage = "Novo nome é obrigatório.")]
    [MaxLength(200)]
    public string NovoNome { get; set; } = string.Empty;
}
```

#### 3f. `Application/DTOs/AlterarEmailDTO.cs`
```csharp
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class AlterarEmailDTO
{
    [Required(ErrorMessage = "Novo email é obrigatório.")]
    [EmailAddress(ErrorMessage = "Email inválido.")]
    [MaxLength(200)]
    public string NovoEmail { get; set; } = string.Empty;
}
```

#### 3g. `Application/DTOs/AlterarSenhaDTO.cs`
```csharp
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class AlterarSenhaDTO
{
    [Required(ErrorMessage = "Nova senha é obrigatória.")]
    [MaxLength(100)]
    public string NovaSenha { get; set; } = string.Empty;
}
```

---

### Component 4: Sanitização Trim nos DTOs (EDIT)

**Purpose:** Aplicar `.Trim()` automaticamente em todos os campos string dos DTOs de entrada.

**Approach:** Usar propriedades com backing field e `Trim()` no setter. Apenas nos DTOs que aceitam input do usuário. DTOs de resposta NÃO precisam de Trim.

**Pattern:**
```csharp
private string _nome = string.Empty;
public string Nome
{
    get => _nome;
    set => _nome = value?.Trim() ?? string.Empty;
}
```

**DTOs com Trim aplicado:**

| DTO | Campos com Trim |
|-----|----------------|
| `CadastrarUsuarioDTO` | Nome, Email, Cpf |
| `CadastrarVendedorDTO` | RazaoSocial, NomeFantasia, Email, Cnpj |
| `LoginDTO` | Email (Trim + ToLowerInvariant) |
| `EventoRequestDTO` | Nome, Descricao, Local |
| `CadastrarCupomDTO` | Codigo |
| `ItemReservaRequestDTO` | CpfParticipante |
| `AlterarNomeDTO` | NovoNome |
| `AlterarEmailDTO` | NovoEmail (Trim + ToLowerInvariant) |

**Nota:** `LoginDTO.Email` já deve aplicar `Trim().ToLowerInvariant()` como especificado em Spec 120.

---

## Data Flow

### Flow 1: Exceção de Domínio Sanitizada

```
POST /api/reserva/fazer-reserva (com dados inválidos)
    │
    ▼
ReservaService.FazerReserva(dto)
    │  Validação de negócio falha
    │  throw new IngressosEsgotados()
    ▼
GlobalExceptionHandlerMiddleware.InvokeAsync()
    │  catch (Exception ex)
    │  │  ex is DomainException
    │  │  _logger.LogError(ex, "Erro capturado...")
    │  ▼
    │  HandleExceptionAsync(context, ex)
    │  │  DomainException → (400, "Dados inválidos. Verifique as informações enviadas.")
    │  │  context.Response.StatusCode = 400
    │  │  WriteAsJsonAsync(new { message = "Dados inválidos. Verifique as informações enviadas." })
    │  ▼
HTTP 400 {"message":"Dados inválidos. Verifique as informações enviadas."}
```

### Flow 2: Validação de Input com Data Annotations

```
POST /api/usuario/cadastrar-comprador (com body vazio)
    │
    ▼
ASP.NET Model Binding
    │  CadastrarUsuarioDTO tem [Required] em Nome, Email, Cpf, Senha
    │  ModelState.IsValid == false
    │  Retorna HTTP 400 automaticamente (antes do controller)
    ▼
HTTP 400 {"errors":{"Nome":["Nome é obrigatório."], ...}}
```

### Flow 3: Acesso Negado a Endpoint Protegido

```
GET /api/ingresso/eventos/{id}/ingressos (sem token JWT)
    │
    ▼
ASP.NET Authentication Middleware
    │  Sem token → HttpContext.User não autenticado
    │  [Authorize] no IngressoController → 401
    ▼
HTTP 401 (resposta padrão do framework)
```

---

## Data Structures

### GlobalExceptionHandlerMiddleware Mapping Table

| Exception Type | HTTP Status | Response Message |
|---------------|-------------|------------------|
| `LoginErro` | 401 | "Email ou senha inválidos." |
| `CnpjJaCadastrado` | 409 | "CNPJ já cadastrado." |
| `EmailJaCadastrado` | 409 | "E-mail já cadastrado." |
| `UsuarioCadastrado` | 409 | "Usuário já cadastrado." |
| `DomainException` (base) | 400 | "Dados inválidos. Verifique as informações enviadas." |
| `KeyNotFoundException` | 404 | "Recurso não encontrado." |
| `UnauthorizedAccessException` | 401 | "Acesso não autorizado." |
| `Exception` (fallback) | 500 | "Erro interno do servidor." |

---

## File / Module Layout

```
ProjetoTicket/
├── Api/
│   ├── Middlewares/
│   │   └── GlobalExceptionHandlerMiddleware.cs  ← EDIT (Component 1)
│   └── Controllers/
│       └── IngressoController.cs                 ← EDIT (Component 2)
├── Application/
│   └── DTOs/
│       ├── CadastrarUsuarioDTO.cs                ← EDIT (Component 3a + 4)
│       ├── CadastrarVendedorDTO.cs               ← EDIT (Component 4 — Trim)
│       ├── LoginDTO.cs                           ← EDIT (Component 4 — Trim+ToLower)
│       ├── ReservarDTO.cs                        ← EDIT (Component 3b)
│       ├── CadastrarCupomDTO.cs                  ← EDIT (Component 3c + 4)
│       ├── ItemReservaRequestDTO.cs              ← EDIT (Component 3d + 4)
│       ├── EventoRequestDTO.cs                   ← EDIT (Component 4 — Trim)
│       ├── AlterarNomeDTO.cs                     ← EDIT (Component 3e + 4)
│       ├── AlterarEmailDTO.cs                    ← EDIT (Component 3f + 4)
│       └── AlterarSenhaDTO.cs                    ← EDIT (Component 3g)
└── tests/
    └── ResilicienciaTests.cs                     ← NEW (testes automatizados)
```

**Total files created:** 1 (`ResilicienciaTests.cs`)
**Total files modified:** 12 (`GlobalExceptionHandlerMiddleware.cs`, `IngressoController.cs`, `CadastrarUsuarioDTO.cs`, `CadastrarVendedorDTO.cs`, `LoginDTO.cs`, `ReservarDTO.cs`, `CadastrarCupomDTO.cs`, `ItemReservaRequestDTO.cs`, `EventoRequestDTO.cs`, `AlterarNomeDTO.cs`, `AlterarEmailDTO.cs`, `AlterarSenhaDTO.cs`)

---

## Testing Strategy

### Test File: `tests/ResilicienciaTests.cs`

### Unit Tests (xUnit + Moq)

| # | Teste | Tipo | FR Coberto | Descrição |
|---|-------|------|------------|-----------|
| T1 | `DomainException_Retorna400_ComMensagemFixa` | Unit | FR-001 | Lança `DomainException("msg interna")`. Verifica 400 + body `{"message":"Dados inválidos. Verifique as informações enviadas."}` |
| T2 | `ExceptionGenerica_Retorna500_ComMensagemGenerica` | Unit | FR-006 | Lança `Exception("detalhes internos")`. Verifica 500 + body `{"message":"Erro interno do servidor."}` |
| T3 | `UnauthorizedAccessException_Retorna401_Fixo` | Unit | FR-001 | Lança `UnauthorizedAccessException("msg interna")`. Verifica 401 + "Acesso não autorizado." |
| T4 | `LoginErro_Retorna401_MensagemLogin` | Unit | FR-001 | Lança `LoginErro`. Verifica 401 + "Email ou senha inválidos." (já era assim, regressão) |

### Integration Tests (WebApplicationFactory)

| # | Teste | Tipo | FR Coberto | Descrição |
|---|-------|------|------------|-----------|
| T5 | `IngressoController_SemToken_Retorna401` | Integration | FR-002 | GET /api/ingresso/eventos/{id}/ingressos sem token → 401 |
| T6 | `IngressoController_ComToken_Retorna200` | Integration | FR-002 | GET /api/ingresso/eventos/{id}/ingressos com token válido → 200 |
| T7 | `CadastrarUsuario_SemNome_Retorna400` | Integration | FR-003 | POST /api/usuario/cadastrar-comprador sem Nome → 400 |
| T8 | `CadastrarUsuario_EmailInvalido_Retorna400` | Integration | FR-003 | POST com email "nao-arroba" → 400 |
| T9 | `DTO_Trim_NomeComEspacos` | Unit | FR-004 | Cria `CadastrarUsuarioDTO { Nome = "  João  " }`. Verifica `Nome == "João"` |
| T10 | `DTO_Trim_EmailComEspacos` | Unit | FR-004 | Cria `LoginDTO { Email = "  TESTE@EMAIL.COM  " }`. Verifica `Email == "teste@email.com"` |

### Manual Verification

| # | Verificação | FR Coberto |
|---|-------------|------------|
| V1 | Swagger: IngressoController mostra cadeado 🔒 (requer token) | FR-002 |
| V2 | `POST /api/usuario/cadastrar-comprador` com body `{}` → 400 com erros de validação | FR-003 |
| V3 | Erro forçado (ex: banco offline) → 500 com `{"message":"Erro interno do servidor."}`, sem stack trace | FR-006 |
| V4 | `dotnet test` — todos os testes passam, sem regressões | FR-001~006 |

---

## Migration / Rollback

### Migration Steps

1. **Sanitizar GlobalExceptionHandlerMiddleware** (Task 1) — substituir `ex.Message` por mensagens fixas.
2. **Adicionar [Authorize] ao IngressoController** (Task 2).
3. **Adicionar data annotations ao CadastrarUsuarioDTO** (Task 3).
4. **Adicionar data annotations ao ReservarDTO** (Task 4).
5. **Adicionar data annotations ao CadastrarCupomDTO** (Task 5).
6. **Adicionar data annotations ao ItemReservaRequestDTO** (Task 6).
7. **Adicionar data annotations aos DTOs Alterar*** (Task 7).
8. **Aplicar Trim nos DTOs de entrada** (Task 8).
9. **Build + verificação** (Task 9).
10. **Testes automatizados** (Task 10).

### Rollback Strategy

Cada task é independentemente reversível via `git checkout -- <arquivo>`:

| Task | Rollback |
|------|----------|
| 1 | `git checkout -- Api/Middlewares/GlobalExceptionHandlerMiddleware.cs` |
| 2 | `git checkout -- Api/Controllers/IngressoController.cs` |
| 3-7 | `git checkout -- Application/DTOs/<arquivo>.cs` |
| 8 | `git checkout -- Application/DTOs/*.cs` |
| 9 | N/A (apenas build) |
| 10 | Deletar `tests/ResilicienciaTests.cs` |

### Breaking Changes

- **DomainException mensagens:** As mensagens de erro para o cliente MUDAM de mensagens internas (ex: "O cpf precisa ser preenchido") para mensagens padronizadas (ex: "Dados inválidos. Verifique as informações enviadas."). O frontend que depende de mensagens específicas pode precisar de ajuste.
- **IngressoController:** Requisições sem token que antes funcionavam passarão a receber 401. Qualquer integração que consuma esses endpoints sem autenticação será quebrada.
