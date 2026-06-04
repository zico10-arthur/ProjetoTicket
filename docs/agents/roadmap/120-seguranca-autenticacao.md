# 120 — Segurança e Autenticação

> **Origem:** Pontos críticos [#1](../../sprints.md), [#3](../../sprints.md), [#4](../../sprints.md) do `sprints.md`
>
> **Problema:** [`visao.md#2`](../../visao.md#2-problema) — Ter autonomia com segurança: senhas vazadas, chave JWT exposta e força bruta comprometem todos os usuários da plataforma.

---

## 120.1 O que resolve

| Ponto crítico | Risco | Solução |
|---|---|---|
| #1 Senhas em texto plano | Vazamento de banco = todas as senhas expostas | BCrypt.Net-Next |
| #3 Login compara senha sem hash | Autenticação quebrada, nunca funciona com hash | `BCrypt.Verify()` |
| #4 Jwt:Key exposta no `appsettings.json` | Chave commitada no Git = qualquer pessoa forja tokens | `dotnet user-secrets` |
| Rate limit ausente | Força bruta no login sem barreira | Middleware 5 req/min por IP |

---

## 120.2 BCrypt — Hash de Senhas

### Instalação

```bash
dotnet add package BCrypt.Net-Next
```

### Cadastro

```csharp
// UsuarioRepository.CadastrarUsuario()
var senhaHash = BCrypt.HashPassword(dto.Senha);  // work factor 11 (padrão)
// INSERT INTO Usuarios (..., Senha) VALUES (..., @senhaHash)
```

### Login

```csharp
// UsuarioService.Login()
var usuario = _repo.BuscarPorEmail(dto.Email);
if (usuario == null || !BCrypt.Verify(dto.Senha, usuario.Senha))
    throw new CredenciaisInvalidasException();  // 401
```

### Admin Seed

```sql
-- Senha NUNCA em texto plano no script
INSERT INTO Usuarios (Cpf, Nome, Email, PerfilId, Senha)
VALUES ('00000000000', 'Admin', 'admin@soldout.com',
        'A1A1A1A1-A1A1-A1A1-A1A1-A1A1A1A1A1A1',
        '$2a$11$...hash...gerado...previamente...');
```

---

## 120.3 Chave JWT — `dotnet user-secrets`

### Configuração

```bash
dotnet user-secrets set "Jwt:Key" "chave-segura-de-produção"
dotnet user-secrets set "Jwt:Issuer" "SoldOutTickets"
dotnet user-secrets set "Jwt:Audience" "SoldOutClients"
```

### `Program.cs`

```csharp
if (builder.Environment.IsDevelopment())
    builder.Configuration.AddUserSecrets<Program>();

var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(...)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            // ...
        };
    });
```

### `.gitignore`

```
# Segurança
appsettings.*.json  # exceto templates sem secrets
secrets.json
```

---

## 120.4 Rate Limiting — Login

```
POST /api/usuario/login
Limite: 5 requisições por minuto por IP
```

### Middleware

```csharp
public class RateLimitMiddleware
{
    private static readonly ConcurrentDictionary<string, SlidingWindow> _clients = new();

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path == "/api/usuario/login" && context.Request.Method == "POST")
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var window = _clients.GetOrAdd(ip, _ => new SlidingWindow());

            if (!window.TryIncrement(limit: 5, interval: TimeSpan.FromMinutes(1)))
            {
                context.Response.StatusCode = 429;
                await context.Response.WriteAsync("Muitas tentativas. Aguarde 1 minuto.");
                return;
            }
        }
        await _next(context);
    }
}
```

---

## 120.5 Validações e Respostas

| Código | Caso |
|--------|------|
| `401 Unauthorized` | Email ou senha inválidos |
| `401 Unauthorized` | Usuário inativo (`Ativo = false`) |
| `429 Too Many Requests` | Excedeu 5 tentativas/minuto no login |
