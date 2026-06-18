---
name: "Segurança (BCrypt, JWT, Rate Limit)"
status: "audited"
references:
  - "requirements.md (este diretório)"
  - "design.md (este diretório)"
---

# Spec 120: Segurança (BCrypt, JWT, Rate Limit) — Implementation Tasks

## Task Order

Tasks MUST be executed sequentially in numerical order. Each task depends on the completion of all preceding tasks.

```
Task 1 (BCrypt package)
  ├──→ Task 2 (hash no cadastro)
  ├──→ Task 3 (Verify no login)
  │     └──→ Task 11 (GlobalExceptionHandler — depende de CredenciaisInvalidasException do Task 3)
  └──→ Task 4 (seed admin com hash)

Task 5 (user-secrets + Program.cs)
  ├──→ Task 6 (.gitignore)
  └──→ Task 10 (varrer hardcoded keys)

Task 7 (SlidingWindow)
  └──→ Task 8 (RateLimitMiddleware)
        └──→ Task 9 (registrar middleware no pipeline)

Task 12 (verificar Senha column size) — independente, pode ser feito a qualquer momento

Tasks 1,2,3,4,5,6,7,8,9,10,11,12 → Task 13 (testes — deve ser a ÚLTIMA task)
```

---

## Tasks

### Task 1: Install BCrypt.Net-Next Package

**Objective:** Add the BCrypt password hashing library to the project.

**Requirements Covered:** FR-001 (dependency), NFR-001 (work factor 11)

**Design References:** design.md — Component 1, Component 2

**Actions:**
1. Open terminal at project root (`/c/Users/rezen/OneDrive/Desktop/ProjetoTicket`)
2. Run: `dotnet add Infraestructure/Infraestructure.csproj package BCrypt.Net-Next`
3. Run: `dotnet add Application/Application.csproj package BCrypt.Net-Next`
4. Run: `dotnet restore`
5. Run: `dotnet build`

**Validation:**
- [ ] `dotnet build` succeeds with 0 errors
- [ ] `Infraestructure/Infraestructure.csproj` contains `<PackageReference Include="BCrypt.Net-Next" Version="..." />`
- [ ] `Application/Application.csproj` contains `<PackageReference Include="BCrypt.Net-Next" Version="..." />`
- [ ] `dotnet list Infraestructure/Infraestructure.csproj package` lists `BCrypt.Net-Next`
- [ ] `dotnet list Application/Application.csproj package` lists `BCrypt.Net-Next`

**Status:** [x] done

---

### Task 2: Apply BCrypt.HashPassword in UsuarioRepository.CadastrarUsuario()

**Objective:** Hash passwords with BCrypt before inserting into the database.

**Requirements Covered:** FR-001

**Design References:** design.md — Component 1

**Actions:**
1. Open file `Infraestructure/Repository/UsuarioRepository.cs`
2. Add `using BCrypt.Net;` at the top of the file
3. Locate method `CadastrarUsuario(Usuario usuario, CancellationToken ct)`
4. Find the line where `usuario.Senha` is passed as parameter to `ExecuteAsync`
5. BEFORE that line, add: `var senhaHash = BCrypt.Net.BCrypt.HashPassword(usuario.Senha);`
6. Replace `Senha = usuario.Senha` with `Senha = senhaHash` in the anonymous object
7. Save the file
8. Run: `dotnet build Infraestructure/Infraestructure.csproj`

**Validation:**
- [ ] `dotnet build` succeeds with 0 errors and 0 warnings
- [ ] Code review: `BCrypt.HashPassword()` is called exactly once, before the INSERT
- [ ] Code review: `usuario.Senha` original is NOT passed to Dapper
- [ ] Code review: no `using BCrypt.Net;` in Domain project files

**Status:** [x] done

---

### Task 3: Apply BCrypt.Verify in UsuarioService.Login()

**Objective:** Verify passwords using BCrypt instead of direct string comparison.

**Requirements Covered:** FR-002, FR-003, FR-007

**Design References:** design.md — Component 2, Data Flow 2

**Actions:**
1. Open file `Application/Service/UsuarioService.cs`
2. Add `using BCrypt.Net;` at the top of the file
3. Locate method `Login(LoginRequestDTO dto, CancellationToken ct)`
4. Find the existing password comparison (likely `usuario.Senha != dto.Senha` or similar)
5. Replace the comparison logic with the following block:

```csharp
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

6. **Verify/add data annotations on `LoginRequestDTO` (FR-007):**
   - Open `Application/DTOs/LoginRequestDTO.cs`
   - Ensure `Email` has `[Required]` and `[EmailAddress]` attributes
   - Ensure `Senha` has `[Required]` attribute
   - If any are missing, add them
7. Ensure the JWT generation code AFTER these checks remains unchanged
8. Save the file
9. Run: `dotnet build Application/Application.csproj`

**Validation:**
- [ ] `dotnet build` succeeds with 0 errors
- [ ] Code review: no direct string comparison of passwords (`==`, `!=`, `.Equals()`)
- [ ] Code review: all 3 failure paths (null, Verify=false, !Ativo) throw the SAME exception type with the SAME message
- [ ] Code review: `SaltParseException` is caught and converted, never propagated
- [ ] Code review: JWT generation is NOT inside any of the failure blocks
- [ ] `LoginRequestDTO.Email` has `[Required]` and `[EmailAddress]` attributes
- [ ] `LoginRequestDTO.Senha` has `[Required]` attribute

**Status:** [x] done

---

### Task 4: Update Admin Seed Script with BCrypt Hash

**Objective:** Replace plain-text admin password in the seed SQL script with a pre-generated BCrypt hash.

**Requirements Covered:** FR-006

**Design References:** design.md — Component 1 (Internal Logic, section "Admin Seed")

**Actions:**
1. Generate a BCrypt hash for the password `Admin@123`:
   - Option A: Create a temporary C# console app: `Console.WriteLine(BCrypt.Net.BCrypt.HashPassword("Admin@123"));`
   - Option B: Use an online BCrypt generator with work factor 11
   - Option C: Add a one-time debug line in Program.cs, run the API, capture the output, then remove the line
2. Copy the generated hash (starts with `$2a$11$...`, 60 characters)
3. Open file `db/Script0001_SeedAdmin.sql` (or the file containing `INSERT INTO Usuarios ... admin@soldout.com`)
4. Replace the plain-text password in the INSERT statement with the BCrypt hash
5. Ensure the script has `IF NOT EXISTS (SELECT 1 FROM Usuarios WHERE Email = 'admin@soldout.com')` guard
6. Add a SQL comment above the INSERT indicating the original password for dev reference:
   ```sql
   -- Admin seed: email=admin@soldout.com, senha=Admin@123 (hash BCrypt abaixo)
   ```
7. Save the file

**Validation:**
- [ ] Script file does NOT contain the string `'Admin@123'` as a value being inserted into `Senha`
- [ ] Script file contains a string starting with `$2a$11$` in the Senha value
- [ ] Script still contains `IF NOT EXISTS` guard for idempotency
- [ ] `git diff db/` shows the plain-text password removed and BCrypt hash added

**Status:** [x] done

---

### Task 5: Configure dotnet user-secrets and Update Program.cs

**Objective:** Remove JWT key from appsettings.json; load from user-secrets in development.

**Requirements Covered:** FR-004, NFR-004

**Design References:** design.md — Component 3, Data Flow 4

**Actions:**
1. **Edit `Api/appsettings.json`:**
   - Locate `"Jwt"` section
   - Change `"Key": "<valor atual>"` to `"Key": ""` (empty string)
   - Keep `"Issuer"` and `"Audience"` as they are

2. **Edit `Api/Program.cs` — Add user-secrets (after `var builder = ...` line):**
   ```csharp
   if (builder.Environment.IsDevelopment())
   {
       builder.Configuration.AddUserSecrets<Program>();
   }
   ```

3. **Edit `Api/Program.cs` — Add Jwt:Key validation (before `AddAuthentication`):**
   ```csharp
   var jwtKey = builder.Configuration["Jwt:Key"];
   if (string.IsNullOrEmpty(jwtKey))
   {
       throw new InvalidOperationException(
           "Jwt:Key não configurada. Em desenvolvimento, execute:\n" +
           "  dotnet user-secrets set \"Jwt:Key\" \"<sua-chave-secreta-de-32-caracteres>\"\n" +
           "  dotnet user-secrets set \"Jwt:Issuer\" \"SoldOutTickets\"\n" +
           "  dotnet user-secrets set \"Jwt:Audience\" \"SoldOutClients\"");
   }
   ```

4. **Edit `Api/Program.cs` — Update JWT Bearer config to use `jwtKey` variable:**
   - Find `IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(...))`
   - Replace the argument with `jwtKey` (the variable from step 3)

5. **Configure user-secrets manually (execute once in terminal):**
   ```bash
   cd Api
   dotnet user-secrets set "Jwt:Key" "<generate-a-32-char-random-string>"
   dotnet user-secrets set "Jwt:Issuer" "SoldOutTickets"
   dotnet user-secrets set "Jwt:Audience" "SoldOutClients"
   ```

6. Run: `dotnet build`

**Validation:**
- [ ] `Api/appsettings.json` has `"Key": ""` (empty string)
- [ ] `grep -r "Jwt.*Key" Api/appsettings.json` shows only empty string or placeholder
- [ ] `dotnet run` starts successfully (with user-secrets configured)
- [ ] `dotnet run` FAILS with clear error message if user-secrets are NOT configured
- [ ] JWT token from login endpoint is valid (can be decoded at jwt.io)

**Status:** [x] done

---

### Task 6: Update .gitignore

**Objective:** Prevent accidental commit of files containing secrets.

**Requirements Covered:** FR-004, NFR-004

**Design References:** design.md — Component 3, File Layout

**Actions:**
1. Open `.gitignore` at project root
2. Add the following lines at the end:
   ```
   # Segurança — não versionar secrets
   appsettings.*.json
   secrets.json
   ```
3. If `Api/appsettings.json` is already tracked by Git:
   ```bash
   git rm --cached Api/appsettings.json
   ```
   (The file stays on disk but is removed from Git tracking)
4. Commit: `git add .gitignore && git commit -m "chore: add appsettings.*.json and secrets.json to .gitignore"`

**Validation:**
- [ ] `.gitignore` contains both `appsettings.*.json` and `secrets.json` entries
- [ ] `git status` does NOT show `Api/appsettings.json` as modified/tracked
- [ ] `git ls-files` does NOT include `Api/appsettings.json`

**Status:** [x] done

---

### Task 7: Create SlidingWindow Class

**Objective:** Implement the sliding window algorithm for rate limiting.

**Requirements Covered:** FR-005, NFR-002

**Design References:** design.md — Component 4

**Actions:**
1. Verify directory exists: `Api/Middleware/`. If not, create it.
2. Create file `Api/Middleware/SlidingWindow.cs` with EXACTLY this content:

```csharp
using System.Collections.Concurrent;

namespace Api.Middleware;

public class SlidingWindow
{
    private readonly ConcurrentQueue<DateTime> _timestamps = new();

    public bool TryIncrement(int limit, TimeSpan interval)
    {
        var now = DateTime.UtcNow;
        var cutoff = now - interval;

        while (_timestamps.TryPeek(out var oldest) && oldest < cutoff)
            _timestamps.TryDequeue(out _);

        if (_timestamps.Count >= limit)
            return false;

        _timestamps.Enqueue(now);
        return true;
    }
}
```

3. Run: `dotnet build Api/Api.csproj`

**Validation:**
- [ ] `dotnet build` succeeds with 0 errors
- [ ] File exists at `Api/Middleware/SlidingWindow.cs`
- [ ] Class is `public`, namespace is `Api.Middleware`
- [ ] Method signature: `public bool TryIncrement(int limit, TimeSpan interval)`

**Status:** [x] done

---

### Task 8: Create RateLimitMiddleware

**Objective:** Implement middleware that applies rate limiting to the login endpoint.

**Requirements Covered:** FR-005, EC-004, EC-005, EC-006

**Design References:** design.md — Component 5, Data Flow 3

**Actions:**
1. Create file `Api/Middleware/RateLimitMiddleware.cs` with EXACTLY this content:

```csharp
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.Extensions;

namespace Api.Middleware;

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
        if (context.Request.Path.StartsWithSegments("/api/usuario/login", StringComparison.OrdinalIgnoreCase)
            && HttpMethods.IsPost(context.Request.Method))
        {
            var ip = GetClientIp(context);
            var window = _clients.GetOrAdd(ip, _ => new SlidingWindow());

            if (!window.TryIncrement(Limit, Interval))
            {
                context.Response.StatusCode = 429;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync(
                    "{\"message\":\"Muitas tentativas. Aguarde 1 minuto.\"}");
                return;
            }
        }

        await _next(context);
    }

    private static string GetClientIp(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
```

2. Run: `dotnet build Api/Api.csproj`

**Validation:**
- [ ] `dotnet build` succeeds with 0 errors
- [ ] File exists at `Api/Middleware/RateLimitMiddleware.cs`
- [ ] Only applies to `POST /api/usuario/login` (hardcoded path check)
- [ ] Respects `X-Forwarded-For` header
- [ ] Fallback to `RemoteIpAddress` with `"unknown"` as last resort

**Status:** [x] done

---

### Task 9: Register RateLimitMiddleware in the HTTP Pipeline

**Objective:** Add the rate limiting middleware to the ASP.NET Core request pipeline.

**Requirements Covered:** FR-005

**Design References:** design.md — Component 3 (Seção 4), Component 5 (Registration)

**Actions:**
1. Open `Api/Program.cs`
2. Locate the line `app.UseAuthentication();`
3. Add BEFORE it: `app.UseMiddleware<RateLimitMiddleware>();`
4. Ensure the order is:
   ```csharp
   app.UseMiddleware<GlobalExceptionHandlerMiddleware>();  // se existir
   app.UseMiddleware<RateLimitMiddleware>();                 // NOVO
   app.UseAuthentication();
   app.UseAuthorization();
   app.MapControllers();
   ```
5. Run: `dotnet build`

**Validation:**
- [ ] `dotnet build` succeeds
- [ ] `RateLimitMiddleware` is registered BEFORE `UseAuthentication()`
- [ ] `RateLimitMiddleware` is registered AFTER `GlobalExceptionHandlerMiddleware` (if it exists)

**Status:** [x] done

---

### Task 10: Scan and Remove Hardcoded JWT Keys

**Objective:** Find and eliminate any remaining hardcoded JWT keys in the codebase.

**Requirements Covered:** FR-004

**Design References:** design.md — Component 3

**Actions:**
1. Search for `Jwt:Key` in all `.cs` and `.json` files:
   ```bash
   grep -r "Jwt.*Key" --include="*.cs" --include="*.json" Api/ Application/ Infraestructure/
   ```
2. Search for hardcoded signing keys:
   ```bash
   grep -r "SymmetricSecurityKey" --include="*.cs" Api/ Application/ Infraestructure/
   ```
3. Search for any string literals that look like JWT secrets (32+ char alphanumeric strings near JWT config)
4. For each finding, determine if it's:
   - A configuration read (`builder.Configuration["Jwt:Key"]`) → OK, keep
   - A validation check (`string.IsNullOrEmpty(jwtKey)`) → OK, keep
   - A hardcoded value (`"my-secret-key"`) → REPLACE with `builder.Configuration["Jwt:Key"]`
5. Fix any hardcoded values found
6. Run: `dotnet build`

**Validation:**
- [ ] No hardcoded JWT key strings found in source files
- [ ] All `SymmetricSecurityKey` usages reference `builder.Configuration["Jwt:Key"]` (or the `jwtKey` variable)
- [ ] `dotnet build` succeeds

**Status:** [x] done

---

### Task 11: Verify/Update GlobalExceptionHandlerMiddleware for CredenciaisInvalidasException

**Objective:** Ensure `CredenciaisInvalidasException` maps to HTTP 401 with the standard body `{"message":"Email ou senha inválidos."}`.

**Requirements Covered:** FR-003, EC-003

**Design References:** design.md — Component 6

**Actions:**
1. Open `Api/Middleware/GlobalExceptionHandlerMiddleware.cs` (if it exists; if not, find the exception handling middleware)
2. Check if there is a `catch` block for `CredenciaisInvalidasException`
3. If it exists, verify:
   - Status code is set to `401`
   - Content type is `"application/json; charset=utf-8"`
   - Body is EXACTLY `{"message":"Email ou senha inválidos."}`
4. If it does NOT exist, add the catch block BEFORE the generic `catch (Exception)` block:
   ```csharp
   catch (CredenciaisInvalidasException ex)
   {
       context.Response.StatusCode = 401;
       context.Response.ContentType = "application/json; charset=utf-8";
       await context.Response.WriteAsync("{\"message\":\"Email ou senha inválidos.\"}");
   }
   ```
5. Run: `dotnet build`

**Validation:**
- [ ] `CredenciaisInvalidasException` is caught in the global exception handler
- [ ] Response status is 401
- [ ] Response body is EXACTLY `{"message":"Email ou senha inválidos."}` (byte-exact, including quotes and period)
- [ ] `dotnet build` succeeds

**Status:** [x] done

---

### Task 12: Verify Usuarios.Senha Column Size

**Objective:** Ensure the `Senha` column in the `Usuarios` table supports BCrypt hashes (60 characters minimum).

**Requirements Covered:** FR-001

**Design References:** design.md — Data Structures (Database section)

**Actions:**
1. Open the database creation/migration scripts in `db/` directory
2. Find the `CREATE TABLE Usuarios` statement (or `ALTER TABLE Usuarios` if the table pre-exists)
3. Check the definition of the `Senha` column:
   - If `VARCHAR(100)` or larger (e.g., `NVARCHAR(255)`) → OK, no change needed
   - If `VARCHAR(50)` or smaller → create a new DbUp migration script to alter the column
4. If migration is needed, create `db/ScriptXXXX_AlterSenhaColumn.sql`:
   ```sql
   IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
              WHERE TABLE_NAME = 'Usuarios' AND COLUMN_NAME = 'Senha'
              AND CHARACTER_MAXIMUM_LENGTH < 100)
   BEGIN
       ALTER TABLE Usuarios ALTER COLUMN Senha VARCHAR(100) NOT NULL;
   END
   ```
5. Run: `dotnet build`

**Validation:**
- [ ] `Senha` column is `VARCHAR(100)` or larger in the schema
- [ ] If migration was needed, DbUp script number is sequential and follows existing naming convention

**Status:** [x] done

---

### Task 13: Write Automated Tests

**Objective:** Create comprehensive test coverage for all security requirements.

**Requirements Covered:** FR-001, FR-002, FR-003, FR-004, FR-005 (automated); FR-006, FR-007 (manual/framework — ver abaixo)

**Design References:** design.md — Testing Strategy

**Actions:**
1. Create file `tests/SegurancaTests.cs`
2. Implement the following 14 test methods (see design.md Testing Strategy for exact scenarios):

**Unit Tests (using Moq for dependencies):**

| Test Method | FR |
|-------------|-----|
| `CadastrarUsuario_SenhaHasheada` — verifica hash `$2a$11$` e length ≥ 60 | FR-001 |
| `CadastrarUsuario_SenhaNaoIgualOriginal` — hash != original | FR-001 |
| `Login_SenhaCorreta_RetornaJWT` — BCrypt.Verify OK → Token não-nulo | FR-002 |
| `Login_SenhaIncorreta_LancaExcecao` — BCrypt.Verify false → CredenciaisInvalidasException | FR-002, FR-003 |
| `Login_EmailInexistente_LancaExcecao` — null user → mesma exceção e mensagem | FR-003 |
| `Login_UsuarioInativo_LancaExcecao` — Ativo=false → mesma exceção e mensagem | FR-003 |
| `Login_SaltParseException_LancaCredenciaisInvalidas` — hash corrupto → mesma exceção | EC-003 |

**Integration Tests (using WebApplicationFactory<Program>):**

| Test Method | FR |
|-------------|-----|
| `RateLimit_5Requisicoes_OK` — 5 POSTs → todos sem 429 | FR-005 |
| `RateLimit_6aRequisicao_429` — 6º POST → 429 com body exato | FR-005 |
| `RateLimit_OutroEndpoint_SemRateLimit` — POST cadastrar → sem rate limit | FR-005 |
| `RateLimit_ResetaApos1Min` — espera 61s → permitido novamente | FR-005, NFR-002 |
| `Login_Resposta401_FormatoCorreto` — body `{"message":"Email ou senha inválidos."}` | FR-003 |
| `Login_Resposta401_BodyIdentico` — 3 cenários = 1 body byte-idêntico | FR-003 |
| `Program_SemJwtKey_FalhaStartup` — sem Jwt:Key → InvalidOperationException | FR-004 |

3. Run: `dotnet test`

**Validation:**
- [ ] All 14 tests pass (green)
- [ ] `dotnet test` output shows "14 Passed, 0 Failed, 0 Skipped"
- [ ] No tests are flaky (run twice, both pass)

**Status:** [x] done

---

## Summary

| # | Layer | File(s) | Action | FRs Covered | Est. Time |
|---|-------|---------|--------|-------------|-----------|
| 1 | Infra/App | `Infraestructure.csproj`, `Application.csproj` | Add BCrypt.Net-Next package | FR-001 | 5 min |
| 2 | Infrastructure | `UsuarioRepository.cs` | Hash password on persist | FR-001 | 15 min |
| 3 | Application | `UsuarioService.cs`, `LoginRequestDTO.cs` | BCrypt.Verify on login + data annotations | FR-002, FR-003, FR-007 | 25 min |
| 4 | db | `Script0001_SeedAdmin.sql` | BCrypt hash in seed | FR-006 | 10 min |
| 5 | Api | `Program.cs`, `appsettings.json` | user-secrets + validation | FR-004 | 20 min |
| 6 | Root | `.gitignore` | Add secrets patterns | FR-004, NFR-004 | 5 min |
| 7 | Api | `SlidingWindow.cs` (NEW) | Sliding window algorithm | FR-005, NFR-002 | 10 min |
| 8 | Api | `RateLimitMiddleware.cs` (NEW) | Rate limit middleware | FR-005 | 20 min |
| 9 | Api | `Program.cs` | Register middleware | FR-005 | 5 min |
| 10 | All | Various `.cs` files | Remove hardcoded JWT keys | FR-004 | 15 min |
| 11 | Api | `GlobalExceptionHandlerMiddleware.cs` | Map CredenciaisInvalidas → 401 | FR-003 | 10 min |
| 12 | db | Migration script (maybe) | Verify Senha column size | FR-001 | 10 min |
| 13 | tests | `SegurancaTests.cs` (NEW) | 14 automated tests (FR-001~005) + manual checks (FR-006, FR-007) | FR-001~007 | 1h 30min |

**Total estimated time:** ~4h 00min

**Total files created:** 3 (`SlidingWindow.cs`, `RateLimitMiddleware.cs`, `SegurancaTests.cs`)

**Total files modified:** 9 (`Infraestructure.csproj`, `Application.csproj`, `UsuarioRepository.cs`, `UsuarioService.cs`, `LoginRequestDTO.cs`, `Program.cs`, `appsettings.json`, `.gitignore`, `Script0001_SeedAdmin.sql`)

**Total files verified (may be modified):** 2 (`GlobalExceptionHandlerMiddleware.cs`, DB migration for Senha column)
