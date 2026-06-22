---
name: "Cupons de Desconto — AdminId via JWT"
status: "audited"
references:
  - "ADR-004 (JWT com roles para autenticação)"
  - "visao.md §2 (Emitir ingressos)"
  - "roadmap.md — Sprint 1 — Segurança e Correções Críticas"
---

# Spec 160: Cupons de Desconto — AdminId via JWT

## Value Delivery

Esta spec entrega o bloco de valor **"Emitir ingressos — AdminId via JWT, não rota"** definido no roadmap Sprint 1:

> **AdminId extraído do JWT** → O identificador do Admin logado é obtido das claims do token JWT (`perfilId`), eliminando a possibilidade de um Admin forjar o ID de outro Admin via parâmetros de rota ou corpo da requisição.

**Problema resolvido:** Elimina 7 ocorrências onde o `AdminId` é passado via rota (`[FromRoute]`), corpo (`[FromBody]`), ou DTO — em vez de ser extraído do token JWT do usuário autenticado. Isso fecha uma brecha de segurança onde um Admin mal-intencionado poderia se passar por outro Admin.

---

## Functional Requirements

### FR-001: Extrair AdminId do JWT no CupomController

**What:** Todos os métodos do `CupomController` que precisam do identificador do Admin logado devem extraí-lo da claim `perfilId` do token JWT (via `User.Claims`), NUNCA de parâmetros de rota (`[FromRoute]`) ou corpo (`[FromBody]`).

**Why:** ADR-004. O JWT já contém a identidade do usuário autenticado nas claims. Passar `AdminId` como parâmetro externo permite que um Admin forje requisições em nome de outro. Extrair do JWT garante que o ID é autêntico e imutável.

**Acceptance Criteria:**
- [ ] `CadastrarCupom` — remove `[FromRoute] Guid Id`, extrai de `User.Claims`
- [ ] `DeletarCupom` — remove `[FromBody] Guid adminId`, extrai de `User.Claims`
- [ ] `AlterarValorMinimo` — remove `dto.AdminId` do DTO, extrai de `User.Claims`
- [ ] `AlterarDataExpiracao` — remove `dto.AdminId` do DTO, extrai de `User.Claims`
- [ ] `AlternarStatus` — remove `[FromBody] Guid adminId`, extrai de `User.Claims`
- [ ] `AlterarDesconto` — remove `dto.AdminId` do DTO, extrai de `User.Claims`
- [ ] `ListarTodosCupons` — extrai de `User.Claims` (substitui `Guid.Empty`)

### FR-002: Remover AdminId dos DTOs

**What:** Os DTOs `AlterarValorMinimoDTO`, `AlterarDataExpiracaoDTO`, e `AlterarDescontoDTO` devem ter o campo `AdminId` removido. O AdminId não é mais um dado de entrada — é obtido do JWT.

**Why:** DTOs representam dados fornecidos pelo cliente. AdminId não é fornecido pelo cliente — é derivado da autenticação. Manter AdminId nos DTOs é uma vulnerabilidade.

**Acceptance Criteria:**
- [ ] `AlterarValorMinimoDTO` — campo `AdminId` removido
- [ ] `AlterarDataExpiracaoDTO` — campo `AdminId` removido
- [ ] `AlterarDescontoDTO` — campo `AdminId` removido
- [ ] Build sem erros após remoção
- [ ] Nenhum outro código referencia `AdminId` nos DTOs

### FR-003: Manter Interface ICupomService Compatível

**What:** A interface `ICupomService` e sua implementação `CupomService` mantêm o parâmetro `Guid AdminLogado` em suas assinaturas — apenas a origem do valor muda (de DTO/rota para JWT). Isso minimiza o escopo da mudança.

**Why:** O service já espera `Guid AdminLogado` como parâmetro. Alterar a interface seria uma mudança desnecessária que aumentaria o risco de regressão. O parâmetro é mantido para futura auditoria (logging).

**Acceptance Criteria:**
- [ ] `ICupomService` — sem alterações na interface
- [ ] `CupomService` — sem alterações na implementação
- [ ] Controller passa o valor extraído do JWT como `Guid AdminLogado`

### FR-004: Validação de Role Mantida

**What:** Todos os endpoints do `CupomController` mantêm o atributo `[Authorize(Roles = "Admin")]`. Apenas usuários com role "Admin" no JWT podem acessar os endpoints de cupom.

**Why:** A autorização por role é a primeira linha de defesa. Mesmo com AdminId extraído do JWT, a role deve ser verificada para garantir que apenas Admins acessem estes endpoints.

**Acceptance Criteria:**
- [ ] Todos os endpoints mantêm `[Authorize(Roles = "Admin")]`
- [ ] `dotnet test` verifica que token sem role "Admin" recebe HTTP 403

---

## Non-Functional Requirements

### NFR-001: Consistência com Padrão Existente

**What:** A extração de claims do JWT deve seguir o mesmo padrão usado em outros controllers do projeto (`EventoController`, `ReservaController`, `PagamentoController`).

**Acceptance Criteria:**
- [ ] Uso de `User.Claims.FirstOrDefault(c => c.Type == "perfilId")?.Value` para extrair o Guid
- [ ] Tratamento de claim ausente (retorna 401 Unauthorized)
- [ ] Nomes de variáveis seguem convenção do projeto

### NFR-002: Build e Testes Mantidos

**What:** As mudanças não devem quebrar o build existente nem os testes existentes do Cupom.

**Acceptance Criteria:**
- [ ] `dotnet build`: 0 erros nos projetos `Api`, `Application`
- [ ] `dotnet test` (CupomTests): todos os testes existentes passam
- [ ] `dotnet test` (full suite): sem regressões

---

## Edge Cases & Error States

| # | Caso | Gatilho | Comportamento Exato |
|---|------|---------|---------------------|
| EC-001 | Claim `perfilId` ausente no token | Token JWT sem claim `perfilId` (token malformado) | Controller retorna HTTP 401 Unauthorized `{"message": "Token inválido: perfil não identificado."}` |
| EC-002 | `perfilId` não é um Guid válido | Claim `perfilId` contém valor não parseável como Guid | Controller retorna HTTP 401 Unauthorized `{"message": "Token inválido: perfil não identificado."}` |
| EC-003 | AdminId não utilizado pelo service | Service recebe `AdminLogado` mas não o utiliza | Comportamento atual mantido — o parâmetro existe na assinatura para auditoria futura |
| EC-004 | Endpoint `CadastrarCupom` sem `{Id}` na rota | Rota muda de `CadastrarCupom/{Id}` para `CadastrarCupom` | Clientes existentes que chamavam a rota antiga recebem HTTP 404. Breaking change documentado. |
| EC-005 | `ListarCuponsValidos` sem autenticação | Endpoint público (sem `[Authorize]`) | Sem alteração — endpoint permanece público e não extrai AdminId |

---

## Constraints

1. **ADR-004 (JWT):** Claims `perfilId`, `cpf`, `email`, `role` já existem no token. Nenhuma nova claim é adicionada.
2. **ADR-001 (Clean Architecture):** Apenas camadas Api e Application são modificadas. Domain e Infrastructure não são alterados.
3. **Stack:** .NET 9, C# 13, sem novas dependências NuGet.

---

## Dependencies

| Dependência | Tipo | Detalhe |
|-------------|------|---------|
| Spec 120 (JWT) | Spec | JWT já implementado e auditado. Claims `perfilId`, `cpf`, `email`, `role` disponíveis. |
| ADR-004 (JWT) | Decisão | Já aprovada. Define estrutura de claims do token. |

---

## Out of Scope

1. **Auditoria/logging do AdminId** — o parâmetro `AdminLogado` no service não é usado para logging nesta spec.
2. **Validação de que o AdminId corresponde a um usuário Admin real** — o `[Authorize(Roles = "Admin")]` já garante que o token pertence a um Admin.
3. **Multi-tenancy de cupons por Admin** — cupons permanecem globais (todos os Admins veem todos os cupons).
4. **Novos endpoints de cupom** — apenas os 7 endpoints existentes são modificados.
5. **Alteração na lógica de negócio dos cupons** — apenas a origem do AdminId muda.
