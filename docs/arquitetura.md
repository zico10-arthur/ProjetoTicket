# Arquitetura do Sistema — SoldOut Tickets

## Visão Geral

O **SoldOut Tickets** é uma plataforma SaaS voltada para **eventos de pequeno porte** (palestras, workshops, cursos, meetups, teatros, shows), conectando **Vendedores** (pessoas jurídicas) a **Compradores** (público), com uma solução completa de criação, gestão, venda de ingressos, reservas multi-participante e cancelamento com reembolso.

O sistema segue o padrão **Clean Architecture**, com 4 camadas principais:

```
ProjetoTicket/
├── Api/              → Controllers REST, Middlewares, BackgroundTasks, Program.cs
├── Application/      → Services (casos de uso), DTOs, Interfaces de Serviço, AutoMapper Profiles
├── Domain/           → Entities (regras de negócio), Exceptions, Interfaces de Repositório, Enums
├── Infraestructure/  → Repository (implementações Dapper), ConnectionFactory, Migrations (DbUp)
├── Web/              → Frontend Blazor Server (MudBlazor)
├── docs/             → Documentação do projeto
├── db/               → Scripts SQL
└── tests/            → Testes automatizados (xUnit)
```

---

## Arquitetura Conceitual do Produto

### 1. Perfis de Usuário (Login Unificado)

Três perfis coexistem na mesma tabela `Usuarios`, com autenticação centralizada em um único endpoint `POST /api/usuario/login` e JWT contendo a `role` correspondente:

| Perfil | Quem é | Responsabilidades |
|--------|--------|--------------------|
| **Comprador** | Pessoa física (CPF) | Comprar/reservar ingressos, gerenciar histórico, cancelar reservas, alterar dados cadastrais, remover conta |
| **Vendedor** | Pessoa jurídica (CNPJ) | Criar e gerenciar eventos, controlar vendas, cancelar eventos, configurar perfil (logo, descrição, site) |
| **Admin** | Administrador da plataforma | Gerenciar vendedores (ativar/desativar, alterar plano), listar compradores, criar cupons globais, visão global da plataforma |

**Pontos-chave:**
- **Auto cadastro do Vendedor** — endpoint público `POST /api/usuario/cadastrar-vendedor`, com validação de dígitos verificadores do CNPJ (`CnpjValidator` no Domain). Vendedor começa a operar imediatamente, sem depender de Admin.
- **Admin** é cadastrado manualmente via SQL seed — não há cadastro público para Admin.
- **BCrypt** para hash e verificação de senhas.
- **JWT** com claims `role`, `cpf`, `email` — a identidade do usuário é sempre extraída do `User.Claims`, nunca de parâmetros de rota.

### 2. Tipos de Evento: Palestra e Teatro

| Tipo | Modelo de Ingresso | Característica |
|------|--------------------|---------------|
| **Palestra** | Controle por vagas (`CapacidadeTotal - SUM(Quantidade)`) | Sem geração de ingressos individuais — ideal para workshops, cursos e meetups |
| **Teatro** | Assentos numerados com setores | Geração automática de N ingressos: **10% VIP** (preço × 1.5), **90% Geral** — ideal para espetáculos com lugar marcado |

Ambos suportam eventos **gratuitos** (`PrecoPadrao = 0`) e **pagos**. Eventos gratuitos têm confirmação direta, sem etapa de pagamento.

### 3. Reserva com Múltiplos Participantes (ItemReserva)

Cada reserva pode conter até **4 `ItemReserva`**, cada um com `CpfParticipante` independente:

- O comprador adquire ingressos para si e para terceiros em uma **única transação**.
- Os CPFs dos participantes **não precisam estar cadastrados** no sistema — basta informar o CPF no momento da compra.
- Cada `ItemReserva` possui `PrecoUnitario` e flag `Reembolsado` independentes — base arquitetural para **reembolso granular** (possibilidade futura de reembolsar itens individuais).

### 4. Cancelamento com Reembolso Atômico

O cancelamento é **lógico** (nunca exclusão física) e opera em **transação atômica** cobrindo até 5 tabelas. Três specs compõem o módulo de cancelamento:

#### 4.1 Comprador Cancela Reserva (spec 40)

`DELETE /api/reserva/{id}` — qualquer perfil autenticado pode cancelar suas próprias reservas.

| Validação | Comportamento |
|-----------|---------------|
| `Reserva.UsuarioCpf == cpf do JWT` | Autorização — apenas o dono cancela |
| `Reserva.Reembolsada == true` | Bloqueio — `409 Conflict`: "Reserva já foi cancelada." |
| `DataEvento <= DateTime.UtcNow` | Bloqueio — `400 Bad Request`: "Não é possível cancelar. O evento já começou." |

**Transação atômica:** `Reservas.Reembolsada = 1` + `ItensReserva.Reembolsado = 1` + `Ingressos.Status = 0` (Livre), `DataBloqueio = NULL` + `Pagamentos.Status = 3` (Reembolsado, se existir).

#### 4.2 Vendedor Cancela Evento (spec 50)

`DELETE /api/evento/{id}` — a semântica mudou de exclusão física para **cancelamento lógico**. Antes de cancelar, o vendedor consulta o impacto via `GET /api/evento/{id}/status-cancelamento`.

| Cenário | Comportamento |
|---------|---------------|
| Evento pago com ingressos vendidos | `Evento.Cancelado = true` + todos `Ingressos.Status = 3` + todas `Reservas.Reembolsada = true` + todos `Pagamentos.Status = 3` |
| Evento gratuito | `Evento.Cancelado = true` + `Reservas.Reembolsada = true` (sem reembolso financeiro) |
| Evento sem ingressos vendidos | Apenas `Evento.Cancelado = true` |
| Evento já cancelado | `409 Conflict`: "Evento já foi cancelado." |

**Autorização:** Vendedor dono do evento (`VendedorCpf == cpf do JWT`) ou Admin. Eventos cancelados são filtrados da listagem pública (`WHERE Cancelado = 0`).

#### 4.3 Visão Unificada de Cancelamento (spec 110)

`GET /api/reserva/minhas` é unificado para todos os perfis (sem restrição de role). O response inclui flags individuais por item:

```json
{
    "reembolsada": true,
    "podeCancelar": false,
    "itens": [
        { "cpfParticipante": "529.885.310-09", "precoUnitario": 50.00, "reembolsado": true }
    ]
}
```

- `podeCancelar` = `!Reembolsada && DataEvento > DateTime.UtcNow` (calculado em memória)
- Query usa `splitOn: "ItemId"` com dicionário em memória para agrupar itens por reserva
- Admin também pode cancelar qualquer reserva via endpoints admin separados

### 5. Cupons Globais do Admin

- Criados exclusivamente pelo **Admin** via `POST /api/cupom/cadastrar` (`[Authorize(Roles = "Admin")]`).
- **Globais**: aplicáveis a qualquer evento pago, de qualquer vendedor — não há cupons restritos a um vendedor específico.
- **Validações no momento da aplicação:**
  - Cupom ativo e não expirado
  - Valor mínimo da reserva atingido
  - Evento não pode ser gratuito
  - Desconto não pode gerar valor negativo (`ValorFinalPago >= 0`)
- Endpoint público `ListarCuponsValidos` — qualquer usuário (logado ou não) consulta cupons disponíveis.

### 6. Isolamento Multi-Tenant (VendedorId)

- Toda query de eventos e reservas é filtrada por `VendedorId`: `SELECT * FROM Eventos WHERE VendedorId = @vendedorId`.
- O `VendedorId` é **extraído do JWT** (`User.Claims`), nunca de parâmetros de rota — impossível um vendedor acessar dados de outro manipulando a URL.
- **Exceção**: cupons são **globais** e gerenciados pelo Admin — não pertencem a um vendedor específico.

### 7. Hangfire — Jobs Recorrentes para Liberação de Assentos (spec 190)

- **Hangfire** com storage SQL Server (mesmo banco do projeto) substituiu o `BackgroundService` anterior.
- Job `LiberacaoAssentosJob.ExecutarAsync()` executado a cada **60 segundos** via `Cron.Minutely`.
- `[DisableConcurrentExecution(timeoutInSeconds: 120)]` impede execuções paralelas.
- **Propósito**: se um comprador iniciar o checkout e abandonar, o assento não fica preso eternamente — outros compradores podem adquiri-lo após **15 minutos**.
- Dashboard em `/hangfire` protegido por autorização Admin (`HangfireAdminAuthorizationFilter`).
- Tabelas do Hangfire (`HangFire.Job`, `HangFire.State`, etc.) criadas automaticamente via `PrepareSchemaIfNecessary = true`.
- Mesmas queries SQL do worker anterior — `DeletarReservasNaoPagasExpiradas(15)` e `LiberarAssentosExpirados(15)` — preservadas.

### 8. Serviço de E-mail Transacional (spec 180)

- Interface `IEmailSender` no Domain, implementada por `EmailBackgroundWorker` (IHostedService singleton).
- Fila em memória via `Channel<EmailMessage>` (`BoundedChannelOptions(100)`, `FullMode = Wait`).
- Envio real via **MailKit** (SMTP) — `SmtpEmailSender`.
- **Graceful degradation**: se SMTP não configurado (sem `Host`), worker loga warning e descarta mensagens.
- **Retry**: 3 tentativas com backoff exponencial (2s, 4s, 8s).
- Templates de e-mail em `EmailTemplates` (classe estática): boas-vindas (Comprador/Vendedor), reserva confirmada, pagamento confirmado, reembolso confirmado, redefinição de senha.
- **Integrações**: `CadastrarComprador`, `CadastrarVendedor`, `FazerReserva`, `ConfirmarCheckout` e hooks para ST-05/ST-06.
- **Redefinição de senha**: `POST /api/usuario/esqueci-senha` (sempre 200 OK — anti-enumeração) + `POST /api/usuario/redefinir-senha` (JWT com claim `purpose=password-reset`, 15 min).

---

## Camadas (Clean Architecture)

Regra de dependência: **Domain → Application → Infraestructure → Api**. Cada camada só conhece a camada interna.

### Domain
- **Entidades** com regras de negócio encapsuladas: `Usuario`, `Evento`, `Ingresso`, `Reserva`, `ItemReserva`, `Cupom`
- **Value Objects** e validações de domínio (ex: `CnpjValidator`, `EmailMessage`)
- **Exceções tipadas** de domínio
- **Interfaces de repositório** e de serviço (ex: `IEmailSender`) — contratos que a Infraestructure implementa
- **Não depende de nenhuma camada externa**

### Application
- **Serviços de aplicação** — implementam os casos de uso do sistema
- **DTOs** de entrada e saída
- **AutoMapper Profiles** para mapeamento DTO ↔ Entidade
- **Interfaces de serviço**
- **Depende apenas do Domain**

### Infrastructure
- **Implementação dos repositórios** com Dapper (queries parametrizadas → proteção contra SQL Injection)
- **ConnectionFactory** para SQL Server (`Max Pool Size=100`)
- **Migrations** automáticas via DbUp
- **Hangfire** com recurring jobs para liberação de assentos expirados (substituiu BackgroundService)
- **Serviço de e-mail** via MailKit com fila Channel\<T\> e worker dedicado (`EmailBackgroundWorker`)
- **Implementa interfaces do Domain**

### Api
- **Controllers REST** documentados via Swagger
- **Middleware global de exceções**
- **Configuração JWT** (chave armazenada em user-secrets)
- **BackgroundTasks**
- **Injeção de dependências** centralizada no `Program.cs`
- **Orquestra todas as camadas** — depende de Application e Infraestructure

### Web
- Frontend **Blazor Server** com **MudBlazor** (componentes UI)
- Consome a API REST internamente via `HttpClient`

---

## Tecnologias

| Tecnologia | Uso |
|---|---|
| .NET 9 | Plataforma base |
| ASP.NET Core | API REST |
| Blazor Server | Frontend web |
| MudBlazor | Biblioteca de componentes UI |
| Dapper | ORM leve para acesso a dados |
| SQL Server | Banco de dados relacional |
| JWT Bearer | Autenticação e autorização |
| BCrypt | Hash de senhas |
| AutoMapper | Mapeamento DTO ↔ Entidade |
| DbUp | Versionamento e migração de banco |
| MailKit | Envio de e-mails transacionais via SMTP |
| Hangfire | Jobs recorrentes com persistência e dashboard |
| xUnit | Testes automatizados |

---

## Fluxo de Autenticação

1. Usuário envia e-mail e senha para `POST /api/usuario/login`
2. API valida credenciais com **BCrypt** e gera token **JWT** com claims (`cpf`, `email`, `role`)
3. Frontend armazena token no `localStorage`
4. Requisições subsequentes enviam token no header `Authorization: Bearer {token}`
5. API valida token e autoriza por role (`[Authorize(Roles = "...")]`)
6. Identidade do usuário (`VendedorId`, `CompradorId`) é sempre extraída do `User.Claims` — nunca de parâmetros de rota

---

## Regras de Negócio Críticas

- **Capacidade e anti-sobrevenda**: ingressos são gerados na criação do evento. A validação de capacidade (`CapacidadeTotal - SUM(Quantidade)`) é feita atomicamente antes de confirmar qualquer reserva — impossível vender além da capacidade.
- **Anti-cambista**: um mesmo CPF só pode ter **uma reserva ativa** por evento.
- **Cupom seguro**: validação de valor mínimo, expiração e não-aplicabilidade a eventos gratuitos antes de aplicar desconto. Desconto nunca gera valor negativo.
- **Bloqueio temporário**: ingresso fica com `Status=1` (Reservado) durante o checkout. Se o checkout não for concluído em **15 minutos**, o job Hangfire (`LiberacaoAssentosJob`) libera o ingresso automaticamente.
- **Isolamento multi-tenant**: todo acesso a eventos e reservas é filtrado por `VendedorId` extraído do JWT. Cupons são a exceção — são globais.
- **Cancelamento atômico com reembolso**: operações de cancelamento que envolvem dinheiro são executadas em transação, garantindo que o reembolso e a liberação de ingressos aconteçam juntos ou não aconteçam. Cancelamento é **lógico** (flags `Reembolsada`, `Cancelado`), nunca exclusão física.
- **E-mails transacionais não bloqueantes**: envio de e-mail é fire-and-forget via `Channel<T>` — nunca bloqueia a resposta HTTP. Se SMTP não estiver configurado, o sistema opera normalmente (graceful degradation).
- **Redefinição de senha segura**: token JWT com claim `purpose=password-reset` e 15 min de validade. Endpoint `esqueci-senha` sempre retorna 200 OK para prevenir enumeração de usuários. Token de reset não é aceito como token de autenticação.

---

> **Documento de Arquitetura do SoldOut Tickets** — Este documento descreve a arquitetura conceitual do produto e a estrutura técnica do sistema. Para a visão de produto, consulte [`visao.md`](./visao.md). Para decisões arquiteturais, consulte [`ADR.md`](./ADR.md). Para o roadmap de specs, consulte [`agents/roadmap.md`](./agents/roadmap.md).
