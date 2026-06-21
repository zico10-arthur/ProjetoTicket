# ADR — Architecture Decision Records

> **Projeto:** SoldOut Tickets  
> **Data:** 04/06/2026

Registro de decisões arquiteturais do sistema. Cada ADR documenta o contexto, alternativas consideradas e a justificativa da escolha.

---

## ADR-001: Clean Architecture com 4 camadas

**Status:** ✅ Aceito

**Contexto:** O sistema precisa ser testável, manutenível e permitir troca de componentes externos (banco, framework) sem reescrever regras de negócio.

**Alternativas consideradas:**

| Opção | Prós | Contras |
|-------|------|---------|
| Clean Architecture (Domain/Application/Infra/Api) | Isolamento de regras de negócio, testabilidade, independência de frameworks | Mais camadas = mais arquivos |
| Monolito com pastas por feature | Simples, rápido de começar | Regras de negócio misturadas com infra, difícil testar |
| Vertical Slice | Features independentes, baixo acoplamento | Difícil para iniciantes, padrão menos conhecido |

**Decisão:** Clean Architecture com 4 camadas.

**Consequências:**
- `Domain/` não importa nenhuma camada externa — entidades com regras de negócio encapsuladas
- `Application/` depende apenas do Domain — serviços, DTOs, interfaces
- `Infraestructure/` implementa interfaces do Domain — repositórios, conexões, migrations
- `Api/` orquestra tudo — controllers, middlewares, injeção de dependências

---

## ADR-002: Dapper como ORM

**Status:** ✅ Aceito

**Contexto:** Precisamos acessar dados relacionais com performance e controle sobre o SQL gerado.

**Alternativas consideradas:**

| Opção | Prós | Contras |
|-------|------|---------|
| **Dapper** | Leve, rápido, SQL explícito, curva baixa | Sem tracking de mudanças, queries manuais |
| Entity Framework Core | Mudanças automáticas, migrations integradas, LINQ | Overhead, SQL gerado menos previsível, N+1 silencioso |
| ADO.NET puro | Máximo controle | Muito código boilerplate |

**Decisão:** Dapper + DbUp para migrations.

**Consequências:**
- SQL escrito manualmente com parâmetros (`@param`) — proteção contra SQL Injection
- DbUp executa scripts SQL versionados na inicialização
- Sem lazy loading — queries são explícitas e previsíveis
- Menos "mágica" = mais fácil para iniciantes entenderem o que acontece

---

## ADR-003: BCrypt para hash de senhas

**Status:** ✅ Aceito

**Contexto:** O código atual armazena e compara senhas em texto plano. Precisamos de hash seguro com salt automático.

**Alternativas consideradas:**

| Opção | Prós | Contras |
|-------|------|---------|
| **BCrypt (BCrypt.Net-Next)** | Salt automático, work factor configurável, amplamente auditado | 100ms por hash (aceitável) |
| SHA-256 com salt manual | Rápido, nativo do .NET | Precisamos implementar salt, vulnerável a GPU/força bruta |
| Argon2 | Mais moderno que BCrypt, resistente a GPU | Ecossistema .NET menor, menos maduro |
| PBKDF2 | Nativo do .NET (`Rfc2898DeriveBytes`) | Configuração mais complexa, não é padrão para senhas web |

**Decisão:** BCrypt.Net-Next com work factor 11 (padrão da lib).

**Consequências:**
- `BCrypt.HashPassword(senha)` no cadastro
- `BCrypt.Verify(senha, hash)` no login
- Nunca comparar strings de senha diretamente
- Senha do Admin no seed SQL usa hash pré-gerado
- Coluna `Senha` precisa suportar 60+ caracteres (VARCHAR(100))

---

## ADR-004: JWT com roles para autenticação

**Status:** ✅ Aceito

**Contexto:** Três perfis de usuário (Admin, Vendedor, Comprador) compartilham o mesmo endpoint de login e precisam de autorização granular.

**Alternativas consideradas:**

| Opção | Prós | Contras |
|-------|------|---------|
| **JWT com roles** | Stateless, auto-contido, padrão de mercado | Token não pode ser revogado antes de expirar |
| Session + cookies | Revogável, simples | Stateful, não escala horizontalmente, CORS complexo |
| API Keys | Simples para machine-to-machine | Não adequado para usuários finais com perfis |

**Decisão:** JWT Bearer com claims `sub` (CPF/CNPJ), `email`, `role` (Admin/Vendedor/Comprador).

**Consequências:**
- `POST /api/usuario/login` gera JWT único para todos os perfis
- `[Authorize(Roles = "Admin")]` protege endpoints administrativos
- Identidade (`sub`) extraída do token, nunca de parâmetro de rota
- Chave JWT armazenada em `dotnet user-secrets` (nunca no código)

---

## ADR-005: Vendedor como perfil na tabela Usuarios

**Status:** ✅ Aceito

**Contexto:** O sistema anterior tinha uma tabela `Empresas` separada para vendedores. A v2.0 unifica tudo.

**Alternativas consideradas:**

| Opção | Prós | Contras |
|-------|------|---------|
| **Vendedor na tabela Usuarios** | Login unificado, schema simples, FK direta | Colunas nullable para Admin/Comprador |
| Tabela Empresas separada | Separação clara PF vs PJ | Login duplicado, joins extras, dois endpoints de auth |
| Herança (table-per-type) | Modelagem "pura" de OO | Complexo com Dapper, queries com UNION |

**Decisão:** Vendedor como perfil na tabela `Usuarios`. Colunas específicas (`Cnpj`, `NomeFantasia`, `LogoUrl`, etc.) são preenchidas apenas quando `PerfilId = B2B2...`.

**Consequências:**
- `POST /api/usuario/login` funciona para Admin, Vendedor e Comprador
- `Eventos.VendedorId` referencia `Usuarios.Cpf`
- Migração SQL: `ALTER TABLE Usuarios ADD Cnpj, NomeFantasia, ...`
- Campo `Cpf` armazena CPF (PF) ou CNPJ (Vendedor) como string de 14 caracteres

---

## ADR-006: Todos os eventos têm assentos numerados

**Status:** ✅ Aceito

**Contexto:** A versão inicial do storytelling diferenciava Palestra (sem assentos, controle de vagas) de Teatro (com assentos). Após revisão, decidiu-se que todos os eventos terão assentos.

**Alternativas consideradas:**

| Opção | Prós | Contras |
|-------|------|---------|
| **Todos com assentos** | Modelo único, ItemReserva sempre tem IngressoId, geração simplificada | Palestras não precisam de mapa complexo |
| Palestra sem assentos (controle de vagas) | Simples para workshops | Dois fluxos de reserva, IngressoId nullable, queries diferentes |
| Só Teatro (sem Palestra) | Um tipo só, nada de enum | Não atende o nicho principal (workshops, cursos) |

**Decisão:** Ambos os tipos (`Teatro` e `Palestra`) geram ingressos na criação do evento. A diferença está na numeração e distribuição: Teatro usa filas de 20 + setores VIP/Geral; Palestra usa "Assento 1" a "Assento N", todos setor Geral.

**Consequências:**
- `ItemReserva.IngressoId` é `NOT NULL` — sempre aponta para um assento
- `GerarLoteIngressos()` chamado para ambos os tipos
- Enum `TipoEvento` mantido para diferenciar a experiência visual (mapa de filas vs grid simples)
- Query de disponibilidade: `COUNT(*) FROM Ingressos WHERE Status = 0` (igual para ambos)

---

## ADR-007: Cupons globais gerenciados por Admin

**Status:** ✅ Aceito

**Contexto:** Cupons de desconto precisam ser criados e gerenciados. Inicialmente considerou-se vincular cupons a vendedores.

**Alternativas consideradas:**

| Opção | Prós | Contras |
|-------|------|---------|
| **Cupons globais (Admin)** | Simples, Admin tem controle total, comprador usa em qualquer evento | Vendedor não pode criar seus próprios cupons |
| Cupons por vendedor (VendedorId) | Vendedor autônomo para marketing | Complexidade de isolamento, Admin precisa gerenciar per-vendedor |
| Ambos (Admin global + Vendedor local) | Flexibilidade máxima | Duas tabelas ou lógica condicional complexa |

**Decisão:** Cupons são globais. Apenas Admin cria e gerencia. Qualquer comprador aplica em qualquer evento pago.

**Consequências:**
- Tabela `Cupons` sem coluna `VendedorId`
- `CupomController` com `[Authorize(Roles = "Admin")]`
- Validação no uso: ativo, não expirado, valor mínimo atingido, evento não gratuito
- Sem restrição de "cupom do mesmo vendedor"

---

## ADR-008: DbUp para migrations versionadas

**Status:** ✅ Aceito

**Contexto:** O schema do banco evolui com as specs. Precisamos versionar e automatizar alterações.

**Alternativas consideradas:**

| Opção | Prós | Contras |
|-------|------|---------|
| **DbUp** | Scripts SQL puros, ordem explícita, idempotente, roda na inicialização | Sem rollback automático, sem "estado" do schema |
| EF Core Migrations | Snapshots do modelo C#, rollback, ferramenta visual | Acoplado ao EF, se não usamos EF pra queries não faz sentido |
| Scripts manuais | Controle total | Esquecível, erro humano, sem automação |

**Decisão:** DbUp com scripts SQL numerados (`Script0001_...sql`) executados na inicialização da API.

**Consequências:**
- Scripts em `Infraestructure/DataBase/Scripts/` com numeração sequencial
- Toda migração usa `IF NOT EXISTS` para ser idempotente
- `DatabaseMigration.cs` configura e executa na inicialização
- Ordem dos scripts é explícita — sem "mágica"

---

## ADR-009: Background Worker para liberação de assentos (ATUALIZADO — spec 190)

**Status:** ✅ Aceito (atualizado em 17/06/2026)

**Contexto:** Quando um comprador inicia o checkout, o assento fica com `Status=1` (Reservado). Se ele abandonar, o assento precisa voltar ao estado Livre. A v1 usava `BackgroundService` com `PeriodicTimer`, mas a ausência de persistência e monitoramento levou à substituição pelo Hangfire na v2.

**Alternativas consideradas:**

| Opção | Prós | Contras |
|-------|------|---------|
| BackgroundService (timer) | Nativo do .NET, simples, sem dependência externa | Executa em memória, para se a API parar, sem dashboard, sem retry |
| **Hangfire** | Jobs persistentes em banco, dashboard com histórico, retry automático, mesma connection string | Dependência externa (NuGet), tabelas extras no banco |
| Quartz.NET | Scheduling cron-like, persistente | Mais complexo de configurar que Hangfire, sem dashboard built-in |

**Decisão:** **Hangfire** com storage SQL Server (mesmo banco do projeto). Job `LiberacaoAssentosJob.ExecutarAsync()` executado a cada 60 segundos via `Cron.Minutely`. O `BackgroundService` anterior (`LiberacaoAssentosWorker.cs`) foi removido.

**Consequências:**
- `LiberacaoAssentosJob.cs` em `Api/BackgroundTasks/` — mesmo diretório, implementação diferente
- `[DisableConcurrentExecution(timeoutInSeconds: 120)]` impede execuções paralelas do mesmo job
- Dashboard em `/hangfire` protegido por `HangfireAdminAuthorizationFilter` (apenas Admin)
- Tabelas do Hangfire (`HangFire.Job`, `HangFire.State`, etc.) criadas automaticamente via `PrepareSchemaIfNecessary = true`
- Mesmas queries SQL do worker anterior — `DeletarReservasNaoPagasExpiradas(15)` e `LiberarAssentosExpirados(15)` — preservadas
- Se a API reiniciar, o Hangfire retoma os jobs do banco sem perda de estado

---

## ADR-010: Rate limiting no endpoint de login

**Status:** ✅ Aceito

**Contexto:** Sem proteção, o endpoint de login pode sofrer força bruta para descobrir senhas.

**Alternativas consideradas:**

| Opção | Prós | Contras |
|-------|------|---------|
| **Middleware customizado** | Simples, sem dependência, controle total | Código próprio para manter |
| AspNetCoreRateLimit (NuGet) | Pronto, configurável, políticas avançadas | Dependência externa, config complexa |
| Sem rate limit | Zero esforço | Vulnerável a força bruta |

**Decisão:** Middleware customizado limitando `POST /api/usuario/login` a 5 requisições por minuto por IP.

**Consequências:**
- `ConcurrentDictionary<IP, SlidingWindow>` em memória
- Resposta `429 Too Many Requests` ao estourar o limite
- Não persiste entre reinicializações (aceitável para esta funcionalidade)
- Aplicado apenas no endpoint de login

---

## ADR-011: Global Exception Handler como middleware

**Status:** ✅ Aceito

**Contexto:** Exceções não tratadas retornam 500 com stack trace. Precisamos de respostas de erro padronizadas em português.

**Alternativas consideradas:**

| Opção | Prós | Contras |
|-------|------|---------|
| **Middleware customizado** | Captura tudo, formato consistente, sem dependência | Código próprio |
| `IExceptionFilter` do ASP.NET | Nativo, por controller | Não cobre erros fora do pipeline MVC |
| Try-catch em cada endpoint | Controle granular | Duplicação de código, inconsistente |

**Decisão:** `GlobalExceptionHandlerMiddleware` como primeiro item do pipeline. Mapeia exceções de domínio para HTTP status codes com mensagens em português.

**Consequências:**
- Toda resposta de erro segue o formato `{ "error": "mensagem" }`
- Exceções de domínio mapeadas: `CredenciaisInvalidasException → 401`, `ConflitoException → 409`, etc.
- `500` nunca expõe stack trace em produção
- Registrado antes de `UseAuthentication()` para capturar todos os erros

---

## ADR-012: Cancelamento Lógico com Reembolso Atômico (specs 40, 50, 110)

**Status:** ✅ Aceito

**Contexto:** O sistema precisa permitir que compradores cancelem suas reservas e que vendedores cancelem eventos inteiros. Em ambos os casos, quando há dinheiro envolvido, o reembolso precisa ser processado de forma consistente. O modelo anterior previa exclusão física — o que inviabilizaria auditoria e histórico.

**Alternativas consideradas:**

| Opção | Prós | Contras |
|-------|------|---------|
| **Cancelamento lógico + transação atômica** | Preserva histórico, reembolso garantido, idempotente | Requer colunas extras (Reembolsada, Cancelado) e transações multi-tabela |
| Exclusão física + INSERT de log | Schema mais limpo | Perde rastreabilidade, log pode divergir do estado real |
| Soft delete genérico (IsDeleted) | Simples | Não diferencia "cancelado com reembolso" de "excluído", semântica pobre |

**Decisão:** Cancelamento lógico com flags explícitas e transações atômicas cobrindo até 5 tabelas. Reserva cancelada → `Reserva.Reembolsada = true` + `ItemReserva.Reembolsado = true` + `Ingressos.Status = 0` (Livre) + `Pagamento.Status = 3` (Reembolsado). Evento cancelado → adiciona `Evento.Cancelado = true` e propaga para todas as reservas do evento.

**Consequências:**
- **Comprador cancela reserva** (spec 40): `DELETE /api/reserva/{id}` — autorização por `UsuarioCpf == cpf do JWT`, válido para qualquer perfil
- **Vendedor/Admin cancela evento** (spec 50): `DELETE /api/evento/{id}` mantém o verbo HTTP mas muda a semântica para cancelamento lógico; `GET /api/evento/{id}/status-cancelamento` consulta impacto antes de cancelar
- **Visão unificada** (spec 110): `GET /api/reserva/minhas` retorna `reembolsada`, `reembolsado` (por item) e `podeCancelar` para todos os perfis
- Coluna `Reservas.Reembolsada` (BIT, NOT NULL DEFAULT 0) — migration `Script0012`
- Eventos cancelados são filtrados da listagem pública (`WHERE Cancelado = 0`)
- Reembolso é **simulado** (status no banco) — sem gateway de pagamento real
- Transações usam `BEGIN/COMMIT/ROLLBACK` — ou tudo acontece, ou nada

---

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

> **Formato baseado em:** [ADR GitHub](https://adr.github.io/) — Michael Nygard  
> **Próximo:** Implementação da Sprint 1 (specs 120, 130, 150, 160, 180)
