---
name: Cancelamento de Reserva — Visão Unificada
status: verified
references:
  - docs/visao.md §6.5
  - docs/arquitetura.md §4.3
  - docs/ADR.md §ADR-012
  - docs/storytelling.md §ST-12
dependencies:
  spec-40: implemented
  spec-50: audited
---

# Spec 110: Cancelamento de Reserva — Visão Unificada — Requirements

## Value Delivery

> **Fonte:** `docs/visao.md §6.5` — Cancelamento com Reembolso Atômico

| Especificação | Valor para o Usuário |
|---|---|
| Comprador cancela reserva se `DataEvento > agora`; ingressos voltam a `Status=0` (Livre) | **Flexibilidade com segurança**: o comprador não perde dinheiro se não puder comparecer, desde que cancele antes do evento começar. As vagas voltam para outros compradores. |

> **Fonte:** `docs/arquitetura.md §4.3` — Visão Unificada de Cancelamento (spec 110)

`GET /api/reserva/minhas` é unificado para todos os perfis (sem restrição de role). O response inclui flags individuais por item: `reembolsado`, `podeCancelar`.

---

## Functional Requirements

### FR-001: Listagem de Minhas Reservas Unificada por Perfil

**What:** `GET /api/reserva/minhas` deve retornar apenas as reservas cujo `UsuarioCpf` corresponde ao CPF do JWT, para qualquer perfil autenticado (Comprador, Vendedor, Admin). O atributo `[Authorize]` não deve ter restrição de role.

**Why:** Vendedores e Admin também podem comprar ingressos (são usuários da plataforma). Suas reservas próprias devem aparecer em "Minhas Reservas" exatamente como as de um Comprador. (visao.md §6.5 — "Flexibilidade com segurança")

**Acceptance Criteria:**
- [ ] `GET /api/reserva/minhas` tem atributo `[Authorize]` sem parâmetro `Roles`
- [ ] Comprador autenticado recebe APENAS reservas onde `Reserva.UsuarioCpf == cpf do JWT`
- [ ] Vendedor autenticado recebe APENAS reservas onde `Reserva.UsuarioCpf == cpf do JWT`
- [ ] Admin autenticado recebe APENAS reservas onde `Reserva.UsuarioCpf == cpf do JWT`
- [ ] Usuário sem reservas recebe array JSON vazio `[]` com `200 OK`
- [ ] Usuário não autenticado recebe `401 Unauthorized`

### FR-002: Visibilidade Granular de Reembolso por Item

**What:** O response de `GET /api/reserva/minhas` deve incluir, para cada reserva, uma lista de `itens`, onde cada item contém `cpfParticipante` (string), `precoUnitario` (decimal) e `reembolsado` (bool). A reserva deve conter o campo `reembolsada` (bool).

**Why:** O comprador precisa saber exatamente quais participantes da sua reserva foram reembolsados, especialmente após cancelamento de evento pela Spec 50, onde todos os itens são marcados como reembolsados. (arquitetura.md §4.3)

**Acceptance Criteria:**
- [ ] Cada objeto no array de resposta contém o campo `reembolsada` (bool)
- [ ] Cada objeto contém o campo `itens` (array de objetos)
- [ ] Cada item do array `itens` contém obrigatoriamente: `cpfParticipante` (string), `precoUnitario` (decimal), `reembolsado` (bool)
- [ ] Reserva cancelada (spec 40): `reembolsada = true`, todos os itens com `reembolsado = true`
- [ ] Reserva ativa (não cancelada): `reembolsada = false`, todos os itens com `reembolsado = false`
- [ ] Reserva de evento cancelado (spec 50): `reembolsada = true`, todos os itens com `reembolsado = true`

### FR-003: Flag PodeCancelar Calculada

**What:** O response de `GET /api/reserva/minhas` deve incluir o campo booleano `podeCancelar`, calculado em memória como `!reembolsada && DataEvento > DateTime.UtcNow`.

**Why:** O frontend precisa saber se deve exibir o botão de cancelamento para cada reserva, sem precisar implementar essa lógica do lado do cliente. (arquitetura.md §4.3)

**Acceptance Criteria:**
- [ ] Reserva não reembolsada + evento futuro: `podeCancelar = true`
- [ ] Reserva reembolsada (independente da data): `podeCancelar = false`
- [ ] Reserva não reembolsada + evento já passado (`DataEvento <= DateTime.UtcNow`): `podeCancelar = false`
- [ ] O cálculo é feito em memória no Service (não no SQL), via loop `foreach` sobre a coleção retornada pelo Repository

### FR-004: Cancelamento de Reserva Unificado por Perfil

**What:** `DELETE /api/reserva/{id}` deve aceitar qualquer perfil autenticado (Comprador, Vendedor, Admin). A autorização é baseada em `Reserva.UsuarioCpf == cpf do JWT`, não na role do usuário.

**Why:** A Spec 40 já implementou o endpoint com `[Authorize]` sem restrição de role. Esta spec valida que o comportamento unificado funciona corretamente para todos os perfis.

**Acceptance Criteria:**
- [ ] `DELETE /api/reserva/{id}` tem atributo `[Authorize]` sem parâmetro `Roles`
- [ ] Comprador cancela sua própria reserva → `200 OK`
- [ ] Vendedor cancela sua própria reserva → `200 OK`
- [ ] Admin cancela sua própria reserva → `200 OK`
- [ ] Qualquer perfil tenta cancelar reserva de outro `UsuarioCpf` → `403 Forbidden`
- [ ] CPF do usuário é extraído de `User.Claims["cpf"]` do JWT

### FR-005: Query de Minhas Reservas sem STRING_AGG

**What:** A query SQL de `ListarReservasDetalhadasPorCpf` no Repository deve usar `splitOn` do Dapper para retornar itens individuais, substituindo o `STRING_AGG` atual que agrega posições de ingressos em string.

**Why:** `STRING_AGG` perde a granularidade dos itens individuais — não é possível expor o flag `reembolsado` por item quando os dados estão agregados em string. (design.md §8 — Decisões de Design)

**Acceptance Criteria:**
- [ ] A query SQL NÃO contém `STRING_AGG`
- [ ] A query SQL retorna colunas individuais: `ir.Id AS ItemId`, `ir.CpfParticipante`, `ir.PrecoUnitario`, `ir.Reembolsado`
- [ ] O mapeamento Dapper usa `splitOn: "ItemId"` com função lambda de agrupamento
- [ ] Os itens são agrupados em memória usando `Dictionary<Guid, ReservaDetalhadaDTO>`
- [ ] A query filtra por `WHERE r.UsuarioCpf = @cpf`

---

## Non-Functional Requirements

### NFR-001: Performance — Consulta Única sem N+1

**What:** A listagem de Minhas Reservas deve executar uma única query SQL que retorna todos os dados (reserva + itens), sem queries adicionais por reserva.

**Acceptance Criteria:**
- [ ] Uma única chamada a `QueryAsync` do Dapper é feita
- [ ] Nenhum loop com queries adicionais por `reservaId`
- [ ] O agrupamento de itens por reserva é feito em memória (dicionário), não no banco

### NFR-002: Segurança — Autorização por UsuarioCpf

**What:** A autorização de acesso às reservas deve ser baseada no CPF do JWT, nunca na role do usuário.

**Acceptance Criteria:**
- [ ] Query SQL filtra por `WHERE r.UsuarioCpf = @cpf`
- [ ] O CPF injetado na query é extraído de `User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value`
- [ ] Nenhum filtro por role é aplicado na query ou no Service

---

## Constraints

- **Clean Architecture:** Domain não referencia camadas externas. Application depende só de Domain. (ADR-001)
- **Dapper:** Todas as queries SQL são parametrizadas com `@param`. (ADR-002)
- **Cancelamento lógico:** Nunca exclusão física. (ADR-012)
- **Nenhuma migration necessária** — colunas `Reservas.Reembolsada` (spec 40), `ItensReserva.Reembolsado` (ST-04), e `Reservas.UsuarioCpf` já existem
- **A interface `IReservaService` e `IReservaRepository`** já contêm os métodos necessários (`ListarMinhasReservas`, `CancelarReserva`, `ListarReservasDetalhadasPorCpf`) — implementados pela spec 40
- **O endpoint `DELETE /api/reserva/{id}`** já existe com `[Authorize]` sem restrição de role (spec 40) — esta spec apenas valida

---

## Edge Cases & Error States

| # | Caso | Comportamento esperado |
|---|------|----------------------|
| B1 | Admin acessa `GET /api/reserva/minhas` sem ter feito nenhuma reserva | Retorna `200 OK` com array vazio `[]` |
| B2 | Vendedor acessa `GET /api/reserva/minhas` sem ter feito nenhuma reserva | Retorna `200 OK` com array vazio `[]` |
| B3 | Reserva com evento cancelado pela spec 50 (`Evento.Cancelado = true`) | `reembolsada = true`, `podeCancelar = false`, itens com `reembolsado = true` |
| B4 | Reserva com múltiplos itens (até 4 CPFs) | Cada item aparece individualmente no array `itens` com seu próprio `reembolsado` |
| B5 | Usuário sem claim `cpf` no JWT | `401 Unauthorized` |
| B6 | `ListarReservasDetalhadasPorCpf` retorna zero linhas do banco | `ReservaService` retorna coleção vazia (não lança exceção) |
| B7 | Reserva com `Reembolsada = true` mas itens com `Reembolsado = false` (estado inconsistente) | `podeCancelar = false` (prevalece o flag da reserva) |

---

## Dependencies

- **Spec 40 (implemented):** `Reserva.Reembolsada`, `Reserva.MarcarReembolsada()`, `ReservaService.CancelarReserva()`, endpoint `DELETE /api/reserva/{id}` com `[Authorize]`
- **Spec 50 (audited):** `Evento.Cancelado` e transação `CancelarComTransacao` que marca `Reservas.Reembolsada = 1` e `ItensReserva.Reembolsado = 1`
- **ST-04 (implemented):** Entidade `ItemReserva` com campo `Reembolsado`
- **Login Unificado (specs 70/80/90):** JWT com claim `cpf` para todos os perfis

---

## Out of Scope

- Admin cancelar reserva de terceiros via endpoint público (Admin usa endpoints admin separados como `GET /api/reserva/Admin/Todas`)
- Paginação em Minhas Reservas
- Filtros avançados (por status, data, evento)
- Ordenação customizável
- Endpoint separado por perfil
