## ADR-013: MailKit + Channel para E-mails Transacionais (spec 180)

**Status:** ✅ Aceito

**Contexto:** O sistema precisa enviar e-mails transacionais (boas-vindas, confirmação de reserva, confirmação de pagamento, reembolso, redefinição de senha) sem bloquear as respostas HTTP. A solução precisa funcionar em desenvolvimento mesmo sem SMTP configurado.

**Alternativas consideradas:**

| Opção | Prós | Contras |
|-------|------|---------|
| **MailKit + Channel<T> (fila em memória)** | Fire-and-forget, sem bloqueio HTTP, graceful degradation, zero dependência de infra | E-mails não sobrevivem a restart (aceitável para transacionais) |
| Envio síncrono no request | Simples, sem worker | Bloqueia resposta HTTP, timeout do SMTP = timeout da API |
| Fila persistente (RabbitMQ/SQS) | Resiste a restart, escalável | Infra complexa demais para o escopo atual |
| SendGrid / Mailgun SDK | Serviço gerenciado, sem SMTP | Custo, dependência externa, lock-in de provedor |

**Decisão:** Interface `IEmailSender` no Domain, implementada por `EmailBackgroundWorker` (IHostedService singleton) com `Channel<EmailMessage>` bounded para 100 mensagens. Envio real via `SmtpEmailSender` com MailKit. Templates como strings constantes em `EmailTemplates`. Se SMTP não configurado, worker opera em modo no-op (log warning, descarta mensagem).

**Consequências:**
- `IEmailSender.EnfileirarAsync()` é chamado após persistência bem-sucedida — nunca antes
- Worker tenta 3 vezes com backoff exponencial (2s, 4s, 8s) antes de desistir
- `Channel<T>` com `BoundedChannelOptions(100)` e `FullMode = Wait` — backpressure natural
- Redefinição de senha: `POST /api/usuario/esqueci-senha` (sempre retorna 200 OK — anti-enumeração) + `POST /api/usuario/redefinir-senha` (JWT com claim `purpose=password-reset`, 15 min)
- Token de redefinição NÃO é aceito como token de autenticação (validação dupla: assinatura + purpose)
- Configuração SMTP externalizada em `appsettings.json` seção "Smtp"
- Pacote NuGet `MailKit` adicionado ao projeto `Infraestructure`

---

