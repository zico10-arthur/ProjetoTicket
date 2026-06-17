# Spec 180 — Requirements: Serviço de E-mail Transacional + Redefinição de Senha

> **Projeto:** SoldOut Tickets
> **Contexto:** [`visao.md §6.9`](../../../visao.md#69-e-mails-transacionais-e-redefinição-de-senha) | [`visao.md §10`](../../../visao.md#10-escopo-atual-v20)
> **Status:** `pendente`

---

## 1. Objetivo

Criar a infraestrutura de envio de e-mails transacionais via SMTP (MailKit) com fila em memória processada por background worker, integrar e-mails de boas-vindas, reserva confirmada, pagamento confirmado e reembolso confirmado nos fluxos existentes, e adicionar o fluxo completo de "esqueci minha senha" com token JWT de curta duração enviado por e-mail.

---

## 2. Histórias de Usuário

### HU-EM01: Receber e-mail de boas-vindas ao se cadastrar

**Como** um novo usuário (Comprador ou Vendedor),
**Quero** receber um e-mail de boas-vindas após me cadastrar,
**Para** ter a confirmação de que minha conta foi criada com sucesso.

### HU-EM02: Receber confirmação de reserva por e-mail

**Como** um comprador,
**Quero** receber um e-mail confirmando minha reserva,
**Para** ter um comprovante com os detalhes da compra.

### HU-EM03: Receber confirmação de pagamento por e-mail

**Como** um comprador,
**Quero** receber um e-mail confirmando que meu pagamento foi processado,
**Para** ter a segurança de que meus ingressos estão garantidos.

### HU-EM04: Receber confirmação de reembolso por e-mail

**Como** um comprador,
**Quero** receber um e-mail confirmando que meu reembolso foi processado,
**Para** ter a garantia de que receberei o valor de volta.

### HU-EM05: Redefinir senha esquecida

**Como** um usuário que esqueceu a senha,
**Quero** solicitar a redefinição da minha senha via e-mail,
**Para** recuperar o acesso à minha conta sem depender de um administrador.

---

## 3. Requisitos Funcionais

| ID | Descrição |
|----|-----------|
| RF-EM01 | O sistema deve ter uma interface `IEmailSender` para envio de e-mails |
| RF-EM02 | O sistema deve implementar `IEmailSender` via SMTP usando MailKit |
| RF-EM03 | O sistema deve processar e-mails em background via `IHostedService` com fila `Channel<EmailMessage>` |
| RF-EM04 | O sistema deve enfileirar e-mail de boas-vindas ao cadastrar Comprador (`CadastrarComprador`) |
| RF-EM05 | O sistema deve enfileirar e-mail de boas-vindas ao cadastrar Vendedor (`CadastrarVendedor`) |
| RF-EM06 | O sistema deve enfileirar e-mail de confirmação ao criar uma reserva (`FazerReserva`) |
| RF-EM07 | O sistema deve enfileirar e-mail de confirmação ao confirmar um pagamento (`ConfirmarCheckout`) |
| RF-EM08 | O sistema deve expor `IEmailSender` para uso futuro no fluxo de reembolso (ST-05/ST-06) |
| RF-EM09 | O sistema deve ter endpoint `POST /api/usuario/esqueci-senha` que recebe e-mail e envia token de redefinição |
| RF-EM10 | O sistema deve ter endpoint `POST /api/usuario/redefinir-senha` que recebe token + nova senha e atualiza a senha |
| RF-EM11 | O token de redefinição de senha deve ser um JWT com 15 minutos de validade e claim `purpose=password-reset` |
| RF-EM12 | O endpoint `esqueci-senha` deve sempre retornar 200 OK (não vaza se o e-mail existe ou não) |
| RF-EM13 | A nova senha deve ser validada e hasheada com BCrypt antes de persistir |
| RF-EM14 | O token de redefinição não deve ser aceito como token de autenticação (claim `purpose` bloqueia) |
| RF-EM15 | O background worker deve tentar reenviar e-mails com falha até 3 vezes com backoff exponencial |
| RF-EM16 | Se a configuração SMTP estiver ausente, o worker deve logar warning e descartar mensagens (não quebrar a aplicação) |

---

## 4. Requisitos Não Funcionais

| ID | Descrição |
|----|-----------|
| RNF-EM01 | Configuração SMTP deve ser externalizada em `appsettings.json` seção `"Smtp"` |
| RNF-EM02 | Em desenvolvimento sem SMTP configurado, o sistema deve subir normalmente (graceful degradation) |
| RNF-EM03 | E-mails devem ser enviados em HTML com fallback texto plano |
| RNF-EM04 | A fila de e-mails é em memória (`Channel<T>`); e-mails não sobrevivem a restart |
| RNF-EM05 | O envio de e-mail não deve bloquear a resposta HTTP (fire-and-forget via Channel) |
| RNF-EM06 | Dados sensíveis (senha) nunca devem aparecer no corpo do e-mail |
| RNF-EM07 | O pacote NuGet `MailKit` deve ser adicionado ao projeto `Infraestructure` |

---

## 5. Critérios de Aceitação (BDD)

### HU-EM01: E-mail de boas-vindas

**Cenário 1 — Boas-vindas ao Comprador**
- **Dado** que um novo usuário preenche CPF, nome, e-mail e senha válidos
- **Quando** o endpoint `POST /api/usuario/CadastrarComprador` é chamado com sucesso
- **Então** um e-mail de boas-vindas é enfileirado para o e-mail do usuário contendo seu nome e informando que a conta foi criada

**Cenário 2 — Boas-vindas ao Vendedor**
- **Dado** que uma empresa preenche CNPJ válido, Razão Social, Nome Fantasia, E-mail, Senha e Telefone
- **Quando** o endpoint `POST /api/usuario/cadastrar-vendedor` é chamado com sucesso
- **Então** um e-mail de boas-vindas é enfileirado para o e-mail do vendedor com Nome Fantasia e Plano "Gratuito"

### HU-EM02: Confirmação de reserva

**Cenário 1 — Reserva confirmada**
- **Dado** que um comprador está logado e seleciona um evento com ingressos disponíveis
- **Quando** o endpoint `POST /api/reserva/criar` é chamado com sucesso
- **Então** um e-mail de confirmação é enfileirado com nome do evento, data, quantidade de itens e valor total

### HU-EM03: Confirmação de pagamento

**Cenário 1 — Pagamento confirmado**
- **Dado** que um comprador tem uma reserva pendente de pagamento
- **Quando** o endpoint `POST /api/pagamento/checkout/{reservaId}` é chamado com sucesso
- **Então** um e-mail de confirmação de pagamento é enfileirado com valor pago, método e data do pagamento

### HU-EM04: Confirmação de reembolso

**Cenário 1 — Reembolso processado**
- **Dado** que uma reserva paga é cancelada (via ST-05 ou ST-06)
- **Quando** o reembolso é processado com sucesso
- **Então** um e-mail de confirmação de reembolso é enfileirado com valor reembolsado e dados da reserva

### HU-EM05: Redefinição de senha

**Cenário 1 — Solicitação de redefinição (e-mail existe)**
- **Dado** que um usuário cadastrado acessa a tela de "Esqueci minha senha"
- **Quando** ele informa seu e-mail em `POST /api/usuario/esqueci-senha`
- **Então** o sistema retorna `200 OK` com mensagem genérica e envia um e-mail com link contendo token JWT válido por 15 minutos

**Cenário 2 — Solicitação de redefinição (e-mail não existe)**
- **Dado** que um e-mail não cadastrado é informado
- **Quando** `POST /api/usuario/esqueci-senha` é chamado
- **Então** o sistema retorna `200 OK` com a mesma mensagem genérica, mas NENHUM e-mail é enviado

**Cenário 3 — Redefinição com token válido**
- **Dado** que o usuário recebeu o e-mail com token de redefinição
- **Quando** ele acessa `POST /api/usuario/redefinir-senha` com o token e uma nova senha válida (8+ caracteres)
- **Então** a senha é atualizada com BCrypt, retorna `200 OK`, e o usuário pode fazer login com a nova senha

**Cenário 4 — Redefinição com token expirado**
- **Dado** que o token de redefinição expirou (mais de 15 minutos)
- **Quando** o usuário tenta redefinir a senha
- **Então** o sistema retorna `400 Bad Request` com "Token expirado ou inválido."

**Cenário 5 — Redefinição com token de autenticação (não é token de reset)**
- **Dado** que o usuário tenta usar um token JWT de login normal
- **Quando** `POST /api/usuario/redefinir-senha` é chamado
- **Então** o sistema retorna `400 Bad Request` com "Token inválido para redefinição de senha."

**Cenário 6 — Redefinição com senha fraca**
- **Dado** que o token é válido
- **Quando** o usuário informa uma senha com menos de 8 caracteres
- **Então** o sistema retorna `400 Bad Request` com "A senha deve ter no mínimo 8 caracteres."

---

## 6. Casos de Borda

| # | Caso | Comportamento esperado |
|---|------|----------------------|
| B1 | SMTP não configurado no `appsettings.json` | Worker loga warning e descarta mensagens. Sistema funciona normalmente. |
| B2 | SMTP offline ou inacessível | Worker tenta 3 vezes com backoff (2s, 4s, 8s). Após 3 falhas, loga erro e descarta. |
| B3 | E-mail de redefinição de senha para conta inativa (`Ativo = false`) | Não envia e-mail. Retorna 200 OK com mensagem genérica. |
| B4 | Token de redefinição reutilizado | Após primeiro uso bem-sucedido, o token não pode ser reutilizado (invalidado pela troca de senha). |
| B5 | Concorrência no Channel (múltiplos e-mails enfileirados simultaneamente) | `Channel<T>` é thread-safe por padrão. Worker processa sequencialmente. |
| B6 | E-mail de boas-vindas para cadastro que falhou (exceção após enfileirar) | O e-mail NÃO deve ser enfileirado antes da persistência. Enfileirar APÓS sucesso no banco. |
| B7 | Evento cancelado no momento do envio do e-mail de reserva | O e-mail já foi enfileirado com os dados do momento da reserva. Isso é aceitável — o e-mail reflete o estado no momento da ação. |

---

## 7. Escopo

### Dentro do escopo
- Interface `IEmailSender` no Domain e implementação SMTP via MailKit
- Background worker `EmailBackgroundWorker` com fila `Channel<EmailMessage>`
- Value object `EmailMessage` (destinatário, assunto, corpo HTML, corpo texto)
- Configuração `SmtpSettings` em `appsettings.json`
- Integração de e-mail de boas-vindas no `UsuarioService` (Comprador e Vendedor)
- Integração de e-mail de confirmação de reserva no `ReservaService`
- Integração de e-mail de confirmação de pagamento no `PagamentoService`
- Integração de e-mail de reembolso como hook disponível para ST-05/ST-06
- Endpoint `POST /api/usuario/esqueci-senha`
- Endpoint `POST /api/usuario/redefinir-senha`
- Geração de token JWT de redefinição com claim `purpose=password-reset`

### Fora do escopo
- Sistema de templates de e-mail (Razor, Liquid, etc.) — templates são strings constantes no código
- Fila persistente (banco de dados) — v1 usa Channel em memória
- Gateway de SMS ou notificações push
- Confirmação de e-mail (verificação de conta) no cadastro
- Envio de e-mails em lote (newsletter, marketing)
- E-mails com anexos (PDF de ingresso)
- Dashboard de e-mails enviados/falhos
