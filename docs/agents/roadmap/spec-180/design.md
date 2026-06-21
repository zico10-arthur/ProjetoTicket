# Spec 180 — Design: Serviço de E-mail Transacional + Redefinição de Senha

> **Requirements:** [`requirements.md`](./requirements.md)
> **Contexto:** Clean Architecture (.NET 9 + Dapper + SQL Server + DbUp)

---

## 1. Modelo de Domínio

### 1.1 Value Object `EmailMessage`

```csharp
// Domain/ValueObjects/EmailMessage.cs
namespace Domain.ValueObjects;

public class EmailMessage
{
    public string Destinatario { get; }
    public string Assunto { get; }
    public string CorpoHtml { get; }
    public string CorpoTexto { get; }

    public EmailMessage(string destinatario, string assunto, string corpoHtml, string corpoTexto)
    {
        if (string.IsNullOrWhiteSpace(destinatario))
            throw new ArgumentException("Destinatário é obrigatório.", nameof(destinatario));
        if (string.IsNullOrWhiteSpace(assunto))
            throw new ArgumentException("Assunto é obrigatório.", nameof(assunto));

        Destinatario = destinatario;
        Assunto = assunto;
        CorpoHtml = corpoHtml ?? string.Empty;
        CorpoTexto = corpoTexto ?? string.Empty;
    }
}
```

### 1.2 Interface `IEmailSender`

```csharp
// Domain/Interface/IEmailSender.cs
namespace Domain.Interface;

public interface IEmailSender
{
    /// <summary>
    /// Enfileira um e-mail para envio assíncrono em background.
    /// </summary>
    ValueTask EnfileirarAsync(EmailMessage email, CancellationToken ct);
}
```

> **Nota:** A interface reside no Domain, mas não usa `ValueObjects.EmailMessage` diretamente para manter o Domain desacoplado de MailKit. O `EmailBackgroundWorker` consome `EmailMessage` do Channel e chama `SmtpEmailSender`.

---

## 2. Infraestrutura de E-mail

### 2.1 Configuração `SmtpSettings`

```csharp
// Infraestructure/Email/SmtpSettings.cs
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

**Configuração em `appsettings.json`:**

```json
{
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "noreply@soldouttickets.com",
    "Password": "",
    "FromName": "SoldOut Tickets",
    "FromAddress": "noreply@soldouttickets.com",
    "EnableSsl": true
  }
}
```

> **Nota:** Em desenvolvimento, a seção `"Smtp"` pode estar ausente. O worker verifica `string.IsNullOrEmpty(Host)` e opera em modo "no-op" (descarta mensagens com warning).

### 2.2 `SmtpEmailSender` (envio real via MailKit)

```csharp
// Infraestructure/Email/SmtpEmailSender.cs
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infraestructure.Email;

public class SmtpEmailSender
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpSettings> settings, ILogger<SmtpEmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public bool Configurado =>
        !string.IsNullOrWhiteSpace(_settings.Host) &&
        !string.IsNullOrWhiteSpace(_settings.FromAddress);

    public async Task EnviarAsync(ValueObjects.EmailMessage email, CancellationToken ct)
    {
        if (!Configurado)
        {
            _logger.LogWarning("SMTP não configurado. E-mail descartado para {Destinatario}", email.Destinatario);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        message.To.Add(MailboxAddress.Parse(email.Destinatario));
        message.Subject = email.Assunto;

        var builder = new BodyBuilder
        {
            HtmlBody = email.CorpoHtml,
            TextBody = email.CorpoTexto
        };
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port,
            _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, ct);

        if (!string.IsNullOrWhiteSpace(_settings.Username))
            await client.AuthenticateAsync(_settings.Username, _settings.Password, ct);

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}
```

### 2.3 `EmailBackgroundWorker` (BackgroundService com Channel)

```csharp
// Infraestructure/Email/EmailBackgroundWorker.cs
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infraestructure.Email;

public class EmailBackgroundWorker : BackgroundService, IEmailSender
{
    private readonly Channel<ValueObjects.EmailMessage> _fila;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailBackgroundWorker> _logger;

    public EmailBackgroundWorker(IServiceScopeFactory scopeFactory, ILogger<EmailBackgroundWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _fila = Channel.CreateBounded<ValueObjects.EmailMessage>(
            new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait
            });
    }

    public async ValueTask EnfileirarAsync(ValueObjects.EmailMessage email, CancellationToken ct)
    {
        await _fila.Writer.WriteAsync(email, ct);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var email in _fila.Reader.ReadAllAsync(stoppingToken))
        {
            await ProcessarComRetentativa(email, stoppingToken);
        }
    }

    private async Task ProcessarComRetentativa(ValueObjects.EmailMessage email, CancellationToken ct)
    {
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
                return;
            }
            catch (Exception ex) when (i < tentativas - 1)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, i + 1)); // 2s, 4s, 8s
                _logger.LogWarning(ex, "Tentativa {Tentativa}/{Total} falhou para {Destinatario}. Retentando em {Delay}s.",
                    i + 1, tentativas, email.Destinatario, delay.TotalSeconds);
                await Task.Delay(delay, ct);
            }
        }
        _logger.LogError("Falha ao enviar e-mail para {Destinatario} após {Tentativas} tentativas.",
            email.Destinatario, tentativas);
    }
}
```

> **Nota:** O `EmailBackgroundWorker` implementa `IEmailSender` para que os services injetem a interface e enfileirem mensagens. O Channel é `BoundedChannelOptions(100)` para limitar memória em picos. O worker usa `IServiceScopeFactory` para criar um scope por envio (já que é singleton).

---

## 3. Templates de E-mail

### 3.1 Classe estática `EmailTemplates`

```csharp
// Infraestructure/Email/EmailTemplates.cs
namespace Infraestructure.Email;

public static class EmailTemplates
{
    public static EmailMessage BoasVindasComprador(string destinatario, string nome)
    {
        var assunto = "Bem-vindo ao SoldOut Tickets!";
        var html = $@"
<h1>Bem-vindo, {nome}!</h1>
<p>Sua conta foi criada com sucesso no <strong>SoldOut Tickets</strong>.</p>
<p>Você já pode descobrir eventos e comprar ingressos.</p>
<p>Acesse: <a href=""https://soldouttickets.com"">soldouttickets.com</a></p>
<p>— Equipe SoldOut Tickets</p>";
        var texto = $"Bem-vindo, {nome}! Sua conta foi criada com sucesso. Acesse soldouttickets.com";
        return new EmailMessage(destinatario, assunto, html, texto);
    }

    public static EmailMessage BoasVindasVendedor(string destinatario, string nomeFantasia, string plano)
    {
        var assunto = "Bem-vindo ao SoldOut Tickets — Comece a vender!";
        var html = $@"
<h1>Bem-vindo, {nomeFantasia}!</h1>
<p>Sua conta de <strong>Vendedor</strong> foi criada com sucesso no <strong>SoldOut Tickets</strong>.</p>
<p>Plano atual: <strong>{plano}</strong></p>
<p>Acesse seu Painel do Vendedor e crie seu primeiro evento em minutos!</p>
<p><a href=""https://soldouttickets.com/painel"">Acessar Painel</a></p>
<p>— Equipe SoldOut Tickets</p>";
        var texto = $"Bem-vindo, {nomeFantasia}! Sua conta de Vendedor foi criada. Plano: {plano}. Acesse soldouttickets.com/painel";
        return new EmailMessage(destinatario, assunto, html, texto);
    }

    public static EmailMessage ReservaConfirmada(string destinatario, string nomeEvento,
        DateTime dataEvento, int quantidadeItens, decimal valorTotal)
    {
        var assunto = $"Reserva confirmada — {nomeEvento}";
        var html = $@"
<h1>Reserva confirmada!</h1>
<p><strong>Evento:</strong> {nomeEvento}</p>
<p><strong>Data:</strong> {dataEvento:dd/MM/yyyy HH:mm}</p>
<p><strong>Participantes:</strong> {quantidadeItens}</p>
<p><strong>Valor total:</strong> R$ {valorTotal:N2}</p>
<p>Se for um evento pago, acesse sua conta para confirmar o pagamento.</p>
<p>— Equipe SoldOut Tickets</p>";
        var texto = $"Reserva confirmada! Evento: {nomeEvento}, Data: {dataEvento:dd/MM/yyyy}, Valor total: R$ {valorTotal:N2}";
        return new EmailMessage(destinatario, assunto, html, texto);
    }

    public static EmailMessage PagamentoConfirmado(string destinatario, string nomeEvento,
        decimal valorPago, string metodo, DateTime dataPagamento)
    {
        var assunto = $"Pagamento confirmado — {nomeEvento}";
        var html = $@"
<h1>Pagamento confirmado!</h1>
<p><strong>Evento:</strong> {nomeEvento}</p>
<p><strong>Valor pago:</strong> R$ {valorPago:N2}</p>
<p><strong>Método:</strong> {metodo}</p>
<p><strong>Data do pagamento:</strong> {dataPagamento:dd/MM/yyyy HH:mm}</p>
<p>Seus ingressos estão garantidos. Aproveite o evento!</p>
<p>— Equipe SoldOut Tickets</p>";
        var texto = $"Pagamento confirmado! Evento: {nomeEvento}, Valor: R$ {valorPago:N2}";
        return new EmailMessage(destinatario, assunto, html, texto);
    }

    public static EmailMessage ReembolsoConfirmado(string destinatario, string nomeEvento,
        decimal valorReembolsado)
    {
        var assunto = $"Reembolso processado — {nomeEvento}";
        var html = $@"
<h1>Reembolso processado</h1>
<p><strong>Evento:</strong> {nomeEvento}</p>
<p><strong>Valor reembolsado:</strong> R$ {valorReembolsado:N2}</p>
<p>O valor será devolvido conforme a política de reembolso da plataforma.</p>
<p>— Equipe SoldOut Tickets</p>";
        var texto = $"Reembolso processado. Evento: {nomeEvento}, Valor: R$ {valorReembolsado:N2}";
        return new EmailMessage(destinatario, assunto, html, texto);
    }

    public static EmailMessage RedefinicaoSenha(string destinatario, string nome, string link)
    {
        var assunto = "Redefinição de senha — SoldOut Tickets";
        var html = $@"
<h1>Redefinição de senha</h1>
<p>Olá, {nome}!</p>
<p>Recebemos uma solicitação para redefinir sua senha no <strong>SoldOut Tickets</strong>.</p>
<p>Clique no link abaixo para criar uma nova senha:</p>
<p><a href=""{link}"">{link}</a></p>
<p><strong>Este link é válido por 15 minutos.</strong></p>
<p>Se você não solicitou esta redefinição, ignore este e-mail.</p>
<p>— Equipe SoldOut Tickets</p>";
        var texto = $"Olá, {nome}! Acesse o link para redefinir sua senha: {link}. Válido por 15 minutos.";
        return new EmailMessage(destinatario, assunto, html, texto);
    }
}
```

---

## 4. API

### 4.1 `POST /api/usuario/esqueci-senha`

```
POST /api/usuario/esqueci-senha
Auth: Público
Content-Type: application/json
```

#### Request Body

```json
{
    "email": "usuario@exemplo.com"
}
```

#### Response `200 OK`

```json
{
    "message": "Se o e-mail estiver cadastrado, um link de redefinição será enviado."
}
```

> **Sempre retorna 200 OK.** Mesmo se o e-mail não existir ou a conta estiver inativa. Isso impede enumeração de usuários.

### 4.2 `POST /api/usuario/redefinir-senha`

```
POST /api/usuario/redefinir-senha
Auth: Público
Content-Type: application/json
```

#### Request Body

```json
{
    "token": "eyJhbGciOiJI...",
    "novaSenha": "NovaSenha@123"
}
```

#### Response `200 OK`

```json
{
    "message": "Senha redefinida com sucesso."
}
```

#### Response `400 Bad Request`

```json
{
    "message": "Token expirado ou inválido."
}
```

```json
{
    "message": "A senha deve ter no mínimo 8 caracteres."
}
```

### 4.3 DTOs

```csharp
// Application/DTOs/EsqueciSenhaDTO.cs
public record EsqueciSenhaDTO(string Email);

// Application/DTOs/RedefinirSenhaDTO.cs
public record RedefinirSenhaDTO(string Token, string NovaSenha);
```

---

## 5. Camada de Aplicação

### 5.1 Novos métodos em `IUsuarioService`

```csharp
// Application/Interfaces/IUsuarioService.cs — adicionar métodos:
/// <summary>
/// Spec 180: Gera token JWT de redefinição e enfileira e-mail com o link.
/// </summary>
Task SolicitarRedefinicaoSenha(string email, CancellationToken ct);

/// <summary>
/// Spec 180: Valida token JWT de redefinição e atualiza a senha com BCrypt.
/// </summary>
Task RedefinirSenha(string token, string novaSenha, CancellationToken ct);
```

### 5.2 Implementação no `UsuarioService`

```csharp
// Application/Service/UsuarioService.cs — adicionar:

// Injetar IEmailSender e IConfiguration
private readonly Domain.Interface.IEmailSender _emailSender;
private readonly IConfiguration _configuration;

public UsuarioService(
    IUsuarioRepository repository, IMapper mapper, ITokenService tokenservice,
    IEmailSender emailSender, IConfiguration configuration)
{
    // ... existentes
    _emailSender = emailSender;
    _configuration = configuration;
}

/// <summary>
/// Spec 180: Integração de e-mail no CadastrarComprador.
/// </summary>
public async Task CadastrarComprador(CadastrarUsuarioDTO dto, CancellationToken ct)
{
    Usuario? cpfbuscado = await _repository.BuscarCpfOuEmail(dto.Cpf, dto.Email, ct);
    if (cpfbuscado != null) throw new UsuarioCadastrado();

    Usuario.ValidarSenhaBruta(dto.Senha);
    string senhaHash = BCrypt.Net.BCrypt.HashPassword(dto.Senha);
    Usuario novocomprador = Usuario.CriarComprador(dto.Cpf, dto.Nome, dto.Email, senhaHash);
    _repository.CadastrarUsuario(novocomprador);

    // Spec 180: Enfileirar e-mail de boas-vindas (fire-and-forget)
    var email = EmailTemplates.BoasVindasComprador(dto.Email, dto.Nome);
    await _emailSender.EnfileirarAsync(email, ct);
}

/// <summary>
/// Spec 180: Integração de e-mail no CadastrarVendedor.
/// </summary>
// Após persistir vendedor (linha 68 em UsuarioService):
// Enfileirar e-mail de boas-vindas:
var emailVendedor = EmailTemplates.BoasVindasVendedor(
    vendedor.Email, vendedor.NomeFantasia!, "Gratuito");
await _emailSender.EnfileirarAsync(emailVendedor, ct);

/// <summary>
/// Spec 180: Esqueci minha senha.
/// </summary>
public async Task SolicitarRedefinicaoSenha(string email, CancellationToken ct)
{
    var usuario = await _repository.BuscarEmail(email, ct);
    if (usuario == null || !usuario.Ativo)
        return; // Não vaza informação — retorna sem enviar e-mail

    // Gerar token JWT de redefinição (15 min, purpose=password-reset)
    var token = _tokenservice.GerarTokenRedefinicaoSenha(usuario);

    // Construir link (base URL configurável)
    var baseUrl = _configuration["App:BaseUrl"] ?? "https://soldouttickets.com";
    var link = $"{baseUrl}/redefinir-senha?token={Uri.EscapeDataString(token)}";

    var msg = EmailTemplates.RedefinicaoSenha(usuario.Email, usuario.Nome, link);
    await _emailSender.EnfileirarAsync(msg, ct);
}

/// <summary>
/// Spec 180: Redefinir senha.
/// </summary>
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

    // 3. Validar e hashear nova senha
    Usuario.ValidarSenhaBruta(novaSenha);
    string senhaHash = BCrypt.Net.BCrypt.HashPassword(novaSenha);

    // 4. Atualizar
    usuario.AlterarSenha(senhaHash);
    await _repository.AtualizarSenha(usuario.Cpf, senhaHash, ct);
}
```

### 5.3 Novos métodos em `ITokenService`

```csharp
// Application/Interfaces/ITokenService.cs — adicionar:
/// <summary>
/// Spec 180: Gera token JWT com 15 min de validade e claim purpose=password-reset.
/// </summary>
string GerarTokenRedefinicaoSenha(Domain.Entities.Usuario usuario);

/// <summary>
/// Spec 180: Valida token JWT de redefinição. Retorna email se válido, null se inválido/expirado.
/// </summary>
string? ValidarTokenRedefinicaoSenha(string token);
```

### 5.4 Implementação no `TokenService`

```csharp
// Application/Service/TokenService.cs — adicionar:

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
        Expires = DateTime.UtcNow.AddMinutes(15),
        Issuer = _configuration["Jwt:Issuer"],
        Audience = _configuration["Jwt:Audience"],
        SigningCredentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256Signature)
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    return tokenHandler.WriteToken(token);
}

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

        // Verificar claim purpose
        var purpose = principal.Claims.FirstOrDefault(c => c.Type == "purpose")?.Value;
        if (purpose != "password-reset")
            return null;

        return principal.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
    }
    catch
    {
        return null;
    }
}
```

---

## 6. Integração com Fluxos Existentes

### 6.1 Cadastro de Comprador e Vendedor

```
POST /api/usuario/CadastrarComprador
  → CadastrarComprador()
  → _repository.CadastrarUsuario()
  → _emailSender.EnfileirarAsync(EmailTemplates.BoasVindasComprador(...))

POST /api/usuario/cadastrar-vendedor
  → CadastrarVendedor()
  → _repository.CadastrarVendedor()
  → _emailSender.EnfileirarAsync(EmailTemplates.BoasVindasVendedor(...))
```

### 6.2 Criação de Reserva

```
POST /api/reserva/criar
  → FazerReserva()
  → _reservaRepo.CadastrarReservaComItens()
  → _emailSender.EnfileirarAsync(EmailTemplates.ReservaConfirmada(...))
```

> `ReservaService` precisa injetar `IEmailSender` e `IEventoRepository` (para buscar nome do evento).

### 6.3 Confirmação de Pagamento

```
POST /api/pagamento/checkout/{reservaId}
  → ConfirmarCheckout()
  → _pagamentoRepo.CriarComTransacao()
  → _emailSender.EnfileirarAsync(EmailTemplates.PagamentoConfirmado(...))
```

> `PagamentoService` precisa injetar `IEmailSender`.

### 6.4 Reembolso (hook para ST-05/ST-06)

O `IEmailSender` fica registrado no container e disponível para os futuros services de cancelamento usarem `EmailTemplates.ReembolsoConfirmado(...)`.

### 6.5 Redefinição de Senha

```
POST /api/usuario/esqueci-senha
  → SolicitarRedefinicaoSenha()
  → Busca usuário por email
  → Se encontrado: gera token, enfileira e-mail
  → Sempre retorna 200 OK

POST /api/usuario/redefinir-senha
  → RedefinirSenha()
  → Valida token → extrai email → busca usuário → hasheia nova senha → atualiza
```

---

## 7. Fluxo de Dados (Sequência)

### 7.1 Envio de E-mail (Boas-Vindas como exemplo)

```
Cliente               Controller         UsuarioService       EmailBackgroundWorker    SmtpEmailSender      SMTP
   │                      │                     │                      │                     │                │
   │ POST /cadastrar      │                     │                      │                     │                │
   │─────────────────────>│                     │                      │                     │                │
   │                      │ CadastrarComprador()│                      │                     │                │
   │                      │────────────────────>│                      │                     │                │
   │                      │                     │ _repository.Cadastrar│                     │                │
   │                      │                     │──────────────────────│                     │                │
   │                      │                     │ EnfileirarAsync()    │                     │                │
   │                      │                     │─────────────────────>│ (Channel.Write)     │                │
   │                      │                     │                      │                     │                │
   │  200 OK              │                     │                      │                     │                │
   │<─────────────────────│                     │                      │                     │                │
   │                      │                     │                      │                     │                │
   │                      │                     │      (background)    │                     │                │
   │                      │                     │                      │ Channel.Read        │                │
   │                      │                     │                      │────────────────────>│                │
   │                      │                     │                      │                     │ EnviarAsync()  │
   │                      │                     │                      │                     │───────────────>│
   │                      │                     │                      │                     │<───────────────│
   │                      │                     │                      │   ✓ enviado         │                │
```

### 7.2 Redefinição de Senha

```
Cliente               Controller         UsuarioService       TokenService          Repository        EmailWorker
   │                      │                     │                    │                     │                │
   │ POST /esqueci-senha  │                     │                    │                     │                │
   │─────────────────────>│                     │                    │                     │                │
   │                      │ SolicitarRedefinicao│                    │                     │                │
   │                      │────────────────────>│                    │                     │                │
   │                      │                     │ BuscarEmail()      │                     │                │
   │                      │                     │───────────────────────────────────────────>│                │
   │                      │                     │<───────────────────────────────────────────│                │
   │                      │                     │ GerarTokenRedef()  │                     │                │
   │                      │                     │───────────────────>│                     │                │
   │                      │                     │<───────────────────│ (JWT 15min)         │                │
   │                      │                     │ EnfileirarAsync()  │                     │                │
   │                      │                     │────────────────────────────────────────────────────────>│
   │  200 OK              │                     │                    │                     │                │
   │<─────────────────────│                     │                    │                     │                │
   │                      │                     │                    │                     │                │
   │ POST /redefinir-senha│                     │                    │                     │                │
   │─────────────────────>│                     │                    │                     │                │
   │                      │ RedefinirSenha()    │                    │                     │                │
   │                      │────────────────────>│                    │                     │                │
   │                      │                     │ ValidarTokenRedef()│                     │                │
   │                      │                     │───────────────────>│                     │                │
   │                      │                     │<───────────────────│ (email ou null)     │                │
   │                      │                     │ AtualizarSenha()   │                     │                │
   │                      │                     │───────────────────────────────────────────>│                │
   │  200 OK              │                     │                    │                     │                │
   │<─────────────────────│                     │                    │                     │                │
```

---

## 8. Decisões de Design

| Decisão | Justificativa |
|---------|---------------|
| `IEmailSender` no Domain, implementação no Infrastructure | Segue Clean Architecture. Domain não conhece MailKit/SMTP. |
| Channel\<T\> (fila em memória) em vez de fila persistente | Simplicidade para v1. E-mails transacionais toleram perda em restart. Evolução futura: tabela `EmailQueue`. |
| `EmailBackgroundWorker` implementa `IEmailSender` | Permite que os services enfileirem e-mails diretamente na interface, sem acoplamento com o worker. |
| `BoundedChannelOptions(100)` com `FullMode = Wait` | Limita memória em picos de cadastro. Backpressure natural — o producer espera se a fila estiver cheia. |
| 3 tentativas com backoff exponencial (2s, 4s, 8s) | Lida com falhas transitórias de rede/SMTP sem complexidade de dead letter queue. |
| Graceful degradation se SMTP não configurado | Sistema sobe normalmente em dev sem configuração SMTP. Worker loga warning e descarta. |
| Token JWT de redefinição com claim `purpose=password-reset` | Impede que um token de autenticação seja usado para redefinir senha. Validação dupla: assinatura + purpose. |
| `esqueci-senha` sempre retorna 200 OK | Previne enumeração de usuários cadastrados (security best practice). |
| Templates como strings constantes (classe estática `EmailTemplates`) | Simples e determinístico. Sem dependência de Razor/Liquid. Fácil migrar para template engine depois. |
| `FromAddress` e `Host` como validação de "SMTP configurado" | Se `Host` estiver vazio, o worker opera em modo no-op. Não quebra a aplicação se a seção `Smtp` estiver ausente. |
