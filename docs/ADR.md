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

## ADR-009: Background Worker para liberação de assentos

**Status:** ✅ Aceito

**Contexto:** Quando um comprador inicia o checkout, o assento fica com `Status=1` (Reservado). Se ele abandonar, o assento precisa voltar ao estado Livre.

**Alternativas consideradas:**

| Opção | Prós | Contras |
|-------|------|---------|
| **BackgroundService (timer)** | Nativo do .NET, simples, sem dependência externa | Executa em memória, para se a API parar |
| Hangfire / Quartz | Persistente, dashboard, retry | Overkill para 1 job, dependência extra |
| Transação com timeout no banco | Atomicidade garantida | Timeout de conexão ≠ timeout de negócio |

**Decisão:** `BackgroundService` do .NET com `Timer` de 60 segundos. Libera ingressos com `Status=1` e `DataBloqueio > 15 minutos`.

**Consequências:**
- `LiberacaoAssentosWorker.cs` em `Api/BackgroundTasks/`
- Registrado como `IHostedService` no `Program.cs`
- Query: `UPDATE Ingressos SET Status=0 WHERE Status=1 AND DATEDIFF(MINUTE, DataBloqueio, GETDATE()) >= 15`
- Se a API reiniciar, o worker reinicia junto — assentos presos são liberados no próximo ciclo

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

> **Formato baseado em:** [ADR GitHub](https://adr.github.io/) — Michael Nygard  
> **Próximo:** Implementação da spec 120 (Segurança — BCrypt + JWT user-secrets + rate limit)
