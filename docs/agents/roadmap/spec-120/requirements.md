---
name: "Segurança (BCrypt, JWT, Rate Limit)"
status: "audited"
references:
  - "ADR-003 (BCrypt para hash de senhas)"
  - "ADR-004 (JWT com roles para autenticação)"
  - "ADR-010 (Rate limiting no endpoint de login)"
  - "ADR-011 (Global Exception Handler como middleware)"
  - "visao.md §6.2 (Login Unificado com JWT — 3 Perfis)"
  - "arquitetura.md §1 (Perfis de Usuário)"
  - "sprints.md Sprint 1 — Segurança e Correções Críticas"
---

# Spec 120: Segurança (BCrypt, JWT, Rate Limit) — Requirements

## Value Delivery

Esta spec entrega o bloco de valor **"Autonomia — segurança para todos os perfis"** definido em [`visao.md §6.2`](../../../visao.md#62-login-unificado-com-jwt-3-perfis):

> **BCrypt para hash e verificação de senhas** → Segurança real: mesmo que o banco vaze, a senha do usuário está protegida por hash com salt. O usuário não precisa se preocupar com vazamento de credenciais.
>
> **JWT com claims `role`, `cpf`, `email`** → Experiência personalizada: após o login, o sistema sabe exatamente o que mostrar — painel de vendedor, home de comprador ou dashboard de admin — sem configuração manual.

Adicionalmente, esta spec entrega proteção contra força bruta (rate limiting) e proteção da chave de assinatura JWT contra vazamento no repositório Git — dois riscos críticos identificados em [`visao.md §12`](../../../visao.md#12-riscos-e-mitigacoes).

**Problema resolvido:** Elimina 3 vulnerabilidades críticas do estado atual do código (linha 107 do roadmap):
1. Senhas armazenadas em texto plano na tabela `Usuarios`
2. Chave `Jwt:Key` exposta em `appsettings.json` versionado
3. Ausência de proteção contra força bruta no endpoint `POST /api/usuario/login`

---

## Functional Requirements

### FR-001: Hash BCrypt no Cadastro de Usuário

**What:** Toda senha fornecida em `POST /api/usuario/cadastrar-comprador` e `POST /api/usuario/cadastrar-vendedor` deve sofrer hash BCrypt com work factor 11 antes da persistência no banco de dados. O hash é aplicado na camada Infrastructure (`UsuarioRepository`), não no Domain ou Application.

**Why:** ADR-003. Impedir que um vazamento do banco de dados exponha senhas em texto plano. BCrypt com work factor 11 fornece ~50ms de custo computacional por hash, tornando ataques de dicionário inviáveis em escala.

**Acceptance Criteria:**
- [ ] `UsuarioRepository.CadastrarUsuario()` chama `BCrypt.Net.BCrypt.HashPassword(usuario.Senha)` antes do `INSERT`
- [ ] O valor persistido na coluna `Senha` começa com o prefixo `$2a$11$` (indicador de BCrypt com work factor 11)
- [ ] O valor persistido NÃO contém a senha original em texto plano
- [ ] O hash tem comprimento ≥ 60 caracteres
- [ ] A entidade `Usuario` no Domain NÃO referencia o namespace `BCrypt.Net`
- [ ] `dotnet test` passa em teste que cadastra um usuário e verifica que `Senha != "senhaFornecida"` e `Senha.StartsWith("$2a$11$")`

### FR-002: Verificação BCrypt no Login

**What:** O método `UsuarioService.Login()` deve verificar a senha fornecida contra o hash armazenado usando `BCrypt.Net.BCrypt.Verify(senhaFornecida, hashArmazenado)`. NUNCA deve comparar strings de senha diretamente (`==` ou `Equals`).

**Why:** ADR-003, ADR-004. O login é o ponto central de autenticação para todos os 3 perfis. A verificação deve ser criptograficamente correta e resistente a timing attacks (BCrypt.Verify já é constant-time).

**Acceptance Criteria:**
- [ ] `UsuarioService.Login()` usa exclusivamente `BCrypt.Verify()` para validar senhas
- [ ] Não existe comparação `senha == hash` ou `senha.Equals(hash)` em nenhum arquivo do projeto
- [ ] Login com senha correta → retorna `LoginResponseDTO` com JWT válido (HTTP 200)
- [ ] Login com senha incorreta → lança `CredenciaisInvalidasException` resultando em HTTP 401
- [ ] `dotnet test` passa em 3 cenários: senha correta (200), senha incorreta (401), email inexistente (401)

### FR-003: Resposta Uniforme para Falha de Autenticação

**What:** Qualquer falha de autenticação — email inexistente, senha incorreta, ou usuário inativo — deve retornar exatamente a mesma resposta HTTP: `401 Unauthorized` com body `{"message":"Email ou senha inválidos."}`. O sistema NUNCA deve revelar se o email existe ou se a conta está inativa.

**Why:** ADR-011. Prevenir enumeração de usuários por atacantes. Se o sistema retornasse "Email não encontrado" vs "Senha incorreta", um atacante poderia mapear quais emails estão cadastrados na plataforma.

**Acceptance Criteria:**
- [ ] Email inexistente → HTTP 401, body `{"message":"Email ou senha inválidos."}`
- [ ] Senha incorreta → HTTP 401, body `{"message":"Email ou senha inválidos."}`
- [ ] Usuário com `Ativo = false` → HTTP 401, body `{"message":"Email ou senha inválidos."}`
- [ ] As 3 condições acima produzem resposta HTTP idêntica (status code, headers, body)
- [ ] `GlobalExceptionHandlerMiddleware` mapeia `CredenciaisInvalidasException` para HTTP 401
- [ ] `dotnet test` verifica que as 3 respostas são idênticas byte a byte no body

### FR-004: Chave JWT Fora do Código-Fonte

**What:** A chave de assinatura JWT (`Jwt:Key`) NÃO deve estar presente em nenhum arquivo versionado no Git. Em desenvolvimento, deve ser carregada via `dotnet user-secrets` (arquivo `secrets.json` em `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\`). Em produção, via variável de ambiente ou cofre seguro.

**Why:** ADR-004. Se a chave JWT vazar no repositório Git, qualquer pessoa com acesso ao repositório pode forjar tokens JWT válidos e se passar por qualquer usuário (Admin, Vendedor, Comprador). Este é um risco crítico identificado em visao.md §12.

**Acceptance Criteria:**
- [ ] `Api/appsettings.json` contém `"Jwt.Key": ""` (string vazia ou placeholder)
- [ ] `git log -p -- Api/appsettings.json` não mostra nenhuma chave JWT real no histórico
- [ ] `Api/Program.cs` chama `builder.Configuration.AddUserSecrets<Program>()` quando `IsDevelopment()`
- [ ] `Api/Program.cs` valida que `Jwt:Key` não é nula/vazia e lança `InvalidOperationException` com mensagem orientando `dotnet user-secrets set` se ausente
- [ ] A API NÃO sobe (`throw` antes de `app.Run()`) se `Jwt:Key` não estiver configurada
- [ ] `.gitignore` contém entradas para `appsettings.*.json` e `secrets.json`
- [ ] `git status` não lista `appsettings.json` como modificado após configurar user-secrets

### FR-005: Rate Limiting no Endpoint de Login

**What:** O endpoint `POST /api/usuario/login` deve ser protegido por um middleware de rate limiting que permite no máximo 5 requisições por minuto por endereço IP, usando algoritmo de sliding window. Requisições que excederem o limite recebem HTTP 429.

**Why:** ADR-010. Sem rate limiting, um atacante pode realizar milhares de tentativas de login por segundo (força bruta) contra uma conta conhecida. Com limite de 5 req/min, um ataque de dicionário de 10.000 senhas levaria mais de 33 horas — tornando-o impraticável.

**Acceptance Criteria:**
- [ ] 1ª a 5ª requisição `POST /api/usuario/login` do mesmo IP em 1 minuto → processadas normalmente
- [ ] 6ª requisição do mesmo IP dentro do mesmo minuto → HTTP 429 `{"message":"Muitas tentativas. Aguarde 1 minuto."}`
- [ ] Após 60 segundos da 1ª requisição, a janela desliza e novas requisições são permitidas
- [ ] O rate limiting NÃO se aplica a outros endpoints (ex: `GET /api/evento`, `POST /api/usuario/cadastrar-comprador`)
- [ ] O rate limiting opera exclusivamente em memória (`ConcurrentDictionary<string, SlidingWindow>`)
- [ ] O middleware respeita o header `X-Forwarded-For` para IP real atrás de proxy/load balancer
- [ ] `dotnet test` verifica: 5 req → OK, 6ª req → 429, após 60s → OK novamente

### FR-006: Script de Seed do Admin com Hash BCrypt

**What:** O script SQL `db/Script0001_SeedAdmin.sql` (ou equivalente) deve conter a senha do Admin pré-hasheada com BCrypt, NUNCA em texto plano. O script deve ser idempotente (`IF NOT EXISTS`).

**Why:** ADR-003, ADR-008. O Admin é o primeiro usuário do sistema e é criado via seed SQL. Se a senha do Admin estiver em texto plano no script, qualquer pessoa com acesso ao repositório terá acesso administrativo ao sistema.

**Acceptance Criteria:**
- [ ] O script de seed contém um hash BCrypt pré-gerado (string `$2a$11$...` com ≥ 60 caracteres)
- [ ] O script NÃO contém a senha original em texto plano (ex: `'Admin@123'`)
- [ ] O script inclui comentário indicando a senha original apenas para desenvolvimento local
- [ ] O script usa `IF NOT EXISTS (SELECT 1 FROM Usuarios WHERE Email = 'admin@soldout.com')` para idempotência
- [ ] `dotnet test` verifica que após execução do DbUp, `SELECT Senha FROM Usuarios WHERE Email = 'admin@soldout.com'` retorna hash BCrypt

### FR-007: Validação Básica de Input no Login

**What:** O endpoint `POST /api/usuario/login` deve validar que os campos `email` e `senha` estão presentes e não são strings vazias antes de qualquer processamento (rate limit, banco, BCrypt). Email deve conter pelo menos um `@`. Campos ausentes ou vazios devem retornar HTTP 400.

**Why:** Prevenir que requisições malformadas cheguem ao banco de dados ou ao BCrypt. A validação de input é a primeira linha de defesa e reduz carga desnecessária no sistema. Também evita que `BCrypt.Verify(null, hash)` cause comportamento inesperado.

**Acceptance Criteria:**
- [ ] `POST /api/usuario/login` com body `{}` (sem email/senha) → HTTP 400 (model binding falha)
- [ ] `POST /api/usuario/login` com `email: ""` → HTTP 400
- [ ] `POST /api/usuario/login` com `senha: ""` → HTTP 400
- [ ] `POST /api/usuario/login` com `email: "sem-arroba"` → HTTP 400
- [ ] A validação usa `[Required]` e `[EmailAddress]` data annotations no `LoginRequestDTO`
- [ ] Nota: no pipeline ASP.NET, model binding ocorre DEPOIS do RateLimitMiddleware. Requisições inválidas consomem cota de rate limit. Isso é aceitável — um atacante queimando sua cota com requisições inválidas não compromete a segurança

---

## Non-Functional Requirements

### NFR-001: Work Factor BCrypt

**What:** O work factor do BCrypt deve ser exatamente 11 (padrão da biblioteca `BCrypt.Net-Next`). Work factor menor compromete segurança; work factor maior degrada performance de login sem benefício proporcional.

**Acceptance Criteria:**
- [ ] Todo hash gerado começa com `$2a$11$` (work factor 11)
- [ ] Tempo médio de `BCrypt.HashPassword()` ≤ 100ms em hardware de desenvolvimento típico
- [ ] Tempo médio de `BCrypt.Verify()` ≤ 100ms em hardware de desenvolvimento típico

### NFR-002: Rate Limiting — Sliding Window Algorithm

**What:** O rate limiting deve usar sliding window, NÃO fixed window. Fixed window permite rajadas na borda da janela (ex: 5 req em t=0:59 + 5 req em t=1:01 = 10 req em 2 segundos). Sliding window impede esse bypass.

**Acceptance Criteria:**
- [ ] `SlidingWindow.TryIncrement()` remove timestamps expirados antes de verificar o limite
- [ ] O algoritmo usa `ConcurrentQueue<DateTime>` com operações thread-safe
- [ ] Não há lock contention significativo (a estrutura é lock-free por design com `ConcurrentQueue`)

### NFR-003: Rate Limiting — Storage em Memória

**What:** O estado do rate limiting reside exclusivamente em memória (`ConcurrentDictionary`). Não requer Redis, SQL Server, ou qualquer infraestrutura externa. O estado é perdido no restart da API — aceitável para o escopo atual.

**Acceptance Criteria:**
- [ ] Nenhuma dependência de pacote NuGet externo para rate limiting
- [ ] Nenhuma tabela de banco de dados criada para armazenar estado de rate limit
- [ ] Após restart da API, o contador de requisições de cada IP começa em zero

### NFR-004: Proteção de Secrets no Versionamento

**What:** Nenhum arquivo contendo secrets (chaves, senhas, connection strings com credenciais) deve ser rastreado pelo Git. O `.gitignore` deve cobrir todos os padrões de arquivo que possam conter secrets.

**Acceptance Criteria:**
- [ ] `.gitignore` contém `appsettings.*.json` (bloqueia `appsettings.Development.json`, `appsettings.Production.json`, etc.)
- [ ] `.gitignore` contém `secrets.json`
- [ ] `git ls-files` NÃO lista nenhum arquivo que contenha a chave JWT real
- [ ] `git log --all --full-history -- Api/appsettings.json` não mostra diff com chave JWT real

### NFR-005: Performance — Latência de Login

**What:** O endpoint `POST /api/usuario/login` deve responder em menos de 500ms (p95) incluindo BCrypt.Verify, query ao banco, geração JWT e rate limiting.

**Acceptance Criteria:**
- [ ] p95 de latência do endpoint de login ≤ 500ms medido com 100 requisições consecutivas
- [ ] BCrypt.Verify contribui ≤ 100ms para a latência total
- [ ] RateLimitMiddleware adiciona ≤ 1ms de overhead para requisições dentro do limite

---

## Constraints

1. **ADR-001 (Clean Architecture):** O Domain NÃO pode referenciar `BCrypt.Net`. O hash é responsabilidade da Infrastructure. A Application pode usar `BCrypt.Verify` pois é lógica de aplicação (verificação de credenciais), não regra de negócio.
2. **ADR-002 (Dapper):** A query de login (`BuscarPorEmail`) é executada via Dapper com parâmetros (`@Email`), nunca concatenação de string.
3. **ADR-004 (JWT):** Claims mínimas do token: `sub` (CPF/CNPJ), `email`, `role` (Admin/Vendedor/Comprador), `exp`, `iat`. `ClockSkew = TimeSpan.Zero`.
4. **ADR-008 (DbUp):** Alterações no script de seed devem ser feitas no arquivo de migração existente, NÃO em um novo script. DbUp executa scripts em ordem numérica; modificar um script já executado requer recriação do banco local.
5. **ADR-010 (Rate Limit):** Middleware customizado (sem AspNetCoreRateLimit ou dependências externas). Aplicado SOMENTE em `POST /api/usuario/login`.
6. **ADR-011 (Exception Handler):** Toda resposta de erro deve seguir o formato `{"message": "..."}`. Exceções de domínio mapeadas para HTTP status codes pelo `GlobalExceptionHandlerMiddleware`.
7. **Stack:** .NET 9, C# 13, Dapper, SQL Server, BCrypt.Net-Next (NuGet), sem dependências adicionais além das já existentes.

---

## Edge Cases & Error States

| # | Caso | Gatilho | Comportamento Exato |
|---|------|---------|---------------------|
| EC-001 | Senha vazia no cadastro | `POST /api/usuario/cadastrar-comprador` com `senha: ""` | `BCrypt.HashPassword("")` produz hash válido. Permitir — validação de complexidade está fora do escopo. |
| EC-002 | Senha muito longa (> 72 chars) | Cadastro com senha de 73+ caracteres | BCrypt trunca silenciosamente em 72 bytes. Documentar no código, mas não rejeitar — compatível com comportamento padrão do BCrypt. |
| EC-003 | Hash BCrypt corrompido no banco | `Senha` no banco não começa com `$2a$` | `BCrypt.Verify()` lança `SaltParseException`. Capturar e converter para `CredenciaisInvalidasException` → HTTP 401. NUNCA expor o erro interno. |
| EC-004 | IP via proxy (X-Forwarded-For) | Requisição passa por Nginx/Cloudflare | Middleware usa `X-Forwarded-For` se presente. Pega o primeiro IP da lista (cliente original). Fallback para `RemoteIpAddress` se header ausente. |
| EC-005 | IP não identificável | `RemoteIpAddress` é null (raro, possível em testes) | Usar string `"unknown"` como chave. Todas as requisições sem IP identificável compartilham o mesmo limite. |
| EC-006 | Concurrency no rate limit | Múltiplas requisições simultâneas do mesmo IP | `ConcurrentQueue` é thread-safe. `TryPeek` + `TryDequeue` pode ter race condition benigna (timestamp expirado removido por outra thread). Limite pode ser excedido em 1 requisição — aceitável. |
| EC-007 | `dotnet user-secrets` não inicializado | Primeiro `dotnet run` sem configurar secrets | `Program.cs` detecta `Jwt:Key` nula e lança `InvalidOperationException` com mensagem: `"Jwt:Key não configurada. Em desenvolvimento, execute:\n  dotnet user-secrets set \"Jwt:Key\" \"<sua-chave-secreta>\""`. API não sobe. |
| EC-008 | Chave JWT comprometida | Suspeita de vazamento da chave | Fora do escopo desta spec. Procedimento manual: gerar nova chave, reiniciar API (invalida todos os tokens existentes). Mecanismo de blacklist será tratado em spec futura. |
| EC-009 | Admin seed executado múltiplas vezes | DbUp re-executa Script0001 em banco limpo | `IF NOT EXISTS (SELECT 1 FROM Usuarios WHERE Email = 'admin@soldout.com')` garante idempotência. Nenhum erro ou duplicação. |
| EC-010 | Login com campos inválidos ou ausentes | `POST /api/usuario/login` com `email: ""`, `senha: ""`, ou `email: "sem-arroba"` | ASP.NET model binding com `[Required]`/`[EmailAddress]` no DTO retorna HTTP 400. O model binding ocorre DEPOIS do RateLimitMiddleware no pipeline — requisições inválidas consomem cota de rate limit (aceitável). Ver FR-007. |
| EC-011 | Login com senha contendo caracteres especiais | Senha com `'`, `"`, `\`, `%`, `_` que podem confundir SQL | Dapper com parâmetros (`@Senha`) previne SQL Injection. BCrypt lida com qualquer caractere UTF-8. Nenhum tratamento especial necessário. |
| EC-012 | Rate limit após restart da API | API reinicia durante ataque de força bruta | Contadores em memória são zerados. Atacante tem nova janela de 5 tentativas. Risco aceito para MVP (documentado em ADR-010). |
| EC-013 | Body vazio ou null no login | `POST /api/usuario/login` com body `{}` ou sem Content-Type | ASP.NET model binding falha → retorna HTTP 400 antes de chegar ao controller. Comportamento padrão do framework. Ver FR-007. |

---

## Dependencies

| Dependência | Tipo | Detalhe |
|-------------|------|---------|
| `BCrypt.Net-Next` (NuGet) | Pacote | Versão estável mais recente compatível com .NET 9. Adicionado aos projetos `Infraestructure` e `Application`. |
| `Microsoft.Extensions.Configuration.UserSecrets` | Pacote | Já incluído no SDK .NET 9. Usado em `Program.cs` apenas em Development. |
| `System.Collections.Concurrent` | Namespace | Já disponível no .NET 9. Usado para `ConcurrentDictionary` e `ConcurrentQueue` no rate limiting. |
| ADR-003 (BCrypt) | Decisão | Já aprovada. Define work factor 11 e localização do hash (Infrastructure). |
| ADR-004 (JWT) | Decisão | Já aprovada. Define claims mínimas e `ClockSkew = Zero`. |
| ADR-010 (Rate Limit) | Decisão | Já aprovada. Define middleware customizado, 5 req/min, sliding window. |
| ADR-011 (Exception Handler) | Decisão | Já aprovada. Define formato `{"message":"..."}` para erros. |
| Spec 180 (E-mail + Redef. Senha) | Spec | Spec 180 adiciona `POST /api/usuario/esqueci-senha` e `POST /api/usuario/redefinir-senha`. Ambos também usam BCrypt e JWT — spec 120 estabelece a base de segurança que spec 180 estende. Não há dependência rígida. |
| DbUp (migrations) | Ferramenta | Script Script0001 já existe e contém seed do Admin. Será modificado in-place. |

---

## Out of Scope

Estes itens NÃO fazem parte desta spec e NÃO devem ser implementados:

1. **Política de complexidade de senha** — comprimento mínimo, caracteres especiais, maiúsculas/minúsculas. Senhas de qualquer formato são aceitas.
2. **Rotação automática de chave JWT** — sem mecanismo de blacklist ou revogação de tokens.
3. **2FA / MFA** — autenticação de dois fatores.
4. **Rate limiting em outros endpoints** — apenas `POST /api/usuario/login` é protegido.
5. **Migração retroativa de senhas plain-text** — se o banco já tiver senhas em texto plano de versão anterior, elas NÃO são migradas automaticamente. Apenas novos cadastros e redefinições de senha (spec 180) usarão BCrypt.
6. **Recuperação de senha** — coberto pela spec 180.
7. **Lockout de conta após N tentativas** — apenas rate limiting por IP, sem bloqueio de conta específica.
8. **Proteção contra CSRF** — a API é stateless com JWT Bearer; CSRF não se aplica.
9. **Headers de segurança HTTP** — HSTS, CSP, X-Frame-Options (responsabilidade do frontend/reverse proxy).
10. **Criptografia de dados em repouso** — apenas senhas são hasheadas. Demais dados (CPF, email, nome) permanecem em texto plano no banco.
