---
name: "Resiliência e Tratamento de Erros"
status: "audited"
references:
  - "150-resiliencia-erros.md (documento original)"
  - "ADR-011 (Global Exception Handler como middleware)"
  - "Spec 120 (Segurança: BCrypt, JWT, rate limit) — middleware de exceção existente"
  - "visao.md §2 (Emitir ingressos — sistema profissional)"
dependencies:
  - "Spec 120 — GlobalExceptionHandlerMiddleware já existe e é base para sanitização"
  - "Spec 130 — Isolamento Multi-Tenant já implementado (controllers com Authorize correto)"
---

# Spec 150: Resiliência e Tratamento de Erros — Requirements

## Value Delivery

Esta spec entrega o bloco **"Emitir ingressos — sistema profissional"** definido em `visao.md §2`:

> **Sanitização de erros:** O sistema nunca expõe `ex.Message` ou stack traces para o usuário. Toda resposta de erro é padronizada em `{"message": "mensagem clara em português"}`.

> **Validação de Input:** Todos os DTOs de entrada têm `[Required]` nos campos obrigatórios e aplicam `.Trim()` em strings, prevenindo dados malformados.

> **Proteção de endpoints:** IngressoController ganha `[Authorize]`, fechando a brecha de endpoint público que deveria ser protegido.

**Problema resolvido:** Elimina 3 fragilidades do sistema atual:
1. `DomainException` expõe `ex.Message` para o cliente (ex: "O cpf precisa ser preenchido", "Cupom Inválido!O cupom selecionado expirou")
2. `IngressoController` sem `[Authorize]` — endpoints de ingressos são públicos
3. Vários DTOs sem data annotations de validação (`[Required]`, `[EmailAddress]`, `[MaxLength]`)

---

## Functional Requirements

### FR-001: Sanitização do GlobalExceptionHandlerMiddleware

**What:** O `GlobalExceptionHandlerMiddleware` NÃO deve expor `ex.Message` para exceções conhecidas. Cada tipo de exceção de domínio deve ser mapeado para uma mensagem fixa, clara e em português. Exceções desconhecidas (genérico `Exception`) devem retornar HTTP 500 com mensagem genérica "Erro interno do servidor", sem detalhes técnicos.

**Why:** ADR-011. O formato atual expõe mensagens internas como "O cpf precisa ser preenchido", "Código Inválido!O código deve ter mais de 5 e menos de 50 digitos." — mensagens inconsistentes, algumas com pontuação incorreta, que confundem o usuário e expõem lógica interna.

**Acceptance Criteria:**
- [ ] `DomainException` e suas subclasses são mapeadas para mensagens fixas (não `ex.Message`)
- [ ] `UnauthorizedAccessException` retorna "Acesso não autorizado." (não `ex.Message`)
- [ ] `KeyNotFoundException` retorna "Recurso não encontrado." (já implementado ✅)
- [ ] Exceções não mapeadas retornam HTTP 500 com "Erro interno do servidor."
- [ ] NENHUMA resposta de erro contém `ex.Message` ou stack trace
- [ ] `dotnet test` verifica: DomainException → 400 com mensagem fixa; Exception genérica → 500 com mensagem genérica

### FR-002: IngressoController com [Authorize]

**What:** Adicionar `[Authorize]` ao `IngressoController`. Ambos os endpoints (`GET /api/ingresso/eventos/{eventoId}/ingressos` e `GET /api/ingresso/{id}`) devem exigir autenticação JWT.

**Why:** O controller atual não tem `[Authorize]`, permitindo que qualquer pessoa sem autenticação liste ingressos e consulte ingressos por ID. Ingressos contêm informações de evento, assentos e preços — dados que devem ser protegidos.

**Acceptance Criteria:**
- [ ] `IngressoController` tem atributo `[Authorize]` na classe
- [ ] Requisição sem token JWT para `GET /api/ingresso/eventos/{id}/ingressos` → HTTP 401
- [ ] Requisição com token JWT válido (qualquer perfil) → HTTP 200
- [ ] `dotnet build` compila sem erros
- [ ] `dotnet test` não apresenta regressões

### FR-003: Data Annotations em DTOs de Entrada

**What:** Todos os DTOs usados como parâmetros de entrada em controllers devem ter `[Required]` nos campos obrigatórios e validações apropriadas (`[EmailAddress]`, `[MaxLength]`, `[Range]`).

**Why:** DTOs sem validação permitem que dados inválidos cheguem à camada de serviço e ao banco, causando erros 500 ou comportamento imprevisível. A validação no DTO é a primeira linha de defesa e retorna erros 400 claros via model binding do ASP.NET.

**Acceptance Criteria:**
- [ ] `CadastrarUsuarioDTO`: `[Required]` em Nome, Email, Cpf, Senha + `[EmailAddress]` em Email
- [ ] `ReservarDTO`: `[Required]` em EventoId, Itens
- [ ] `CadastrarCupomDTO`: `[Required]` em Codigo + `[Range]` em PorcentagemDesconto + `[Range]` em ValorMinimo
- [ ] `ItemReservaRequestDTO`: `[Required]` em CpfParticipante, IngressoId
- [ ] `AlterarNomeDTO`: `[Required]` em NovoNome + `[MaxLength(200)]`
- [ ] `AlterarEmailDTO`: `[Required]` em NovoEmail + `[EmailAddress]` + `[MaxLength(200)]`
- [ ] `AlterarSenhaDTO`: `[Required]` em NovaSenha + `[MaxLength(100)]`
- [ ] Body vazio ou campo obrigatório ausente → HTTP 400 (model binding do ASP.NET)
- [ ] `dotnet test` verifica validações com model binding

### FR-004: Sanitização de Strings (Trim)

**What:** Todos os campos `string` dos DTOs de entrada devem ter `.Trim()` aplicado para remover espaços em branco no início e fim. O Trim deve ser aplicado via `set` property ou no service, garantindo que strings como `"  João  "` sejam persistidas como `"João"`.

**Why:** Espaços em branco acidentais em campos como CPF, email, nome causam falhas de validação downstream (ex: " 12345678901" não é um CPF válido). O Trim previne esse problema na origem.

**Acceptance Criteria:**
- [ ] `CadastrarUsuarioDTO`: Nome, Email, Cpf aplicam `.Trim()` no set
- [ ] `CadastrarVendedorDTO`: RazaoSocial, NomeFantasia, Email, Cnpj aplicam `.Trim()` no set
- [ ] `LoginDTO`: Email aplica `.Trim().ToLowerInvariant()` no set
- [ ] `EventoRequestDTO`: Nome, Descricao, Local aplicam `.Trim()` no set
- [ ] `CadastrarCupomDTO`: Codigo aplica `.Trim()` no set
- [ ] `dotnet test` verifica: `"  João  "` → `"João"` após binding

### FR-005: Formato de Erro Uniforme

**What:** Toda resposta de erro da API deve seguir o formato `{"message": "..."}` com Content-Type `application/json; charset=utf-8`. Nenhuma resposta de erro deve usar formatos diferentes ou conter campos extras como `error`, `statusCode`, `traceId`.

**Why:** Consistência para o frontend — um único formato de erro simplifica o tratamento no Blazor. Atualmente o sistema já usa `{"message": "..."}` na maioria dos casos, mas algumas exceções podem escapar com formatos diferentes.

**Acceptance Criteria:**
- [ ] Toda resposta 4xx e 5xx usa o formato `{"message": "..."}`
- [ ] Content-Type é `application/json; charset=utf-8` em todas as respostas de erro
- [ ] Nenhuma resposta contém campos como `error`, `statusCode`, `stackTrace`, `exception`
- [ ] `dotnet test` verifica formato de resposta para cada código de status

### FR-006: Fallback Genérico para Exceções Não Mapeadas

**What:** Exceções que não são de domínio (ex: `SqlException`, `NullReferenceException`, `TaskCanceledException`) devem ser capturadas pelo `GlobalExceptionHandlerMiddleware` e retornar HTTP 500 com mensagem genérica "Erro interno do servidor." O log detalhado deve ser registrado no `ILogger` para debugging.

**Why:** Segurança — stack traces, queries SQL e detalhes de infraestrutura nunca devem ser expostos ao cliente. Apenas logs internos devem conter esses detalhes.

**Acceptance Criteria:**
- [ ] `NullReferenceException` → HTTP 500, body `{"message":"Erro interno do servidor."}`
- [ ] `SqlException` → HTTP 500, body `{"message":"Erro interno do servidor."}`
- [ ] `TaskCanceledException` → HTTP 500 (ou 499 se o cliente desconectou), body `{"message":"Erro interno do servidor."}`
- [ ] O `ILogger` registra o erro completo (incluindo stack trace) para debugging
- [ ] `dotnet test` verifica que exceções genéricas retornam 500 sem detalhes

---

## Non-Functional Requirements

### NFR-001: Consistência de Content-Type

**What:** Toda resposta de erro deve ter `Content-Type: application/json; charset=utf-8`. O middleware já faz isso, mas deve ser verificado para todas as branches.

**Acceptance Criteria:**
- [ ] `GlobalExceptionHandlerMiddleware` define `context.Response.ContentType = "application/json; charset=utf-8"` em TODOS os caminhos
- [ ] RateLimitMiddleware também usa o mesmo Content-Type (já implementado ✅)

### NFR-002: Performance do Middleware

**What:** O `GlobalExceptionHandlerMiddleware` não deve adicionar overhead mensurável para requisições bem-sucedidas (caminho sem exceção). Para requisições com erro, o overhead deve ser < 1ms.

**Acceptance Criteria:**
- [ ] O middleware apenas envolve `_next(context)` em try/catch — sem alocações desnecessárias no caminho feliz
- [ ] O switch de mapeamento de exceções usa pattern matching (já é O(1) amortizado)

### NFR-003: Cobertura de Todas as Exceções de Domínio

**What:** Toda exceção que herda de `DomainException` e é lançada em qualquer camada deve ser mapeada no `GlobalExceptionHandlerMiddleware`. Nenhuma `DomainException` deve cair no fallback genérico de 500.

**Acceptance Criteria:**
- [ ] Todas as subclasses de `DomainException` nos namespaces `Domain.Exceptions` e `Application.Exceptions` têm um mapeamento explícito OU um catch-all para `DomainException` com mensagem genérica
- [ ] Nenhuma `DomainException` resulta em HTTP 500

---

## Constraints

1. **ADR-001 (Clean Architecture):** A sanitização (Trim) é responsabilidade do Application/Domain, não da Api. O middleware de exceção fica na Api.
2. **ADR-011 (Exception Handler):** Toda resposta de erro segue `{"message": "..."}`. Exceções de domínio são mapeadas para HTTP status codes pelo `GlobalExceptionHandlerMiddleware`.
3. **Stack:** .NET 9, C# 13, ASP.NET Core, sem dependências externas adicionais.
4. **Spec 120:** O `GlobalExceptionHandlerMiddleware` existente é a base — apenas modificado, não reescrito.
5. **Retrocompatibilidade:** As alterações não devem quebrar endpoints existentes. Build e testes devem continuar passando.

---

## Edge Cases & Error States

| # | Caso | Gatilho | Comportamento Exato |
|---|------|---------|---------------------|
| EC-001 | Exceção com inner exception | `SqlException` com `InnerException` | Apenas a mensagem genérica é exposta. Inner exception NUNCA é serializada na resposta. |
| EC-002 | DomainException com mensagem longa | Exceção com mensagem > 500 caracteres | A mensagem original é truncada ou substituída por mensagem genérica. Não expor mensagens longas. |
| EC-003 | Múltiplas exceções simultâneas | Várias requisições falham ao mesmo tempo | Cada resposta é independente. O middleware é stateless por requisição. |
| EC-004 | Exceção após resposta já iniciada | Erro durante streaming/file download | O middleware não consegue alterar o status code. Loga o erro e deixa a resposta parcial. |
| EC-005 | Body HTTP 400 do model binding | ASP.NET retorna 400 automático por validação | O formato padrão do ASP.NET é `{"errors": {...}}`. Avaliar se usar `ApiBehaviorOptions.SuppressModelStateInvalidFilter` + middleware para unificar. |
| EC-006 | Token JWT expirado | `GET /api/ingresso` com token expirado | ASP.NET retorna 401 automaticamente (antes do controller). Formato padrão do framework — aceitável. |
| EC-007 | Campo string com apenas espaços | Nome = `"   "` após Trim → `""` | String vazia é aceita pelo banco. Validação adicional (não vazio) está fora do escopo. |
| EC-008 | DTO com valor null em campo [Required] | `POST` com `{"Nome": null}` | ASP.NET model binding trata null como ausente → HTTP 400. Não requer tratamento especial. |

---

## Dependencies

| Dependência | Tipo | Detalhe |
|-------------|------|---------|
| `GlobalExceptionHandlerMiddleware` (Spec 120) | Código existente | Base para sanitização. Já captura todas as exceções e retorna JSON. |
| `DomainException` | Classe existente | Base class para todas as exceções de domínio. Já existe em `Domain/Exceptions/DomainException.cs`. |
| `System.ComponentModel.DataAnnotations` | Namespace | Já usado em alguns DTOs (`LoginDTO`, `CadastrarVendedorDTO`, `EventoRequestDTO`). |
| ASP.NET Model Binding | Framework | Valida automaticamente Data Annotations antes de chamar o controller. |
| Spec 120 (Segurança) | Spec | `GlobalExceptionHandlerMiddleware` e `RateLimitMiddleware` já implementados e no pipeline. |

---

## Out of Scope

Estes itens NÃO fazem parte desta spec e NÃO devem ser implementados:

1. **Health Checks** — endpoints de saúde para monitoramento ( `/health`, `/ready`).
2. **Polly / Retry** — retentativas automáticas em falhas de banco ou rede.
3. **Result Pattern** — substituir exceções por `Result<T>` ou `OneOf<T, Error>`.
4. **Serilog / OpenTelemetry** — logging estruturado ou tracing distribuído.
5. **Validação de CPF/CNPJ** — algoritmos de validação de dígitos verificadores.
6. **Sanitização de Output** — encoding HTML, proteção contra XSS no frontend.
7. **Resilience no DbUp** — retry em migrations falhas.
8. **API de validação customizada** — `FluentValidation` ou similar.
9. **Tratamento de Timeout** — `CancellationToken` já é propagado pelos serviços.
10. **Circuit Breaker** — padrão de circuit breaker para serviços externos.
