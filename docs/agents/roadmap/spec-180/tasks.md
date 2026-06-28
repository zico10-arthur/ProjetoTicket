# Spec 180: Serviço de E-mail Transacional + Redefinição de Senha — Implementation Tasks

> **Requirements:** [`requirements.md`](./requirements.md)
> **Design:** [`design.md`](./design.md)

---

## Task Order

Tasks MUST be executed sequentially in numerical order. Each task depends on the completion of all preceding tasks.

---

## Tasks

### Task 1: Value Object `EmailMessage` (Domain)

**Objective:** Criar o value object imutável que representa uma mensagem de e-mail.

**Requirements Covered:** FR-002

**Design References:** [Component 1: EmailMessage](./design.md#component-1-emailmessage-value-object--domain)

**Actions:**
1. Criar arquivo `Domain/ValueObjects/EmailMessage.cs`.
2. Namespace: `Domain.ValueObjects`.
3. Implementar classe `EmailMessage` com 4 propriedades read-only, construtor com validação (destinatário/assunto obrigatórios, corpoHtml/corpoTexto default `string.Empty`).
4. Código exato conforme [design.md §Component 1](./design.md#component-1-emailmessage-value-object--domain).

**Validation:**
- `dotnet build Domain/Domain.csproj` — compila sem erros.
- `new EmailMessage("a@b.com", "Assunto", "<h1>Oi</h1>", "Oi")` cria sem exceção.
- `new EmailMessage("", "Assunto", "", "")` lança `ArgumentException` com "Destinatário".
- `new EmailMessage("a@b.com", "", "", "")` lança `ArgumentException` com "Assunto".
- `new EmailMessage("a@b.com", "A", null, null)` tem `CorpoHtml == string.Empty` e `CorpoTexto == string.Empty`.

**Status:** [x] done

---

### Task 2: Interface `IEmailSender` (Domain)

**Objective:** Definir o contrato para enfileiramento de e-mails.

**Requirements Covered:** FR-001

**Design References:** [Component 2: IEmailSender](./design.md#component-2-iemailsender-interface--domain)

**Actions:**
1. Criar arquivo `Domain/Interface/IEmailSender.cs`.
2. Namespace: `Domain.Interface`.
3. Interface com único método: `ValueTask EnfileirarAsync(Domain.ValueObjects.EmailMessage email, CancellationToken ct)`.
4. Adicionar `using Domain.ValueObjects;` para referenciar `EmailMessage`.
5. Código exato conforme [design.md §Component 2](./design.md#component-2-iemailsender-interface--domain).

**Validation:**
- `dotnet build Domain/Domain.csproj` — compila sem erros.
- Interface visível para outros projetos que referenciam `Domain`.

**Status:** [x] done

---

### Task 3: `SmtpSettings` (Infrastructure)

**Objective:** Criar a classe POCO de configuração SMTP.

**Requirements Covered:** FR-003

**Design References:** [Component 3: SmtpSettings](./design.md#component-3-smtpsettings-configuration-poco--infrastructure)

**Actions:**
1. Criar diretório `Infraestructure/Email/` se não existir.
2. Criar arquivo `Infraestructure/Email/SmtpSettings.cs`.
3. Namespace: `Infraestructure.Email`.
4. Classe com 7 propriedades e defaults conforme design.
5. Código exato conforme [design.md §Component 3](./design.md#component-3-smtpsettings-configuration-poco--infrastructure).

**Validation:**
- `dotnet build Infraestructure/Infraestructure.csproj` — compila sem erros.
- `new SmtpSettings().Port == 587`.
- `new SmtpSettings().EnableSsl == true`.
- `new SmtpSettings().FromName == "SoldOut Tickets"`.

**Status:** [x] done

---

### Task 4: Adicionar Pacote NuGet MailKit (Infrastructure)

**Objective:** Instalar MailKit no projeto `Infraestructure`.

**Requirements Covered:** FR-004 (dependency)

**Design References:** [Component 4: SmtpEmailSender dependencies](./design.md#component-4-smtpemailsender-smtp-client--infrastructure)

**Actions:**
1. Executar: `dotnet add Infraestructure/Infraestructure.csproj package MailKit`
2. Verificar que `<PackageReference Include="MailKit" ... />` foi adicionado ao `.csproj`.

**Validation:**
- `dotnet restore` conclui sem erros.
- `Infraestructure/Infraestructure.csproj` contém `PackageReference` para MailKit.

**Status:** [x] done

---

### Task 5: `EmailTemplates` (Infrastructure)

**Objective:** Criar classe estática com 6 templates de e-mail.

**Requirements Covered:** FR-006

**Design References:** [Component 6: EmailTemplates](./design.md#component-6-emailtemplates-templates-estáticos--infrastructure)

**Actions:**
1. Criar arquivo `Infraestructure/Email/EmailTemplates.cs`.
2. Namespace: `Infraestructure.Email`.
3. Classe `public static class EmailTemplates` com 6 métodos estáticos conforme design.
4. Cada método:
   - Aplica `System.Net.WebUtility.HtmlEncode()` nos valores dinâmicos inseridos no HTML.
   - Constrói `string` HTML e `string` texto plano.
   - Retorna `new Domain.ValueObjects.EmailMessage(destinatario, assunto, corpoHtml, corpoTexto)`.
5. Adicionar `using Domain.ValueObjects;`.
6. Código exato conforme [design.md §Component 6](./design.md#component-6-emailtemplates-templates-estáticos--infrastructure).

**Validation:**
- `dotnet build Infraestructure/Infraestructure.csproj` — compila sem erros.
- `EmailTemplates.BoasVindasComprador("a@b.com", "João")` retorna `EmailMessage` com `Destinatario == "a@b.com"`, `Assunto` contendo "Bem-vindo", `CorpoHtml` contendo `<h1>`, `CorpoTexto` sem tags HTML.
- Mesma verificação para os outros 5 métodos.

**Status:** [x] done

---

### Task 6: `SmtpEmailSender` (Infrastructure)

**Objective:** Implementar envio de e-mail via SMTP com MailKit.

**Requirements Covered:** FR-004

**Design References:** [Component 4: SmtpEmailSender](./design.md#component-4-smtpemailsender-smtp-client--infrastructure)

**Actions:**
1. Criar arquivo `Infraestructure/Email/SmtpEmailSender.cs`.
2. Namespace: `Infraestructure.Email`.
3. Construtor recebe `IOptions<SmtpSettings>` e `ILogger<SmtpEmailSender>`.
4. Propriedade `Configurado`: `!string.IsNullOrWhiteSpace(_settings.Host) && !string.IsNullOrWhiteSpace(_settings.FromAddress)`.
5. Método `EnviarAsync`: se não configurado → log warning + return; senão → criar MimeMessage, conectar SMTP, autenticar (se tiver username), enviar, desconectar.
6. Adicionar usings: `MailKit.Net.Smtp`, `MailKit.Security`, `MimeKit`, `Microsoft.Extensions.Logging`, `Microsoft.Extensions.Options`, `Domain.ValueObjects`.
7. Código exato conforme [design.md §Component 4](./design.md#component-4-smtpemailsender-smtp-client--infrastructure).

**Validation:**
- `dotnet build Infraestructure/Infraestructure.csproj` — compila sem erros.
- `Configurado` retorna `false` quando `Host` vazio.
- `Configurado` retorna `true` quando `Host` e `FromAddress` preenchidos.
- `EnviarAsync` com SMTP não configurado → NÃO lança exceção, loga warning.

**Status:** [x] done

---

### Task 7: `EmailBackgroundWorker` (Infrastructure)

**Objective:** Criar BackgroundService com Channel que processa e-mails com retry.

**Requirements Covered:** FR-005, NFR-001, NFR-002, NFR-006

**Design References:** [Component 5: EmailBackgroundWorker](./design.md#component-5-emailbackgroundworker-background-service--fila--infrastructure)

**Actions:**
1. Criar arquivo `Infraestructure/Email/EmailBackgroundWorker.cs`.
2. Namespace: `Infraestructure.Email`.
3. Classe: `public class EmailBackgroundWorker : BackgroundService, Domain.Interface.IEmailSender`.
4. Construtor: recebe `IServiceScopeFactory` e `ILogger<EmailBackgroundWorker>`, cria `Channel.CreateBounded<Domain.ValueObjects.EmailMessage>(new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.Wait })`.
5. `EnfileirarAsync`: `await _fila.Writer.WriteAsync(email, ct)`.
6. `ExecuteAsync`: `await foreach (var email in _fila.Reader.ReadAllAsync(stoppingToken)) { await ProcessarComRetentativa(email, stoppingToken); }`.
7. `ProcessarComRetentativa`: 3 tentativas com backoff 2s/4s/8s. A cada tentativa, cria scope, resolve `SmtpEmailSender`, chama `EnviarAsync`. Sucesso → log info. Falha final → log error.
8. Adicionar usings: `System.Threading.Channels`, `Microsoft.Extensions.DependencyInjection`, `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.Logging`, `Domain.Interface`, `Domain.ValueObjects`.
9. Código exato conforme [design.md §Component 5](./design.md#component-5-emailbackgroundworker-background-service--fila--infrastructure).

**Validation:**
- `dotnet build Infraestructure/Infraestructure.csproj` — compila sem erros.
- A classe declara `: BackgroundService, IEmailSender`.
- `EnfileirarAsync` escreve no Channel.
- `ExecuteAsync` lê do Channel com `ReadAllAsync`.
- Retentativa: loop com 3 iterações, `Math.Pow(2, i + 1)` segundos.

**Status:** [x] done

---

### Task 8: DTOs de Redefinição de Senha (Application)

**Objective:** Criar records de request para os novos endpoints.

**Requirements Covered:** FR-011, FR-012

**Design References:** [Component 7: DTOs](./design.md#component-7-esquecisenhadto-e-redefinirsenhadto-dtos--application)

**Actions:**
1. Criar arquivo `Application/DTOs/EsqueciSenhaDTO.cs`:
   ```csharp
   namespace Application.DTOs;
   public record EsqueciSenhaDTO(string Email);
   ```
2. Criar arquivo `Application/DTOs/RedefinirSenhaDTO.cs`:
   ```csharp
   namespace Application.DTOs;
   public record RedefinirSenhaDTO(string Token, string NovaSenha);
   ```

**Validation:**
- `dotnet build Application/Application.csproj` — compila sem erros.
- Ambos os records são acessíveis no namespace `Application.DTOs`.

**Status:** [x] done

---

### Task 9: Exceção `TokenRedefinicaoInvalido` (Application)

**Objective:** Criar exceção tipada para token de redefinição inválido.

**Requirements Covered:** FR-012, FR-016

**Design References:** [Component 8: TokenRedefinicaoInvalido](./design.md#component-8-tokenredefinicaoinvalido-exception--application)

**Actions:**
1. Criar arquivo `Application/Exceptions/TokenRedefinicaoExceptions.cs`:
   ```csharp
   namespace Application.Exceptions;

   public class TokenRedefinicaoInvalido : Exception
   {
       public TokenRedefinicaoInvalido()
           : base("Token expirado ou inválido.") { }
   }
   ```

**Validation:**
- `dotnet build Application/Application.csproj` — compila sem erros.
- `new TokenRedefinicaoInvalido().Message == "Token expirado ou inválido."`.

**Status:** [x] done

---

### Task 10: Atualizar `ITokenService` (Application Interface)

**Objective:** Adicionar assinaturas de token de redefinição à interface existente.

**Requirements Covered:** FR-013, FR-014

**Design References:** [Component 9: Extensão de ITokenService](./design.md#component-9-extensão-de-itokenservice-interface--application)

**Actions:**
1. **LER** o arquivo `Application/Interfaces/ITokenService.cs` para confirmar o conteúdo atual.
2. **EDITAR:** Adicionar 2 novos métodos ANTES do `}` final da interface:
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
3. **NÃO alterar** o método existente `GerarToken`.

**Validation:**
- `dotnet build Application/Application.csproj` — compila sem erros.
- `TokenService` reporta erro de compilação (não implementa os novos métodos) — isso é esperado e será resolvido na Task 11.

**Status:** [x] done

---

### Task 11: Implementar `TokenService` (Application Service)

**Objective:** Implementar `GerarTokenRedefinicaoSenha` e `ValidarTokenRedefinicaoSenha`.

**Requirements Covered:** FR-013, FR-014

**Design References:** [Component 10: Extensão de TokenService](./design.md#component-10-extensão-de-tokenservice-implementation--application)

**Actions:**
1. **LER** o arquivo `Application/Service/TokenService.cs` para entender a estrutura atual (construtor, `GerarToken`, mapeamento de PerfilId para role).
2. **EDITAR:** Adicionar 2 novos métodos ao final da classe (antes do `}`).
3. `GerarTokenRedefinicaoSenha(Usuario usuario)`:
   - Usar `JwtSecurityTokenHandler` (mesmo do `GerarToken`).
   - Ler `Jwt:Key` de `_configuration`.
   - Criar `ClaimsIdentity` com claims: `email`, `purpose=password-reset`, `jti`.
   - **NÃO** incluir claims `cpf`, `perfilId`, `role`.
   - `Expires = DateTime.UtcNow.AddMinutes(15)` (não 24h).
   - Mesmo `Issuer`, `Audience`, `SigningCredentials` do `GerarToken`.
4. `ValidarTokenRedefinicaoSenha(string token)`:
   - Usar `tokenHandler.ValidateToken` com mesmos `TokenValidationParameters` do `Program.cs`.
   - Após validação, verificar claim `purpose == "password-reset"`.
   - Se sucesso → retornar claim `email`.
   - Qualquer falha → catch → retornar `null`.
5. Adicionar usings se necessário: `System.Security.Claims`, `System.IdentityModel.Tokens.Jwt`, `Microsoft.IdentityModel.Tokens`.
6. Código exato conforme [design.md §Component 10](./design.md#component-10-extensão-de-tokenservice-implementation--application).

**Validation:**
- `dotnet build Application/Application.csproj` — compila sem erros.
- Token gerado contém claim `purpose` com valor `"password-reset"`.
- Token gerado NÃO contém claim `role`.
- Token gerado expira em 15 minutos (verificar `exp` claim).
- `ValidarTokenRedefinicaoSenha` com token de login (`GerarToken`) → retorna `null`.
- `ValidarTokenRedefinicaoSenha` com token expirado → retorna `null`.

**Status:** [x] done

---

### Task 12: Atualizar `IUsuarioService` (Application Interface)

**Objective:** Adicionar assinaturas de redefinição de senha.

**Requirements Covered:** FR-015, FR-016

**Design References:** [Component 11: Extensão de IUsuarioService](./design.md#component-11-extensão-de-iusuarioservice-interface--application)

**Actions:**
1. **LER** o arquivo `Application/Interfaces/IUsuarioService.cs` para confirmar conteúdo atual.
2. **EDITAR:** Adicionar 2 novos métodos ANTES do `}` final:
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

**Validation:**
- `dotnet build Application/Application.csproj` — compila com erro no `UsuarioService` (não implementa novos métodos) — esperado, será resolvido na Task 13.

**Status:** [x] done

---

### Task 13: Atualizar `UsuarioService` (Application Service)

**Objective:** Adicionar DI de `IEmailSender` e `IConfiguration`, integrar e-mails de boas-vindas, implementar redefinição de senha.

**Requirements Covered:** FR-007, FR-008, FR-015, FR-016

**Design References:** [Component 12: Extensão de UsuarioService](./design.md#component-12-extensão-de-usuarioservice-implementation--application)

**Actions:**
1. **LER** o arquivo `Application/Service/UsuarioService.cs` para confirmar a localização exata de:
   - Construtor
   - `CadastrarComprador` (linha onde `_repository.CadastrarUsuario` é chamado)
   - `CadastrarVendedor` (linha onde `_repository.CadastrarVendedor` é chamado)
   - Final da classe (antes do `}`)
2. **EDITAR — Construtor:**
   - Adicionar 2 novos parâmetros: `Domain.Interface.IEmailSender emailSender`, `IConfiguration configuration`.
   - Atribuir a campos privados `_emailSender` e `_configuration`.
3. **EDITAR — `CadastrarComprador`:**
   - Após `_repository.CadastrarUsuario(novocomprador);`, adicionar:
     ```csharp
     // Spec 180: Enfileirar e-mail de boas-vindas (fire-and-forget)
     var email = Infraestructure.Email.EmailTemplates.BoasVindasComprador(dto.Email, dto.Nome);
     await _emailSender.EnfileirarAsync(email, ct);
     ```
4. **EDITAR — `CadastrarVendedor`:**
   - Após `_repository.CadastrarVendedor(vendedor);`, adicionar (antes do `return`):
     ```csharp
     // Spec 180: Enfileirar e-mail de boas-vindas
     var email = Infraestructure.Email.EmailTemplates.BoasVindasVendedor(
         vendedor.Email, vendedor.NomeFantasia!, "Gratuito");
     await _emailSender.EnfileirarAsync(email, ct);
     ```
5. **EDITAR — Adicionar 2 novos métodos ao final da classe:**
   - `SolicitarRedefinicaoSenha(string email, CancellationToken ct)` — busca por email, se null/inativo retorna, gera token, constrói link, enfileira e-mail.
   - `RedefinirSenha(string token, string novaSenha, CancellationToken ct)` — valida token, busca usuário, valida senha bruta, hasheia com BCrypt, atualiza.
6. **EDITAR — Adicionar using no topo:**
   ```csharp
   using Infraestructure.Email;
   using Application.Exceptions;  // para TokenRedefinicaoInvalido
   using Domain.Interface;        // para IEmailSender
   ```
7. Código exato conforme [design.md §Component 12](./design.md#component-12-extensão-de-usuarioservice-implementation--application).

**Validation:**
- `dotnet build Application/Application.csproj` — compila sem erros.
- `CadastrarComprador`: `EnfileirarAsync` é chamado APÓS `CadastrarUsuario`.
- `CadastrarVendedor`: `EnfileirarAsync` é chamado APÓS `CadastrarVendedor`.
- `SolicitarRedefinicaoSenha`: com email inexistente → não enfileira, não lança exceção.
- `RedefinirSenha`: com token inválido → lança `TokenRedefinicaoInvalido`.

**Status:** [x] done

---

### Task 14: Atualizar `ReservaService` (Application Service)

**Objective:** Injetar `IEmailSender` e enfileirar e-mail de confirmação de reserva.

**Requirements Covered:** FR-009

**Design References:** [Component 13: Extensão de ReservaService](./design.md#component-13-extensão-de-reservaservice-application)

**Actions:**
1. **LER** o arquivo `Application/Service/ReservaService.cs` para confirmar:
   - Construtor (5 parâmetros atuais)
   - `FazerReserva` — localização de `_repositoryReserva.CadastrarReservaComItens` e `return novaReserva.Id`
2. **EDITAR — Construtor:**
   - Adicionar 6º parâmetro: `Domain.Interface.IEmailSender emailSender`.
   - Atribuir a `_emailSender`.
3. **EDITAR — `FazerReserva`:**
   - Após `await _repositoryReserva.CadastrarReservaComItens(novaReserva, ct);`, adicionar ANTES do `return novaReserva.Id;`:
     ```csharp
     // Spec 180: Enfileirar e-mail de confirmação de reserva
     var usuario = await _repositoryUsuario.BuscarCpf(usuarioCpf, ct);
     var destinatario = usuario?.Email ?? usuarioCpf;
     var nomeEvento = evento.Nome;
     var email = Infraestructure.Email.EmailTemplates.ReservaConfirmada(
         destinatario, nomeEvento, evento.DataEvento, itens.Count, novaReserva.ValorFinalPago);
     await _emailSender.EnfileirarAsync(email, ct);
     ```
4. **EDITAR — Adicionar using no topo:**
   ```csharp
   using Infraestructure.Email;
   using Domain.Interface;
   ```

**Validation:**
- `dotnet build Application/Application.csproj` — compila sem erros.
- `FazerReserva`: e-mail enfileirado após persistência com dados corretos do evento.
- `usuario?.Email ?? usuarioCpf`: fallback funciona se `BuscarCpf` retornar null.

**Status:** [x] done

---

### Task 15: Atualizar `PagamentoService` (Application Service)

**Objective:** Injetar `IEmailSender` e `IUsuarioRepository`, enfileirar e-mail de confirmação de pagamento.

**Requirements Covered:** FR-010

**Design References:** [Component 14: Extensão de PagamentoService](./design.md#component-14-extensão-de-pagamentoservice-application)

**Actions:**
1. **LER** o arquivo `Application/Service/PagamentoService.cs` para confirmar:
   - Construtor (3 parâmetros atuais: `IPagamentoRepository`, `IReservaRepository`, `IEventoRepository`)
   - `ConfirmarCheckout` — localização de `_pagamentoRepo.CriarComTransacao` e `return`
2. **EDITAR — Construtor:**
   - Adicionar 4º parâmetro: `Domain.Interface.IEmailSender emailSender`.
   - Adicionar 5º parâmetro: `IUsuarioRepository usuarioRepo`.
   - Atribuir a `_emailSender` e `_usuarioRepo`.
3. **EDITAR — `ConfirmarCheckout`:**
   - Após `await _pagamentoRepo.CriarComTransacao(pagamento, reserva, evento, ct);`, adicionar ANTES do `return`:
     ```csharp
     // Spec 180: Enfileirar e-mail de confirmação de pagamento
     var comprador = await _usuarioRepo.BuscarCpf(reserva.UsuarioCpf, ct);
     var destinatario = comprador?.Email ?? reserva.UsuarioCpf;
     var email = Infraestructure.Email.EmailTemplates.PagamentoConfirmado(
         destinatario, evento.Nome, pagamento.ValorPago, metodo, pagamento.DataPagamento);
     await _emailSender.EnfileirarAsync(email, ct);
     ```
4. **EDITAR — Adicionar using no topo:**
   ```csharp
   using Infraestructure.Email;
   using Domain.Interface;
   ```

**Validation:**
- `dotnet build Application/Application.csproj` — compila sem erros.
- `ConfirmarCheckout`: e-mail enfileirado após persistência com dados corretos.
- `comprador?.Email ?? reserva.UsuarioCpf`: fallback funciona se `BuscarCpf` retornar null.

**Status:** [x] done

---

### Task 16: Atualizar `UsuarioController` (API)

**Objective:** Adicionar endpoints `esqueci-senha` e `redefinir-senha`.

**Requirements Covered:** FR-011, FR-012

**Design References:** [Component 15: Extensão de UsuarioController](./design.md#component-15-extensão-de-usuariocontroller-api)

**Actions:**
1. **LER** o arquivo `Api/Controllers/UsuarioController.cs` para confirmar a estrutura da classe.
2. **EDITAR — Adicionar using:**
   ```csharp
   using Application.Exceptions;  // para TokenRedefinicaoInvalido
   ```
3. **EDITAR — Adicionar 2 novos métodos ANTES do `}` final da classe:**
   - `EsqueciSenha`: `[HttpPost("esqueci-senha")]`, recebe `[FromBody] EsqueciSenhaDTO dto`, chama `_service.SolicitarRedefinicaoSenha`, retorna `200 OK` com mensagem genérica.
   - `RedefinirSenha`: `[HttpPost("redefinir-senha")]`, recebe `[FromBody] RedefinirSenhaDTO dto`, chama `_service.RedefinirSenha`. Try/catch:
     - `TokenRedefinicaoInvalido` → `400 BadRequest` com `ex.Message`.
     - `Domain.Exceptions.SenhaInvalida` → `400 BadRequest` com `ex.Message`.
     - `Domain.Exceptions.Senha8digitos` → `400 BadRequest` com `ex.Message`.
     - `Domain.Exceptions.SenhaVazia` → `400 BadRequest` com `ex.Message`.
4. Código exato conforme [design.md §Component 15](./design.md#component-15-extensão-de-usuariocontroller-api).

**Validation:**
- `dotnet build Api/Api.csproj` — compila sem erros.
- Swagger exibe `POST /api/usuario/esqueci-senha` e `POST /api/usuario/redefinir-senha`.
- Ambos sem `[Authorize]` (públicos).
- `EsqueciSenha` sempre retorna `200 OK`.
- `RedefinirSenha` com token inválido → `400 Bad Request` com "Token expirado ou inválido."
- `RedefinirSenha` com senha fraca (< 8 caracteres) → `400 Bad Request` com mensagem de validação.
- `RedefinirSenha` com senha vazia → `400 Bad Request` com mensagem de validação.

**Status:** [x] done

---

### Task 17: Adicionar Configurações ao `appsettings.json` (API)

**Objective:** Adicionar seções `Smtp` e `App` ao arquivo de configuração.

**Requirements Covered:** FR-003, FR-015

**Design References:** [Component 3 config](./design.md#component-3-smtpsettings-configuration-poco--infrastructure), [Component 12 baseUrl](./design.md#component-12-extensão-de-usuarioservice-implementation--application)

**Actions:**
1. **LER** o arquivo `Api/appsettings.json` para confirmar o conteúdo atual.
2. **EDITAR — Adicionar seção `"Smtp"`** (após `"AllowedHosts"` ou no final do JSON):
   ```json
   "Smtp": {
       "Host": "",
       "Port": 587,
       "Username": "",
       "Password": "",
       "FromName": "SoldOut Tickets",
       "FromAddress": "",
       "EnableSsl": true
   },
   "App": {
       "BaseUrl": "https://localhost:5001"
   }
   ```
3. Garantir que o JSON é válido (vírgulas corretas).

**Validation:**
- `dotnet build Api/Api.csproj` — compila sem erros.
- API sobe sem erros (SMTP não configurado → modo no-op).
- `_configuration["App:BaseUrl"]` retorna `"https://localhost:5001"`.

**Status:** [x] done

---

### Task 18: Registrar DI no `Program.cs` (API)

**Objective:** Registrar novas dependências no container de injeção.

**Requirements Covered:** FR-017

**Design References:** [Component 16: Registro de DI](./design.md#component-16-registro-de-di-em-programcs-api)

**Actions:**
1. **LER** o arquivo `Api/Program.cs` para confirmar:
   - Localização dos `builder.Services.AddScoped<...>` existentes
   - Se `using Domain.Interface;` já existe
2. **EDITAR — Adicionar using (se não existir):**
   ```csharp
   using Infraestructure.Email;
   ```
3. **EDITAR — Adicionar 5 linhas de registro** após os `AddScoped` existentes (antes do `AddAutoMapper` ou `AddAuthentication`):
   ```csharp
   // Spec 180: Configuração SMTP
   builder.Services.Configure<Infraestructure.Email.SmtpSettings>(
       builder.Configuration.GetSection("Smtp"));

   // Spec 180: Serviço de envio de e-mail (scoped)
   builder.Services.AddScoped<Infraestructure.Email.SmtpEmailSender>();

   // Spec 180: Background worker de e-mail (singleton — mesma instância = mesmo Channel)
   builder.Services.AddSingleton<Infraestructure.Email.EmailBackgroundWorker>();
   builder.Services.AddSingleton<Domain.Interface.IEmailSender>(
       sp => sp.GetRequiredService<Infraestructure.Email.EmailBackgroundWorker>());
   builder.Services.AddHostedService(
       sp => sp.GetRequiredService<Infraestructure.Email.EmailBackgroundWorker>());
   ```

**Validation:**
- `dotnet build Api/Api.csproj` — compila sem erros.
- API inicia sem erros de resolução de dependência.
- Worker inicia e loga (verificar console output).

**Status:** [x] done

---

### Task 19: Testes Automatizados

**Objective:** Garantir cobertura de testes para os fluxos de e-mail e redefinição de senha.

**Requirements Covered:** Todos (validação de integridade)

**Design References:** [Testing Strategy](./design.md#testing-strategy)

**Actions:**
1. Criar arquivo `tests/EmailTests.cs` (usar diretório de testes existente).
2. Configurar mocks: `Mock<IUsuarioRepository>`, `Mock<ITokenService>`, `Mock<IEmailSender>`, `Mock<IConfiguration>`, `Mock<IMapper>`.
3. Implementar 23 testes listados na [Testing Strategy](./design.md#testing-strategy).
4. Cada teste segue o padrão Arrange-Act-Assert.
5. Usar `xUnit` (já configurado no projeto).

**Testes obrigatórios:**
- T1-T4: `EmailMessage` (validação)
- T5: `SmtpSettings` (defaults)
- T6-T7: `SmtpEmailSender` (Configurado)
- T8-T10: `EsqueciSenha` (existente, inexistente, inativo)
- T11-T12: Token claims (purpose, sem role)
- T13-T18: `RedefinirSenha` (válido, expirado, auth token, purpose errado, senha fraca, inativo)
- T19-T21: Cadastros (comprador/vendedor enfileiram; falha não enfileira)
- T22: `FazerReserva` enfileira
- T23: `ConfirmarCheckout` enfileira

**Validation:**
- `dotnet test` passa com todos os 23 testes verdes.
- Nenhum teste quebra compilação existente.
- Cobertura ≥ 80% para novos métodos.

**Status:** [x] done

---

## Summary

| # | Camada | Arquivo(s) | Ação | FR |
|---|--------|-----------|------|-----|
| 1 | Domain | `ValueObjects/EmailMessage.cs` | **Criar** | FR-002 |
| 2 | Domain | `Interface/IEmailSender.cs` | **Criar** | FR-001 |
| 3 | Infrastructure | `Email/SmtpSettings.cs` | **Criar** | FR-003 |
| 4 | Infrastructure | `Infraestructure.csproj` | **Editar** (NuGet) | FR-004 |
| 5 | Infrastructure | `Email/EmailTemplates.cs` | **Criar** | FR-006 |
| 6 | Infrastructure | `Email/SmtpEmailSender.cs` | **Criar** | FR-004 |
| 7 | Infrastructure | `Email/EmailBackgroundWorker.cs` | **Criar** | FR-005 |
| 8 | Application | `DTOs/EsqueciSenhaDTO.cs`, `DTOs/RedefinirSenhaDTO.cs` | **Criar** | FR-011, FR-012 |
| 9 | Application | `Exceptions/TokenRedefinicaoExceptions.cs` | **Criar** | FR-016 |
| 10 | Application | `Interfaces/ITokenService.cs` | **Editar** (+2) | FR-013, FR-014 |
| 11 | Application | `Service/TokenService.cs` | **Editar** (+2) | FR-013, FR-014 |
| 12 | Application | `Interfaces/IUsuarioService.cs` | **Editar** (+2) | FR-015, FR-016 |
| 13 | Application | `Service/UsuarioService.cs` | **Editar** (DI+boas-vindas+redef) | FR-007, FR-008, FR-015, FR-016 |
| 14 | Application | `Service/ReservaService.cs` | **Editar** (DI+email) | FR-009 |
| 15 | Application | `Service/PagamentoService.cs` | **Editar** (DI+email) | FR-010 |
| 16 | Api | `Controllers/UsuarioController.cs` | **Editar** (+2) | FR-011, FR-012 |
| 17 | Api | `appsettings.json` | **Editar** (Smtp+App) | FR-003, FR-015 |
| 18 | Api | `Program.cs` | **Editar** (DI) | FR-017 |
| 19 | tests | `EmailTests.cs` | **Criar** | Todos |

---

## Dependency Graph

```
Task 1 (EmailMessage)
  ├→ Task 2 (IEmailSender)
  │     └→ Task 7 (EmailBackgroundWorker) ──┐
  │           └→ Task 18 (DI Program.cs)     │
  │                                          │
  └→ Task 5 (EmailTemplates) ────────────────┤
        └→ Task 13 (UsuarioService) ─────────┤
        └→ Task 14 (ReservaService) ─────────┤
        └→ Task 15 (PagamentoService) ───────┤
                                             │
Task 3 (SmtpSettings)                        │
  └→ Task 6 (SmtpEmailSender) ───────────────┤
        └→ Task 7 (worker) ──────────────────┤
                                             │
Task 4 (MailKit NuGet)                       │
  └→ Task 6 (SmtpEmailSender)                │
                                             │
Task 8 (DTOs)                                │
  └→ Task 12 (IUsuarioService)               │
        └→ Task 13 (UsuarioService) ─────────┤
              └→ Task 16 (UsuarioController)─┤
                                             │
Task 9 (exceção)                             │
  └→ Task 13 (UsuarioService)                │
      └→ Task 16 (UsuarioController)         │
                                             │
Task 10 (ITokenService)                      │
  └→ Task 11 (TokenService)                  │
        └→ Task 13 (UsuarioService)          │
                                             │
Task 17 (appsettings) ───────────────────────┤
  └→ Task 18 (DI)                            │
                                             │
Tasks 1-18 ──→ Task 19 (Testes)             │
```
