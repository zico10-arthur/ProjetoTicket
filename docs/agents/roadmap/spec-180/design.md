# Spec 180: Serviço de E-mail Transacional + Redefinição de Senha — Design

> **Requirements:** [`requirements.md`](./requirements.md)
> **Arquitetura:** [Clean Architecture](../../arquitetura.md) | [ADR-013](../../ADR.md#adr-013-mailkit--channel-para-e-mails-transacionais-spec-180)
> **Projeto Base:** .NET 9 + Dapper + SQL Server + DbUp + JWT Bearer + BCrypt

---

## Design Approach

**High-level strategy:** Criar infraestrutura de e-mail não-bloqueante usando o padrão Producer-Consumer com `Channel<T>`. A interface `IEmailSender` reside no Domain (Clean Architecture); a implementação concreta fica no Infrastructure (MailKit + Channel + BackgroundService). Templates são strings constantes (classe estática) para máxima simplicidade. O fluxo de redefinição de senha estende `TokenService` e `UsuarioService` existentes com novos métodos, reutilizando a stack JWT/BCrypt já auditada.

**Princípios aplicados:**
- **Fire-and-forget:** HTTP request enfileira e retorna; SMTP acontece em background.
- **Graceful degradation:** Sem SMTP configurado → sistema opera normalmente, e-mails descartados com warning.
- **Retry with backoff:** 3 tentativas (2s, 4s, 8s) para falhas transitórias de rede.
- **Security-first na redefinição:** Token JWT com `purpose=password-reset` impede reuso como token de autenticação. `esqueci-senha` sempre retorna 200 OK (anti-enumeração).

---

## Architecture Decisions

| # | Decisão | Justificativa |
|---|---------|---------------|
| **D1** | `IEmailSender` no Domain, implementação no Infrastructure | ADR-001 (Clean Architecture). Domain não conhece MailKit/SMTP. |
| **D2** | `Channel<EmailMessage>` bounded (100) com `FullMode = Wait` | ADR-013. Limita memória em picos. Backpressure natural — se a fila encher, o producer HTTP aguarda. |
| **D3** | `EmailBackgroundWorker` implementa `IEmailSender` | Permite que a mesma instância do worker seja injetada nos services como `IEmailSender`. Producers e consumer compartilham o mesmo Channel. |
| **D4** | `EmailBackgroundWorker` registrado como singleton + `IHostedService` | Garante que a fila (Channel) é única. O `AddHostedService` inicia o `ExecuteAsync`. |
| **D5** | `SmtpEmailSender` registrado como scoped | Resolvido via `IServiceScopeFactory` dentro do worker (que é singleton). Cada tentativa de envio tem seu próprio scope. |
| **D6** | Token de redefinição usa mesma chave JWT (`Jwt:Key`) | ADR-004. A diferenciação é via claim `purpose`, não via chave separada. Simplifica configuração. |
| **D7** | Token de redefinição sem claims `cpf`, `perfilId`, `role` | Segurança. Impede que o token seja usado como token de acesso. O `[Authorize]` middleware rejeita por falta de claim `role`. |
| **D8** | Templates como strings constantes em classe estática | KISS. Sem dependência de Razor/Liquid. Fácil migrar para template engine no futuro se necessário. |
| **D9** | `UsuarioService` injeta `IConfiguration` para ler `App:BaseUrl` | Evita criar um settings class dedicado para uma única chave. Consistente com `TokenService` que já injeta `IConfiguration`. |

---

## Component / Module Breakdown

### Component 1: `EmailMessage` (Value Object — Domain)

**Purpose:** Encapsular os dados de um e-mail. Imutável. Validação no construtor.

**File:** `Domain/ValueObjects/EmailMessage.cs`

**Interface:**
```csharp
namespace Domain.ValueObjects;

public class EmailMessage
{
    public string Destinatario { get; }
    public string Assunto { get; }
    public string CorpoHtml { get; }
    public string CorpoTexto { get; }

    public EmailMessage(string destinatario, string assunto, string corpoHtml, string corpoTexto);
}
```

**Internal Logic:**
1. Construtor valida: `string.IsNullOrWhiteSpace(destinatario)` → `throw new ArgumentException("Destinatário é obrigatório.", nameof(destinatario))`.
2. Construtor valida: `string.IsNullOrWhiteSpace(assunto)` → `throw new ArgumentException("Assunto é obrigatório.", nameof(assunto))`.
3. `CorpoHtml` e `CorpoTexto`: se null, armazena `string.Empty`.
4. Todas as 4 propriedades são `{ get; }` (read-only auto-properties, sem setter privado — C# 9+ permite init-only, mas para compatibilidade usar get-only constructor-assigned).

**Dependencies:** Nenhuma. Não referencia nenhum pacote externo.

**Error Handling:** `ArgumentException` no construtor se destinatário ou assunto vazios.

---

### Component 2: `IEmailSender` (Interface — Domain)

**Purpose:** Contrato para enfileiramento de e-mails. Todos os services dependem desta interface.

**File:** `Domain/Interface/IEmailSender.cs`

**Interface:**
```csharp
namespace Domain.Interface;

public interface IEmailSender
{
    /// <summary>
    /// Enfileira um e-mail para envio assíncrono em background.
    /// Retorna imediatamente — o envio real ocorre em background.
    /// </summary>
    ValueTask EnfileirarAsync(Domain.ValueObjects.EmailMessage email, CancellationToken ct);
}
```

**Dependencies:** Referencia `Domain.ValueObjects.EmailMessage`.

---

### Component 3: `SmtpSettings` (Configuration POCO — Infrastructure)

**Purpose:** Mapear seção `"Smtp"` do `appsettings.json` para um objeto tipado.

**File:** `Infraestructure/Email/SmtpSettings.cs`

**Interface:**
```csharp
namespace Infraestructure.Email;

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromName { get; set; } = "SoldOut Tickets";
    public string FromAddress { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
}
```

**Dependencies:** Nenhuma. POCO puro.

**Registration:** `builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));` em `Api/Program.cs`.

**Configuração em `Api/appsettings.json`:**
```json
"Smtp": {
    "Host": "",
    "Port": 587,
    "Username": "",
    "Password": "",
    "FromName": "SoldOut Tickets",
    "FromAddress": "",
    "EnableSsl": true
}
```

**Error Handling:** Se a seção `"Smtp"` estiver ausente, a classe assume os defaults (Host="", Port=587, etc.). `SmtpEmailSender.Configurado` verifica `Host` e `FromAddress` antes de tentar enviar.

---

### Component 4: `SmtpEmailSender` (SMTP Client — Infrastructure)

**Purpose:** Enviar e-mails via SMTP usando MailKit. Classe de baixo nível — não deve ser usada diretamente pelos services (use `IEmailSender`).

**File:** `Infraestructure/Email/SmtpEmailSender.cs`

**Interface:**
```csharp
namespace Infraestructure.Email;

public class SmtpEmailSender
{
    public SmtpEmailSender(IOptions<SmtpSettings> settings, ILogger<SmtpEmailSender> logger);

    public bool Configurado { get; }  // true se Host e FromAddress não vazios

    public async Task EnviarAsync(Domain.ValueObjects.EmailMessage email, CancellationToken ct);
}
```

**Internal Logic — `Configurado`:**
```
return !string.IsNullOrWhiteSpace(_settings.Host)
    && !string.IsNullOrWhiteSpace(_settings.FromAddress);
```

**Internal Logic — `EnviarAsync`:**
1. Se `!Configurado` → `_logger.LogWarning("SMTP não configurado. E-mail descartado para {Destinatario}", email.Destinatario); return;`
2. Criar `MimeMessage`:
   - `message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress))`
   - `message.To.Add(MailboxAddress.Parse(email.Destinatario))`
   - `message.Subject = email.Assunto`
3. Criar `BodyBuilder` com `HtmlBody = email.CorpoHtml` e `TextBody = email.CorpoTexto`.
4. `message.Body = builder.ToMessageBody()`
5. Criar `using var client = new SmtpClient()`
6. `await client.ConnectAsync(_settings.Host, _settings.Port, _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, ct)`
7. Se `!string.IsNullOrWhiteSpace(_settings.Username)` → `await client.AuthenticateAsync(_settings.Username, _settings.Password, ct)`
8. `await client.SendAsync(message, ct)`
9. `await client.DisconnectAsync(true, ct)`

**Dependencies:**
- `IOptions<SmtpSettings>` — configuração tipada
- `ILogger<SmtpEmailSender>` — logging
- `MailKit.Net.Smtp.SmtpClient` — envio SMTP
- `MimeKit.MimeMessage`, `MimeKit.BodyBuilder`, `MimeKit.MailboxAddress` — construção da mensagem

**Error Handling:**
- Exceções de rede (DNS, timeout) propagam para o caller (`EmailBackgroundWorker.ProcessarComRetentativa`).
- Não captura exceções internamente — deixa o worker decidir sobre retry.

---

### Component 5: `EmailBackgroundWorker` (Background Service + Fila — Infrastructure)

**Purpose:** Gerenciar a fila de e-mails em memória com `Channel<EmailMessage>` e processar envios com retry.

**File:** `Infraestructure/Email/EmailBackgroundWorker.cs`

**Interface:**
```csharp
namespace Infraestructure.Email;

public class EmailBackgroundWorker : BackgroundService, Domain.Interface.IEmailSender
{
    public EmailBackgroundWorker(IServiceScopeFactory scopeFactory, ILogger<EmailBackgroundWorker> logger);

    // IEmailSender implementation
    public async ValueTask EnfileirarAsync(Domain.ValueObjects.EmailMessage email, CancellationToken ct);

    // BackgroundService implementation
    protected override async Task ExecuteAsync(CancellationToken stoppingToken);

    // Private
    private async Task ProcessarComRetentativa(Domain.ValueObjects.EmailMessage email, CancellationToken ct);
}
```

**Internal Logic — Constructor:**
1. Armazenar `_scopeFactory` e `_logger`.
2. Criar Channel:
```csharp
_fila = Channel.CreateBounded<Domain.ValueObjects.EmailMessage>(
    new BoundedChannelOptions(100)
    {
        FullMode = BoundedChannelFullMode.Wait
    });
```

**Internal Logic — `EnfileirarAsync`:**
```csharp
await _fila.Writer.WriteAsync(email, ct);
```
Única operação. Retorna imediatamente se houver espaço no Channel.

**Internal Logic — `ExecuteAsync`:**
```csharp
await foreach (var email in _fila.Reader.ReadAllAsync(stoppingToken))
{
    await ProcessarComRetentativa(email, stoppingToken);
}
```
`ReadAllAsync` bloqueia até que haja mensagens. Itera até que o Channel seja completado (aplicação shutting down).

**Internal Logic — `ProcessarComRetentativa`:**
```csharp
var tentativas = 3;
for (int i = 0; i < tentativas; i++)
{
    try
    {
        using var scope = _scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<SmtpEmailSender>();
        await sender.EnviarAsync(email, ct);
        _logger.LogInformation("E-mail enviado para {Destinatario}: {Assunto}",
            email.Destinatario, email.Assunto);
        return; // Sucesso → sair
    }
    catch (Exception ex) when (i < tentativas - 1)
    {
        var delay = TimeSpan.FromSeconds(Math.Pow(2, i + 1)); // 2s, 4s, 8s
        _logger.LogWarning(ex,
            "Tentativa {Tentativa}/{Total} falhou para {Destinatario}. Retentando em {Delay}s.",
            i + 1, tentativas, email.Destinatario, delay.TotalSeconds);
        await Task.Delay(delay, ct);
    }
}
_logger.LogError("Falha ao enviar e-mail para {Destinatario} após {Tentativas} tentativas.",
    email.Destinatario, tentativas);
```

**Dependencies:**
- `IServiceScopeFactory` — criar scope por envio (worker é singleton, `SmtpEmailSender` é scoped)
- `ILogger<EmailBackgroundWorker>` — logging
- `System.Threading.Channels` — fila em memória

**Error Handling:**
- Retry 3x com backoff exponencial (2s → 4s → 8s).
- Após 3 falhas, `LogError` e descarta a mensagem (não tenta novamente).
- Exceção no `ExecuteAsync` (ex: `OperationCanceledException` no shutdown) é esperada e tratada pelo `BackgroundService` base.

---

### Component 6: `EmailTemplates` (Templates Estáticos — Infrastructure)

**Purpose:** Fornecer métodos de fábrica que constroem `EmailMessage` com HTML e texto plano para cada tipo de e-mail transacional.

**File:** `Infraestructure/Email/EmailTemplates.cs`

**Interface (6 métodos estáticos):**
```csharp
namespace Infraestructure.Email;

public static class EmailTemplates
{
    public static EmailMessage BoasVindasComprador(string destinatario, string nome);
    public static EmailMessage BoasVindasVendedor(string destinatario, string nomeFantasia, string plano);
    public static EmailMessage ReservaConfirmada(string destinatario, string nomeEvento,
        DateTime dataEvento, int quantidadeItens, decimal valorTotal);
    public static EmailMessage PagamentoConfirmado(string destinatario, string nomeEvento,
        decimal valorPago, string metodo, DateTime dataPagamento);
    public static EmailMessage ReembolsoConfirmado(string destinatario, string nomeEvento,
        decimal valorReembolsado);
    public static EmailMessage RedefinicaoSenha(string destinatario, string nome, string link);
}
```

**Internal Logic — cada método:**
1. Aplicar `System.Net.WebUtility.HtmlEncode()` em todos os valores dinâmicos inseridos no HTML.
2. Construir string HTML com tags básicas (`<h1>`, `<p>`, `<strong>`, `<a>`).
3. Construir string texto plano (fallback) com os mesmos dados, sem tags.
4. Retornar `new EmailMessage(destinatario, assunto, corpoHtml, corpoTexto)`.

**Detalhes de cada template:**

**`BoasVindasComprador`:**
- Assunto: `"Bem-vindo ao SoldOut Tickets!"`
- HTML: `<h1>Bem-vindo, {HtmlEncode(nome)}!</h1><p>Sua conta foi criada com sucesso no <strong>SoldOut Tickets</strong>.</p><p>Você já pode descobrir eventos e comprar ingressos.</p><p>Acesse: <a href="https://soldouttickets.com">soldouttickets.com</a></p><p>— Equipe SoldOut Tickets</p>`
- Texto: `"Bem-vindo, {nome}! Sua conta foi criada com sucesso. Acesse soldouttickets.com"`

**`BoasVindasVendedor`:**
- Assunto: `"Bem-vindo ao SoldOut Tickets — Comece a vender!"`
- HTML: `<h1>Bem-vindo, {HtmlEncode(nomeFantasia)}!</h1><p>Sua conta de <strong>Vendedor</strong> foi criada com sucesso no <strong>SoldOut Tickets</strong>.</p><p>Plano atual: <strong>{HtmlEncode(plano)}</strong></p><p>Acesse seu Painel do Vendedor e crie seu primeiro evento em minutos!</p><p><a href="https://soldouttickets.com/painel">Acessar Painel</a></p><p>— Equipe SoldOut Tickets</p>`
- Texto: `"Bem-vindo, {nomeFantasia}! Sua conta de Vendedor foi criada. Plano: {plano}. Acesse soldouttickets.com/painel"`

**`ReservaConfirmada`:**
- Assunto: `$"Reserva confirmada — {nomeEvento}"`
- HTML: `<h1>Reserva confirmada!</h1><p><strong>Evento:</strong> {HtmlEncode(nomeEvento)}</p><p><strong>Data:</strong> {dataEvento:dd/MM/yyyy HH:mm}</p><p><strong>Participantes:</strong> {quantidadeItens}</p><p><strong>Valor total:</strong> R$ {valorTotal:N2}</p><p>Se for um evento pago, acesse sua conta para confirmar o pagamento.</p><p>— Equipe SoldOut Tickets</p>`
- Texto: `$"Reserva confirmada! Evento: {nomeEvento}, Data: {dataEvento:dd/MM/yyyy}, Valor total: R$ {valorTotal:N2}"`

**`PagamentoConfirmado`:**
- Assunto: `$"Pagamento confirmado — {nomeEvento}"`
- HTML: `<h1>Pagamento confirmado!</h1><p><strong>Evento:</strong> {HtmlEncode(nomeEvento)}</p><p><strong>Valor pago:</strong> R$ {valorPago:N2}</p><p><strong>Método:</strong> {HtmlEncode(metodo)}</p><p><strong>Data do pagamento:</strong> {dataPagamento:dd/MM/yyyy HH:mm}</p><p>Seus ingressos estão garantidos. Aproveite o evento!</p><p>— Equipe SoldOut Tickets</p>`
- Texto: `$"Pagamento confirmado! Evento: {nomeEvento}, Valor: R$ {valorPago:N2}"`

**`ReembolsoConfirmado`:**
- Assunto: `$"Reembolso processado — {nomeEvento}"`
- HTML: `<h1>Reembolso processado</h1><p><strong>Evento:</strong> {HtmlEncode(nomeEvento)}</p><p><strong>Valor reembolsado:</strong> R$ {valorReembolsado:N2}</p><p>O valor será devolvido conforme a política de reembolso da plataforma.</p><p>— Equipe SoldOut Tickets</p>`
- Texto: `$"Reembolso processado. Evento: {nomeEvento}, Valor: R$ {valorReembolsado:N2}"`

**`RedefinicaoSenha`:**
- Assunto: `"Redefinição de senha — SoldOut Tickets"`
- HTML: `<h1>Redefinição de senha</h1><p>Olá, {HtmlEncode(nome)}!</p><p>Recebemos uma solicitação para redefinir sua senha no <strong>SoldOut Tickets</strong>.</p><p>Clique no link abaixo para criar uma nova senha:</p><p><a href="{HtmlEncode(link)}">{HtmlEncode(link)}</a></p><p><strong>Este link é válido por 15 minutos.</strong></p><p>Se você não solicitou esta redefinição, ignore este e-mail.</p><p>— Equipe SoldOut Tickets</p>`
- Texto: `$"Olá, {nome}! Acesse o link para redefinir sua senha: {link}. Válido por 15 minutos."`

**Dependencies:** `Domain.ValueObjects.EmailMessage`.

---

### Component 7: `EsqueciSenhaDTO` e `RedefinirSenhaDTO` (DTOs — Application)

**Purpose:** Modelos de request body para os endpoints de redefinição de senha.

**Files:**
- `Application/DTOs/EsqueciSenhaDTO.cs`
- `Application/DTOs/RedefinirSenhaDTO.cs`

**Interface:**
```csharp
// Application/DTOs/EsqueciSenhaDTO.cs
namespace Application.DTOs;
public record EsqueciSenhaDTO(string Email);

// Application/DTOs/RedefinirSenhaDTO.cs
namespace Application.DTOs;
public record RedefinirSenhaDTO(string Token, string NovaSenha);
```

**Dependencies:** Nenhuma. Records puros.

---

### Component 8: `TokenRedefinicaoInvalido` (Exception — Application)

**Purpose:** Exceção tipada para token de redefinição inválido/expirado.

**File:** `Application/Exceptions/TokenRedefinicaoExceptions.cs`

**Interface:**
```csharp
namespace Application.Exceptions;

public class TokenRedefinicaoInvalido : Exception
{
    public TokenRedefinicaoInvalido()
        : base("Token expirado ou inválido.") { }
}
```

**Dependencies:** Nenhuma. Herda de `System.Exception`.

---

### Component 9: Extensão de `ITokenService` (Interface — Application)

**Purpose:** Adicionar métodos de geração e validação de token de redefinição.

**File:** `Application/Interfaces/ITokenService.cs` (EDITAR — adicionar 2 métodos)

**Novos métodos (adicionar antes do `}` final):**
```csharp
/// <summary>
/// Spec 180: Gera token JWT com 15 min de validade e claim purpose=password-reset.
/// NÃO inclui claims de autenticação (role, cpf, perfilId).
/// </summary>
string GerarTokenRedefinicaoSenha(Domain.Entities.Usuario usuario);

/// <summary>
/// Spec 180: Valida token JWT de redefinição.
/// Retorna email se válido, null se inválido/expirado/purpose errado.
/// NUNCA lança exceção.
/// </summary>
string? ValidarTokenRedefinicaoSenha(string token);
```

---

### Component 10: Extensão de `TokenService` (Implementation — Application)

**Purpose:** Implementar os novos métodos de `ITokenService`.

**File:** `Application/Service/TokenService.cs` (EDITAR — adicionar 2 métodos)

**Método `GerarTokenRedefinicaoSenha`:**
```csharp
public string GerarTokenRedefinicaoSenha(Usuario usuario)
{
    var tokenHandler = new JwtSecurityTokenHandler();
    var secretKey = _configuration["Jwt:Key"];
    if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 32)
        throw new InvalidOperationException("A chave JWT deve ter pelo menos 32 caracteres.");

    var key = Encoding.UTF8.GetBytes(secretKey);

    var claimsIdentity = new ClaimsIdentity(new[]
    {
        new Claim("email", usuario.Email),
        new Claim("purpose", "password-reset"),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    });

    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = claimsIdentity,
        Expires = DateTime.UtcNow.AddMinutes(15),     // 15 min, não 24h
        Issuer = _configuration["Jwt:Issuer"],
        Audience = _configuration["Jwt:Audience"],
        SigningCredentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256Signature)
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    return tokenHandler.WriteToken(token);
}
```

**IMPORTANTE:** Este método NÃO inclui as claims `cpf`, `perfilId`, `role`. Apenas `email`, `purpose`, `jti`. Expiração 15 min vs 24h do `GerarToken`.

**Método `ValidarTokenRedefinicaoSenha`:**
```csharp
public string? ValidarTokenRedefinicaoSenha(string token)
{
    try
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var secretKey = _configuration["Jwt:Key"];

        var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidAudience = _configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
            RoleClaimType = "role",
            NameClaimType = "email"
        }, out _);

        // Verificar claim purpose — dupla validação
        var purpose = principal.Claims.FirstOrDefault(c => c.Type == "purpose")?.Value;
        if (purpose != "password-reset")
            return null;

        return principal.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
    }
    catch
    {
        return null; // Qualquer falha → null (token expirado, assinatura inválida, etc.)
    }
}
```

---

### Component 11: Extensão de `IUsuarioService` (Interface — Application)

**Purpose:** Adicionar métodos de redefinição de senha.

**File:** `Application/Interfaces/IUsuarioService.cs` (EDITAR — adicionar 2 métodos)

**Novos métodos (adicionar antes do `}` final):**
```csharp
/// <summary>
/// Spec 180: Busca usuário por email, gera token JWT de redefinição,
/// enfileira e-mail com link. Se email não existir ou usuário inativo,
/// retorna sem fazer nada (não vaza informação).
/// </summary>
Task SolicitarRedefinicaoSenha(string email, CancellationToken ct);

/// <summary>
/// Spec 180: Valida token JWT de redefinição, valida nova senha,
/// aplica BCrypt e atualiza no banco.
/// Lança TokenRedefinicaoInvalido se token inválido/expirado.
/// Lança exceção de validação se senha fraca.
/// </summary>
Task RedefinirSenha(string token, string novaSenha, CancellationToken ct);
```

---

### Component 12: Extensão de `UsuarioService` (Implementation — Application)

**Purpose:** Integrar e-mails de boas-vindas e implementar redefinição de senha.

**File:** `Application/Service/UsuarioService.cs` (EDITAR)

**Mudanças no construtor:**

Adicionar 2 novos parâmetros e campos:
```csharp
private readonly Domain.Interface.IEmailSender _emailSender;
private readonly IConfiguration _configuration;

public UsuarioService(
    IUsuarioRepository repository,
    IMapper mapper,
    ITokenService tokenservice,
    Domain.Interface.IEmailSender emailSender,     // NOVO
    IConfiguration configuration)                   // NOVO
{
    _repository = repository;
    _mapper = mapper;
    _tokenservice = tokenservice;
    _emailSender = emailSender;                    // NOVO
    _configuration = configuration;                 // NOVO
}
```

**Adicionar using no topo do arquivo:**
```csharp
using Infraestructure.Email;  // Para EmailTemplates
```

**Mudança em `CadastrarComprador`:**

Localizar a linha `_repository.CadastrarUsuario(novocomprador);` (aproximadamente linha 35 do arquivo atual). Após esta linha e antes do fim do método, adicionar:
```csharp
// Spec 180: Enfileirar e-mail de boas-vindas (fire-and-forget)
var email = EmailTemplates.BoasVindasComprador(dto.Email, dto.Nome);
await _emailSender.EnfileirarAsync(email, ct);
```

**Mudança em `CadastrarVendedor`:**

Localizar a linha `_repository.CadastrarVendedor(vendedor);` (aproximadamente linha 68). Após esta linha e antes do `return`, adicionar:
```csharp
// Spec 180: Enfileirar e-mail de boas-vindas
var email = EmailTemplates.BoasVindasVendedor(
    vendedor.Email, vendedor.NomeFantasia!, "Gratuito");
await _emailSender.EnfileirarAsync(email, ct);
```

**Novo método `SolicitarRedefinicaoSenha`:**
```csharp
public async Task SolicitarRedefinicaoSenha(string email, CancellationToken ct)
{
    var usuario = await _repository.BuscarEmail(email, ct);
    if (usuario == null || !usuario.Ativo)
        return; // Não vaza informação

    var token = _tokenservice.GerarTokenRedefinicaoSenha(usuario);

    var baseUrl = _configuration["App:BaseUrl"] ?? "https://soldouttickets.com";
    var link = $"{baseUrl}/redefinir-senha?token={Uri.EscapeDataString(token)}";

    var msg = EmailTemplates.RedefinicaoSenha(usuario.Email, usuario.Nome, link);
    await _emailSender.EnfileirarAsync(msg, ct);
}
```

**Novo método `RedefinirSenha`:**
```csharp
public async Task RedefinirSenha(string token, string novaSenha, CancellationToken ct)
{
    // 1. Validar token JWT
    var email = _tokenservice.ValidarTokenRedefinicaoSenha(token);
    if (email == null)
        throw new TokenRedefinicaoInvalido();

    // 2. Buscar usuário
    var usuario = await _repository.BuscarEmail(email, ct);
    if (usuario == null || !usuario.Ativo)
        throw new TokenRedefinicaoInvalido();

    // 3. Validar senha bruta (lança exceção se inválida)
    Usuario.ValidarSenhaBruta(novaSenha);

    // 4. Hashear e persistir
    string senhaHash = BCrypt.Net.BCrypt.HashPassword(novaSenha);
    usuario.AlterarSenha(senhaHash);
    await _repository.AtualizarSenha(usuario.Cpf, senhaHash, ct);
}
```

---

### Component 13: Extensão de `ReservaService` (Application)

**Purpose:** Enfileirar e-mail de confirmação de reserva.

**File:** `Application/Service/ReservaService.cs` (EDITAR)

**Mudanças no construtor:**

Adicionar 1 novo parâmetro após os 5 existentes:
```csharp
private readonly Domain.Interface.IEmailSender _emailSender;  // NOVO campo

public ReservaService(
    IReservaRepository repositoryReserva,
    IUsuarioRepository repositoryUsuario,
    IEventoRepository repositoryEvento,
    ICupomRepository repositoryCupom,
    IIngressoRepository repositoryIngresso,
    Domain.Interface.IEmailSender emailSender)  // NOVO parâmetro
{
    // ... existentes ...
    _emailSender = emailSender;  // NOVO
}
```

**Adicionar using no topo:**
```csharp
using Infraestructure.Email;
```

**Mudança em `FazerReserva`:**

Localizar a linha `await _repositoryReserva.CadastrarReservaComItens(novaReserva, ct);` (aproximadamente linha 88). Após esta linha e antes do `return novaReserva.Id;`, adicionar:
```csharp
// Spec 180: Enfileirar e-mail de confirmação de reserva
var usuario = await _repositoryUsuario.BuscarCpf(usuarioCpf, ct);
var destinatario = usuario?.Email ?? usuarioCpf; // fallback: CPF se email não encontrado
var nomeEvento = evento.Nome; // 'evento' já está carregado no método
var email = EmailTemplates.ReservaConfirmada(
    destinatario,
    nomeEvento,
    evento.DataEvento,
    itens.Count,
    novaReserva.ValorFinalPago);
await _emailSender.EnfileirarAsync(email, ct);
```

---

### Component 14: Extensão de `PagamentoService` (Application)

**Purpose:** Enfileirar e-mail de confirmação de pagamento.

**File:** `Application/Service/PagamentoService.cs` (EDITAR)

**Mudanças no construtor:**

Adicionar 2 novos parâmetros:
```csharp
private readonly Domain.Interface.IEmailSender _emailSender;     // NOVO
private readonly IUsuarioRepository _usuarioRepo;                 // NOVO

public PagamentoService(
    IPagamentoRepository pagamentoRepo,
    IReservaRepository reservaRepo,
    IEventoRepository eventoRepo,
    Domain.Interface.IEmailSender emailSender,                    // NOVO
    IUsuarioRepository usuarioRepo)                               // NOVO
{
    _pagamentoRepo = pagamentoRepo;
    _reservaRepo = reservaRepo;
    _eventoRepo = eventoRepo;
    _emailSender = emailSender;                                   // NOVO
    _usuarioRepo = usuarioRepo;                                   // NOVO
}
```

**Adicionar using no topo:**
```csharp
using Infraestructure.Email;
```

**Mudança em `ConfirmarCheckout`:**

Localizar a linha `await _pagamentoRepo.CriarComTransacao(pagamento, reserva, evento, ct);` (aproximadamente linha 53). Após esta linha e antes do `return`, adicionar:
```csharp
// Spec 180: Enfileirar e-mail de confirmação de pagamento
var comprador = await _usuarioRepo.BuscarCpf(reserva.UsuarioCpf, ct);
var destinatario = comprador?.Email ?? reserva.UsuarioCpf;
var email = EmailTemplates.PagamentoConfirmado(
    destinatario,
    evento.Nome,
    pagamento.ValorPago,
    metodo,
    pagamento.DataPagamento);
await _emailSender.EnfileirarAsync(email, ct);
```

---

### Component 15: Extensão de `UsuarioController` (API)

**Purpose:** Adicionar endpoints de redefinição de senha.

**File:** `Api/Controllers/UsuarioController.cs` (EDITAR)

**Adicionar using no topo:**
```csharp
using Application.Exceptions;  // Para TokenRedefinicaoInvalido
```

**Novos métodos (adicionar antes do `}` final da classe):**
```csharp
/// <summary>
/// Spec 180: Solicitar redefinição de senha. Sempre retorna 200 OK.
/// </summary>
[HttpPost("esqueci-senha")]
public async Task<IActionResult> EsqueciSenha([FromBody] EsqueciSenhaDTO dto, CancellationToken ct)
{
    await _service.SolicitarRedefinicaoSenha(dto.Email, ct);
    return Ok(new { message = "Se o e-mail estiver cadastrado, um link de redefinição será enviado." });
}

/// <summary>
/// Spec 180: Redefinir senha com token JWT.
/// </summary>
[HttpPost("redefinir-senha")]
public async Task<IActionResult> RedefinirSenha([FromBody] RedefinirSenhaDTO dto, CancellationToken ct)
{
    try
    {
        await _service.RedefinirSenha(dto.Token, dto.NovaSenha, ct);
        return Ok(new { message = "Senha redefinida com sucesso." });
    }
    catch (TokenRedefinicaoInvalido ex)
    {
        return BadRequest(new { message = ex.Message });
    }
    catch (Domain.Exceptions.SenhaInvalida ex)
    {
        return BadRequest(new { message = ex.Message });
    }
    catch (Domain.Exceptions.Senha8digitos ex)
    {
        return BadRequest(new { message = ex.Message });
    }
    catch (Domain.Exceptions.SenhaVazia ex)
    {
        return BadRequest(new { message = ex.Message });
    }
}
```

**Nota:** `UsuarioController` já injeta `_service` (tipo `IUsuarioService`). Não são necessárias mudanças no construtor do controller. As exceções de validação de senha (`SenhaInvalida`, `Senha8digitos`, `SenhaVazia`) do namespace `Domain.Exceptions` são capturadas explicitamente para garantir resposta `400 Bad Request` com a mensagem de validação, em vez de depender implicitamente do `GlobalExceptionHandlerMiddleware`. Se novos tipos de exceção de senha forem adicionados futuramente, adicionar novos `catch` blocks correspondentes.

---

### Component 16: Registro de DI em `Program.cs` (API)

**Purpose:** Registrar todas as novas dependências no container.

**File:** `Api/Program.cs` (EDITAR)

**Adicionar using:**
```csharp
using Infraestructure.Email;
```

**Linhas a adicionar — após os registros `AddScoped` existentes (aproximadamente linha 54):**
```csharp
// Spec 180: Configuração SMTP
builder.Services.Configure<Infraestructure.Email.SmtpSettings>(
    builder.Configuration.GetSection("Smtp"));

// Spec 180: Serviço de envio de e-mail (scoped — um por request)
builder.Services.AddScoped<Infraestructure.Email.SmtpEmailSender>();

// Spec 180: Background worker de e-mail (singleton — mesma instância compartilha o Channel)
builder.Services.AddSingleton<Infraestructure.Email.EmailBackgroundWorker>();
builder.Services.AddSingleton<Domain.Interface.IEmailSender>(
    sp => sp.GetRequiredService<Infraestructure.Email.EmailBackgroundWorker>());
builder.Services.AddHostedService(
    sp => sp.GetRequiredService<Infraestructure.Email.EmailBackgroundWorker>());
```

**IMPORTANTE:** Garantir que `IEmailSender` está corretamente importado:
```csharp
using Domain.Interface;  // Já deve existir no Program.cs
```

---

## Data Flow

### Flow 1: E-mail de Boas-Vindas (Comprador)

```
1. POST /api/usuario/CadastrarComprador
2. UsuarioController.CadastrarComprador(dto, ct)
3. UsuarioService.CadastrarComprador(dto, ct)
   a. _repository.CadastrarUsuario(novocomprador)  → INSERT no banco
   b. EmailTemplates.BoasVindasComprador(dto.Email, dto.Nome) → EmailMessage
   c. _emailSender.EnfileirarAsync(email, ct) → Channel.Writer.WriteAsync()
   d. return (HTTP 200)
4. [BACKGROUND] EmailBackgroundWorker.ExecuteAsync
   a. Channel.Reader.ReadAllAsync → obtém EmailMessage
   b. ProcessarComRetentativa:
      - scope.CreateScope() → resolve SmtpEmailSender
      - SmtpEmailSender.EnviarAsync(email, ct)
      - Se falha: retry 3x (2s, 4s, 8s)
      - Se sucesso: LogInformation
```

### Flow 2: Redefinição de Senha (Sequência Completa)

```
--- ETAPA 1: Solicitar Redefinição ---
1. POST /api/usuario/esqueci-senha  { "email": "joao@teste.com" }
2. UsuarioController.EsqueciSenha(dto, ct)
3. UsuarioService.SolicitarRedefinicaoSenha("joao@teste.com", ct)
   a. _repository.BuscarEmail("joao@teste.com", ct) → Usuario?
   b. Se null ou !Ativo → return (nada acontece)
   c. _tokenservice.GerarTokenRedefinicaoSenha(usuario) → JWT string
      - Claims: email, purpose=password-reset, jti
      - Expires: UtcNow + 15min
   d. Construir link: "{baseUrl}/redefinir-senha?token={Uri.EscapeDataString(token)}"
   e. EmailTemplates.RedefinicaoSenha(email, nome, link) → EmailMessage
   f. _emailSender.EnfileirarAsync(msg, ct) → Channel
4. HTTP 200 { "message": "Se o e-mail estiver cadastrado..." }

--- BACKGROUND ---
5. EmailBackgroundWorker processa e envia e-mail via SMTP

--- ETAPA 2: Redefinir Senha ---
6. POST /api/usuario/redefinir-senha  { "token": "eyJ...", "novaSenha": "Nova@123" }
7. UsuarioController.RedefinirSenha(dto, ct)
8. UsuarioService.RedefinirSenha(token, novaSenha, ct)
   a. _tokenservice.ValidarTokenRedefinicaoSenha(token) → email ou null
      - ValidateToken (assinatura, issuer, audience, lifetime)
      - Verificar claim purpose == "password-reset"
   b. Se null → throw TokenRedefinicaoInvalido → 400
   c. _repository.BuscarEmail(email, ct) → Usuario?
   d. Se null ou !Ativo → throw TokenRedefinicaoInvalido → 400
   e. Usuario.ValidarSenhaBruta(novaSenha) → valida 8+ chars, letra, dígito, especial
   f. BCrypt.HashPassword(novaSenha) → hash
   g. usuario.AlterarSenha(hash)
   h. _repository.AtualizarSenha(usuario.Cpf, hash, ct) → UPDATE no banco
9. HTTP 200 { "message": "Senha redefinida com sucesso." }
```

### Flow 3: Confirmação de Reserva

```
1. POST /api/reserva/criar
2. ReservaController.Criar(dto, ct)
3. ReservaService.FazerReserva(cpfLogado, dto, ct)
   a. Validar ingressos, cupom, anti-cambista...
   b. _repositoryReserva.CadastrarReservaComItens(novaReserva, ct) → INSERT
   c. _repositoryUsuario.BuscarCpf(usuarioCpf, ct) → Usuario? (para obter email)
   d. EmailTemplates.ReservaConfirmada(destinatario, nomeEvento, data, qtd, valor)
   e. _emailSender.EnfileirarAsync(email, ct)
   f. return novaReserva.Id
```

### Flow 4: Confirmação de Pagamento

```
1. POST /api/pagamento/checkout/{reservaId}
2. PagamentoController.Checkout(reservaId, metodo, ct)
3. PagamentoService.ConfirmarCheckout(reservaId, usuarioCpf, metodo, ct)
   a. Validar reserva, evento, não pago...
   b. _pagamentoRepo.CriarComTransacao(pagamento, reserva, evento, ct) → INSERT + UPDATE
   c. _usuarioRepo.BuscarCpf(reserva.UsuarioCpf, ct) → Usuario? (para obter email)
   d. EmailTemplates.PagamentoConfirmado(destinatario, nomeEvento, valor, metodo, data)
   e. _emailSender.EnfileirarAsync(email, ct)
   f. return CheckoutResponseDTO
```

---

## Data Structures

### `EmailMessage` (Domain Value Object)
```
+-----------------------+
| EmailMessage          |
+-----------------------+
| + Destinatario: string |  NOT NULL, NOT EMPTY
| + Assunto: string      |  NOT NULL, NOT EMPTY
| + CorpoHtml: string    |  nullable → string.Empty
| + CorpoTexto: string   |  nullable → string.Empty
+-----------------------+
```

### `SmtpSettings` (Configuration POCO)
```
+---------------------------+
| SmtpSettings              |
+---------------------------+
| + Host: string            |  default: ""
| + Port: int               |  default: 587
| + Username: string        |  default: ""
| + Password: string        |  default: ""
| + FromName: string        |  default: "SoldOut Tickets"
| + FromAddress: string     |  default: ""
| + EnableSsl: bool         |  default: true
+---------------------------+
```

### `EsqueciSenhaDTO` (Request DTO)
```
+------------------+
| EsqueciSenhaDTO  |
+------------------+
| + Email: string  |
+------------------+
```

### `RedefinirSenhaDTO` (Request DTO)
```
+--------------------+
| RedefinirSenhaDTO  |
+--------------------+
| + Token: string    |
| + NovaSenha: string|
+--------------------+
```

### Token JWT de Redefinição (Claims)
```
+-----------------------------+
| JWT Claims (password-reset) |
+-----------------------------+
| email: "joao@teste.com"    |
| purpose: "password-reset"  |
| jti: "{guid}"              |
| iss: "{Jwt:Issuer}"        |
| aud: "{Jwt:Audience}"      |
| exp: UtcNow + 15min        |
+-----------------------------+
```

---

## File / Module Layout

### Novos Arquivos (8 arquivos)
```
Domain/
  ValueObjects/
    EmailMessage.cs              — Value object de e-mail
  Interface/
    IEmailSender.cs              — Interface de enfileiramento

Infraestructure/
  Email/
    SmtpSettings.cs              — POCO de configuração SMTP
    SmtpEmailSender.cs           — Envio via MailKit
    EmailBackgroundWorker.cs     — BackgroundService + Channel<T>
    EmailTemplates.cs            — Templates estáticos de e-mail

Application/
  DTOs/
    EsqueciSenhaDTO.cs           — Request DTO
    RedefinirSenhaDTO.cs         — Request DTO
  Exceptions/
    TokenRedefinicaoExceptions.cs — Exceção tipada
```

### Arquivos Editados (8 arquivos)
```
Application/
  Interfaces/
    ITokenService.cs             — +2 métodos
    IUsuarioService.cs           — +2 métodos
  Service/
    TokenService.cs              — +2 implementações
    UsuarioService.cs            — DI + boas-vindas + redefinição
    ReservaService.cs            — DI + confirmação reserva
    PagamentoService.cs          — DI + confirmação pagamento

Api/
  Controllers/
    UsuarioController.cs         — +2 endpoints
  Program.cs                     — DI registrations
  appsettings.json               — seções Smtp + App:BaseUrl

Infraestructure/
  Infraestructure.csproj         — +PackageReference MailKit
```

---

## Testing Strategy

### Testes Unitários (xUnit + Moq)

| # | Teste | O que verifica | FR |
|---|-------|---------------|-----|
| T1 | `EmailMessage_Valido` | Criar com destinatário e assunto → sem exceção, propriedades corretas | FR-002 |
| T2 | `EmailMessage_SemDestinatario_LancaExcecao` | `new EmailMessage("", "A", "", "")` → `ArgumentException` com "Destinatário" | FR-002 |
| T3 | `EmailMessage_SemAssunto_LancaExcecao` | `new EmailMessage("a@b.com", "", "", "")` → `ArgumentException` com "Assunto" | FR-002 |
| T4 | `EmailMessage_CamposNulos_ViramEmpty` | `new EmailMessage("a@b.com", "A", null, null)` → `CorpoHtml == "" && CorpoTexto == ""` | FR-002 |
| T5 | `SmtpSettings_DefaultValues` | `new SmtpSettings()` → `Port == 587 && EnableSsl == true && FromName == "SoldOut Tickets"` | FR-003 |
| T6 | `SmtpEmailSender_Configurado_False_QuandoHostVazio` | `Host = ""` → `Configurado == false` | FR-004 |
| T7 | `SmtpEmailSender_Configurado_True_QuandoConfigurado` | `Host = "smtp.test.com"`, `FromAddress = "a@b.com"` → `Configurado == true` | FR-004 |
| T8 | `EsqueciSenha_EmailExiste_EnviaEmail` | Mock: `BuscarEmail` retorna usuário ativo → `EnfileirarAsync` chamado 1 vez | FR-011, FR-015 |
| T9 | `EsqueciSenha_EmailNaoExiste_NaoEnviaEmail` | Mock: `BuscarEmail` retorna null → `EnfileirarAsync` NUNCA chamado | FR-011, FR-015 |
| T10 | `EsqueciSenha_ContaInativa_NaoEnviaEmail` | Mock: `BuscarEmail` retorna usuário com `Ativo=false` → `EnfileirarAsync` NUNCA chamado | FR-011, FR-015 |
| T11 | `EsqueciSenha_GerarToken_ContemPurpose` | Verificar que `GerarTokenRedefinicaoSenha` gera token com claim `purpose=password-reset` | FR-013 |
| T12 | `EsqueciSenha_TokenNaoTemRole` | Token de redefinição NÃO contém claim `role` | FR-013 |
| T13 | `RedefinirSenha_TokenValido_Sucesso` | `ValidarTokenRedefinicaoSenha` retorna email → `AtualizarSenha` chamado 1 vez | FR-012, FR-016 |
| T14 | `RedefinirSenha_TokenExpirado_400` | `ValidarTokenRedefinicaoSenha` retorna null → `TokenRedefinicaoInvalido` lançada | FR-012, FR-016 |
| T15 | `RedefinirSenha_TokenAutenticacao_400` | Token sem `purpose` → `ValidarTokenRedefinicaoSenha` retorna null | FR-012, FR-014 |
| T16 | `RedefinirSenha_TokenPurposeErrado_400` | Token com `purpose=wrong` → `ValidarTokenRedefinicaoSenha` retorna null | FR-014 |
| T17 | `RedefinirSenha_SenhaFraca_400` | `ValidarSenhaBruta` lança exceção → propagada | FR-012, FR-016 |
| T18 | `RedefinirSenha_UsuarioInativo_400` | Token válido mas `Ativo=false` → `TokenRedefinicaoInvalido` | FR-012, FR-016 |
| T19 | `CadastrarComprador_EnfileiraBoasVindas` | Cadastro bem-sucedido → `EnfileirarAsync` chamado 1 vez com `BoasVindasComprador` | FR-007 |
| T20 | `CadastrarComprador_FalhaPersistencia_NaoEnfileira` | `CadastrarUsuario` lança exceção → `EnfileirarAsync` NUNCA chamado | FR-007 |
| T21 | `CadastrarVendedor_EnfileiraBoasVindas` | Cadastro bem-sucedido → `EnfileirarAsync` chamado 1 vez com `BoasVindasVendedor` | FR-008 |
| T22 | `FazerReserva_EnfileiraConfirmacao` | Reserva criada → `EnfileirarAsync` chamado 1 vez | FR-009 |
| T23 | `ConfirmarCheckout_EnfileiraPagamento` | Pagamento confirmado → `EnfileirarAsync` chamado 1 vez | FR-010 |

**Tooling:** xUnit (já no projeto), Moq (para mocks de `IEmailSender`, `IUsuarioRepository`, `ITokenService`, etc.).

**Boilerplate de teste (exemplo):**
```csharp
public class EmailTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepoMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly UsuarioService _service;

    public EmailTests()
    {
        _usuarioRepoMock = new Mock<IUsuarioRepository>();
        _tokenServiceMock = new Mock<ITokenService>();
        _emailSenderMock = new Mock<IEmailSender>();
        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["App:BaseUrl"]).Returns("https://localhost:5001");

        _service = new UsuarioService(
            _usuarioRepoMock.Object,
            Mock.Of<IMapper>(),
            _tokenServiceMock.Object,
            _emailSenderMock.Object,
            _configMock.Object);
    }
}
```

---

## Migration / Rollback

### Migration
Esta spec não requer scripts SQL (DbUp migration). Nenhuma alteração no schema do banco.

- `EmailMessage` é um value object em memória.
- `Channel<T>` é em memória.
- Tokens JWT são stateless.
- Configuração SMTP é externalizada em `appsettings.json`.

### Rollback
Para reverter esta spec:
1. Remover os 8 novos arquivos.
2. Reverter edições nos 8 arquivos existentes (via git revert).
3. Remover `MailKit` do `Infraestructure.csproj` (`dotnet remove package MailKit`).
4. Remover seções `"Smtp"` e `"App"` do `appsettings.json`.
5. Remover registros DI do `Program.cs`.

Não há migração de dados para reverter porque nenhum dado novo é persistido.
