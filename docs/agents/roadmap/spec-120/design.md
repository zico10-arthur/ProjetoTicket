---
name: "Segurança (BCrypt, JWT, Rate Limit)"
status: "audited"
references:
  - "requirements.md (este diretório)"
  - "ADR-003 (BCrypt para hash de senhas)"
  - "ADR-004 (JWT com roles para autenticação)"
  - "ADR-010 (Rate limiting no endpoint de login)"
  - "ADR-011 (Global Exception Handler como middleware)"
---

# Spec 120: Segurança (BCrypt, JWT, Rate Limit) — Design

## Design Approach

**Estratégia:** Modificação cirúrgica de 3 arquivos existentes + criação de 2 novos arquivos. Nenhuma alteração no Domain ou no schema do banco. A segurança é adicionada como camada transversal (cross-cutting) na Infrastructure (hash), Application (verificação) e Api (rate limiting, configuração JWT).

**Princípios:**
1. **KISS:** Middleware de rate limit customizado (150 linhas) em vez de biblioteca externa com dezenas de arquivos de configuração.
2. **DRY:** Hash e verificação BCrypt concentrados em 2 pontos únicos (Repository e Service), sem duplicação.
3. **Separation of Concerns:** Domain não conhece BCrypt; Infrastructure aplica hash; Application verifica; Api configura e protege.
4. **Fail-Fast:** API recusa iniciar sem `Jwt:Key` configurada — sem fallback silencioso para chave insegura.

---

## Architecture Decisions

| Decisão | Justificativa | Referência |
|---------|---------------|------------|
| BCrypt work factor 11 | Padrão da lib `BCrypt.Net-Next`. ~50ms por hash — seguro sem degradar UX. | ADR-003 |
| Hash no Repository, Verify no Service | Repository (Infrastructure) persiste — aplica hash. Service (Application) autentica — verifica. Domain permanece puro. | ADR-001, ADR-003 |
| Sliding window em vez de fixed window | Fixed window permite 2x limite na borda (5 req em t=0:59 + 5 em t=1:01). Sliding window é estritamente "N req nos últimos X segundos". | ADR-010 |
| Rate limit em memória (sem Redis) | Simplicidade para MVP. Se escala horizontal for necessária, migrar para Redis com `StackExchange.Redis`. | ADR-010 |
| `X-Forwarded-For` como fonte primária de IP | Compatível com proxy reverso (Nginx, Cloudflare) em produção. Fallback para `RemoteIpAddress` em desenvolvimento local. | ADR-010 |
| `InvalidOperationException` no startup se sem `Jwt:Key` | API não deve subir em estado inseguro. Mensagem clara orienta desenvolvedor a configurar. | ADR-004 |
| `appsettings.*.json` no `.gitignore` | Protege contra commit acidental de qualquer variante de ambiente. | ADR-004 |

---

## Component / Module Breakdown

### Component 1: BCrypt Password Hashing (Application)

**Purpose:** Aplicar hash BCrypt na senha do usuário antes da persistência. Responsabilidade do Application Service — a senha já chega hasheada ao repositório.

**Nota de implementação (audit):** O hash BCrypt é aplicado na camada Application (`UsuarioService.CadastrarComprador` e `UsuarioService.CadastrarVendedor`), que já o fazia desde ST-08. O repositório (`UsuarioRepository`) apenas persiste o valor recebido, sem re-hashear. Tentar hashear em ambas as camadas causaria double-hashing e impediria o login. O pacote `BCrypt.Net-Next` permanece referenciado por `Infraestructure.csproj` (usado pelo `DatabaseSeeder`) e `Application.csproj` (usado pelo `UsuarioService`).

**Interface:** `UsuarioService.CadastrarComprador()` e `UsuarioService.CadastrarVendedor()` chamam `BCrypt.Net.BCrypt.HashPassword()` antes de passar a senha para o repositório.

**File:** `Application/Service/UsuarioService.cs` (pre-existing, unchanged by spec 120 for hashing)

---

### Component 2: BCrypt Password Verification (Application)

**Purpose:** Verificar a senha fornecida no login contra o hash BCrypt armazenado no banco.

**Interface:** Modificação interna do método `Login` em `UsuarioService`. Nenhuma nova interface pública.

**File:** `Application/Service/UsuarioService.cs` (EDIT)

**Internal Logic:**
```
1. Recebe LoginRequestDTO { Email, Senha }
2. Busca usuário por email via IUsuarioRepository.BuscarPorEmail(email)
3. Se usuário == null → lança CredenciaisInvalidasException
4. Chama BCrypt.Net.BCrypt.Verify(dto.Senha, usuario.Senha)
5. Se Verify == false → lança CredenciaisInvalidasException
6. Se usuario.Ativo == false → lança CredenciaisInvalidasException
7. Gera JWT com claims: sub=Cpf, email=Email, role=Perfil.Nome
8. Retorna LoginResponseDTO { Token, Nome, Email, Role }
```

**Dependencies:**
- `BCrypt.Net` (NuGet, adicionado ao projeto `Application`)
- `IUsuarioRepository` (já existente, Domain)
- `CredenciaisInvalidasException` (já existente, Domain)
- JWT configuration (`IConfiguration`, já existente)

**Error Handling:**
- `BCrypt.Verify()` não lança exceção para inputs válidos
- Se `usuario.Senha` (hash do banco) for inválido (não começa com `$2a$`), `BCrypt.Verify()` lança `SaltParseException` → capturar e converter para `CredenciaisInvalidasException`

**Exact Code Change:**
```csharp
// Antes (padrão identificado — comparação direta de senha):
if (usuario == null || usuario.Senha != dto.Senha)
    throw new CredenciaisInvalidasException("Email ou senha inválidos.");

// Depois:
if (usuario == null)
    throw new CredenciaisInvalidasException("Email ou senha inválidos.");

try
{
    if (!BCrypt.Net.BCrypt.Verify(dto.Senha, usuario.Senha))
        throw new CredenciaisInvalidasException("Email ou senha inválidos.");
}
catch (BCrypt.Net.SaltParseException)
{
    throw new CredenciaisInvalidasException("Email ou senha inválidos.");
}

if (!usuario.Ativo)
    throw new CredenciaisInvalidasException("Email ou senha inválidos.");
```

---

### Component 3: JWT Key Configuration (Api)

**Purpose:** Carregar chave JWT de fonte segura (user-secrets em dev, env vars em prod) e validar sua presença antes do startup.

**File:** `Api/Program.cs` (EDIT)

**Interface:** Modificação no método `Main` / top-level statements de `Program.cs`. Nenhuma nova classe.

**Internal Logic:**
```
1. Após builder.Configuration padrão, em Development:
   builder.Configuration.AddUserSecrets<Program>()
2. var jwtKey = builder.Configuration["Jwt:Key"]
3. Se string.IsNullOrEmpty(jwtKey):
   throw new InvalidOperationException("Jwt:Key não configurada...")
4. Configurar JWT Bearer com jwtKey como IssuerSigningKey
5. Registrar RateLimitMiddleware antes de UseAuthentication()
```

**Exact Code Placement (Program.cs):**
```csharp
// === SEÇÃO 1: User Secrets (após var builder = WebApplication.CreateBuilder(args)) ===
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

// === SEÇÃO 2: Validação Jwt:Key (antes de AddAuthentication) ===
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException(
        "Jwt:Key não configurada. Em desenvolvimento, execute:\n" +
        "  dotnet user-secrets set \"Jwt:Key\" \"<sua-chave-secreta-de-32-caracteres>\"\n" +
        "  dotnet user-secrets set \"Jwt:Issuer\" \"SoldOutTickets\"\n" +
        "  dotnet user-secrets set \"Jwt:Audience\" \"SoldOutClients\"");
}

// === SEÇÃO 3: JWT Bearer config (já existente, ajustar para usar jwtKey variável) ===
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// === SEÇÃO 4: Middleware Pipeline (após var app = builder.Build()) ===
app.UseMiddleware<RateLimitMiddleware>();  // ANTES de UseAuthentication
app.UseAuthentication();
app.UseAuthorization();
```

**Dependencies:**
- `Microsoft.Extensions.Configuration.UserSecrets` (incluído no SDK .NET 9)
- `Microsoft.AspNetCore.Authentication.JwtBearer` (já existente)

**Error Handling:**
- Falta de `Jwt:Key` → `InvalidOperationException` antes de `app.Run()`. API não sobe. Processo termina com exit code != 0.

---

### Component 4: SlidingWindow (Api — New)

**Purpose:** Algoritmo de janela deslizante para contagem de eventos (requisições) por intervalo de tempo. Thread-safe.

**File:** `Api/Middleware/SlidingWindow.cs` (NEW)

**Interface:**
```csharp
namespace Api.Middleware;

/// <summary>
/// Thread-safe sliding window counter.
/// Tracks timestamps of events and enforces a maximum count within a time interval.
/// </summary>
public class SlidingWindow
{
    private readonly ConcurrentQueue<DateTime> _timestamps = new();

    /// <summary>
    /// Attempts to record a new event.
    /// </summary>
    /// <param name="limit">Maximum number of events allowed within the interval.</param>
    /// <param name="interval">The time window duration.</param>
    /// <returns>true if the event is within the limit and was recorded; false if the limit has been exceeded.</returns>
    public bool TryIncrement(int limit, TimeSpan interval)
    {
        var now = DateTime.UtcNow;
        var cutoff = now - interval;

        // Remove expired timestamps (lock-free cleanup)
        while (_timestamps.TryPeek(out var oldest) && oldest < cutoff)
        {
            _timestamps.TryDequeue(out _);
        }

        if (_timestamps.Count >= limit)
        {
            return false; // Limit exceeded
        }

        _timestamps.Enqueue(now);
        return true; // Allowed
    }
}
```

**Internal Logic:**
1. Calcula `cutoff = now - interval` (ex: 1 minuto atrás)
2. Remove todos os timestamps anteriores ao cutoff do início da fila
3. Se Count >= limit → retorna false (limite excedido)
4. Senão → enfileira timestamp atual e retorna true

**Dependencies:**
- `System.Collections.Concurrent` (runtime .NET 9)
- Nenhuma dependência externa

**Error Handling:**
- `DateTime.UtcNow` nunca lança
- `ConcurrentQueue.TryPeek/TryDequeue/TryEnqueue` nunca lançam para uso normal
- Se `limit <= 0` ou `interval <= TimeSpan.Zero`, comportamento é matematicamente definido (sempre false ou sempre true respectivamente) — validar no caller

**Thread Safety:**
- `ConcurrentQueue` é lock-free para operações single-producer/single-consumer
- `TryPeek` + `TryDequeue` em loop: condição de corrida benigna — um timestamp expirado pode ser visto por `TryPeek` mas removido por outra thread antes do `TryDequeue`. O loop simplesmente tenta o próximo.

---

### Component 5: RateLimitMiddleware (Api — New)

**Purpose:** Middleware HTTP que intercepta requisições `POST /api/usuario/login` e aplica limite de 5 requisições por minuto por IP.

**File:** `Api/Middleware/RateLimitMiddleware.cs` (NEW)

**Interface:**
```csharp
namespace Api.Middleware;

/// <summary>
/// Rate-limiting middleware for the login endpoint.
/// Limits POST /api/usuario/login to 5 requests per minute per IP address.
/// Uses sliding window algorithm with in-memory storage.
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ConcurrentDictionary<string, SlidingWindow> _clients = new();
    private const int Limit = 5;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    public RateLimitMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldRateLimit(context))
        {
            var ip = GetClientIp(context);
            var window = _clients.GetOrAdd(ip, _ => new SlidingWindow());

            if (!window.TryIncrement(Limit, Interval))
            {
                context.Response.StatusCode = 429; // Too Many Requests
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync(
                    "{\"message\":\"Muitas tentativas. Aguarde 1 minuto.\"}");
                return;
            }
        }

        await _next(context);
    }

    private static bool ShouldRateLimit(HttpContext context)
    {
        return context.Request.Path.StartsWithSegments("/api/usuario/login", StringComparison.OrdinalIgnoreCase)
            && HttpMethods.IsPost(context.Request.Method);
    }

    private static string GetClientIp(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
        {
            // X-Forwarded-For pode conter múltiplos IPs: "client, proxy1, proxy2"
            // O primeiro é o IP real do cliente
            return forwarded.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
```

**Internal Logic:**
1. Verifica se a requisição é `POST /api/usuario/login` (case-insensitive)
2. Se não for, chama `_next(context)` imediatamente (zero overhead para outros endpoints)
3. Extrai IP do cliente (preferindo `X-Forwarded-For`)
4. Obtém ou cria `SlidingWindow` para o IP no dicionário estático
5. Chama `TryIncrement(5, 1min)`
6. Se false → HTTP 429 com body JSON e retorna sem chamar `_next`
7. Se true → chama `_next(context)` (continua pipeline normal)

**Dependencies:**
- `SlidingWindow` (Component 4, mesmo namespace)
- `System.Collections.Concurrent` (runtime .NET 9)
- `Microsoft.AspNetCore.Http` (framework ASP.NET Core)

**Error Handling:**
- `Split(',')` em string vazia retorna `[""]` → `Trim()` produz `""` → fallback para `RemoteIpAddress`
- `RemoteIpAddress` pode ser null (raro) → fallback para `"unknown"`
- `GetOrAdd` é thread-safe e atômico por chave
- Se `WriteAsync` falhar (conexão fechada), exceção é capturada pelo `GlobalExceptionHandlerMiddleware`

**Registration in Program.cs:**
```csharp
// DEVE vir ANTES de UseAuthentication() / UseAuthorization()
app.UseMiddleware<RateLimitMiddleware>();
```

---

### Component 6: GlobalExceptionHandlerMiddleware — CredenciaisInvalidasException Mapping

**Purpose:** Garantir que `CredenciaisInvalidasException` seja mapeada para HTTP 401 com o body padronizado `{"message":"Email ou senha inválidos."}`.

**File:** `Api/Middleware/GlobalExceptionHandlerMiddleware.cs` (EDIT, se necessário)

**Interface:** Verificar se o mapeamento já existe. Se não, adicionar:

```csharp
// Dentro do catch de exceções de domínio:
catch (CredenciaisInvalidasException ex)
{
    context.Response.StatusCode = 401;
    context.Response.ContentType = "application/json; charset=utf-8";
    await context.Response.WriteAsync("{\"message\":\"Email ou senha inválidos.\"}");
}
```

**Dependencies:**
- `Domain/Exceptions/CredenciaisInvalidasException` (já existente)

---

## Data Flow

### Flow 1: Cadastro de Usuário (Comprador ou Vendedor)

```
HTTP POST /api/usuario/cadastrar-comprador (ou /cadastrar-vendedor)
    │
    ▼
UsuarioController.CadastrarComprador(CadastrarCompradorDTO dto)
    │
    ▼
UsuarioService.CadastrarComprador(dto)
    │  Cria entidade Usuario { ..., Senha = dto.Senha }  ← texto plano no Domain
    ▼
IUsuarioRepository.CadastrarUsuario(usuario)
    │
    ▼
UsuarioRepository.CadastrarUsuario(usuario)           ← Infrastructure
    │  var hash = BCrypt.Net.BCrypt.HashPassword(usuario.Senha)
    │  INSERT INTO Usuarios (..., Senha) VALUES (..., @hash)
    │  hash = "$2a$11$..."                             ← BCrypt com salt aleatório
    ▼
Banco: Usuarios.Senha = "$2a$11$..." (60+ caracteres)
```

**Key:** A entidade `Usuario` no Domain mantém `Senha` como string plain-text. O hash é aplicado APENAS no repositório, imediatamente antes do INSERT. A entidade NÃO é modificada.

### Flow 2: Login

```
HTTP POST /api/usuario/login
    │
    ▼
RateLimitMiddleware.InvokeAsync()
    │  GET client IP (X-Forwarded-For ou RemoteIpAddress)
    │  SlidingWindow.TryIncrement(5, 1min)
    │  ├─ false → HTTP 429 {"message":"Muitas tentativas. Aguarde 1 minuto."}
    │  └─ true  → continua
    ▼
UsuarioController.Login(LoginRequestDTO dto)
    │
    ▼
UsuarioService.Login(dto)
    │
    ├─ IUsuarioRepository.BuscarPorEmail(dto.Email)
    │   └→ Dapper: SELECT * FROM Usuarios WHERE Email = @Email
    │
    ├─ usuario == null?
    │   └→ throw CredenciaisInvalidasException("Email ou senha inválidos.")
    │
    ├─ BCrypt.Net.BCrypt.Verify(dto.Senha, usuario.Senha)
    │   ├─ SaltParseException?
    │   │   └→ throw CredenciaisInvalidasException("Email ou senha inválidos.")
    │   └─ false?
    │       └→ throw CredenciaisInvalidasException("Email ou senha inválidos.")
    │
    ├─ !usuario.Ativo?
    │   └→ throw CredenciaisInvalidasException("Email ou senha inválidos.")
    │
    └─ Gerar JWT
        ├─ Claims: sub=Cpf, email=Email, role=Perfil.Nome
        ├─ Assinatura: SymmetricSecurityKey(Jwt:Key do user-secrets)
        └─ Retorna LoginResponseDTO { Token, Nome, Email, Role }
            │
            ▼
        HTTP 200 {"token":"eyJ...", "nome":"...", "email":"...", "role":"..."}
```

### Flow 3: Rate Limit Excedido

```
HTTP POST /api/usuario/login (6ª tentativa do mesmo IP em < 1 min)
    │
    ▼
RateLimitMiddleware.InvokeAsync()
    │  SlidingWindow.TryIncrement(5, 1min) → false
    │  context.Response.StatusCode = 429
    │  context.Response.WriteAsync("{\"message\":\"Muitas tentativas. Aguarde 1 minuto.\"}")
    │  return (NÃO chama _next)
    ▼
HTTP 429 {"message":"Muitas tentativas. Aguarde 1 minuto."}
```

### Flow 4: Startup sem Jwt:Key

```
dotnet run (ambiente Development)
    │
    ▼
Program.cs: builder.Configuration.AddUserSecrets<Program>()
    │  User secrets carregados de %APPDATA%\Microsoft\UserSecrets\<id>\secrets.json
    ▼
Program.cs: var jwtKey = builder.Configuration["Jwt:Key"]
    │  jwtKey == null ou ""
    ▼
Program.cs: throw new InvalidOperationException("Jwt:Key não configurada...")
    │
    ▼
Processo termina com exit code != 0
Mensagem no console: "Jwt:Key não configurada. Em desenvolvimento, execute: ..."
```

---

## Data Structures

### Existing: Usuario (Domain/Entities/Usuario.cs)

Nenhuma alteração estrutural. A propriedade `Senha` permanece como `string` — contendo texto plano no Domain e hash após persistência.

```csharp
// Sem alterações na entidade
public class Usuario
{
    public string Cpf { get; set; }
    public string Nome { get; set; }
    public string Email { get; set; }
    public string Senha { get; set; }        // ← texto plano no Domain
    public Guid PerfilId { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
    // ... demais propriedades
}
```

### Existing: LoginRequestDTO (Application/DTOs/LoginRequestDTO.cs)

**Change needed:** Adicionar data annotations para validação básica (FR-007):
```csharp
public class LoginRequestDTO
{
    [Required(ErrorMessage = "Email é obrigatório.")]
    [EmailAddress(ErrorMessage = "Email inválido.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Senha é obrigatória.")]
    public string Senha { get; set; }
}
```

Se as annotations já existirem, apenas verificar que cobrem `[Required]` em ambos os campos e `[EmailAddress]` no Email.

### Existing: LoginResponseDTO (Application/DTOs/LoginResponseDTO.cs)

Nenhuma alteração. Já contém `Token`, `Nome`, `Email`, `Role`.

### Existing: CredenciaisInvalidasException (Domain/Exceptions/CredenciaisInvalidasException.cs)

Nenhuma alteração. Já existe e é usada pelo `UsuarioService.Login()`.

### New: SlidingWindow (Api/Middleware/SlidingWindow.cs)

```csharp
public class SlidingWindow
{
    // State
    private readonly ConcurrentQueue<DateTime> _timestamps;

    // Public API
    public bool TryIncrement(int limit, TimeSpan interval);
}
```

### Database: Usuarios.Senha Column

Nenhuma alteração de schema necessária. A coluna `Senha` já deve ser `VARCHAR(100)` ou maior (hash BCrypt tem 60 caracteres). Verificar se a coluna existente suporta ≥ 60 caracteres. Se for `VARCHAR(50)` ou menor, criar script de migração para `ALTER TABLE Usuarios ALTER COLUMN Senha VARCHAR(100) NOT NULL`.

### appsettings.json Changes

```json
{
  "Jwt": {
    "Key": "",
    "Issuer": "SoldOutTickets",
    "Audience": "SoldOutClients"
  }
}
```

Apenas `Key` muda: de valor real para string vazia.

### User Secrets (não versionado — %APPDATA%)

```json
{
  "Jwt": {
    "Key": "<chave-gerada-de-32-caracteres>",
    "Issuer": "SoldOutTickets",
    "Audience": "SoldOutClients"
  }
}
```

---

## File / Module Layout

```
ProjetoTicket/
├── Api/
│   ├── Middleware/
│   │   ├── RateLimitMiddleware.cs      ← NEW (Component 5)
│   │   ├── SlidingWindow.cs            ← NEW (Component 4)
│   │   └── GlobalExceptionHandlerMiddleware.cs ← EDIT (Component 6, se necessário)
│   ├── Program.cs                      ← EDIT (Component 3: user-secrets, validação Jwt:Key, middleware pipeline)
│   ├── appsettings.json                ← EDIT (remover Jwt:Key real)
│   └── Api.csproj                      ← (sem alteração — BCrypt transitivo via Application/Infraestructure)
├── Application/
│   ├── Service/
│   │   └── UsuarioService.cs           ← EDIT (Component 2: BCrypt.Verify)
│   └── Application.csproj              ← EDIT (adicionar pacote BCrypt.Net-Next)
├── Infraestructure/
│   ├── Repository/
│   │   └── UsuarioRepository.cs        ← EDIT (Component 1: BCrypt.HashPassword)
│   └── Infraestructure.csproj          ← EDIT (adicionar pacote BCrypt.Net-Next)
├── db/
│   └── Script0001_SeedAdmin.sql        ← EDIT (substituir senha plain-text por hash BCrypt)
├── .gitignore                          ← EDIT (adicionar appsettings.*.json e secrets.json)
└── tests/
    └── SegurancaTests.cs               ← NEW (testes de integração)
```

**Nota sobre localização do pacote BCrypt:** `BCrypt.Net-Next` será adicionado aos projetos `Infraestructure/Infraestructure.csproj` e `Application/Application.csproj` (ambos precisam de referência direta ao `BCrypt.Net` para compilar — `Infraestructure` para `HashPassword()`, `Application` para `Verify()`). O projeto `Api` recebe acesso transitivo via referência a ambos. Task 1 deve adicionar o pacote a AMBOS os projetos.

---

## Testing Strategy

### Test File: `tests/SegurancaTests.cs`

### Unit Tests (xUnit + Moq)

| # | Teste | Tipo | FR Coberto | Descrição |
|---|-------|------|------------|-----------|
| T1 | `CadastrarUsuario_SenhaHasheada` | Unit | FR-001 | Mock do Dapper. Captura o parâmetro `Senha` passado para `ExecuteAsync`. Verifica que começa com `$2a$11$` e tem ≥ 60 caracteres. |
| T2 | `CadastrarUsuario_SenhaNaoIgualOriginal` | Unit | FR-001 | Similar a T1. Verifica que o hash NÃO contém a senha original como substring. |
| T3 | `Login_SenhaCorreta_RetornaJWT` | Unit | FR-002 | Mock do repositório retornando usuário com hash BCrypt da senha "Teste@123". Chama Login com "Teste@123". Verifica que retorna LoginResponseDTO com Token não-nulo. |
| T4 | `Login_SenhaIncorreta_LancaExcecao` | Unit | FR-002, FR-003 | Mock retorna usuário. Chama Login com senha errada. Verifica `CredenciaisInvalidasException` com mensagem "Email ou senha inválidos." |
| T5 | `Login_EmailInexistente_LancaExcecao` | Unit | FR-002, FR-003 | Mock retorna null. Chama Login. Verifica `CredenciaisInvalidasException` com a MESMA mensagem. |
| T6 | `Login_UsuarioInativo_LancaExcecao` | Unit | FR-003 | Mock retorna usuário com `Ativo=false`. Chama Login com senha correta. Verifica `CredenciaisInvalidasException` com a MESMA mensagem. |
| T7 | `Login_SaltParseException_LancaCredenciaisInvalidas` | Unit | FR-002, EC-003 | Mock retorna usuário com `Senha = "hash-invalido"`. BCrypt.Verify lança `SaltParseException`. Verifica que é convertida para `CredenciaisInvalidasException`. |

### Integration Tests (WebApplicationFactory)

| # | Teste | Tipo | FR Coberto | Descrição |
|---|-------|------|------------|-----------|
| T8 | `RateLimit_5Requisicoes_OK` | Integration | FR-005 | Envia 5 POST /api/usuario/login em sequência. Verifica que todos retornam 401 (credenciais inválidas) — NÃO 429. |
| T9 | `RateLimit_6aRequisicao_429` | Integration | FR-005 | Envia 6 POST /api/usuario/login. Verifica que o 6º retorna 429 com body `{"message":"Muitas tentativas. Aguarde 1 minuto."}`. |
| T10 | `RateLimit_OutroEndpoint_SemRateLimit` | Integration | FR-005 | Envia 10 POST /api/usuario/cadastrar-comprador. Verifica que todos são processados (sem 429). |
| T11 | `RateLimit_ResetaApos1Min` | Integration | FR-005, NFR-002 | Envia 5 req, espera 61 segundos, envia mais 5. Verifica que as últimas 5 NÃO recebem 429. |
| T12 | `Program_SemJwtKey_FalhaStartup` | Integration | FR-004 | Inicia WebApplicationFactory SEM configurar Jwt:Key. Verifica que `InvalidOperationException` é lançada com mensagem contendo "dotnet user-secrets". |
| T13 | `Login_Resposta401_FormatoCorreto` | Integration | FR-003 | POST /api/usuario/login com credenciais inválidas. Verifica status 401 e body exato: `{"message":"Email ou senha inválidos."}`. |
| T14 | `Login_Resposta401_BodyIdentico` | Integration | FR-003 | Compara os bodies de 3 cenários (email inexistente, senha incorreta, inativo). Verifica que são byte-idênticos. |

### Manual Verification (não automatizado)

| # | Verificação | FR Coberto |
|---|-------------|------------|
| V1 | `git log -p -- Api/appsettings.json` não mostra chave JWT real | FR-004, NFR-004 |
| V2 | `BCrypt.HashPassword("Admin@123")` gera hash começando com `$2a$11$` | NFR-001 |
| V3 | Admin seed: `SELECT Senha FROM Usuarios WHERE Email='admin@soldout.com'` retorna `$2a$11$...` | FR-006 |
| V4 | `POST /api/usuario/login` com body `{}` retorna HTTP 400 (model binding) | FR-007 |

**Nota sobre FR-006 e FR-007:** FR-006 (seed do Admin) é validado via verificação manual V3 e pelas validações da Task 4. FR-007 (data annotations no DTO) é validado por comportamento padrão do ASP.NET model binding e pelas validações da Task 3 — não requer teste automatizado dedicado.

---

## Migration / Rollback

### Migration Steps

1. **Instalar BCrypt.Net-Next** (Task 1) — adicionar pacote NuGet a Infraestructure e Application.
2. **Modificar UsuarioRepository** (Task 2) — hash no cadastro.
3. **Modificar UsuarioService + LoginRequestDTO** (Task 3) — verificação no login + data annotations.
4. **Atualizar Admin Seed** (Task 4) — hash no script SQL.
5. **Configurar user-secrets** (Task 5) — remover chave do appsettings.json.
6. **Atualizar .gitignore** (Task 6).
7. **Criar SlidingWindow** (Task 7).
8. **Criar RateLimitMiddleware** (Task 8).
9. **Registrar middleware** (Task 9) — pipeline no Program.cs.
10. **Varrer chaves hardcoded** (Task 10).
11. **Verificar GlobalExceptionHandler** (Task 11) — mapeamento CredenciaisInvalidasException → 401.
12. **Verificar Senha column size** (Task 12) — garantir VARCHAR(100).
13. **Testes automatizados** (Task 13) — 14 testes, cobertura completa.

### Rollback Strategy

Cada task é independentemente reversível:

| Task | Rollback |
|------|----------|
| 1 | `dotnet remove package BCrypt.Net-Next` (executar em Infraestructure/ e Application/) |
| 2, 3 | `git checkout -- UsuarioRepository.cs UsuarioService.cs LoginRequestDTO.cs` |
| 4 | `git checkout -- Script0001_SeedAdmin.sql` + reexecutar DbUp |
| 5 | `git checkout -- Program.cs appsettings.json` + limpar user-secrets: `dotnet user-secrets clear` |
| 6 | `git checkout -- .gitignore` |
| 7, 8, 9 | Deletar `RateLimitMiddleware.cs`, `SlidingWindow.cs` + `git checkout -- Program.cs` |
| 10 | N/A (apenas auditoria — sem alterações permanentes) |
| 11 | `git checkout -- GlobalExceptionHandlerMiddleware.cs` |
| 12 | Deletar script de migração criado (se houver) |
| 13 | `git checkout -- tests/SegurancaTests.cs` |

### Breaking Change: Senhas Existentes

Após o deploy desta spec, TODAS as senhas armazenadas em texto plano no banco DEIXARÃO de funcionar para login — `BCrypt.Verify("senha_plana", "hash_bcrypt_inexistente")` lançará `SaltParseException` (convertida para 401).

**Mitigação (se houver usuários reais no banco):**
- Executar script de migração que faz hash de todas as senhas plain-text existentes:
  ```sql
  -- APENAS se Senha NÃO começa com $2a$ (senhas ainda em plain-text)
  -- Este script deve ser executado UMA vez, ANTES do deploy
  -- NÃO é idempotente — se executado 2x, fará hash do hash
  ```
- OU: instruir usuários a usar "esqueci minha senha" (spec 180) no primeiro login pós-deploy.
- Para ambiente de desenvolvimento: recriar banco (DbUp executa seed com hash).

**Decisão:** Para o escopo atual (MVP, sem usuários reais), recriar o banco de desenvolvimento é a abordagem recomendada. Nenhum script de migração de senhas é necessário.
