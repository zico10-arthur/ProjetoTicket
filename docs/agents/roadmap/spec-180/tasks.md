# Spec 180 — Tasks: Serviço de E-mail Transacional + Redefinição de Senha

> **Requirements:** [`requirements.md`](./requirements.md)
> **Design:** [`design.md`](./design.md)
> **Ordem:** Cada task depende das anteriores. Seguir a sequência numérica.

---

## Task 1 — Value Object `EmailMessage` (Domain)

**Objetivo:** Criar o value object que representa uma mensagem de e-mail.

| Campo | Valor |
|-------|-------|
| Arquivo novo | `Domain/ValueObjects/EmailMessage.cs` |
| Dependências | Nenhuma |

**Conteúdo:** Conforme especificado em [`design.md §1.1`](./design.md#11-value-object-emailmessage).

**Verificação:**
- Projeto `Domain` compila sem erros.
- `new EmailMessage("a@b.com", "Assunto", "<h1>Oi</h1>", "Oi")` cria objeto sem exceção.
- `new EmailMessage("", "Assunto", "", "")` lança `ArgumentException`.
- `new EmailMessage("a@b.com", "", "", "")` lança `ArgumentException`.

---

## Task 2 — Interface `IEmailSender` (Domain)

**Objetivo:** Definir o contrato para enfileiramento de e-mails.

| Campo | Valor |
|-------|-------|
| Arquivo novo | `Domain/Interface/IEmailSender.cs` |
| Dependências | Task 1 (EmailMessage) |

**Conteúdo:** Conforme especificado em [`design.md §1.2`](./design.md#12-interface-iemailsender).

```csharp
// Domain/Interface/IEmailSender.cs
using Domain.ValueObjects;

namespace Domain.Interface;

public interface IEmailSender
{
    ValueTask EnfileirarAsync(EmailMessage email, CancellationToken ct);
}
```

**Verificação:** Projeto `Domain` compila sem erros.

---

## Task 3 — `SmtpSettings` (Infrastructure)

**Objetivo:** Criar a classe de configuração SMTP mapeada de `appsettings.json`.

| Campo | Valor |
|-------|-------|
| Arquivo novo | `Infraestructure/Email/SmtpSettings.cs` |
| Dependências | Nenhuma |

**Conteúdo:** Conforme especificado em [`design.md §2.1`](./design.md#21-configuração-smtpsettings).

**Verificação:** Projeto `Infraestructure` compila sem erros.

---

## Task 4 — `EmailTemplates` (Infrastructure)

**Objetivo:** Criar classe estática com templates HTML de e-mail.

| Campo | Valor |
|-------|-------|
| Arquivo novo | `Infraestructure/Email/EmailTemplates.cs` |
| Dependências | Task 1 (EmailMessage) |

**Conteúdo:** Conforme especificado em [`design.md §3.1`](./design.md#31-classe-estática-emailtemplates) — 6 métodos:
- `BoasVindasComprador(string destinatario, string nome)`
- `BoasVindasVendedor(string destinatario, string nomeFantasia, string plano)`
- `ReservaConfirmada(string destinatario, string nomeEvento, DateTime dataEvento, int quantidadeItens, decimal valorTotal)`
- `PagamentoConfirmado(string destinatario, string nomeEvento, decimal valorPago, string metodo, DateTime dataPagamento)`
- `ReembolsoConfirmado(string destinatario, string nomeEvento, decimal valorReembolsado)`
- `RedefinicaoSenha(string destinatario, string nome, string link)`

**Verificação:** Projeto `Infraestructure` compila sem erros.

---

## Task 5 — Adicionar pacote NuGet MailKit (Infrastructure)

**Objetivo:** Instalar `MailKit` no projeto `Infraestructure`.

| Campo | Valor |
|-------|-------|
| Comando | `dotnet add Infraestructure/Infraestructure.csproj package MailKit` |
| Dependências | Nenhuma |

**Verificação:**
- `dotnet restore` conclui sem erros.
- `Infraestructure.csproj` contém `<PackageReference Include="MailKit" ... />`.

---

## Task 6 — `SmtpEmailSender` (Infrastructure)

**Objetivo:** Implementar envio real de e-mail via SMTP usando MailKit.

| Campo | Valor |
|-------|-------|
| Arquivo novo | `Infraestructure/Email/SmtpEmailSender.cs` |
| Dependências | Task 3 (SmtpSettings), Task 5 (MailKit) |

**Conteúdo:** Conforme especificado em [`design.md §2.2`](./design.md#22-smtpemailsender-envio-real-via-mailkit).

**Verificação:**
- Projeto `Infraestructure` compila sem erros.
- Classe tem propriedade `Configurado` que retorna `false` se `Host` vazio.

---

## Task 7 — `EmailBackgroundWorker` (Infrastructure)

**Objetivo:** Criar BackgroundService com Channel\<T\> que processa e-mails da fila.

| Campo | Valor |
|-------|-------|
| Arquivo novo | `Infraestructure/Email/EmailBackgroundWorker.cs` |
| Dependências | Task 2 (IEmailSender), Task 6 (SmtpEmailSender) |

**Conteúdo:** Conforme especificado em [`design.md §2.3`](./design.md#23-emailbackgroundworker-backgroundservice-com-channel).

**Pontos de atenção:**
- Implementa `IEmailSender` (a interface do Domain) para que os services a injetem.
- `Channel.CreateBounded<EmailMessage>(100)` com `FullMode = Wait`.
- `ExecuteAsync` lê do Channel com `ReadAllAsync`.
- Retentativa: 3 tentativas com backoff 2s, 4s, 8s.
- Usa `IServiceScopeFactory` para criar scope por envio (o worker é singleton).
- Não quebra se SMTP não configurado — delega para `SmtpEmailSender.Configurado`.

**Verificação:**
- Projeto `Infraestructure` compila sem erros.
- Worker sobe e fica aguardando mensagens no Channel.

---

## Task 8 — DTOs de Redefinição de Senha (Application)

**Objetivo:** Criar os records de request para os novos endpoints.

| Campo | Valor |
|-------|-------|
| Arquivos novos | `Application/DTOs/EsqueciSenhaDTO.cs` |
|                | `Application/DTOs/RedefinirSenhaDTO.cs` |
| Dependências | Nenhuma |

```csharp
// Application/DTOs/EsqueciSenhaDTO.cs
namespace Application.DTOs;
public record EsqueciSenhaDTO(string Email);

// Application/DTOs/RedefinirSenhaDTO.cs
namespace Application.DTOs;
public record RedefinirSenhaDTO(string Token, string NovaSenha);
```

**Verificação:** Projeto `Application` compila sem erros.

---

## Task 9 — Exceção `TokenRedefinicaoInvalido` (Application)

**Objetivo:** Criar exceção específica para token de redefinição inválido/expirado.

| Campo | Valor |
|-------|-------|
| Arquivo novo | `Application/Exceptions/TokenRedefinicaoExceptions.cs` |
| Dependências | Nenhuma |

```csharp
// Application/Exceptions/TokenRedefinicaoExceptions.cs
namespace Application.Exceptions;

public class TokenRedefinicaoInvalido : Exception
{
    public TokenRedefinicaoInvalido()
        : base("Token expirado ou inválido.") { }
}
```

**Verificação:** Projeto `Application` compila sem erros.

---

## Task 10 — Novos métodos em `ITokenService` (Application)

**Objetivo:** Adicionar assinaturas para geração e validação de token de redefinição de senha.

| Campo | Valor |
|-------|-------|
| Arquivo | `Application/Interfaces/ITokenService.cs` (editar) |
| Dependências | Nenhuma |

**Adicionar ao final da interface (antes do `}`):**

```csharp
/// <summary>
/// Spec 180: Gera token JWT com 15 min de validade e claim purpose=password-reset.
/// </summary>
string GerarTokenRedefinicaoSenha(Domain.Entities.Usuario usuario);

/// <summary>
/// Spec 180: Valida token JWT de redefinição. Retorna email se válido, null se inválido/expirado.
/// </summary>
string? ValidarTokenRedefinicaoSenha(string token);
```

> **Cuidado:** Apenas ADICIONAR. Não alterar os métodos existentes (`GerarToken`).

**Verificação:** Projetos `Application` e `Domain` compilam sem erros.

---

## Task 11 — Implementar novos métodos em `TokenService` (Application)

**Objetivo:** Implementar `GerarTokenRedefinicaoSenha` e `ValidarTokenRedefinicaoSenha`.

| Campo | Valor |
|-------|-------|
| Arquivo | `Application/Service/TokenService.cs` (editar) |
| Dependências | Task 10 (interface) |

**Conteúdo:** Conforme especificado em [`design.md §5.4`](./design.md#54-implementação-no-tokenservice).

**`GerarTokenRedefinicaoSenha`:**
- Mesma estrutura do `GerarToken` existente.
- Diferenças:
  - Claims: apenas `email`, `purpose=password-reset`, `jti`.
  - **Sem** claims `cpf`, `perfilId`, `role` (não é token de autenticação).
  - Expiração: `DateTime.UtcNow.AddMinutes(15)` (não 24h).

**`ValidarTokenRedefinicaoSenha`:**
- Usa `tokenHandler.ValidateToken()` com os mesmos `TokenValidationParameters` do `Program.cs`.
- Após validação, verifica se claim `purpose == "password-reset"`.
- Retorna `email` claim se tudo ok; `null` em qualquer falha (try/catch).

**Verificação:**
- Projeto `Application` compila sem erros.
- Token gerado contém claim `purpose=password-reset`.
- Token de login (sem `purpose`) é rejeitado por `ValidarTokenRedefinicaoSenha`.

---

## Task 12 — Atualizar `IUsuarioService` (Application)

**Objetivo:** Adicionar assinaturas dos novos métodos de redefinição de senha.

| Campo | Valor |
|-------|-------|
| Arquivo | `Application/Interfaces/IUsuarioService.cs` (editar) |
| Dependências | Task 8 (DTOs), Task 9 (exceção) |

**Adicionar antes do `}` final:**

```csharp
/// <summary>
/// Spec 180: Gera token JWT de redefinição e enfileira e-mail com o link.
/// </summary>
Task SolicitarRedefinicaoSenha(string email, CancellationToken ct);

/// <summary>
/// Spec 180: Valida token JWT de redefinição e atualiza a senha com BCrypt.
/// </summary>
Task RedefinirSenha(string token, string novaSenha, CancellationToken ct);
```

**Verificação:** Projeto `Application` compila sem erros.

---

## Task 13 — Atualizar `UsuarioService` (Application)

**Objetivo:** Injetar `IEmailSender`, integrar e-mails de boas-vindas, e implementar redefinição de senha.

| Campo | Valor |
|-------|-------|
| Arquivo | `Application/Service/UsuarioService.cs` (editar) |
| Dependências | Task 2 (IEmailSender), Task 4 (EmailTemplates), Task 11 (TokenService), Task 12 (interface) |

**Mudanças no construtor:**
- Adicionar parâmetros: `Domain.Interface.IEmailSender emailSender`, `IConfiguration configuration`.
- Atribuir a campos privados `_emailSender` e `_configuration`.

**Mudanças em `CadastrarComprador`:**
- Após `_repository.CadastrarUsuario(novocomprador);` (linha ~35), adicionar ao final do método:
```csharp
// Spec 180: Enfileirar e-mail de boas-vindas (fire-and-forget)
var email = Infraestructure.Email.EmailTemplates.BoasVindasComprador(dto.Email, dto.Nome);
await _emailSender.EnfileirarAsync(email, ct);
```

**Mudanças em `CadastrarVendedor`:**
- Após `_repository.CadastrarVendedor(vendedor);` (linha ~68), adicionar antes do return:
```csharp
// Spec 180: Enfileirar e-mail de boas-vindas
var email = Infraestructure.Email.EmailTemplates.BoasVindasVendedor(
    vendedor.Email, vendedor.NomeFantasia!, "Gratuito");
await _emailSender.EnfileirarAsync(email, ct);
```

**Novos métodos (adicionar ao final da classe, antes do `}`):**
- `SolicitarRedefinicaoSenha` e `RedefinirSenha` conforme [`design.md §5.2`](./design.md#52-implementação-no-usuarioservice).

**Adicionar using:**
```csharp
using Infraestructure.Email;
```

**Verificação:**
- Projeto `Application` compila sem erros.
- `CadastrarComprador` enfileira e-mail após persistência.
- `CadastrarVendedor` enfileira e-mail após persistência.
- `SolicitarRedefinicaoSenha` com e-mail inexistente → não enfileira e-mail, não lança exceção.
- `RedefinirSenha` com token inválido → lança `TokenRedefinicaoInvalido`.

---

## Task 14 — Atualizar `ReservaService` (Application)

**Objetivo:** Injetar `IEmailSender` e `IEventoRepository`, enfileirar e-mail de confirmação de reserva.

| Campo | Valor |
|-------|-------|
| Arquivo | `Application/Service/ReservaService.cs` (editar) |
| Dependências | Task 2 (IEmailSender), Task 4 (EmailTemplates) |

**Mudanças no construtor:**
- Adicionar parâmetro: `Domain.Interface.IEmailSender emailSender`.
- Atribuir a campo privado `_emailSender`.

**Mudanças em `FazerReserva`:**
- Após `await _repositoryReserva.CadastrarReservaComItens(novaReserva, ct);` (linha ~88), adicionar antes do `return`:
```csharp
// Spec 180: Enfileirar e-mail de confirmação de reserva
var nomeEvento = evento.Nome;
var email = EmailTemplates.ReservaConfirmada(
    usuarioCpf, // será substituído pelo e-mail do usuário logado — buscar com _repositoryUsuario
    nomeEvento,
    evento.DataEvento,
    itens.Count,
    novaReserva.ValorFinalPago);
await _emailSender.EnfileirarAsync(email, ct);
```

> **Importante:** Buscar o e-mail do usuário via `_repositoryUsuario.BuscarCpf(usuarioCpf, ct)` antes de montar o e-mail. Se não encontrar, usar `usuarioCpf` como fallback (não quebrar o fluxo).

**Adicionar using:**
```csharp
using Infraestructure.Email;
```

**Verificação:**
- Projeto `Application` compila sem erros.
- Criar reserva → e-mail de confirmação enfileirado com dados corretos do evento.

---

## Task 15 — Atualizar `PagamentoService` (Application)

**Objetivo:** Injetar `IEmailSender` e enfileirar e-mail de confirmação de pagamento.

| Campo | Valor |
|-------|-------|
| Arquivo | `Application/Service/PagamentoService.cs` (editar) |
| Dependências | Task 2 (IEmailSender), Task 4 (EmailTemplates) |

**Mudanças no construtor:**
- Adicionar parâmetro: `Domain.Interface.IEmailSender emailSender`.
- Atribuir a campo privado `_emailSender`.

**Mudanças em `ConfirmarCheckout`:**
- Após `await _pagamentoRepo.CriarComTransacao(pagamento, reserva, evento, ct);` (linha ~53), adicionar antes do `return`:
```csharp
// Spec 180: Enfileirar e-mail de confirmação de pagamento
// Buscar e-mail do usuário
var usuario = await _reservaRepo.BuscarUsuarioPorReserva(reservaId, ct);
var destinatario = usuario?.Email ?? reserva.UsuarioCpf;
var email = EmailTemplates.PagamentoConfirmado(
    destinatario,
    evento.Nome,
    pagamento.ValorPago,
    metodo,
    pagamento.DataPagamento);
await _emailSender.EnfileirarAsync(email, ct);
```

> **Nota:** Se `IReservaRepository` não tiver `BuscarUsuarioPorReserva`, usar `IUsuarioRepository` adicional no construtor. Se não quiser adicionar dependência extra, usar `reserva.UsuarioCpf` como fallback — o SPF da `Reserva` tem `UsuarioCpf`.

**Adicionar using:**
```csharp
using Infraestructure.Email;
```

**Verificação:**
- Projeto `Application` compila sem erros.
- Confirmar pagamento → e-mail de confirmação enfileirado.

---

## Task 16 — Atualizar `UsuarioController` (API)

**Objetivo:** Adicionar os dois novos endpoints de redefinição de senha.

| Campo | Valor |
|-------|-------|
| Arquivo | `Api/Controllers/UsuarioController.cs` (editar) |
| Dependências | Task 13 (UsuarioService atualizado) |

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
    catch (UsuarioNotFound)
    {
        return BadRequest(new { message = "Token expirado ou inválido." });
    }
}
```

**Adicionar using:**
```csharp
using Application.Exceptions;
```

**Verificação:**
- Projeto `Api` compila sem erros.
- Swagger exibe `POST /api/usuario/esqueci-senha` e `POST /api/usuario/redefinir-senha`.
- `esqueci-senha` com e-mail inexistente → `200 OK`.
- `redefinir-senha` com token inválido → `400 Bad Request`.

---

## Task 17 — Adicionar seção `Smtp` ao `appsettings.json` (API)

**Objetivo:** Adicionar a configuração SMTP ao arquivo de configuração.

| Campo | Valor |
|-------|-------|
| Arquivo | `Api/appsettings.json` (editar) |
| Dependências | Task 3 (SmtpSettings) |

**Adicionar nova seção após `"AllowedHosts"`:**

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

> **Nota:** Em desenvolvimento, os campos ficam vazios. O worker opera em modo no-op. Em produção, preencher via `user-secrets` ou variáveis de ambiente.

**Verificação:** API sobe sem erros (SMTP não configurado → modo no-op).

---

## Task 18 — Registrar DI no `Program.cs` (API)

**Objetivo:** Registrar as novas dependências no contêiner de injeção.

| Campo | Valor |
|-------|-------|
| Arquivo | `Api/Program.cs` (editar) |
| Dependências | Task 6 (SmtpEmailSender), Task 7 (EmailBackgroundWorker) |

**Linhas a adicionar (após os registros existentes de `AddScoped`):**

```csharp
// Spec 180: Configuração SMTP
builder.Services.Configure<Infraestructure.Email.SmtpSettings>(
    builder.Configuration.GetSection("Smtp"));

// Spec 180: Serviço de envio de e-mail (scoped — um por request)
builder.Services.AddScoped<Infraestructure.Email.SmtpEmailSender>();

// Spec 180: Background worker de e-mail (singleton — mesma instância é a fila)
builder.Services.AddSingleton<Infraestructure.Email.EmailBackgroundWorker>();
builder.Services.AddSingleton<Domain.Interface.IEmailSender>(
    sp => sp.GetRequiredService<Infraestructure.Email.EmailBackgroundWorker>());
builder.Services.AddHostedService(
    sp => sp.GetRequiredService<Infraestructure.Email.EmailBackgroundWorker>());
```

**Adicionar using:**
```csharp
using Infraestructure.Email;
```

> **Explicação:** `EmailBackgroundWorker` é registrado como singleton e também como `IHostedService`. A interface `IEmailSender` resolve para a mesma instância, garantindo que producers e consumer compartilhem o mesmo Channel.

**Verificação:**
- API sobe sem erros de resolução de dependência.
- Worker inicia e fica aguardando mensagens no Channel (verificar logs: "EmailBackgroundWorker iniciado").

---

## Task 19 — Adicionar `App:BaseUrl` ao `appsettings.json` (API)

**Objetivo:** Adicionar URL base para compor link de redefinição de senha.

| Campo | Valor |
|-------|-------|
| Arquivo | `Api/appsettings.json` (editar) |
| Dependências | Task 13 (UsuarioService usa `App:BaseUrl`) |

**Adicionar após `"AllowedHosts"`:**

```json
"App": {
    "BaseUrl": "https://localhost:5001"
}
```

**Verificação:** Configuração acessível via `_configuration["App:BaseUrl"]`.

---

## Task 20 — Testes Automatizados

**Objetivo:** Garantir cobertura de testes para o fluxo de e-mail e redefinição de senha.

| Campo | Valor |
|-------|-------|
| Arquivo novo | `tests/EmailTests.cs` |
| Dependências | Tasks 1-19 |

**Testes a implementar:**

| # | Teste | Cenário |
|---|-------|---------|
| T1 | `EmailMessage_Valido` | Criar com destinatário e assunto → sucesso |
| T2 | `EmailMessage_SemDestinatario_LancaExcecao` | `new EmailMessage("", "A", "", "")` → `ArgumentException` |
| T3 | `EmailMessage_SemAssunto_LancaExcecao` | `new EmailMessage("a@b.com", "", "", "")` → `ArgumentException` |
| T4 | `SmtpSettings_DefaultValues` | `new SmtpSettings()` → `Port = 587`, `EnableSsl = true` |
| T5 | `EsqueciSenha_EmailExiste_EnviaEmail` | Buscar email existente → token gerado, e-mail enfileirado |
| T6 | `EsqueciSenha_EmailNaoExiste_NaoEnviaEmail` | Buscar email inexistente → retorna sem enfileirar, sem exceção |
| T7 | `EsqueciSenha_ContaInativa_NaoEnviaEmail` | Usuário com `Ativo = false` → não enfileira e-mail |
| T8 | `RedefinirSenha_TokenValido_Sucesso` | Token válido + senha forte → senha atualizada |
| T9 | `RedefinirSenha_TokenExpirado_400` | Token com `exp` no passado → `TokenRedefinicaoInvalido` |
| T10 | `RedefinirSenha_TokenAutenticacao_400` | Token sem claim `purpose=password-reset` → `TokenRedefinicaoInvalido` |
| T11 | `RedefinirSenha_SenhaFraca_400` | Token válido + senha com 5 caracteres → exceção de validação |
| T12 | `CadastrarComprador_EnfileiraBoasVindas` | Cadastro bem-sucedido → `EnfileirarAsync` chamado 1 vez |
| T13 | `CadastrarVendedor_EnfileiraBoasVindas` | Cadastro bem-sucedido → `EnfileirarAsync` chamado 1 vez |
| T14 | `FazerReserva_EnfileiraConfirmacao` | Reserva criada → `EnfileirarAsync` chamado 1 vez |
| T15 | `ConfirmarCheckout_EnfileiraPagamento` | Pagamento confirmado → `EnfileirarAsync` chamado 1 vez |

**Estrutura dos testes (xUnit + Moq):**

```csharp
[Fact]
public async Task EsqueciSenha_EmailExiste_EnviaEmail()
{
    // Arrange
    var usuario = Usuario.CriarComprador("52988531009", "João", "joao@teste.com", "hash123");
    _usuarioRepoMock.Setup(r => r.BuscarEmail("joao@teste.com", It.IsAny<CancellationToken>()))
        .ReturnsAsync(usuario);
    _tokenServiceMock.Setup(t => t.GerarTokenRedefinicaoSenha(usuario))
        .Returns("fake-jwt-token");

    // Act
    await _service.SolicitarRedefinicaoSenha("joao@teste.com", CancellationToken.None);

    // Assert
    _emailSenderMock.Verify(e => e.EnfileirarAsync(
        It.Is<EmailMessage>(m => m.Destinatario == "joao@teste.com"),
        It.IsAny<CancellationToken>()), Times.Once);
}

[Fact]
public async Task EsqueciSenha_EmailNaoExiste_NaoEnviaEmail()
{
    // Arrange
    _usuarioRepoMock.Setup(r => r.BuscarEmail("x@y.com", It.IsAny<CancellationToken>()))
        .ReturnsAsync((Usuario?)null);

    // Act
    await _service.SolicitarRedefinicaoSenha("x@y.com", CancellationToken.None);

    // Assert
    _emailSenderMock.Verify(e => e.EnfileirarAsync(
        It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
}
```

**Verificação:**
- `dotnet test` passa com todos os 15 testes verdes.
- Cobertura do fluxo de e-mail e redefinição de senha ≥ 80%.

---

## Resumo de Tarefas

| # | Camada | Arquivo(s) | Ação |
|---|--------|-----------|------|
| 1 | Domain | `ValueObjects/EmailMessage.cs` | **Criar** |
| 2 | Domain | `Interface/IEmailSender.cs` | **Criar** |
| 3 | Infraestructure | `Email/SmtpSettings.cs` | **Criar** |
| 4 | Infraestructure | `Email/EmailTemplates.cs` | **Criar** |
| 5 | Infraestructure | `Infraestructure.csproj` | **Editar** (NuGet MailKit) |
| 6 | Infraestructure | `Email/SmtpEmailSender.cs` | **Criar** |
| 7 | Infraestructure | `Email/EmailBackgroundWorker.cs` | **Criar** |
| 8 | Application | `DTOs/EsqueciSenhaDTO.cs`, `RedefinirSenhaDTO.cs` | **Criar** |
| 9 | Application | `Exceptions/TokenRedefinicaoExceptions.cs` | **Criar** |
| 10 | Application | `Interfaces/ITokenService.cs` | **Editar** (+2 métodos) |
| 11 | Application | `Service/TokenService.cs` | **Editar** (+2 implementações) |
| 12 | Application | `Interfaces/IUsuarioService.cs` | **Editar** (+2 métodos) |
| 13 | Application | `Service/UsuarioService.cs` | **Editar** (DI + boas-vindas + redefinição) |
| 14 | Application | `Service/ReservaService.cs` | **Editar** (DI + confirmação reserva) |
| 15 | Application | `Service/PagamentoService.cs` | **Editar** (DI + confirmação pagamento) |
| 16 | Api | `Controllers/UsuarioController.cs` | **Editar** (+2 endpoints) |
| 17 | Api | `appsettings.json` | **Editar** (seção Smtp) |
| 18 | Api | `Program.cs` | **Editar** (DI registrations) |
| 19 | Api | `appsettings.json` | **Editar** (App:BaseUrl) |
| 20 | tests | `EmailTests.cs` | **Criar** |

---

## Dependências entre Tasks

```
Task 1 (EmailMessage)
  ├→ Task 2 (IEmailSender)
  │     └→ Task 7 (EmailBackgroundWorker) ──┐
  │           └→ Task 18 (DI Program.cs)     │
  │                                          │
  └→ Task 4 (EmailTemplates) ────────────────┤
        └→ Task 13 (UsuarioService) ─────────┤
        └→ Task 14 (ReservaService) ─────────┤
        └→ Task 15 (PagamentoService) ───────┤
                                             │
Task 3 (SmtpSettings)                        │
  └→ Task 6 (SmtpEmailSender) ───────────────┤
        └→ Task 7 (worker) ──────────────────┤
                                             │
Task 5 (MailKit NuGet)                       │
  └→ Task 6 (SmtpEmailSender)                │
                                             │
Task 8 (DTOs)                                │
  └→ Task 12 (IUsuarioService)               │
        └→ Task 13 (UsuarioService) ─────────┤
              └→ Task 16 (UsuarioController)─┤
                                             │
Task 9 (exceção)                             │
  └→ Task 13 (UsuarioService)                │
                                             │
Task 10 (ITokenService)                      │
  └→ Task 11 (TokenService)                  │
        └→ Task 13 (UsuarioService)          │
                                             │
Task 17 (appsettings Smtp) ──────────────────┤
  └→ Task 18 (DI)                            │
                                             │
Task 19 (appsettings BaseUrl) ───────────────┤
  └→ Task 13 (UsuarioService)                │
                                             │
Tasks 1-19 ──→ Task 20 (Testes)             │
```

---

## Estimativa de Esforço

| Bloco | Tasks | Esforço |
|-------|-------|---------|
| Domain | 1, 2 | 30min |
| Infrastructure | 3, 4, 5, 6, 7 | 2h |
| Application | 8, 9, 10, 11, 12, 13, 14, 15 | 2h30 |
| API | 16, 17, 18, 19 | 1h |
| Tests | 20 | 1h30 |
| **Total** | **20 tasks** | **~7h30** |
