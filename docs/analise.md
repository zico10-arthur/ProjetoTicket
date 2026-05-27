# Análise do Sistema — SoldOut Tickets

## 1. Visão Geral

O **SoldOut Tickets** é um sistema de venda de ingressos para eventos (shows, teatros, eventos corporativos), desenvolvido em **.NET 9** seguindo os princípios da **Clean Architecture**. O sistema possui uma **API REST** (ASP.NET Core) e um **frontend Blazor Server** com componentes **MudBlazor**, utilizando **SQL Server** como banco de dados e **Dapper** como ORM leve.

---

## 2. Estrutura de Diretórios

```
ProjetoTicket/
├── Api/                  → Camada de apresentação (Controllers, Middlewares, BackgroundTasks)
├── Application/          → Casos de uso (Services, DTOs, Interfaces, Mappings, Exceptions)
├── Domain/               → Núcleo do negócio (Entities, Exceptions, Repository Interfaces, DTOs)
├── Infraestructure/      → Infraestrutura (Repositories, Database, Migrations)
├── Web/                  → Frontend Blazor Server (MudBlazor)
├── tests/                → Projeto de testes unitários
├── db/                   → Scripts SQL de setup
└── docs/                 → Documentação
```

---

## 3. Análise por Camada

### 3.1 Domain (Núcleo do Negócio)

**Entidades:**

| Entidade | Propósito | Pontos Fortes | Pontos Fracos |
|---|---|---|---|
| [`Evento`](Domain/Entities/Evento.cs:3) | Evento com nome, capacidade, data, preço e lote de ingressos | Geração automática de ingressos com setores VIP (10%) e Geral (90%); validação de capacidade | `GerarLoteIngressos` acoplada à entidade — mistura regra de negócio com criação de dados; `VendedorCpf` como `string` (sem value object) |
| [`Ingresso`](Domain/Entities/Ingresso.cs:3) | Assento individual com posição, setor, preço e status (0=Livre, 1=Reservado, 2=Vendido) | Métodos de mudança de estado (`Reservar()`, `Vender()`, `Liberar()`) bem definidos | Excessão de `DataBloqueio` na entidade sem validação temporal |
| [`Reserva`](Domain/Entities/Reserva.cs:6) | Reserva de um ingresso por um usuário, com validação de CPF e aplicação de cupom | Factory method `Criar()` com validações embutidas; validação de dígitos do CPF | Lógica de cálculo de desconto e validação de cupom dentro da entidade — responsabilidade poderia estar em um serviço de domínio |
| [`Cupom`](Domain/Entities/Cupom.cs:5) | Cupom de desconto com código, porcentagem, valor mínimo e data de expiração | Validações robustas (código alfanumérico, expiração, valor mínimo); propriedade `EstaValidoParaUso` | Código de validação do formato complexo e frágil (limite de 3 dígitos, regras confusas) |
| [`Usuario`](Domain/Entities/Usuario.cs:6) | Usuário do sistema (Comprador, Vendedor, Admin) com CPF, nome, email e senha | Validações de CPF com dígitos verificadores; validação de senha com requisitos de segurança | Senha armazenada em texto puro (sem hash); CPF como `string` sem value object |
| [`Pagamento`](Domain/Entities/Pagamento.cs:3) | Registro de pagamento de uma reserva | Estrutura simples e direta | Sem validações ou regras de negócio; não possui relação de fato com o fluxo atual |
| [`Perfil`](Domain/Entities/Perfil.cs:3) | Perfil de usuário (Admin, Vendedor, Comprador) com ID fixo | Simples e funcional | IDs hardcoded como GUIDs fixos no código |

**DTOs de Domínio:**
- [`ReservaDetalhadaDTO`](Domain/DTOs/ReservaDetalhadaDTO.cs:3) — DTO para exibição de reservas com joins
- [`ReservaAdminDTO`](Domain/DTOs/ReservaAdminDTO.cs:3) — Herda de `ReservaDetalhadaDTO` e adiciona dados do usuário

**Exceptions de Domínio:**
- [`DomainException`](Domain/Exceptions/DomainException.cs:3) — Classe base para exceções de domínio
- Exceções tipadas para Usuário, Cupom e Reserva

**Interfaces de Repositório:**
- Definem os contratos de persistência para cada entidade
- Pontos de atenção: Assinaturas misturam métodos síncronos (`CadastrarUsuario`, `CadastrarCupom`) e assíncronos

**Pontos de Melhoria no Domain:**
- ❗ Senhas armazenadas em texto puro — sem Hashing (violação do RNF02)
- ❗ PerfilId como GUID fixo hardcoded no código (deveria ser um enum ou busca por nome)
- ⚠️ CPF como `string` espalhada — ideal seria um Value Object `Cpf`
- ⚠️ Duplicação de classes de exceção entre `Domain.Exceptions` e `Application.Exceptions` (ex: `DescontoInvalido`, `ValorMinimoInvalido`, `DataExpiracaoInvalida` aparecem em ambos os namespaces)
- ✅ Bons encapsulamentos com construtores privados e factory methods

---

### 3.2 Application (Casos de Uso)

**Services:**

| Service | Responsabilidade | Pontos de Atenção |
|---|---|---|
| [`UsuarioService`](Application/Service/UsuarioService.cs:9) | Cadastro, login, alteração de dados | Login compara senha em texto puro; perfis GUID fixos; `CadastrarVendedor` busca admin por `PerfilId` em vez de extrair do JWT |
| [`EventoService`](Application/Service/EventoService.cs:9) | CRUD de eventos com geração de ingressos | Boa validação de permissão (vendedor vs admin); validação de data futura |
| [`ReservaService`](Application/Service/ReservaService.cs:10) | Fluxo de reserva com bloqueio de assento e cupom | Boa orquestração (verifica duplicidade, bloqueia ingresso, aplica cupom, cadastra reserva) |
| [`CupomService`](Application/Service/CupomService.cs:9) | CRUD de cupons com validações de admin | `CadastrarCupom` com `Task.Run` desnecessário (`await Task.Run(() => _repositoryCupom.CadastrarCupom(novoCupom), ct)`) |
| [`IngressoService`](Application/Service/IngressoService.cs:10) | Listagem de ingressos por evento | Simples e direto, apenas delegando para o repositório |
| [`TokenService`](Application/Service/TokenService.cs:11) | Geração de JWT com claims (cpf, email, role) | Mapeamento de PerfilId para role via switch; 8 horas de expiração |

**DTOs:**
- Bem organizados em `Application/DTOs/` com separação por funcionalidade
- [`ReservarDTO`](Application/DTOs/ReservarDTO.cs:3) — Simples e funcional
- [`EventoRequestDTO`](Application/DTOs/EventoRequestDTO.cs:5) — Com validações Data Annotations

**Mappings (AutoMapper):**
- [`UsuarioProfile`](Application/Mappings/UsuarioProfile.cs) — Mapeamento de Usuário
- [`EventoProfile`](Application/Mappings/EventoProfile.cs) — Mapeamento de Evento
- [`IngressoProfile`](Application/Mappings/IngressoProfile.cs) — Mapeamento de Ingresso

**Exceções de Aplicação:**
- [`ApplicationCupomExceptions.cs`](Application/Exceptions/ApplicationCupomExceptions.cs) — Exceções de cupom duplicado, não autorizado, inexistente
- [`ApplicationReservaExceptions.cs`](Application/Exceptions/ApplicationReservaExceptions.cs) — Exceções de reserva duplicada, ingresso esgotado
- [`UsuarioExceptions.cs`](Application/Exceptions/UsuarioExceptions.cs) — Exceções de usuário já cadastrado, login inválido

**Pontos de Melhoria na Application:**
- ❗ Senha comparada em texto puro no `UsuarioService.Login()` — sem hashing
- ❗ `CupomService.CadastrarCupom` usa `Task.Run` desnecessário (ThreadPool desperdiçado)
- ⚠️ Nenhum uso de transações nos serviços de aplicação (apenas no repositório `EventoRepository`)
- ⚠️ Mistura de conceitos: algumas validações de negócio estão na Application em vez do Domain
- ⚠️ `AdminLogado` nos métodos de CupomService recebem `Guid` do admin, mas nunca validam se o admin realmente existe

---

### 3.3 Infraestructure (Infraestrutura)

**Banco de Dados:**
- [`ConnectionFactory`](Infraestructure/DataBase/ConnectionFactory.cs:6) — Fábrica de conexões SQL Server via `SqlConnection`
- [`DatabaseMigration`](Infraestructure/DataBase/DatabaseMigration.cs:7) — Migrations automáticas com **DbUp** (scripts embutidos no assembly)
- Scripts SQL versionados de `Script0001` a `Script0008`

**Repositories:**
- Todos implementam Dapper com consultas parametrizadas (proteção contra SQL Injection ✅)
- `ConnectionFactory` usada via injeção de dependência
- Uso de transações em operações críticas (`EventoRepository.CriarEventoCompletoAsync`, `EventoRepository.DeleteAsync`)
- `ReservaRepository` — Funções de limpeza de reservas expiradas com SQL

**Pontos de Melhoria na Infraestructure:**
- ❗ `CupomRepository.CadastrarCupom` e `UsuarioRepository.CadastrarUsuario` são **síncronos** — inconsistente com o resto da aplicação
- ❗ `CupomRepository.ListarTodosCupons` faz `CAST(CASE WHEN GETDATE() > DataExpiracao THEN 0 ELSE Ativo END AS BIT) AS Ativo` — lógica de negócio no SQL (deveria estar no domínio)
- ⚠️ Scripts SQL em `db/` e também em `Infraestructure/DataBase/Scripts/` — duplicação ou redundância?
- ⚠️ Falta validação de linhas afetadas em updates (ex: `AtualizarSenha` não verifica se o CPF existe)

---

### 3.4 Api (Apresentação)

**Controllers:**

| Controller | Endpoints | Observações |
|---|---|---|
| [`UsuarioController`](Api/Controllers/UsuarioController.cs:9) | Cadastro (Comprador/Vendedor), Login, CRUD de dados | `ListarUsuarioEspecifico` sem `[Authorize]` — qualquer um pode consultar CPF de qualquer usuário |
| [`EventoController`](Api/Controllers/EventoController.cs:10) | CRUD de eventos, listagem por vendedor | Boa separação de roles; tratamento de exceções consistente |
| [`ReservaController`](Api/Controllers/ReservaController.cs:11) | Fazer reserva, listar por CPF, Admin listar tudo, confirmar pagamento | Depende diretamente de `IReservaRepository` (quebra de Clean Architecture?) |
| [`IngressoController`](Api/Controllers/IngressoController.cs:9) | Listar ingressos de evento, buscar por ID | Endpoints públicos sem autenticação para listagem |
| [`CupomController`](Api/Controllers/CupomController.cs:11) | CRUD de cupons, listar válidos | Recebe `AdminId` nos DTOs em vez de extrair do JWT; `DeletarCupom` recebe `adminId` no body (deveria ser do token) |

**Middlewares:**
- [`GlobalExceptionHandlerMiddleware`](Api/Middlewares/GlobalExceptionHandlerMiddleware.cs:6) — Tratamento global de exceções: `DomainException` → 400, `KeyNotFoundException` → 404, outras → 500

**Background Tasks:**
- [`LiberacaoAssentosWorker`](Api/BackgroundTasks/LiberacaoAssentosWorker.cs:5) — Worker que roda a cada 1 minuto liberando assentos com reserva expirada (15 minutos de tolerância)

**Program.cs:**
- [`Program.cs`](Api/Program.cs:1) — Configuração completa: DI, JWT, AutoMapper, DbUp, Swagger
- Registro de serviços bem organizado

**Pontos de Melhoria na API:**
- ❗ `UsuarioController.ListarUsuarioEspecifico` — endpoint público sem `[Authorize]`, permitindo consulta de CPF
- ❗ `CupomController.DeletarCupom` — recebe `adminId` no body em vez de extrair do JWT
- ❗ `CupomController.CadastrarCupom` — rota `CadastrarCupom/{Id}` recebe ID na URL sem uso claro
- ⚠️ Tratamento de exceções nos controllers: mistura `try/catch` com `GlobalExceptionHandler`
- ⚠️ `ReservaController` injeta repositórios diretamente — quebra o padrão de usar apenas services

---

### 3.5 Web (Frontend — Blazor Server)

**Tecnologias:** Blazor Server, MudBlazor, HttpClient para API

**Páginas Principais:**

| Página | Funcionalidade |
|---|---|
| [`Home.razor`](Web/Components/Pages/Home.razor:1) | Lista de eventos disponíveis com `CartaoEvento` |
| [`ComprarIngressos.razor`](Web/Components/Pages/ComprarIngressos.razor:1) | Mapa visual de assentos (VIP dourado, disponível cinza, indisponível vermelho, selecionado roxo) |
| [`CheckoutReserva.razor`](Web/Components/Pages/CheckoutReserva.razor:1) | Resumo da reserva com seleção de cupom |
| [`Pagamento.razor`](Web/Components/Pages/Pagamento.razor) | Tela de pagamento |
| [`Login.razor`](Web/Components/Pages/Login.razor) | Autenticação |
| [`Cadastro.razor`](Web/Components/Pages/Cadastro.razor) | Cadastro de comprador |
| [`MinhasReservas.razor`](Web/Components/Pages/MinhasReservas.razor) | Histórico do usuário |
| [`Cupons.razor`](Web/Components/Pages/Cupons.razor) | Lista de cupons disponíveis |

**Páginas Admin:**
- [`Admin/AdminEventos.razor`](Web/Components/Pages/Admin/AdminEventos.razor) — Gestão de eventos (admin)
- [`Admin/AdminReservas.razor`](Web/Components/Pages/Admin/AdminReservas.razor) — Lista todas reservas
- [`Admin/Cupons.razor`](Web/Components/Pages/Admin/Cupons.razor) — CRUD de cupons
- [`Admin/Usuarios.razor`](Web/Components/Pages/Admin/Usuarios.razor) — Lista usuários

**Páginas Vendedor:**
- [`Vendedor/CriarEvento.razor`](Web/Components/Pages/Vendedor/CriarEvento.razor) — Criação de evento
- [`Vendedor/MeusEventos.razor`](Web/Components/Pages/Vendedor/MeusEventos.razor) — Lista eventos do vendedor

**Autenticação:**
- [`CustomAuthStateProvider`](Web/Auth/CustomAuthStateProvider.cs:9) — Provedor de autenticação customizado que lê token JWT do `localStorage`
- Parse manual do JWT para extrair claims

**Pontos de Melhoria no Web:**
- ⚠️ Cálculo de desconto duplicado no frontend (`CheckoutReserva.razor:AplicarCupom`) — o backend também valida, mas o frontend replica a lógica
- ⚠️ Token armazenado em `localStorage` (vulnerável a XSS)
- ⚠️ HttpClient com `ServerCertificateCustomValidationCallback` ignorando certificados — inseguro para produção
- ⚠️ URL da API hardcoded (`http://localhost:5007/`) — deveria estar em configuração

---

## 4. Análise de Segurança

### 4.1 Problemas Críticos

| # | Problema | Localização | Impacto |
|---|---|---|---|
| 1 | **Senhas armazenadas em texto puro** | [`Usuario.cs:30`](Domain/Entities/Usuario.cs:30), [`UsuarioService.cs:53`](Application/Service/UsuarioService.cs:53) | Exposição total de credenciais se o banco for comprometido |
| 2 | **Endpoint público de consulta de CPF** | [`UsuarioController.cs:52`](Api/Controllers/UsuarioController.cs:52) | Qualquer pessoa não autenticada pode consultar CPFs |
| 3 | **AdminId no body em vez do JWT** | [`CupomController.cs:36`](Api/Controllers/CupomController.cs:36) | Possível impersonificação de admin |
| 4 | **Ignorar certificado SSL no frontend** | [`Web/Program.cs:22`](Web/Program.cs:22) | Vulnerável a MITM em produção |
| 5 | **Token JWT no localStorage** | Todo o frontend | Vulnerável a ataques XSS |

### 4.2 Problemas Moderados

- Falta de rate limiting na API de login
- `ListarUsuarioEspecifico` sem autorização de role
- Rota `DebugClaims` expõe claims do JWT ([`CupomController.cs:119`](Api/Controllers/CupomController.cs:119))

---

## 5. Fluxos Principais

### 5.1 Fluxo de Compra
```
Home → ComprarIngressos → CheckoutReserva → Pagamento
1. Usuário vê eventos na Home
2. Seleciona evento → mapa de assentos
3. Escolhe assento disponível → checkout
4. Aplica cupom (opcional) → confirma reserva
5. API bloqueia assento (Status=1)
6. Usuário confirma pagamento → Status=2 (Vendido)
7. Background Worker libera assentos não pagos após 15 min
```

### 5.2 Fluxo de Autenticação
```
1. Usuário envia email+senha → POST /api/Usuario/login
2. API valida credenciais (sem hash)
3. Gera token JWT com claims (cpf, email, role, perfilId)
4. Frontend armazena no localStorage
5. CustomAuthStateProvider lê token e cria ClaimsPrincipal
6. Requisições seguintes usam token no header Authorization
```

### 5.3 Fluxo de Criação de Evento
```
1. Vendedor preenche nome, capacidade, data, preço
2. API cria Evento e gera lote de ingressos (10% VIP, 90% Geral)
3. Ingressos numerados por fila (20 assentos/fila)
4. Preço VIP = PreçoPadrão × 1.5
```

---

## 6. Análise de Arquitetura

### 6.1 Clean Architecture — Avaliação

✅ **Acertos:**
- Separação clara em 4 camadas
- Domain sem dependências externas
- Interfaces de repositório no Domain, implementações na Infraestructure
- Injeção de dependências via DI

⚠️ **Violações / Desvios:**
- [`ReservaController`](Api/Controllers/ReservaController.cs:58) injeta `IReservaRepository` e `IIngressoRepository` diretamente — quebra a camada de Application
- [`CupomService.CadastrarCupom`](Application/Service/CupomService.cs:26) usa `Task.Run` em método síncrono do repositório — inconsistência de async/sync
- Métodos síncronos nos repositórios (`CadastrarUsuario`, `CadastrarCupom`) misturados com métodos assíncronos
- Lógica de negócio no SQL (`CupomRepository.ListarTodosCupons` com `CASE WHEN` para expiração)

### 6.2 Padrões Utilizados

| Padrão | Onde | Status |
|---|---|---|
| Factory Method | `Usuario.Criar()`, `Reserva.Criar()`, `Cupom.Criar()` | ✅ |
| Repository | `I*Repository` / `*Repository` | ✅ |
| DTO | `Application/DTOs/` | ✅ |
| AutoMapper | Mapeamentos em `Application/Mappings/` | ✅ |
| Background Service | `LiberacaoAssentosWorker` | ✅ |
| Middleware | `GlobalExceptionHandlerMiddleware` | ✅ |
| Strategy (via DI) | Services injetados nos Controllers | ✅ |

---

## 7. Testes

O projeto de testes [`tests/SoldOutTickets.Tests.csproj`](tests/SoldOutTickets.Tests.csproj) contém:

| Arquivo | Conteúdo |
|---|---|
| [`tests/CupomTests.cs`](tests/CupomTests.cs) | Testes de validação de cupom |
| [`tests/ReservaTests.cs`](tests/ReservaTests.cs) | Testes de regras de reserva |
| [`tests/UsuarioTests.cs`](tests/UsuarioTests.cs) | Testes de validação de usuário |
| [`tests/UnitTest1.cs`](tests/UnitTest1.cs) | Teste genérico |

**Avaliação:** Testes focados apenas no Domain (validações). Faltam:
- Testes de Application Services
- Testes de integração com banco
- Testes de repositórios (mock)
- Testes de Controllers
- Testes de Frontend

---

## 8. Banco de Dados

### 8.1 Estrutura

```sql
Perfis (Id UNIQUEIDENTIFIER PK, Nome VARCHAR(50))
Usuarios (Cpf VARCHAR(11) PK, Nome, Email, PerfilId FK→Perfis, Senha)
Cupons (Codigo VARCHAR(49) PK, PorcentagemDesconto INT, ValorMinimo DECIMAL, DataExpiracao DATETIME, Ativo BIT)
Eventos (Id UNIQUEIDENTIFIER PK, Nome, CapacidadeTotal, DataEvento, PrecoPadrao, VendedorCpf)
Ingressos (Id UNIQUEIDENTIFIER PK, EventoId FK→Eventos, Preco, Posicao, Setor, Status, DataBloqueio)
Reservas (Id UNIQUEIDENTIFIER PK, UsuarioCpf FK→Usuarios, EventoId FK→Eventos, IngressoId FK→Ingressos, CupomUtilizado, ValorFinalPago, DataBloqueio)
```

### 8.2 Migrations (DbUp)

- 8 scripts versionados de `Script0001` a `Script0008`
- Scripts embutidos no assembly da Infraestructure
- Script de setup completo em [`db/Script0000_Setup_Completo.sql`](db/Script0000_Setup_Completo.sql)

---

## 9. Pontos de Melhoria Prioritários

### 🔴 Críticos (Segurança e Dados)

1. **Implementar hash de senha** (BCrypt ou PBKDF2) — [`UsuarioService.Login`](Application/Service/UsuarioService.cs:48) e [`Usuario.Senha`](Domain/Entities/Usuario.cs:18)
2. **Proteger endpoint de consulta de CPF** — [`UsuarioController.ListarUsuarioEspecifico`](Api/Controllers/UsuarioController.cs:52)
3. **Extrair adminId do JWT** em vez de receber no body — [`CupomController.DeletarCupom`](Api/Controllers/CupomController.cs:35)
4. **Remover `DebugClaims`** endpoint em produção — [`CupomController.DebugClaims`](Api/Controllers/CupomController.cs:119)

### 🟡 Altos (Arquitetura e Boas Práticas)

5. **Eliminar duplicação de exceções** entre `Domain.Exceptions` e `Application.Exceptions`
6. **Padronizar async/await** nos repositórios (tornar `CadastrarUsuario` e `CadastrarCupom` assíncronos)
7. **Remover `Task.Run`** desnecessário em [`CupomService.CadastrarCupom`](Application/Service/CupomService.cs:26)
8. **Corrigir injeção direta de repositórios** em [`ReservaController`](Api/Controllers/ReservaController.cs:11)
9. **Mover lógica de expiração de cupom** do SQL para o Domain ([`CupomRepository.ListarTodosCupons`](Infraestructure/Repository/CupomRepository.cs:162))

### 🔵 Médios (Manutenibilidade)

10. **Extrair PerfilIds fixos** para constantes ou enum
11. **Centralizar validação de `adminId`** em um filtro/middleware em vez de repetir nos services
12. **Mover URL da API** para configuração no frontend
13. **Adicionar validação de linhas afetadas** nos updates dos repositórios
14. **Corrigir rota `CadastrarCupom/{Id}`** que recebe ID na URL sem uso claro

### 🟢 Baixos (Qualidade)

15. **Adicionar testes de integração**
16. **Adicionar testes de serviços**
17. **Documentar Swagger** com descrições e exemplos
18. **Adicionar validação de ModelState** nos controllers
19. **Remover scripts SQL duplicados** (em `db/` e `Infraestructure/DataBase/Scripts/`)

---

## 10. Requisitos Atendidos vs. Pendentes

Com base em [`docs/requisitos.md`](docs/requisitos.md):

| Requisito | Status | Observação |
|---|---|---|
| RF01 (Cadastro comprador) | ✅ | [`UsuarioController.CadastrarComprador`](Api/Controllers/UsuarioController.cs:22) |
| RF02 (Cadastro vendedor) | ✅ | [`UsuarioController.CadastrarVendedor`](Api/Controllers/UsuarioController.cs:33) |
| RF03 (Login) | ✅ | [`UsuarioController.Login`](Api/Controllers/UsuarioController.cs:42) — sem hash |
| RF04 (Token JWT) | ✅ | [`TokenService.GerarToken`](Application/Service/TokenService.cs:20) |
| RF05 (Alterar dados) | ✅ | Endpoints de alteração de nome, email, senha |
| RF06 (Remover conta) | ✅ | [`UsuarioController.RemoverUsuario`](Api/Controllers/UsuarioController.cs:60) |
| RF07 (Criar evento) | ✅ | [`EventoController.CreateAsync`](Api/Controllers/EventoController.cs:54) |
| RF08 (Ingressos 10% VIP) | ✅ | [`Evento.GerarLoteIngressos`](Domain/Entities/Evento.cs:33) |
| RF09 (Vendedor edita/exclui próprios) | ✅ | Validação por CPF do vendedor |
| RF10 (Admin exclui qualquer) | ✅ | [`EventoController.DeleteAsync`](Api/Controllers/EventoController.cs:88) |
| RF11 (Visualizar eventos) | ✅ | [`EventoController.GetAllAsync`](Api/Controllers/EventoController.cs:19) |
| RF12 (Ingressos com status visual) | ✅ | Frontend com cores por status |
| RF13 (Selecionar assento) | ✅ | [`ComprarIngressos.razor`](Web/Components/Pages/ComprarIngressos.razor:1) |
| RF14 (Bloqueio temporário) | ✅ | [`LiberacaoAssentosWorker`](Api/BackgroundTasks/LiberacaoAssentosWorker.cs:5) |
| RF15 (Reservar ingresso) | ✅ | [`ReservaController.FazerReserva`](Api/Controllers/ReservaController.cs:24) |
| RF16 (Impedir duplicata) | ✅ | [`ReservaService.FazerReserva`](Application/Service/ReservaService.cs:49) |
| RF17 (Liberar assentos expirados) | ✅ | Background worker a cada 1 minuto |
| RF18 (Cupom na reserva) | ✅ | [`Reserva.Criar`](Domain/Entities/Reserva.cs:26) |
| RF19 (CRUD cupons) | ✅ | [`CupomController`](Api/Controllers/CupomController.cs:1) |
| RF20 (Validar cupom) | ✅ | [`Cupom.EstaValidoParaUso`](Domain/Entities/Cupom.cs:13) |
| RF21 (Valor negativo impedido) | ✅ | Validação no Domain |
| RNF01 (SQL Injection) | ✅ | Dapper com parâmetros |
| RNF02 (Hash de senha) | ❌ **NÃO IMPLEMENTADO** | Senha em texto puro |
| RNF03 (JWT com roles) | ✅ | `TokenService` com claims de role |
| RNF04 (Migrations DbUp) | ✅ | Scripts versionados |
| RNF05 (Transações) | ✅ | Transações em operações críticas |

---

## 11. Conclusão

O **SoldOut Tickets** é um sistema funcional com boa arquitetura base, seguindo Clean Architecture com separação clara de responsabilidades. O uso de Dapper, JWT, DbUp e MudBlazor demonstra boas escolhas tecnológicas.

**Principais forças:**
- Entidades de domínio com boas validações encapsuladas
- Proteção contra SQL Injection via Dapper parametrizado
- Migrations automáticas com DbUp
- Background worker para liberação de assentos
- Interface visual atrativa com MudBlazor

**Principais fraquezas:**
- **Senhas em texto puro** — o problema de segurança mais crítico
- **Duplicação de código** nas exceções (Domain vs Application)
- **Inconsistência async/sync** nos repositórios
- **Ausência de testes de integração** e de serviços
- **Alguns endpoints com problemas de segurança** (CPF público, adminId no body)

O sistema está maduro para uso em produção **desde que** as issues críticas de segurança (hash de senha, proteção de endpoints) sejam resolvidas primeiro.
