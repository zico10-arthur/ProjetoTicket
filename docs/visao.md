# Documento de Visão — SoldOut Tickets

> **Versão:** 1.0  
> **Data:** 04/06/2026  
> **Autores:** Arthur Rezende de Oliveira (06010228), Ian Carlos de Oliveira Leite (06012992), Eduardo Leal (06013706), Erick Lopes dos Santos Carvalho (06010632)

---

## 1. Resumo Executivo

O **SoldOut Tickets** é uma plataforma SaaS (Software as a Service) voltada para **eventos de pequeno porte** — palestras, workshops, cursos, meetups, teatros e shows — que conecta **Vendedores** (pessoas jurídicas que criam e comercializam eventos) a **Compradores** (o público), oferecendo uma solução completa de criação, gestão, venda de ingressos e controle de reservas, com suporte a cancelamentos e reembolsos.

---

## 2. Problema

Pequenos organizadores de eventos (palestrantes, escolas, espaços culturais, coletivos) enfrentam dificuldades para:

- **Criar e divulgar eventos** de forma simples, sem depender de plataformas complexas e caras.
- **Gerenciar inscrições e vagas** sem controles manuais em planilhas.
- **Emitir e validar ingressos** de maneira profissional.
- **Processar cancelamentos e reembolsos** de forma organizada e transparente.
- **Ter autonomia** para se cadastrar e começar a vender imediatamente, sem depender de um administrador de plataforma.

---

## 3. Solução Proposta

O SoldOut Tickets resolve esses problemas com uma plataforma web onde:

- **Vendedores se auto cadastram** com CNPJ e criam eventos em minutos.
- Dois tipos de evento são suportados:
  - **Palestra** (foco principal): controle de vagas por lotação geral, sem assentos fixos — ideal para workshops e cursos.
  - **Teatro**: assentos numerados com setores VIP e Geral, com mapa visual.
- Eventos podem ser **gratuitos** (confirmação direta, sem pagamento) ou **pagos**.
- **Reservas com múltiplos participantes** (até 4 CPFs por compra) — o comprador adquire ingressos para si e para terceiros em uma única transação.
- **Cupons de desconto** globais, gerenciados pelo Admin, aplicáveis em eventos pagos de qualquer vendedor.
- **Cancelamento com reembolso** — compradores cancelam reservas antes do evento começar; vendedores cancelam eventos com reembolso obrigatório dos ingressos vendidos.
- **Três perfis de usuário** unificados na mesma tabela e mesmo endpoint de login: Admin, Vendedor e Comprador.

---

## 4. Público-Alvo

| Perfil | Quem é | Necessidade principal |
|--------|--------|----------------------|
| **Comprador** | Pessoa física | Descobrir eventos, comprar/reservar ingressos para si e acompanhantes, gerenciar histórico de compras |
| **Vendedor** | Pessoa jurídica (CNPJ) | Criar e gerenciar eventos de pequeno porte, controlar vendas, emitir cupons, cancelar eventos |
| **Admin** | Administrador da plataforma | Gerenciar vendedores (ativar/desativar, alterar plano), visualizar dados globais da plataforma |

---

## 5. Principais Funcionalidades

### 5.1 Para o Comprador

- Cadastro com CPF, nome, e-mail e senha
- Login unificado com JWT
- Visualização da lista de eventos disponíveis (ativos e não cancelados)
- Compra de ingressos com múltiplos participantes (até 4 CPFs por reserva)
- Aplicação de cupons de desconto em eventos pagos
- Confirmação instantânea para eventos gratuitos
- Histórico de reservas (Minhas Reservas)
- Cancelamento de reserva com reembolso (antes do início do evento)
- Alteração de dados cadastrais (nome, e-mail, senha)
- Remoção da própria conta

### 5.2 Para o Vendedor

- **Auto cadastro** com CNPJ, Razão Social, Nome Fantasia, E-mail, Senha e Telefone
- Painel do Vendedor com:
  - Criação de eventos (Palestra ou Teatro, gratuito ou pago)
  - Lista dos seus eventos (Meus Eventos)
  - Relatórios de vendas
  - Configurações de perfil (logo, descrição, site)
- Cancelamento de evento com alerta e reembolso obrigatório (se houver ingressos vendidos)
- Edição de dados do evento e do próprio perfil

### 5.3 Para o Admin

- Cadastro manual via SQL seed (não há cadastro público)
- Login pelo mesmo endpoint de usuário
- Listagem e gestão de vendedores (ativar/desativar, alterar plano)
- Listagem de compradores
- **Gestão de cupons globais** (criar, alterar, ativar/desativar, remover)
- Visão global de todos os eventos e reservas
- Possibilidade de comprar ingressos como qualquer usuário

---

## 6. Especificações-Chave e Valor para o Usuário

> Cada especificação técnica abaixo foi projetada para resolver uma necessidade real dos usuários. Esta seção conecta o "o quê" técnico ao "por quê" de valor.

### 6.1 Auto Cadastro do Vendedor com Validação de CNPJ

| Especificação | Valor para o Usuário |
|---|---|
| `POST /api/usuario/cadastrar-vendedor` — endpoint público, sem dependência de Admin | **Autonomia imediata**: um organizador não precisa esperar aprovação de terceiros. Em menos de 5 minutos após o cadastro, já pode criar seu primeiro evento. |
| Validação de dígitos verificadores do CNPJ no Domain (`CnpjValidator`) | **Confiança na plataforma**: impede CNPJs falsos ou com erro de digitação, garantindo que apenas empresas legítimas operem, o que protege os compradores. |
| Plano inicial Gratuito com até 3 eventos | **Experimentação sem risco**: o vendedor testa a plataforma sem custo, valida se atende suas necessidades, e só paga se precisar escalar. |

### 6.2 Login Unificado com JWT (3 Perfis)

| Especificação | Valor para o Usuário |
|---|---|
| Único endpoint `POST /api/usuario/login` para Admin, Vendedor e Comprador | **Simplicidade**: o usuário nunca precisa descobrir "qual tela de login" usar. Um campo de e-mail e senha resolve para qualquer perfil. |
| JWT com claims `role`, `cpf`, `email` | **Experiência personalizada**: após o login, o sistema sabe exatamente o que mostrar — painel de vendedor, home de comprador ou dashboard de admin — sem configuração manual. |
| BCrypt para hash e verificação de senhas | **Segurança real**: mesmo que o banco vaze, a senha do usuário está protegida por hash com salt. O usuário não precisa se preocupar com vazamento de credenciais. |

### 6.3 Dois Tipos de Evento: Palestra (vagas) e Teatro (assentos)

| Especificação | Valor para o Usuário |
|---|---|
| **Palestra**: sem geração de ingressos físicos, controle por `CapacidadeTotal - SUM(Quantidade)` | **Agilidade para o vendedor**: criar um workshop ou curso é instantâneo — sem precisar configurar mapa de assentos. Ideal para o nicho de pequeno porte. |
| **Teatro**: geração automática de N ingressos, 10% VIP (preço × 1.5), 90% Geral | **Profissionalismo**: para eventos que exigem lugar marcado, o comprador escolhe exatamente onde vai sentar, como em uma casa de espetáculos real. |
| Ambos suportam `PrecoPadrao = 0` (evento gratuito) | **Inclusão**: meetups e eventos comunitários gratuitos têm o mesmo tratamento profissional que eventos pagos, com controle de presença e confirmação instantânea. |

### 6.4 Reserva com Múltiplos Participantes (ItemReserva)

| Especificação | Valor para o Usuário |
|---|---|
| Até 4 `ItemReserva` por reserva, cada um com `CpfParticipante` próprio | **Compra em grupo simplificada**: um comprador leva amigos/família em uma única transação. Não precisa fazer 4 compras separadas, preencher 4 vezes os dados de pagamento. |
| CPFs dos participantes não precisam estar cadastrados no sistema | **Sem fricção para convidados**: o comprador pode incluir qualquer pessoa — basta o CPF. O convidado não precisa criar conta na plataforma. |
| Cada `ItemReserva` tem `Reembolsado` e `PrecoUnitario` independentes | **Reembolso granular**: se um participante desistir, a plataforma está pronta para reembolsar itens individuais no futuro (base arquitetural para a feature). |

### 6.5 Cancelamento com Reembolso Atômico

| Especificação | Valor para o Usuário |
|---|---|
| Comprador cancela reserva se `DataEvento > agora`; ingressos voltam a `Status=0` (Livre) | **Flexibilidade com segurança**: o comprador não perde dinheiro se não puder comparecer, desde que cancele antes do evento começar. As vagas voltam para outros compradores. |
| Vendedor cancela evento pago → alerta de reembolso obrigatório → transação atômica (`Evento.Cancelado=true` + `Ingresso.Status=3` + `Reserva.Reembolsada=true`) | **Transparência e responsabilidade**: o vendedor é alertado sobre o impacto financeiro antes de cancelar. Compradores têm a garantia de que serão reembolsados — o sistema força isso na transação. |
| Evento gratuito cancelado sem reembolso | **Coerência**: como não houve cobrança, não há reembolso — o sistema não cobra nem devolve o que não existe, evitando confusão. |

### 6.6 Cupons Globais do Admin

| Especificação | Valor para o Usuário |
|---|---|
| `POST /api/cupom/cadastrar` — restrito a `[Authorize(Roles = "Admin")]` | **Controle centralizado**: apenas o Admin cria cupons, garantindo que a plataforma controle as políticas de desconto de forma unificada. |
| Cupons são **globais** — aplicáveis a qualquer evento pago, de qualquer vendedor | **Simplicidade para o comprador**: um cupom como `PROMO10` funciona em qualquer evento da plataforma, sem confusão sobre "de qual vendedor" o cupom é. |
| Validação: cupom ativo + não expirado + `ValorMinimo` atingido + não aplicável a evento gratuito | **Uso justo**: o comprador só usa cupons válidos. O vendedor não tem surpresas com descontos aplicados indevidamente em eventos gratuitos. |
| Desconto não pode gerar valor negativo (`ValorFinalPago >= 0`) | **Previsibilidade financeira**: o vendedor nunca é lesado por um cupom que geraria saldo negativo. |
| Endpoints `ListarCuponsValidos` é público — qualquer usuário pode consultar cupons disponíveis | **Transparência**: compradores descobrem cupons ativos sem precisar de login específico. |

### 6.7 Isolamento de Dados Multi-Tenant (VendedorId)

| Especificação | Valor para o Usuário |
|---|---|
| Toda query filtra por `VendedorId`: `SELECT * FROM Eventos WHERE VendedorId = @vendedorId` | **Privacidade e segurança**: o Vendedor X nunca vê os eventos ou reservas do Vendedor Y. Cada organizador opera como se estivesse em sua própria plataforma. |
| `VendedorId` extraído do JWT (`User.Claims`), nunca da rota | **Impossibilidade de falsificação**: um vendedor mal-intencionado não consegue acessar dados de outro manipulando a URL. A identidade vem do token criptografado. |

### 6.8 Hangfire Recurring Job de Liberação de Assentos

| Especificação | Valor para o Usuário |
|---|---|
| Hangfire recurring job executa a cada 60s com persistência em banco: libera ingressos com `Status=1` (Reservado) e `DataBloqueio` > 15 minutos | **Justiça para todos**: se um comprador iniciar uma reserva e abandonar o checkout, o assento não fica preso eternamente. Outros compradores têm chance real de adquiri-lo após 15 minutos. |
| Dashboard de monitoramento em `/hangfire` (restrito a Admin) com histórico de execuções, falhas e tempo de cada job | **Confiabilidade operacional**: o Admin monitora a saúde dos jobs em tempo real, sem precisar acessar logs do servidor. Jobs sobrevivem a restarts do servidor. |

### 6.9 E-mails Transacionais e Redefinição de Senha

| Especificação | Valor para o Usuário |
|---|---|
| E-mail de boas-vindas enviado ao se cadastrar como Comprador ou Vendedor | **Confirmação imediata**: o usuário recebe a certeza de que a conta foi criada com sucesso, sem precisar fazer login para verificar. |
| E-mail de confirmação de reserva com nome do evento, data, quantidade de participantes e valor total | **Comprovante digital**: o comprador tem um registro da compra no e-mail, acessível mesmo sem estar logado na plataforma. |
| E-mail de confirmação de pagamento com valor pago e método | **Segurança e transparência**: o comprador sabe exatamente quando e quanto pagou, com registro para eventuais contestações. |
| E-mail de reembolso confirmado (quando reserva ou evento é cancelado) | **Tranquilidade financeira**: o comprador tem a garantia documentada de que o reembolso foi processado. |
| `POST /api/usuario/esqueci-senha` + `POST /api/usuario/redefinir-senha` com token JWT de 15 minutos enviado por e-mail | **Autonomia total**: o usuário recupera o acesso à conta sozinho, sem abrir chamado nem depender do Admin. Nenhum outro perfil precisa intervir. |
| Envio assíncrono via background worker com fila em memória — não bloqueia a resposta da API | **Experiência fluida**: o cadastro, reserva ou pagamento são confirmados instantaneamente. O e-mail chega em seguida, sem atrasar a navegação. |
| Graceful degradation: se SMTP não estiver configurado, o sistema funciona normalmente sem e-mails | **Resiliência em desenvolvimento**: o time pode rodar o sistema localmente sem configurar servidor de e-mail. |

---

## 7. Modelo de Negócio

| Item | Descrição |
|------|-----------|
| **Plataforma** | SaaS — Vendedor se cadastra e começa a vender |
| **Planos do Vendedor** | Gratuito (até 3 eventos), Básico (até 10 eventos/mês), Profissional (eventos ilimitados, relatórios, branding próprio) |
| **Monetização** | A ser definida (planos pagos, comissão sobre vendas, ou freemium) |
| **Auto cadastro** | Vendedor se cadastra sozinho — sem depender do Admin |
| **Admin** | Cadastrado manualmente no banco (SQL seed) — não há cadastro público para Admin |

---

## 8. Arquitetura e Tecnologia

### 8.1 Stack Tecnológica

| Tecnologia | Uso |
|------------|-----|
| .NET 9 | Plataforma base |
| ASP.NET Core | API REST |
| Blazor Server | Frontend web |
| MudBlazor | Biblioteca de componentes UI |
| Dapper | ORM leve para acesso a dados |
| SQL Server | Banco de dados relacional |
| JWT Bearer | Autenticação e autorização |
| BCrypt | Hash de senhas |
| AutoMapper | Mapeamento de objetos (DTO ↔ Entidade) |
| DbUp | Versionamento e migração de banco de dados |
| Hangfire | Agendamento e monitoramento de jobs em background |
| xUnit | Testes automatizados |

### 8.2 Arquitetura de Camadas (Clean Architecture)

```
ProjetoTicket/
├── Api/              → Controllers REST, Middlewares, BackgroundTasks, Program.cs
├── Application/      → Services (casos de uso), DTOs, Interfaces de Serviço, AutoMapper Profiles
├── Domain/           → Entities (regras de negócio), Exceptions, Interfaces de Repositório, Enums
├── Infraestructure/  → Repository (implementações Dapper), ConnectionFactory, Migrations (DbUp)
├── Web/              → Frontend Blazor Server (MudBlazor)
├── docs/             → Documentação do projeto
├── db/               → Scripts SQL
└── tests/            → Testes automatizados
```

### 8.3 Regras de Arquitetura

- **Domain** não depende de nenhuma camada externa
- **Application** depende apenas do Domain
- **Infraestructure** implementa interfaces do Domain
- **Api** orquestra tudo, depende de Application e Infraestructure
- Injeção de dependência configurada no `Program.cs`

---

## 9. Diferenciais e Decisões de Design

1. **Auto cadastro do Vendedor** — elimina barreira de entrada; qualquer PJ pode começar a vender imediatamente.
2. **Login unificado** — único endpoint (`POST /api/usuario/login`) para Admin, Vendedor e Comprador, com JWT contendo a role correspondente.
3. **Vendedor como perfil na tabela Usuarios** — simplifica o modelo de dados (sem tabela `Empresas` separada), propriedades específicas como colunas nullable.
4. **Foco em Palestras** — eventos sem assentos fixos, controle por vagas, sem geração massiva de ingressos — adequado ao nicho de pequeno porte.
5. **Reserva multi-participante** — entidade `ItemReserva` permite até 4 CPFs por compra, cada um gerando um item independente com possibilidade de reembolso individual.
6. **Cancelamento com reembolso atômico** — operações de cancelamento de evento e reserva executadas em transação, garantindo consistência.
7. **Isolamento de dados por VendedorId** — segurança multi-tenant: cada vendedor acessa apenas seus próprios eventos e reservas. Cupons são globais, gerenciados pelo Admin.
8. **Segurança** — BCrypt para senhas, Dapper parametrizado contra SQL Injection, JWT com roles, chave JWT em user-secrets.

---

## 10. Escopo Atual (v2.0)

### O que está incluído

- Cadastro, login e gestão de perfil para os 3 tipos de usuário
- Criação e gestão de eventos (Palestra e Teatro, gratuito e pago)
- Reserva com múltiplos participantes (ItemReserva)
- Aplicação de cupons de desconto
- Cancelamento de reserva com reembolso
- Cancelamento de evento com reembolso obrigatório
- Hangfire recurring job para liberação de assentos expirados
- Envio de e-mails transacionais (boas-vindas, confirmação de reserva, pagamento e reembolso) e redefinição de senha
- API REST documentada via Swagger
- Frontend Blazor Server com MudBlazor
- Migrations automáticas via DbUp

### O que está fora do escopo atual

- Gateway de pagamento real (simulado no sistema)
- Aplicativo mobile
- Integração com redes sociais
- Check-in / validação de ingresso no dia do evento
- Relatórios avançados e dashboards analíticos

---

## 11. Métricas de Sucesso (objetivos)

| Métrica | Alvo |
|---------|------|
| Tempo para um vendedor criar seu primeiro evento | < 5 minutos |
| Tempo de resposta da API (p95) | < 500ms |
| Cobertura de testes | > 80% |
| Disponibilidade do sistema | 99,5% |
| Satisfação do usuário (NPS) | > 70 |

---

## 12. Riscos e Mitigações

| Risco | Impacto | Mitigação |
|-------|---------|-----------|
| Vazamento de senhas (senhas em texto plano) | Crítico | BCrypt implementado na Sprint 1 |
| Falsificação de identidade (AdminId via rota) | Crítico | Extrair identidade do JWT (`User.Claims`) |
| Exposição de chave JWT no código-fonte | Alto | Migrar para `dotnet user-secrets` |
| Conflito de reservas (sobrevenda de vagas) | Alto | Validação atômica da capacidade antes de confirmar reserva |
| Isolamento de dados entre vendedores | Alto | Toda query de eventos e reservas filtra por `VendedorId`; cupons são globais |
| Esgotamento de conexões com banco | Médio | Configurar `Max Pool Size=100` |

---

## 13. Referências

- [`requisitos.md`](./requisitos.md) — Histórias de usuário e critérios de aceitação (versão AV1)
- [`storytelling.md`](./storytelling.md) — Novas funcionalidades e melhorias da v2.0
- [`especificacoes.md`](./especificacoes.md) — Especificações técnicas completas do sistema
- [`arquitetura.md`](./arquitetura.md) — Visão geral da arquitetura
- [`sprints.md`](./sprints.md) — Planejamento detalhado das sprints
- [`agents/roadmap.md`](./agents/roadmap.md) — Roadmap e fases do projeto
- [`operacao.md`](./operacao.md) — Guia de como rodar o projeto
- [`README.md`](../README.md) — Informações gerais do repositório

---

> **Documento de Visão do SoldOut Tickets** — Este documento fornece uma visão de alto nível do produto, seu propósito, público-alvo, funcionalidades principais, arquitetura e planejamento estratégico. Para detalhes técnicos e especificações, consulte os documentos referenciados acima.
