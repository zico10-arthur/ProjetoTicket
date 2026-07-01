# Análise de Padrões Arquiteturais — SoldOut Tickets

> Documento gerado conforme exigência de análise arquitetural do ProjetoTicket.

---

## Cenário 1: Separação em 5 Projetos com Dependências Unidirecionais

### Contexto

O ProjetoTicket está organizado em 5 projetos distintos (`Domain`, `Application`,
`Infraestructure`, `Api`, `Web`) mais o projeto de testes. As dependências fluem
em sentido único: `Domain` não referencia nada; `Application` referencia somente
`Domain`; `Infraestructure` referencia `Domain` e `Application`; `Api` referencia
`Infraestructure`, `Application` e `Domain`; `Web` é um projeto Blazor
independente que consome a `Api` via HTTP.

```
Domain  ←  Application  ←  Infraestructure  ←  Api
  ↑                                              ↑
  └────────────── Web (consome via HTTP) ─────────┘
```

### Padrão Arquitetural Provável

**Clean Architecture (Arquitetura Limpa) / Onion Architecture** — também conhecida
como Arquitetura em Camadas com Dependência Unidirecional. A camada mais interna
(`Domain`) contém as regras de negócio puras (entidades, validadores, exceções de
domínio, interfaces de repositório) e não conhece nenhuma camada externa. As
camadas externas dependem das internas, nunca o contrário. O fluxo de controle
parte da `Api` (Controllers), passa pela `Application` (Services de caso de uso)
e chega ao `Domain`, com a `Infraestructure` fornecendo implementações concretas
(repositórios Dapper, DbUp, conexão com SQL Server).

### Trade-off

**Positivo:** A regra de negócio fica totalmente isolada no projeto `Domain`.
Mudanças no banco de dados (ex: trocar Dapper por Entity Framework, migrar de
SQL Server para PostgreSQL) ou na camada de apresentação (ex: trocar Blazor por
React) não exigem alteração nas entidades, validadores ou exceções de domínio. Os
testes unitários em `tests/` validam as regras de negócio sem necessidade de
conexão com banco real, usando apenas `Mock<T>`.

**Negativo:** A quantidade de projetos e mapeamentos entre camadas aumenta a
complexidade inicial. Para cada entidade nova, é necessário criar DTOs na
`Application`, interfaces no `Domain`, implementações na `Infraestructure`,
controllers na `Api` e páginas no `Web` — resultando em mais arquivos e maior
sobrecarga de boilerplate em comparação com uma arquitetura monolítica de
projeto único.

---

## Cenário 2: Interfaces de Repositório no Domain, Implementações Dapper na Infraestructure

### Contexto

O projeto `Domain` define contratos como `IEventoRepository`, `IUsuarioRepository`,
`IReservaRepository`, `IPagamentoRepository`, `ICupomRepository` e
`IIngressoRepository`. O projeto `Infraestructure` implementa esses contratos
usando Dapper (micro-ORM) com SQL manual. A camada `Application` (serviços como
`EventoService`, `UsuarioService`, `ReservaService`, `PagamentoService`) recebe
as interfaces via injeção de dependência e opera exclusivamente contra as
abstrações, sem conhecer Dapper, SQL Server ou detalhes de conexão.

```
Domain/Interface/IEventoRepository.cs    (abstração)
        ↑
Infraestructure/Repository/EventoRepository.cs  (implementação Dapper)
        ↑
Application/Service/EventoService.cs     (consumidor via interface)
```

### Padrão Arquitetural Provável

**Repository Pattern (Padrão Repositório)** — um padrão estrutural que abstrai
a camada de persistência atrás de interfaces definidas no domínio. O objetivo é
desacoplar a lógica de negócio da tecnologia de acesso a dados. Neste projeto, a
variação utiliza Dapper em vez de um ORM completo (Entity Framework), o que
confere mais controle sobre as queries SQL e performance, mantendo a abstração.

### Trade-off

**Positivo:** Testabilidade e substituição de infraestrutura. Os serviços da
camada `Application` podem ser testados com `Mock<IEventoRepository>()` (ver
`EventoCancelamentoTests.cs`, `PagamentoTests.cs`, `ReservaTests.cs`), sem subir
banco de dados real. Se o time decidir migrar de Dapper para Entity Framework ou
para um banco NoSQL, basta criar novas implementações das interfaces no
`Infraestructure` — o `Domain` e a `Application` permanecem intocados.

**Negativo:** Vazamento de abstração em operações transacionais complexas. O
método `CancelarComTransacao` no `IEventoRepository` e `CriarComTransacao` no
`IPagamentoRepository` expõem a necessidade de transações atômicas no contrato do
repositório, o que é um conceito de infraestrutura. Isso força cada implementação
de repositório a gerenciar transações manualmente (Dapper + `DbConnection` +
`DbTransaction`), e dificulta cenários onde múltiplos repositórios precisam
participar da mesma transação — um problema clássico do Repository Pattern sem
Unit of Work explícito.

---

## Cenário 3: Serviços com Dependências Injetadas via Construtor

### Contexto

Todos os serviços da camada `Application` recebem suas dependências exclusivamente
pelo construtor:

```csharp
// Exemplo real do código
public class ReservaService
{
    public ReservaService(
        IReservaRepository reservaRepo,
        IUsuarioRepository usuarioRepo,
        IEventoRepository eventoRepo,
        ICupomRepository cupomRepo,
        IIngressoRepository ingressoRepo,
        IEmailSender emailSender)
    { ... }
}
```

O contêiner de Injeção de Dependência (DI) nativo do ASP.NET Core (`builder.Services`
no `Program.cs`) registra todos os serviços e repositórios no escopo adequado
(`Scoped`, `Singleton` ou `Transient`). Os Controllers da `Api` também recebem os
serviços via construtor, completando a cadeia de resolução.

### Padrão Arquitetural Provável

**Inversion of Control / Dependency Injection (IoC/DI)** — padrão onde o controle
sobre a criação e ciclo de vida dos objetos é delegado a um contêiner externo, em
vez de ser gerenciado manualmente pelas classes. Os princípios SOLID aplicados
são: **Dependency Inversion Principle** (depender de abstrações, não de
implementações concretas) e **Single Responsibility Principle** (cada serviço tem
uma única razão para mudar, pois as dependências são injetadas).

### Trade-off

**Positivo:** Desacoplamento total entre serviços e suas dependências concretas.
O `EventoService` não sabe se o `IEventoRepository` é implementado com Dapper,
Entity Framework ou um mock. Isso permite testar cada serviço isoladamente
(`new EventoService(mockRepo.Object, null!)`), trocar implementações sem alterar
os consumidores e controlar ciclos de vida (ex: `DbContext` como Scoped,
`IEmailSender` como Singleton). O contêiner DI do ASP.NET Core gerencia tudo isso
automaticamente, sem bibliotecas externas.

**Negativo:** Resolução implícita dificulta debugging. Quando um serviço possui 5
ou 6 dependências (como `ReservaService`), erros de registro no contêiner
(ex: esquecer `builder.Services.AddScoped<IEmailSender, ...>()`) só são
descobertos em tempo de execução, com mensagens de erro genéricas do tipo
`Unable to resolve service for type 'IEmailSender'`. Além disso, o grafo de
dependências se torna difícil de visualizar mentalmente — um desenvolvedor novo
precisa rastrear a cadeia completa de registros no `Program.cs` para entender
como uma requisição HTTP se transforma em uma operação de banco de dados.

---

## Resumo dos Padrões Identificados

| Cenário | Padrão | Positivo | Negativo |
|---|---|---|---|
| 5 projetos com dependências unidirecionais | **Clean Architecture** | Domínio isolado, troca de infra sem afetar regras | Boilerplate elevado (N projetos, DTOs, mapeamentos) |
| Interfaces no Domain, Dapper na Infra | **Repository Pattern** | Testabilidade com mocks, substituição de ORM/banco | Transações multi-repositório vazam abstração |
| Serviços com dependências via construtor | **Dependency Injection (IoC)** | Desacoplamento total, testes isolados, ciclos de vida gerenciados | Erros em runtime, grafo de dependências difícil de visualizar |

---

## Análise de Violações Arquiteturais

> Trechos analisados do código-fonte real do ProjetoTicket (Api, Application, Domain,
> Infraestructure).

---

### Violação 1: Camada Application referencia Infraestructure diretamente

**Problema:** O projeto `Application` possui uma referência direta ao projeto
`Infraestructure`, violando o princípio fundamental da Clean Architecture — a
camada de aplicação deve depender apenas do domínio.

**Evidência:** `Application/Application.csproj`, linha 5:
```xml
<ProjectReference Include="..\Infraestructure\Infraestructure.csproj" />
```

**Impacto:** A camada Application se torna acoplada a detalhes de infraestrutura
(Dapper, SQL Server, DbUp). Qualquer alteração no `Infraestructure` força a
recompilação da `Application`. Pior: serviços da Application podem importar
classes concretas do Infrastructure (ex: `ConnectionFactory`, repositórios
concretos), quebrando a inversão de dependência. O princípio da Clean
Architecture exige que as dependências apontem para dentro: Infrastructure → 
Application → Domain, nunca Application → Infrastructure.

**Ação Recomendada:** Remover a referência `Infraestructure` do `Application.csproj`.
Mover as dependências de pacotes de infraestrutura (BCrypt.Net, JWT) para o
projeto `Infraestructure`. Criar interfaces no `Domain` ou `Application` para
serviços como hash de senha e geração de token, implementando-as no
`Infraestructure`. O contêiner DI fará a resolução em runtime.

---

### Violação 2: Controller acessa Repository diretamente (bypass da camada Application)

**Problema:** O `UsuarioController` injeta e utiliza diretamente
`IUsuarioRepository`, ignorando a camada `Application` (que contém
`IUsuarioService`). Isso quebra o fluxo em camadas: o Controller deveria delegar
toda lógica de negócio ao Service, que coordena os Repositories.

**Evidência:** `Api/Controllers/UsuarioController.cs`, linhas 15 e 98:
```csharp
private readonly IUsuarioRepository _repository;  // ← violação: repositório no controller

public UsuarioController(IUsuarioService service, IUsuarioRepository repository)
{
    _service = service;
    _repository = repository;  // ← Controller depende diretamente de repositório
}

[HttpGet("Todos")]
public async Task<IActionResult> ListarTodos(CancellationToken ct)
{
    var usuarios = await _repository.ListarTodos(ct);  // ← bypass do Service
    return Ok(usuarios);
}
```

**Impacto:** Duas violações simultâneas: (1) o Controller tem duas fontes de
verdade — `_service` e `_repository` — e decide quando usar cada uma, o que
dificulta auditoria e manutenção; (2) a lógica de negócio que deveria estar no
Service (ex: filtros, autorização baseada em papel) é delegada ao Controller ou
simplesmente ignorada. Isso também viola o Single Responsibility Principle: o
Controller passa a ter múltiplas razões para mudar.

**Ação Recomendada:** Remover `IUsuarioRepository` do construtor do
`UsuarioController`. Criar um método `ListarTodosAsync` no `IUsuarioService`
(Application) que encapsule a chamada ao repositório com as devidas validações de
negócio. O Controller deve chamar apenas `_service.ListarTodosAsync(ct)`.

---

### Violação 3: Uso de Reflection para setar propriedade de entidade

**Problema:** O `EventoService.CriarEventoAsync` utiliza `typeof().GetProperty()`
com reflection para definir `VendedorId` após o AutoMapper criar a entidade.
Além de frágil, é um "hack" que contorna a tipagem estática e oculta a
dependência real.

**Evidência:** `Application/Service/EventoService.cs`, linhas 77-79:
```csharp
var evento = _mapper.Map<Evento>(eventoDto);
// Spec 200: VendedorId setado via Guid, não via DTO
typeof(Evento).GetProperty("VendedorId")?.SetValue(evento, vendedorId);
```

**Impacto:** (1) Se a propriedade `VendedorId` for renomeada ou removida, o
código compila normalmente mas falha silenciosamente em runtime (retorna null no
`GetProperty` e o valor nunca é setado). (2) Viola a segurança de tipo do C# — o
compilador não consegue verificar a existência da propriedade. (3) Torna
impossível "Find All References" no IDE para rastrear quem define `VendedorId`.
(4) Indica que o AutoMapper não está configurado corretamente ou que o DTO não
contém o campo necessário.

**Ação Recomendada:** Configurar o AutoMapper Profile para mapear `VendedorId`
adequadamente, ou adicionar `VendedorId` ao `EventoRequestDTO`, ou usar um
construtor/factory method na entidade `Evento` que receba `VendedorId`
explicitamente. Remover o reflection.

---

### Violação 4: Extração de userId do JWT duplicada em 5 métodos

**Problema:** O mesmo bloco de código — extrair `userId` dos claims do JWT e
validar se é um `Guid` — está copiado e colado em 5 métodos do
`EventoController`. Isso viola DRY (Don't Repeat Yourself) e dificulta a
manutenção.

**Evidência:** `Api/Controllers/EventoController.cs`, linhas 39-43, 71-75,
93-97, 115-119, 145-149 — todas contendo exatamente:
```csharp
var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value
                ?? User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value;

if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
    return Unauthorized();
```

**Impacto:** (1) Se a claim do JWT mudar de nome (ex: `"userId"` → `"sub"`), 5
lugares precisam ser alterados — com alto risco de esquecer algum. (2) A lógica
de fallback `"userId" ?? "cpf"` revela uma migração incompleta de identificação
(CPF → Guid) que está espalhada e não documentada. (3) Aumenta o acoplamento do
Controller com detalhes de autenticação JWT — a extração de claims deveria ser
responsabilidade de um componente dedicado.

**Ação Recomendada:** Criar um método auxiliar (ex: `Guid? GetUserId()` como
método privado do Controller, ou um `IUserContext` injetável) e substituir as 5
ocorrências. Considerar remover o fallback `"cpf"` se a migração para `"userId"`
já estiver completa.

---

### Violação 5: BCrypt.Net referenciado diretamente na camada Application

**Problema:** O projeto `Application` referencia o pacote NuGet `BCrypt.Net-Next`
e o utiliza diretamente em serviços como `UsuarioService`. Hashing de senha é uma
preocupação de infraestrutura — a camada Application deve depender de uma
abstração (interface), não de uma biblioteca concreta de criptografia.

**Evidência:** `Application/Application.csproj`, linha 10:
```xml
<PackageReference Include="BCrypt.Net-Next" Version="4.2.0" />
```

E seu uso em `SegurancaTests.cs`, linhas 50, 82, 131, entre outros:
```csharp
var senhaHash = BCrypt.Net.BCrypt.HashPassword(SenhaCorreta);
```

**Impacto:** (1) Se o time decidir migrar de BCrypt para Argon2 ou PBKDF2, a
camada Application inteira precisa ser alterada e retestada. (2) A escolha do
algoritmo de hash vira uma decisão de Application, quando deveria ser de
Infraestructure. (3) Os testes de Application (`SegurancaTests`) testam
indiretamente a biblioteca BCrypt em vez de testar apenas a lógica de negócio —
os testes quebram se a biblioteca for trocada, mesmo que a lógica de aplicação
permaneça a mesma.

**Ação Recomendada:** Criar uma interface `IPasswordHasher` no `Domain` ou
`Application` com métodos `string Hash(string senha)` e `bool Verify(string
senha, string hash)`. Implementar `BCryptPasswordHasher` no `Infraestructure`
como adaptador do BCrypt.Net. Registrar no DI e injetar nos serviços da
Application. Mover o pacote `BCrypt.Net-Next` do `Application.csproj` para o
`Infraestructure.csproj`.

