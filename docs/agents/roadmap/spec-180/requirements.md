# Spec 180: Serviço de E-mail Transacional + Redefinição de Senha — Requirements

> **Projeto:** SoldOut Tickets
> **Status:** `audited`
> **Referências:** [`visao.md §6.9`](../../visao.md#69-e-mails-transacionais-e-redefinição-de-senha) | [`ADR-013`](../../ADR.md#adr-013-mailkit--channel-para-e-mails-transacionais-spec-180) | [`arquitetura.md §8`](../../arquitetura.md#8-serviço-de-e-mail-transacional-spec-180) | [`sprints.md`](../../sprints.md)

---

## Value Delivery

Do [`visao.md §6.9`](../../visao.md#69-e-mails-transacionais-e-redefinição-de-senha):

> **Confirmação imediata:** o usuário recebe a certeza de que a conta foi criada com sucesso, sem precisar fazer login para verificar.
>
> **Comprovante digital:** o comprador tem um registro da compra no e-mail, acessível mesmo sem estar logado na plataforma.
>
> **Segurança e transparência:** o comprador sabe exatamente quando e quanto pagou, com registro para eventuais contestações.
>
> **Tranquilidade financeira:** o comprador tem a garantia documentada de que o reembolso foi processado.
>
> **Autonomia total:** o usuário recupera o acesso à conta sozinho, sem abrir chamado nem depender do Admin. Nenhum outro perfil precisa intervir.
>
> **Experiência fluida:** o cadastro, reserva ou pagamento são confirmados instantaneamente. O e-mail chega em seguida, sem atrasar a navegação.
>
> **Resiliência em desenvolvimento:** o time pode rodar o sistema localmente sem configurar servidor de e-mail.

---

## Functional Requirements

### FR-001: Interface `IEmailSender` no Domain

**What:** O sistema deve expor uma interface `IEmailSender` no namespace `Domain.Interface` com um único método `ValueTask EnfileirarAsync(EmailMessage email, CancellationToken ct)`. Esta interface é o contrato que todos os services injetam para enfileirar e-mails. A implementação concreta reside no `Infraestructure`.

**Why:** Clean Architecture — o Domain não pode depender de MailKit/SMTP. A interface define o contrato; a Infraestructure implementa.

**Acceptance Criteria:**
- [ ] Arquivo `Domain/Interface/IEmailSender.cs` existe.
- [ ] Contém exatamente: `ValueTask EnfileirarAsync(Domain.ValueObjects.EmailMessage email, CancellationToken ct);`.
- [ ] Projeto `Domain` compila sem erros.
- [ ] A interface usa `Domain.ValueObjects.EmailMessage` como tipo do parâmetro (não `string` ou tipo genérico).

### FR-002: Value Object `EmailMessage` no Domain

**What:** O sistema deve ter um value object imutável `EmailMessage` em `Domain/ValueObjects/EmailMessage.cs` com quatro propriedades read-only: `Destinatario` (string), `Assunto` (string), `CorpoHtml` (string), `CorpoTexto` (string). O construtor deve lançar `ArgumentException` se `Destinatario` ou `Assunto` forem null ou whitespace. `CorpoHtml` e `CorpoTexto` aceitam null (convertidos para `string.Empty`).

**Why:** Encapsula os dados de um e-mail em um tipo de domínio. Garante que e-mails inválidos (sem destinatário ou assunto) sejam detectados no momento da criação, não no envio.

**Acceptance Criteria:**
- [ ] Arquivo `Domain/ValueObjects/EmailMessage.cs` existe.
- [ ] `new EmailMessage("a@b.com", "Assunto", "<h1>Oi</h1>", "Oi")` cria objeto sem exceção.
- [ ] `new EmailMessage("", "Assunto", "", "")` lança `ArgumentException` com mensagem contendo "Destinatário".
- [ ] `new EmailMessage("a@b.com", "", "", "")` lança `ArgumentException` com mensagem contendo "Assunto".
- [ ] `new EmailMessage("a@b.com", "A", null, null)` cria objeto com `CorpoHtml == string.Empty` e `CorpoTexto == string.Empty`.
- [ ] Todas as 4 propriedades são `{ get; }` (read-only, sem setter público).

### FR-003: Configuração SMTP Externalizada

**What:** O sistema deve ler configurações SMTP da seção `"Smtp"` do `appsettings.json` (e de `user-secrets` ou variáveis de ambiente, via sistema de configuração padrão do .NET). A classe `SmtpSettings` em `Infraestructure.Email` deve ter propriedades: `Host` (string, default `""`), `Port` (int, default `587`), `Username` (string, default `""`), `Password` (string, default `""`), `FromName` (string, default `"SoldOut Tickets"`), `FromAddress` (string, default `""`), `EnableSsl` (bool, default `true`).

**Why:** Externalização de configuração sensível. Os valores padrão permitem que o sistema suba sem SMTP configurado (graceful degradation).

**Acceptance Criteria:**
- [ ] Arquivo `Infraestructure/Email/SmtpSettings.cs` existe com as 7 propriedades e defaults especificados.
- [ ] `appsettings.json` contém seção `"Smtp"` com todas as 7 chaves, valores vazios exceto `Port: 587`, `FromName: "SoldOut Tickets"`, `EnableSsl: true`.
- [ ] `Program.cs` registra: `builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"))`.
- [ ] Se `"Smtp"` estiver ausente do `appsettings.json`, a aplicação NÃO quebra — as propriedades assumem os defaults da classe.

### FR-004: Envio de E-mail via SMTP com MailKit

**What:** O sistema deve ter uma classe `SmtpEmailSender` em `Infraestructure.Email` que recebe `SmtpSettings` via `IOptions<SmtpSettings>` e `ILogger<SmtpEmailSender>` no construtor. Deve expor:
- Propriedade `bool Configurado` → `true` se `!string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromAddress)`.
- Método `async Task EnviarAsync(EmailMessage email, CancellationToken ct)`:
  - Se `Configurado == false`, loga `LogWarning` com o destinatário e retorna sem enviar.
  - Se configurado, cria `MimeMessage`, conecta via `SmtpClient` do MailKit com `ConnectAsync` (usando `SecureSocketOptions.StartTls` se `EnableSsl`, senão `SecureSocketOptions.None`).
  - Autentica se `Username` não for vazio: `AuthenticateAsync`.
  - Envia: `SendAsync`.
  - Desconecta: `DisconnectAsync(true)`.

**Why:** Implementação concreta de envio SMTP. MailKit é a biblioteca .NET padrão para SMTP com suporte a TLS e autenticação moderna.

**Acceptance Criteria:**
- [ ] Arquivo `Infraestructure/Email/SmtpEmailSender.cs` existe.
- [ ] `SmtpEmailSender.Configurado` retorna `false` se `Host` for `""`.
- [ ] `SmtpEmailSender.Configurado` retorna `false` se `FromAddress` for `""`.
- [ ] `SmtpEmailSender.Configurado` retorna `true` se `Host` e `FromAddress` forem preenchidos.
- [ ] `EnviarAsync` com `Configurado == false` NÃO lança exceção, apenas loga warning.
- [ ] Projeto `Infraestructure.csproj` contém `<PackageReference Include="MailKit" Version="..."/>`.
- [ ] `dotnet restore` conclui sem erros.

### FR-005: Background Worker com Fila em Memória (`Channel<EmailMessage>`)

**What:** O sistema deve ter uma classe `EmailBackgroundWorker` em `Infraestructure.Email` que:
- Herda de `BackgroundService`.
- Implementa `IEmailSender` (a interface do Domain).
- No construtor, recebe `IServiceScopeFactory` e `ILogger<EmailBackgroundWorker>`.
- Cria um `Channel<EmailMessage>` bounded para 100 itens com `BoundedChannelFullMode.Wait`.
- `EnfileirarAsync` escreve no Channel via `_fila.Writer.WriteAsync`.
- `ExecuteAsync` lê do Channel via `_fila.Reader.ReadAllAsync` e para cada mensagem chama `ProcessarComRetentativa`.
- `ProcessarComRetentativa`: tenta enviar até 3 vezes com backoff exponencial (2s, 4s, 8s entre tentativas). A cada tentativa, cria um scope via `IServiceScopeFactory`, resolve `SmtpEmailSender`, e chama `EnviarAsync`. Se falhar na 3ª tentativa, loga `LogError`.
- Em caso de sucesso, loga `LogInformation` com destinatário e assunto.

**Why:** Fila em memória desacopla produção de e-mails (HTTP request) do consumo (envio SMTP). `BoundedChannel(100)` limita memória em picos. Retentativa com backoff lida com falhas transitórias de rede. `IServiceScopeFactory` é necessário porque o worker é singleton e `SmtpEmailSender` é scoped.

**Acceptance Criteria:**
- [ ] Arquivo `Infraestructure/Email/EmailBackgroundWorker.cs` existe.
- [ ] A classe declara `: BackgroundService, IEmailSender`.
- [ ] `EnfileirarAsync` escreve no Channel sem bloquear a thread por mais de 1ms (fire-and-forget).
- [ ] `ExecuteAsync` processa mensagens sequencialmente (uma por vez).
- [ ] Se `SmtpEmailSender.EnviarAsync` lançar exceção, o worker tenta até 3 vezes.
- [ ] Após 3 falhas, loga `LogError` e descarta a mensagem (não tenta novamente).
- [ ] Worker NÃO quebra se SMTP não estiver configurado — delega a verificação para `SmtpEmailSender.Configurado`.

### FR-006: Templates de E-mail como Classe Estática

**What:** O sistema deve ter uma classe estática `EmailTemplates` em `Infraestructure.Email` com 6 métodos de fábrica que retornam `EmailMessage`:
1. `BoasVindasComprador(string destinatario, string nome)` — assunto: "Bem-vindo ao SoldOut Tickets!"
2. `BoasVindasVendedor(string destinatario, string nomeFantasia, string plano)` — assunto: "Bem-vindo ao SoldOut Tickets — Comece a vender!"
3. `ReservaConfirmada(string destinatario, string nomeEvento, DateTime dataEvento, int quantidadeItens, decimal valorTotal)` — assunto: "Reserva confirmada — {nomeEvento}"
4. `PagamentoConfirmado(string destinatario, string nomeEvento, decimal valorPago, string metodo, DateTime dataPagamento)` — assunto: "Pagamento confirmado — {nomeEvento}"
5. `ReembolsoConfirmado(string destinatario, string nomeEvento, decimal valorReembolsado)` — assunto: "Reembolso processado — {nomeEvento}"
6. `RedefinicaoSenha(string destinatario, string nome, string link)` — assunto: "Redefinição de senha — SoldOut Tickets"

Cada método deve gerar corpo HTML e corpo texto plano (fallback). O HTML deve usar tags básicas (`<h1>`, `<p>`, `<strong>`, `<a>`). Nomes de variáveis (`{nome}`, `{nomeFantasia}`, etc.) devem ser corretamente escapados para evitar injeção de HTML (usar `System.Net.WebUtility.HtmlEncode` nos valores inseridos no HTML).

**Why:** Templates como strings constantes eliminam dependência de engine de template (Razor, Liquid). HTML + texto plano garante compatibilidade com clientes de e-mail. A classe estática permite que qualquer service chame `EmailTemplates.Xxx(...)` sem injeção de dependência.

**Acceptance Criteria:**
- [ ] Arquivo `Infraestructure/Email/EmailTemplates.cs` existe.
- [ ] Contém exatamente 6 métodos estáticos com as assinaturas especificadas.
- [ ] Cada método retorna `EmailMessage` com `CorpoHtml` e `CorpoTexto` não vazios.
- [ ] Valores dinâmicos inseridos no HTML passam por `HtmlEncode`.
- [ ] `CorpoTexto` contém uma versão texto plano da mensagem (sem tags HTML).
- [ ] Projeto `Infraestructure` compila sem erros.

### FR-007: E-mail de Boas-Vindas no Cadastro de Comprador

**What:** Após persistir o comprador com sucesso em `UsuarioService.CadastrarComprador`, o sistema deve enfileirar um e-mail de boas-vindas chamando `_emailSender.EnfileirarAsync(EmailTemplates.BoasVindasComprador(dto.Email, dto.Nome), ct)`. O enfileiramento deve ocorrer APÓS a chamada a `_repository.CadastrarUsuario(novocomprador)` — nunca antes. Se o cadastro falhar, nenhum e-mail é enfileirado.

**Why:** "Confirmação imediata" — o usuário recebe a certeza de que a conta foi criada sem precisar fazer login.

**Acceptance Criteria:**
- [ ] `UsuarioService` injeta `IEmailSender` no construtor.
- [ ] Após `_repository.CadastrarUsuario(novocomprador)` em `CadastrarComprador`, há uma chamada a `_emailSender.EnfileirarAsync`.
- [ ] O e-mail enfileirado tem `Destinatario == dto.Email` e `Assunto` contendo "Bem-vindo".
- [ ] Se `_repository.CadastrarUsuario` lançar exceção, `EnfileirarAsync` NÃO é chamado (a ordem do código garante isso).
- [ ] `CadastrarComprador` não espera o envio do e-mail — retorna imediatamente após enfileirar.

### FR-008: E-mail de Boas-Vindas no Cadastro de Vendedor

**What:** Após persistir o vendedor com sucesso em `UsuarioService.CadastrarVendedor`, o sistema deve enfileirar um e-mail de boas-vindas chamando `_emailSender.EnfileirarAsync(EmailTemplates.BoasVindasVendedor(vendedor.Email, vendedor.NomeFantasia!, "Gratuito"), ct)`. O enfileiramento deve ocorrer APÓS `_repository.CadastrarVendedor(vendedor)` — nunca antes.

**Why:** Igual FR-007, mas para o perfil Vendedor. Inclui NomeFantasia e Plano para personalização.

**Acceptance Criteria:**
- [ ] Após `_repository.CadastrarVendedor(vendedor)` em `CadastrarVendedor`, há uma chamada a `_emailSender.EnfileirarAsync`.
- [ ] O e-mail enfileirado tem `Destinatario == vendedor.Email` e `Assunto` contendo "Comece a vender".
- [ ] Se `CadastrarVendedor` falhar antes da persistência, `EnfileirarAsync` NÃO é chamado.

### FR-009: E-mail de Confirmação de Reserva

**What:** Após persistir a reserva com sucesso em `ReservaService.FazerReserva`, o sistema deve enfileirar um e-mail de confirmação. Para obter o e-mail do comprador, o `ReservaService` deve usar `_repositoryUsuario.BuscarCpf(usuarioCpf, ct)` (dependência já existente). Se o usuário não for encontrado, usar `usuarioCpf` como fallback para o destinatário (não quebrar o fluxo). O e-mail deve incluir: nome do evento, data formatada `dd/MM/yyyy HH:mm`, quantidade de itens, valor total.

**Why:** "Comprovante digital" — o comprador tem um registro da compra no e-mail, mesmo offline.

**Acceptance Criteria:**
- [ ] `ReservaService` injeta `IEmailSender` no construtor (parâmetro adicional).
- [ ] Após `_repositoryReserva.CadastrarReservaComItens(novaReserva, ct)` em `FazerReserva`, há uma chamada a `_emailSender.EnfileirarAsync`.
- [ ] O e-mail contém `nomeEvento` (do objeto `evento` já carregado), `dataEvento`, `quantidadeItens` (`itens.Count`), `valorTotal` (`novaReserva.ValorFinalPago`).
- [ ] O destinatário é o e-mail do usuário logado (buscado via `_repositoryUsuario.BuscarCpf`), com fallback para `usuarioCpf` se não encontrado.
- [ ] Se `CadastrarReservaComItens` lançar exceção, `EnfileirarAsync` NÃO é chamado.

### FR-010: E-mail de Confirmação de Pagamento

**What:** Após processar o pagamento com sucesso em `PagamentoService.ConfirmarCheckout`, o sistema deve enfileirar um e-mail de confirmação de pagamento. O `PagamentoService` deve injetar `IEmailSender`. Para obter o e-mail do comprador, usar `_reservaRepo` (já injetado) para buscar a reserva e dela extrair `UsuarioCpf`, então buscar o e-mail via `IUsuarioRepository` (nova dependência) ou usar `reserva.UsuarioCpf` como fallback.

**Why:** "Segurança e transparência" — o comprador sabe exatamente quanto pagou e quando.

**Acceptance Criteria:**
- [ ] `PagamentoService` injeta `IEmailSender` no construtor.
- [ ] `PagamentoService` injeta `IUsuarioRepository` no construtor (para buscar e-mail do comprador).
- [ ] Após `_pagamentoRepo.CriarComTransacao(pagamento, reserva, evento, ct)` em `ConfirmarCheckout`, há uma chamada a `_emailSender.EnfileirarAsync`.
- [ ] O e-mail contém `nomeEvento`, `valorPago`, `metodo`, `dataPagamento`.
- [ ] Se `CriarComTransacao` lançar exceção, `EnfileirarAsync` NÃO é chamado.

### FR-011: Endpoint `POST /api/usuario/esqueci-senha`

**What:** O sistema deve expor um endpoint público (sem `[Authorize]`) `POST /api/usuario/esqueci-senha` que recebe `{ "email": "string" }` no body (formato JSON). O endpoint chama `UsuarioService.SolicitarRedefinicaoSenha(string email, CancellationToken ct)`. **O endpoint SEMPRE retorna `200 OK`** com body `{ "message": "Se o e-mail estiver cadastrado, um link de redefinição será enviado." }`, independentemente de o e-mail existir ou não no banco.

**Why:** "Autonomia total" + segurança. O usuário recupera acesso sem Admin. Retornar sempre 200 OK impede enumeração de e-mails cadastrados (security best practice).

**Acceptance Criteria:**
- [ ] `UsuarioController` tem método `EsqueciSenha` com `[HttpPost("esqueci-senha")]`.
- [ ] Não possui atributo `[Authorize]`.
- [ ] Recebe `[FromBody] EsqueciSenhaDTO dto` onde `EsqueciSenhaDTO` é `record EsqueciSenhaDTO(string Email)`.
- [ ] Com e-mail existente e usuário ativo: retorna `200 OK`, enfileira e-mail de redefinição.
- [ ] Com e-mail inexistente: retorna `200 OK`, NÃO enfileira e-mail, NÃO lança exceção.
- [ ] Com e-mail existente mas usuário inativo (`Ativo == false`): retorna `200 OK`, NÃO enfileira e-mail.
- [ ] O body da resposta é sempre `{ "message": "Se o e-mail estiver cadastrado, um link de redefinição será enviado." }`.

### FR-012: Endpoint `POST /api/usuario/redefinir-senha`

**What:** O sistema deve expor um endpoint público `POST /api/usuario/redefinir-senha` que recebe `{ "token": "string", "novaSenha": "string" }`. O endpoint chama `UsuarioService.RedefinirSenha(string token, string novaSenha, CancellationToken ct)`. Retorna `200 OK` com `{ "message": "Senha redefinida com sucesso." }` em caso de sucesso. Retorna `400 Bad Request` com mensagem apropriada nos casos de erro.

**Why:** Segunda etapa do fluxo de redefinição. O token JWT garante que apenas o destinatário do e-mail pode redefinir a senha.

**Acceptance Criteria:**
- [ ] `UsuarioController` tem método `RedefinirSenha` com `[HttpPost("redefinir-senha")]`.
- [ ] Não possui atributo `[Authorize]`.
- [ ] Recebe `[FromBody] RedefinirSenhaDTO dto` onde `RedefinirSenhaDTO` é `record RedefinirSenhaDTO(string Token, string NovaSenha)`.
- [ ] Token válido + senha forte (8+ caracteres): retorna `200 OK`, senha atualizada com BCrypt.
- [ ] Token expirado (mais de 15 min): retorna `400 Bad Request` com `{ "message": "Token expirado ou inválido." }`.
- [ ] Token de autenticação (sem claim `purpose=password-reset`): retorna `400 Bad Request` com `{ "message": "Token expirado ou inválido." }`.
- [ ] Token válido mas senha com menos de 8 caracteres: retorna `400 Bad Request` com mensagem da validação de senha (ex: "A senha deve ter no mínimo 8 caracteres.").
- [ ] Token válido mas usuário não encontrado ou inativo: retorna `400 Bad Request` com `{ "message": "Token expirado ou inválido." }`.

### FR-013: Geração de Token JWT de Redefinição de Senha

**What:** `TokenService` deve implementar `GerarTokenRedefinicaoSenha(Usuario usuario)` que gera um JWT com:
- Claim `"email"` = `usuario.Email`
- Claim `"purpose"` = `"password-reset"`
- Claim `"jti"` = `Guid.NewGuid().ToString()`
- **NÃO** inclui claims `cpf`, `perfilId`, `role` (não é token de autenticação)
- Expiração: `DateTime.UtcNow.AddMinutes(15)` (não 24h)
- Assinatura: HMAC-SHA256 com a mesma `Jwt:Key` do token de autenticação
- Mesmo `Issuer` e `Audience` do token de autenticação

**Why:** Token JWT permite validação stateless. `purpose=password-reset` impede que um token de autenticação seja usado para redefinir senha. 15 minutos limita a janela de ataque. Sem claims de autorização, não pode ser usado como token de acesso.

**Acceptance Criteria:**
- [ ] `ITokenService` declara: `string GerarTokenRedefinicaoSenha(Domain.Entities.Usuario usuario);`
- [ ] `TokenService` implementa o método conforme especificado.
- [ ] Token gerado contém claim `purpose` com valor `"password-reset"`.
- [ ] Token gerado NÃO contém claim `role`, `cpf`, ou `perfilId`.
- [ ] Token expira em 15 minutos (`exp` claim = `DateTime.UtcNow.AddMinutes(15)`).
- [ ] Token usa a mesma `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience` do `GerarToken` existente.

### FR-014: Validação de Token JWT de Redefinição de Senha

**What:** `TokenService` deve implementar `ValidarTokenRedefinicaoSenha(string token)` que:
1. Valida o JWT usando os mesmos `TokenValidationParameters` do `Program.cs` (issuer, audience, lifetime, signing key).
2. Após validação, verifica se a claim `"purpose"` existe e tem valor `"password-reset"`.
3. Se tudo válido, retorna o valor da claim `"email"` (string).
4. Em qualquer falha (token expirado, assinatura inválida, purpose ausente, purpose != "password-reset"), retorna `null`.
5. NUNCA lança exceção — toda falha resulta em `null`.

**Why:** Validação dupla (assinatura + purpose) garante que apenas tokens de redefinição são aceitos. Retornar `null` em vez de lançar exceção simplifica o tratamento no service caller.

**Acceptance Criteria:**
- [ ] `ITokenService` declara: `string? ValidarTokenRedefinicaoSenha(string token);`
- [ ] `TokenService` implementa o método conforme especificado.
- [ ] Token válido com `purpose=password-reset` → retorna o email.
- [ ] Token expirado → retorna `null`.
- [ ] Token com assinatura inválida → retorna `null`.
- [ ] Token sem claim `purpose` → retorna `null`.
- [ ] Token com `purpose` diferente de `"password-reset"` → retorna `null`.
- [ ] O método NUNCA lança exceção (usa try/catch internamente).

### FR-015: Método `SolicitarRedefinicaoSenha` no `UsuarioService`

**What:** `UsuarioService` deve implementar `SolicitarRedefinicaoSenha(string email, CancellationToken ct)`:
1. Buscar usuário por e-mail: `_repository.BuscarEmail(email, ct)`.
2. Se `usuario == null || !usuario.Ativo` → retornar sem fazer nada (não enfileirar e-mail, não lançar exceção).
3. Gerar token: `_tokenservice.GerarTokenRedefinicaoSenha(usuario)`.
4. Construir link: `$"{baseUrl}/redefinir-senha?token={Uri.EscapeDataString(token)}"` onde `baseUrl` vem de `_configuration["App:BaseUrl"]` com fallback `"https://soldouttickets.com"`.
5. Enfileirar e-mail: `_emailSender.EnfileirarAsync(EmailTemplates.RedefinicaoSenha(usuario.Email, usuario.Nome, link), ct)`.

**Why:** Lógica de negócio do fluxo "esqueci minha senha". A verificação de `Ativo` impede envio para contas desativadas.

**Acceptance Criteria:**
- [ ] `IUsuarioService` declara: `Task SolicitarRedefinicaoSenha(string email, CancellationToken ct);`
- [ ] E-mail existente e usuário ativo → token gerado, e-mail enfileirado.
- [ ] E-mail inexistente → retorna sem enfileirar, sem exceção.
- [ ] E-mail existente mas `Ativo == false` → retorna sem enfileirar, sem exceção.
- [ ] `UsuarioService` injeta `IConfiguration` no construtor para ler `App:BaseUrl`.
- [ ] O link no e-mail usa `Uri.EscapeDataString(token)` para garantir que o token é seguro em URL.
- [ ] `appsettings.json` contém seção `"App": { "BaseUrl": "https://localhost:5001" }`.

### FR-016: Método `RedefinirSenha` no `UsuarioService`

**What:** `UsuarioService` deve implementar `RedefinirSenha(string token, string novaSenha, CancellationToken ct)`:
1. Validar token: `var email = _tokenservice.ValidarTokenRedefinicaoSenha(token)` — se `null`, lançar `TokenRedefinicaoInvalido`.
2. Buscar usuário: `var usuario = await _repository.BuscarEmail(email, ct)` — se `null || !usuario.Ativo`, lançar `TokenRedefinicaoInvalido`.
3. Validar nova senha: `Usuario.ValidarSenhaBruta(novaSenha)` — se inválida, a própria validação lança exceção (ex: `Senha8digitos`).
4. Hashear: `BCrypt.Net.BCrypt.HashPassword(novaSenha)`.
5. Atualizar entidade: `usuario.AlterarSenha(senhaHash)`.
6. Persistir: `await _repository.AtualizarSenha(usuario.Cpf, senhaHash, ct)`.

**Why:** Conclui o fluxo de redefinição. Validação em múltiplas camadas (token expiry + purpose + senha forte + BCrypt hash) garante segurança.

**Acceptance Criteria:**
- [ ] `IUsuarioService` declara: `Task RedefinirSenha(string token, string novaSenha, CancellationToken ct);`
- [ ] Token inválido/expirado → lança `TokenRedefinicaoInvalido` com mensagem "Token expirado ou inválido."
- [ ] Token válido + senha forte → senha atualizada no banco, 200 OK.
- [ ] Token válido + senha fraca (5 caracteres) → lança exceção de validação de senha.
- [ ] Senha é hasheada com BCrypt antes de persistir (nunca armazenada em texto plano).
- [ ] `Application/Exceptions/TokenRedefinicaoExceptions.cs` existe com classe `TokenRedefinicaoInvalido : Exception`.

### FR-017: Registro de Injeção de Dependência

**What:** `Api/Program.cs` deve registrar:
- `builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"))` — configuração tipada.
- `builder.Services.AddScoped<SmtpEmailSender>()` — um por request HTTP (scoped).
- `builder.Services.AddSingleton<EmailBackgroundWorker>()` — mesma instância compartilha o Channel.
- `builder.Services.AddSingleton<IEmailSender>(sp => sp.GetRequiredService<EmailBackgroundWorker>())` — a interface resolve para o singleton.
- `builder.Services.AddHostedService(sp => sp.GetRequiredService<EmailBackgroundWorker>())` — inicia o worker como hosted service.

**Why:** DI é a cola da aplicação. O `EmailBackgroundWorker` é singleton para que producers e consumer compartilhem o mesmo Channel. É também `IHostedService` para iniciar/parar com a aplicação.

**Acceptance Criteria:**
- [ ] `Program.cs` contém as 5 linhas de registro especificadas.
- [ ] Nenhuma dependência não resolvida ao iniciar a API.
- [ ] `EmailBackgroundWorker.ExecuteAsync` inicia junto com a aplicação.
- [ ] `IEmailSender` injetado em `UsuarioService`, `ReservaService` e `PagamentoService` resolve para a mesma instância do `EmailBackgroundWorker`.

---

## Non-Functional Requirements

### NFR-001: Performance — E-mail Não Bloqueia Resposta HTTP

**What:** O enfileiramento de e-mail (`EnfileirarAsync`) deve retornar em menos de 1ms (operações normais de escrita em Channel). O envio real (SMTP) ocorre em background, nunca no thread da requisição HTTP.

**Acceptance Criteria:**
- [ ] `Channel<T>.Writer.WriteAsync` é a única operação em `EnfileirarAsync` — sem await de operação SMTP.
- [ ] Tempo de resposta dos endpoints (`CadastrarComprador`, `FazerReserva`, `ConfirmarCheckout`) não aumenta mais que 5ms após integração do e-mail.

### NFR-002: Resiliência — Graceful Degradation sem SMTP

**What:** Se a seção `"Smtp"` estiver ausente ou com `Host` vazio, o sistema deve iniciar e operar normalmente. E-mails são descartados com log warning. Nenhum endpoint retorna 500 por falta de SMTP.

**Acceptance Criteria:**
- [ ] Sem seção `"Smtp"` no `appsettings.json` → API sobe normalmente.
- [ ] Com `"Smtp.Host": ""` → API sobe normalmente.
- [ ] `CadastrarComprador` conclui sem erros mesmo sem SMTP (e-mail enfileirado, worker descarta com warning).
- [ ] Nenhum log de erro (`LogError`) é emitido por falta de configuração SMTP — apenas `LogWarning`.

### NFR-003: Segurança — Anti-Enumeração de Usuários

**What:** O endpoint `esqueci-senha` nunca pode revelar se um e-mail está cadastrado. Resposta e tempo de resposta devem ser idênticos para e-mails existentes e inexistentes.

**Acceptance Criteria:**
- [ ] Body da resposta é idêntico para e-mail existente e inexistente.
- [ ] Status code é sempre `200`.
- [ ] Não há diferença mensurável de tempo de resposta (>100ms) entre e-mail existente e inexistente (a diferença é apenas a busca no banco + geração de token, que é desprezível comparada a latência de rede).

### NFR-004: Segurança — Token de Reset Não Concede Acesso

**What:** Um token JWT gerado para redefinição de senha (`purpose=password-reset`) não pode ser usado como token de autenticação em nenhum endpoint protegido.

**Acceptance Criteria:**
- [ ] Token de redefinição não contém claim `role` → `[Authorize(Roles = "...")]` rejeita com 401.
- [ ] Token de redefinição não contém claim `cpf` → middlewares que extraem `cpf` do token não encontram o valor.
- [ ] `ValidarTokenRedefinicaoSenha` rejeita tokens sem `purpose=password-reset`.

### NFR-005: Manutenibilidade — Configuração Externalizada

**What:** Todas as configurações (SMTP, URL base) devem ser lidas do sistema de configuração do .NET (`appsettings.json`, `user-secrets`, variáveis de ambiente), nunca hardcoded.

**Acceptance Criteria:**
- [ ] `Host`, `Username`, `Password`, `FromAddress` nunca aparecem como strings literais no código (apenas como chaves de configuração).
- [ ] `App:BaseUrl` é configurável em `appsettings.json`.
- [ ] Valores padrão (Port=587, EnableSsl=true, FromName="SoldOut Tickets") são definidos como defaults da classe, não no `appsettings.json`.

### NFR-006: Observabilidade — Logging Estruturado

**What:** O sistema deve logar informações estruturadas via `ILogger`:
- `LogInformation`: e-mail enviado com sucesso (destinatário, assunto).
- `LogWarning`: SMTP não configurado (destinatário), tentativa de reenvio N/M (destinatário, tentativa, delay).
- `LogError`: falha após 3 tentativas (destinatário, tentativas).

**Acceptance Criteria:**
- [ ] Logs usam interpolação estruturada (`"E-mail enviado para {Destinatario}"`), não concatenação de strings.
- [ ] Nenhum `Console.WriteLine` — apenas `ILogger`.
- [ ] Mensagens de log estão em português (padrão do projeto).

---

## Constraints

- **C1:** MailKit deve ser adicionado ao projeto `Infraestructure` (não ao `Api` ou `Application`). O `Application` não referencia MailKit.
- **C2:** O `Domain` não pode referenciar nenhum pacote externo além dos já existentes. `EmailMessage` e `IEmailSender` não podem depender de MailKit, MimeKit, ou qualquer biblioteca de e-mail.
- **C3:** A fila de e-mails é em memória (`Channel<T>`, sem persistência). E-mails enfileirados e não processados são perdidos em caso de restart da aplicação. Isso é aceitável para e-mails transacionais (ADR-013).
- **C4:** Templates de e-mail são strings constantes em C# — não usar Razor, Liquid, ou qualquer template engine.
- **C5:** O token de redefinição de senha usa a mesma chave JWT (`Jwt:Key`) e mesmos parâmetros (`Issuer`, `Audience`) do token de autenticação. A diferenciação é feita pela claim `purpose`.
- **C6:** O sistema deve compilar e rodar em .NET 9, compatível com o resto do projeto.
- **C7:** Nomes de arquivos, namespaces e classes devem seguir o padrão do projeto: PascalCase, namespaces alinhados com a estrutura de pastas.
- **C8:** Os endpoints `esqueci-senha` e `redefinir-senha` devem ser públicos (sem `[Authorize]`), consistente com o fato de que o usuário não está autenticado quando esquece a senha.

---

## Edge Cases & Error States

| # | Caso | Comportamento Esperado |
|---|------|------------------------|
| **E1** | Seção `"Smtp"` completamente ausente do `appsettings.json` | `SmtpSettings` assume defaults. `Configurado` retorna `false`. Worker loga warning ao processar cada e-mail e descarta. |
| **E2** | `Smtp.Host` configurado mas servidor SMTP offline | Worker tenta 3 vezes (2s, 4s, 8s). Após 3 falhas, loga `LogError` e descarta a mensagem. Não afeta outras mensagens na fila. |
| **E3** | `CadastrarComprador` falha após enfileirar e-mail | IMPOSSÍVEL por design — `EnfileirarAsync` é a última linha do método, após `_repository.CadastrarUsuario`. Se o repositório falhar, a exceção é lançada antes de enfileirar. |
| **E4** | Usuário solicita redefinição para conta inativa (`Ativo == false`) | NÃO envia e-mail. Retorna 200 OK com a mesma mensagem genérica. Comportamento idêntico a e-mail inexistente. |
| **E5** | Token de redefinição é reutilizado após sucesso | O token é validado, mas o usuário é buscado novamente e a senha já foi alterada. O segundo uso redefiniria a senha novamente, o que é aceitável (token válido até expirar). O token NÃO é invalidado após primeiro uso porque é stateless (JWT). A janela de 15 minutos limita o risco. |
| **E6** | `Channel<T>` cheio (100 mensagens pendentes) | `Channel.CreateBounded` com `FullMode = Wait` faz o producer (thread HTTP) aguardar até que haja espaço. Isso cria backpressure natural — a resposta HTTP demora mais, mas não perde mensagens. |
| **E7** | `ReservaService` enfileira e-mail mas `_repositoryUsuario.BuscarCpf` retorna null | Usar `usuarioCpf` (string) como fallback para o campo `Destinatario`. O e-mail não será entregue (não é um e-mail válido), mas o fluxo não quebra. Logar warning. |
| **E8** | Evento pago mas gratuito (PrecoPadrao=0 com cupom de 100%) | O e-mail de pagamento ainda é enviado com `valorPago = 0`. Isso é correto — houve uma transação de pagamento. |
| **E9** | Concorrência: 10 cadastros simultâneos | Cada um escreve no Channel. Se o Channel tiver espaço, todos escrevem sem bloquear. Se encher, os últimos aguardam. Worker processa sequencialmente. |
| **E10** | `RedefinirSenha` chamado com CPF formatado (pontos e traços) | Não se aplica — o método recebe token, não CPF. O e-mail extraído do token é usado para buscar o usuário via `BuscarEmail`. |
| **E11** | Worker é interrompido durante envio (aplicação shutting down) | O `CancellationToken` é propagado para `SmtpClient`. MailKit cancela a operação. O e-mail é perdido (consistente com fila em memória). |

---

## Dependencies

### Internal (Specs do Projeto)
- **Spec 120 (Segurança):** BCrypt e JWT já implementados. `UsuarioService` já usa `BCrypt.HashPassword` e `BCrypt.Verify`. `TokenService` já usa `JwtSecurityTokenHandler`. Esta spec estende `TokenService` e `UsuarioService`.
- **Spec 40 (Cancelamento de Reserva):** `IEmailSender` fica disponível para envio de e-mail de reembolso quando a spec for implementada. Nenhuma dependência de build — apenas disponibilidade de interface.
- **Spec 50 (Cancelamento de Evento):** Idem spec 40. `EmailTemplates.ReembolsoConfirmado` já existe como template pronto.

### External (Pacotes NuGet)
- **MailKit:** Adicionado ao projeto `Infraestructure` (via `dotnet add package MailKit`). Usado apenas por `SmtpEmailSender`.
- **System.Threading.Channels:** Já disponível no .NET 9 runtime. Usado por `EmailBackgroundWorker`.

### Configuration
- **`Jwt:Key`**, **`Jwt:Issuer`**, **`Jwt:Audience`**: Já existem em `user-secrets` (spec 120). Reutilizados para token de redefinição.
- **`Smtp`**: Nova seção no `appsettings.json`.
- **`App:BaseUrl`**: Nova seção no `appsettings.json`.

---

## Out of Scope

- Sistema de templates de e-mail (Razor, Liquid, Handlebars) — templates são strings constantes em C#.
- Fila persistente (banco de dados, RabbitMQ, Azure Service Bus) — v1 usa `Channel<T>` em memória.
- Gateway de SMS, notificações push, ou qualquer canal além de e-mail.
- Confirmação de e-mail (verificação de conta) no cadastro — usuário não precisa validar e-mail para usar a conta.
- Envio de e-mails em lote (newsletter, marketing).
- E-mails com anexos (PDF de ingresso, QR code).
- Dashboard de e-mails enviados/falhos.
- Métricas de entrega (taxa de bounce, abertura, clique).
- Suporte a múltiplos provedores SMTP simultaneamente.
- Envio de e-mail de reembolso — o template `ReembolsoConfirmado` existe, mas a integração no fluxo de cancelamento pertence às specs 40 e 50.
