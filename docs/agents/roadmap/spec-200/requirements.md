---
name: "Guid Id como PK de Usuarios (substituir Cpf)"
status: "verified"
references:
  - "visao.md §2 (Autonomia — cadastrar e vender sem Admin)"
  - "Spec 130 (Isolamento Multi-Tenant)"
  - "ST-01 (Auto Cadastro de Vendedor)"
  - "ST-09 (Vendedor na tabela Usuarios)"
dependencies:
  - "Spec 120 — BCrypt, JWT e rate limit já implementados"
  - "Spec 130 — Isolamento Multi-Tenant já implementado"
  - "Spec 150 — Resiliência e Tratamento de Erros já implementado"
  - "Spec 160 — Cupons de Desconto já implementado"
  - "Spec 170 — Pagamento Simulado já implementado"
---

# Spec 200: Guid Id como PK de Usuarios — Requirements

## Value Delivery

Esta spec entrega o bloco **"Autonomia — cadastrar e vender sem Admin"** definido em `visao.md §2`:

> **Migração de PK:** A tabela `Usuarios` troca sua chave primária de `Cpf` (string) para `Id` (Guid). Com isso, o cadastro de vendedor (que não tem CPF) funciona normalmente, e o campo `Cpf` torna-se opcional — obrigatório apenas para compradores.

**Problema resolvido:** O sistema atual usa `Cpf` como PRIMARY KEY da tabela `Usuarios`. O auto cadastro de vendedor (ST-01) tenta persistir um usuário com `Cpf = null`, o que é inviável com a constraint atual. Isso obrigou a criar uma gambiarra onde `Cpf` é setado como `null` (`Cpf = null` em `Usuario.CriarVendedor()`), o que fere a integridade do banco e das entidades.

**Resultado esperado:** Todo usuário (comprador ou vendedor) tem um `Guid Id` como identificador único. O CPF continua sendo validado e obrigatório para compradores, mas é opcional para vendedores. Todas as relações estrangeiras (Reservas, Eventos) passam a usar o `Guid Id` em vez do `Cpf`.

---

## Functional Requirements

### FR-001: Adicionar coluna Id (Guid) na tabela Usuarios

**What:** Criar nova coluna `Id UNIQUEIDENTIFIER NOT NULL` na tabela `Usuarios`. Migrar todas as linhas existentes atribuindo `NEWID()` a cada uma. Em seguida, definir `Id` como a nova PRIMARY KEY e remover a constraint PK do `Cpf`. O `Cpf` deve tornar-se `NVARCHAR(11) NULL` (opcional).

**Why:** `Cpf` não pode ser PK porque vendedores não têm CPF. `Guid` é o padrão usado em todas as outras tabelas do sistema (Reservas, Eventos, Ingressos, ItensReserva, Pagamentos, Cupons, Perfis).

**Acceptance Criteria:**
- [ ] Coluna `Id UNIQUEIDENTIFIER NOT NULL` adicionada com valores `NEWID()` para registros existentes
- [ ] `PRIMARY KEY` movida de `Cpf` para `Id`
- [ ] Coluna `Cpf` alterada para `NVARCHAR(11) NULL`
- [ ] Índice único em `Cpf` (se não nulo) para evitar duplicatas
- [ ] Migration script `Script0013_MigrarPkGuidUsuarios.sql` executável e idempotente

### FR-002: Atualizar FKs de Reservas (UsuarioCpf → UsuarioId)

**What:** A tabela `Reservas` tem duas colunas que referenciam `Usuarios(Cpf)`: `UsuarioCpf` e `VendedorCpf`. Ambas devem ser substituídas por `UsuarioId UNIQUEIDENTIFIER` e `VendedorId UNIQUEIDENTIFIER`, com FKs apontando para `Usuarios(Id)`.

**Why:** FKs baseadas em string são frágeis e quebram com a troca de PK. FKs baseadas em Guid são consistentes com o resto do schema.

**Acceptance Criteria:**
- [ ] Coluna `UsuarioId UNIQUEIDENTIFIER NOT NULL` adicionada a Reservas, com valor migrado via JOIN com Usuarios
- [ ] Coluna `VendedorId UNIQUEIDENTIFIER NULL` adicionada a Reservas, com valor migrado via JOIN com Usuarios
- [ ] Colunas `UsuarioCpf` e `VendedorCpf` removidas de Reservas
- [ ] FK `FK_Reservas_Usuarios` apontando para `Usuarios(Id)` criada
- [ ] FK `FK_Reservas_Vendedor` apontando para `Usuarios(Id)` criada

### FR-003: Atualizar FK de Eventos (VendedorCpf → VendedorId)

**What:** A tabela `Eventos` tem coluna `VendedorCpf NVARCHAR(11)` que referencia o vendedor. Substituir por `VendedorId UNIQUEIDENTIFIER NULL` com FK para `Usuarios(Id)`.

**Why:** Mesmo motivo de FR-002 — consistência de FKs e desacoplamento do CPF como identificador.

**Acceptance Criteria:**
- [ ] Coluna `VendedorId UNIQUEIDENTIFIER NULL` adicionada a Eventos, com valor migrado via JOIN com Usuarios
- [ ] Coluna `VendedorCpf` removida de Eventos
- [ ] FK `FK_Eventos_Vendedor` apontando para `Usuarios(Id)` criada (opcional)

### FR-004: Atualizar entidade Usuario (Domain)

**What:** Adicionar propriedade `Guid Id` na entidade `Usuario`. Tornar `Cpf` opcional (string vazia ou null para vendedores). A factory `CriarVendedor` deve gerar um `Guid.NewGuid()` para o `Id`.

**Why:** A entidade de domínio deve refletir o schema do banco. O `Id` torna-se o identificador canônico.

**Acceptance Criteria:**
- [ ] `Usuario` tem propriedade `public Guid Id { get; private set; } = Guid.NewGuid();`
- [ ] `Cpf` mantém validação apenas quando preenchido (comprador)
- [ ] `CriarComprador`: seta `Id = Guid.NewGuid()`, mantém validação de CPF
- [ ] `CriarVendedor`: seta `Id = Guid.NewGuid()`, não seta Cpf
- [ ] Construtor `Usuario(string cpf, ...)` atualizado para aceitar Guid ou gerar automaticamente

### FR-005: Atualizar repositório UsuarioRepository

**What:** Todos os métodos do `UsuarioRepository` que usam `Cpf` como parâmetro de busca ou chave devem ser atualizados para usar `Id` (Guid) ou aceitar ambos. O INSERT agora inclui a coluna `Id`.

**Why:** As queries SQL precisam refletir o novo schema: `WHERE Id = @Id` em vez de `WHERE Cpf = @Cpf`.

**Acceptance Criteria:**
- [ ] `CadastrarUsuario`: INSERT inclui `Id` com `Guid.NewGuid()`
- [ ] `CadastrarVendedor`: INSERT inclui `Id` com `Guid.NewGuid()`
- [ ] `BuscarCpf`: query usa `WHERE Cpf = @Cpf` (ainda necessário para busca por CPF)
- [ ] `RemoverUsuario`: query usa `WHERE Id = @Id`
- [ ] `AtualizarSenha`: query usa `WHERE Id = @Id`
- [ ] `AtualizarEmailAsync`: query usa `WHERE Id = @Id`
- [ ] `AtualizarNomeAsync`: query usa `WHERE Id = @Id`
- [ ] Novos métodos `BuscarPorId(Guid id, ...)` adicionados

### FR-006: Atualizar UsuarioService

**What:** `UsuarioService` deve usar `Guid Id` para operações de busca/atualização quando disponível. O CPF continua sendo usado para busca de compradores no cadastro.

**Why:** A camada de serviço é a principal consumidora do repositório e deve refletir a nova API.

**Acceptance Criteria:**
- [ ] `UsuarioEspecifico(Guid id, ...)` — busca por Id, não por Cpf
- [ ] `RemoverUsuario(Guid id, ...)` — remove por Id, não por Cpf
- [ ] `AlterarSenha(Guid id, ...)` — atualiza por Id
- [ ] `AlterarEmailAsync(Guid id, ...)` — atualiza por Id
- [ ] `AlterarNomeAsync(Guid id, ...)` — atualiza por Id
- [ ] `CadastrarComprador` — mantém busca por CPF/Email para verificar duplicidade
- [ ] `Login` — mantém busca por Email (não mudou)

### FR-007: Atualizar UsuarioController

**What:** As rotas do `UsuarioController` que usam `{cpf}` no path devem ser alteradas para `{id}` (Guid).

**Why:** O CPF não identifica mais unicamente um usuário (vendedores não têm CPF).

**Acceptance Criteria:**
- [ ] `GET /api/usuario/ListarUsuarioEspecifico/{id}` — Guid, não string
- [ ] `DELETE /api/usuario/DeletarUsuario/{id}` — Guid, não string
- [ ] `PUT /api/usuario/alterarsenha/{id}` — Guid, não string
- [ ] `PUT /api/usuario/alterarnome/{id}` — Guid, não string
- [ ] `PUT /api/usuario/alteraremail/{id}` — Guid, não string

### FR-008: Atualizar TokenService (JWT claims)

**What:** O JWT gerado em `TokenService.GerarToken()` deve incluir o claim `userId` (Guid) no lugar ou em adição ao claim `cpf`. O claim `cpf` deve ser opcional (presente apenas se o usuário tiver CPF).

**Why:** Os controllers que extraem o identificador do usuário via `User.Claims.FirstOrDefault(c => c.Type == "cpf")` precisam migrar para `userId`.

**Acceptance Criteria:**
- [ ] Claim `"userId"` com `usuario.Id.ToString()` adicionado ao JWT
- [ ] Claim `"cpf"` condicional: só adicionado se `!string.IsNullOrEmpty(usuario.Cpf)`
- [ ] Claim `"cpf"` mantido para retrocompatibilidade temporária

### FR-009: Atualizar ReservaController e ReservaService

**What:** `ReservaController` extrai `cpf` do JWT para identificar o usuário logado. Deve passar a usar `userId` (Guid). O mesmo vale para `ReservaService` e `ReservaRepository` — os métodos que aceitam `string cpf` para busca de reservas devem migrar para `Guid usuarioId`.

**Why:** Consistência com a nova PK. As queries SQL nas tabelas `Reservas` e `ItensReserva` usam `UsuarioCpf` — que será substituído por `UsuarioId`.

**Acceptance Criteria:**
- [ ] `ReservaController.CriarReserva`: extrai `userId` do JWT, não `cpf`
- [ ] `ReservaController.ListarMinhasReservas`: usa `userId`
- [ ] `ReservaController.ListarMinhasVendas`: usa `userId`
- [ ] `ReservaRepository.ListarPorCpf` → `ListarPorUsuarioId(Guid usuarioId)`
- [ ] `ReservaRepository.ListarReservasDetalhadasPorCpf` → `ListarReservasDetalhadasPorUsuarioId`
- [ ] `ReservaRepository.ListarReservasDetalhadasPorVendedor(string cpf)` → `(Guid vendedorId)`
- [ ] Queries SQL usam `UsuarioId`/`VendedorId` em vez de `UsuarioCpf`/`VendedorCpf`

### FR-010: Atualizar outras entidades e DTOs

**What:** Entidades `Reserva` e `Evento` trocam `string VendedorCpf`/`string UsuarioCpf` por `Guid` correspondentes. DTOs de resposta devem refletir as mudanças.

**Why:** A camada de domínio deve espelhar o banco de dados.

**Acceptance Criteria:**
- [ ] `Reserva.UsuarioCpf` → `Reserva.UsuarioId` (Guid)
- [ ] `Reserva.VendedorCpf` → `Reserva.VendedorId` (Guid?)
- [ ] `Evento.VendedorCpf` → `Evento.VendedorId` (Guid?)
- [ ] `EventoService` atualizado para usar Guid
- [ ] DTOs de resposta atualizados: `UsuarioResponseDTO`, `UsuarioSaidaDTO`, `VendedorCadastradoDTO`, `LoginResponseDTO` incluem `Id`

### FR-011: Atualizar DatabaseSeeder

**What:** O `DatabaseSeeder` cria o Admin padrão. Deve gerar um `Guid` para o `Id` e incluir a coluna `Id` no INSERT.

**Acceptance Criteria:**
- [ ] Admin seed usa `Guid.NewGuid()` para `Id`
- [ ] INSERT do Admin inclui coluna `Id`
- [ ] Admin continua funcional após migration

### FR-012: Atualizar testes existentes

**What:** Os testes existentes que referenciam `Cpf` como identificador ou que mockam o `UsuarioRepository` devem ser atualizados para o novo schema.

**Acceptance Criteria:**
- [ ] Testes do `UsuarioService` atualizados para usar `Guid Id`
- [ ] Testes do `ReservaService` atualizados para usar `Guid`
- [ ] `SegurancaTests` (Spec 120) — 8/8 passando
- [ ] `CupomTests` (Spec 160) — passando
- [ ] `ResilicienciaTests` (Spec 150) — 8/8 passando
- [ ] `dotnet test` completo sem novas falhas (exceto as 2 falhas pré-existentes do `AutoCadastroVendedorTests`)

---

## Non-Functional Requirements

### NFR-001: Idempotência da Migration

**What:** O script de migration `Script0013` deve ser executável múltiplas vezes sem erro (checks `IF EXISTS`/`IF NOT EXISTS`).

**Acceptance Criteria:**
- [ ] Rodar o script duas vezes não causa erro
- [ ] Colunas antigas só são removidas após migração dos dados

### NFR-002: Performance das Queries

**What:** As queries que usam `WHERE Id = @Id` (Guid) devem ter performance igual ou superior às queries com `WHERE Cpf = @Cpf`.

**Acceptance Criteria:**
- [ ] Índices clusterizados ou non-clustered nas colunas FK (`UsuarioId`, `VendedorId`)
- [ ] Nenhuma query faz table scan por falta de índice

### NFR-003: Retrocompatibilidade do JWT

**What:** Tokens JWT emitidos antes da migration (com claim `cpf` apenas) devem continuar funcionando durante o período de transição.

**Acceptance Criteria:**
- [ ] Claim `cpf` mantido no JWT (além do novo claim `userId`)
- [ ] Controllers verificam `userId` primeiro, com fallback para `cpf` se ausente
- [ ] Após 30 dias (tokens expiram em 24h), o fallback pode ser removido

---

## Constraints

1. **ADR-001 (Clean Architecture):** A migração de PK é responsabilidade do banco (script SQL). O Domain reflete o banco. Application e Api usam o Domain.
2. **Stack:** .NET 9, C# 13, ASP.NET Core, SQL Server, Dapper, DbUp.
3. **Spec 120:** JWT e BCrypt não são alterados — apenas o conteúdo dos claims é expandido.
4. **Spec 130:** Isolamento Multi-Tenant usa `VendedorCpf` em várias queries — todas devem migrar para `VendedorId`.
5. **Retrocompatibilidade:** A API não pode quebrar para clientes existentes (Blazor frontend).
6. **Banco existente:** A migration deve preservar todos os dados existentes.

---

## Edge Cases & Error States

| # | Caso | Gatilho | Comportamento Exato |
|---|------|---------|---------------------|
| EC-001 | Admin seed após migration | Migration já executada, seeder roda no startup | Admin criado com `Id = Guid.NewGuid()`, sem CPF (00000000000). Deve manter o mesmo Email para evitar duplicata. |
| EC-002 | Vendedor existente com Cpf=null | Migration tenta criar índice único em Cpf | Índice deve ser `UNIQUE WHERE Cpf IS NOT NULL` (filtered index) |
| EC-003 | Reserva existente com UsuarioCpf | Migration do script SQL | `UPDATE Reservas SET UsuarioId = u.Id FROM Reservas r INNER JOIN Usuarios u ON r.UsuarioCpf = u.Cpf` |
| EC-004 | Token antigo (só cpf) após deploy | Controller extrai `userId` do JWT | Fallback: se `userId` não existe, busca `cpf` e faz lookup no banco |
| EC-005 | Dois compradores com mesmo CPF | Cadastro tenta duplicar CPF | Filtered unique index `WHERE Cpf IS NOT NULL` impede duplicatas |
| EC-006 | Vendedor tenta logar com CPF | Login busca por Email (não CPF) | Sem impacto — login já é por Email, não CPF |

---

## Dependencies

| Dependência | Tipo | Detalhe |
|-------------|------|---------|
| `Script0000` a `Script0012` | Scripts SQL existentes | A nova migration (`Script0013`) roda depois de todas as existentes |
| `UsuarioRepository` | Código existente | Base para refatoração das queries |
| `IUsuarioRepository` | Interface | Métodos precisam de novas assinaturas |
| `ReservaRepository` | Código existente | Todas as queries que fazem JOIN com Usuarios.Cpf |
| `TokenService` | Código existente | Claims do JWT expandidos |
| `DatabaseSeeder` | Código existente | Admin seed atualizado |

---

## Out of Scope

Estes itens NÃO fazem parte desta spec e NÃO devem ser implementados:

1. **Remover completamente o claim `cpf` do JWT** — feito em spec futura após período de transição.
2. **CRUD de vendedor por Admin** — endpoints de gerenciamento de vendedores.
3. **Unificar comprador e vendedor em entidades separadas** — herança ou tabelas separadas.
4. **Migrar Perfis para usar Guids gerados dinamicamente** — os GUIDs fixos atuais permanecem.
5. **Alterar a tabela ItensReserva.CpfParticipante** — essa coluna não referencia Usuarios, é um campo livre.
6. **Validação de CNPJ no banco** — feita no Domain.
7. **Frontend Blazor** — ajustes de UI são responsabilidade de outra spec.
