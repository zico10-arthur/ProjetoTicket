---
name: Vendedor Cancela Evento com Reembolso Obrigatório
status: verified
references:
  - docs/visao.md §6.5
  - docs/arquitetura.md §4.2
  - docs/ADR.md §ADR-012
  - docs/sprints.md §ST-06
dependencies:
  spec-40: implemented
---

# Spec 50: Vendedor Cancela Evento com Reembolso Obrigatório — Requirements

## Value Delivery

> **Fonte:** `docs/visao.md §6.5` — Cancelamento com Reembolso Atômico

| Especificação | Valor para o Usuário |
|---|---|
| Vendedor cancela evento pago → alerta de reembolso obrigatório → transação atômica (`Evento.Cancelado=true` + `Ingresso.Status=3` + `Reserva.Reembolsada=true`) | **Transparência e responsabilidade**: o vendedor é alertado sobre o impacto financeiro antes de cancelar. Compradores têm a garantia de que serão reembolsados — o sistema força isso na transação. |
| Evento gratuito cancelado sem reembolso | **Coerência**: como não houve cobrança, não há reembolso — o sistema não cobra nem devolve o que não existe, evitando confusão. |

> **Fonte:** `docs/arquitetura.md §4.2` — Vendedor Cancela Evento (spec 50)

`DELETE /api/evento/{id}` — a semântica mudou de exclusão física para cancelamento lógico. Antes de cancelar, o vendedor consulta o impacto via `GET /api/evento/{id}/status-cancelamento`.

---

## Functional Requirements

### FR-001: Endpoint de Consulta de Status de Cancelamento

**What:** O sistema deve expor `GET /api/evento/{id}/status-cancelamento` que retorna dados agregados sobre o impacto financeiro do cancelamento antes que ele seja executado.

**Why:** O vendedor precisa tomar uma decisão informada antes de cancelar um evento, conhecendo o número de ingressos vendidos e o valor total de reembolso necessário (visao.md §6.5 — "Transparência e responsabilidade").

**Acceptance Criteria:**
- [ ] `GET /api/evento/{id}/status-cancelamento` retorna `200 OK` com o JSON contendo obrigatoriamente os campos: `eventoId` (Guid), `nome` (string), `gratuito` (bool), `totalIngressosVendidos` (int), `totalReservasAtivas` (int), `reembolsoNecessario` (bool), `valorTotalReembolso` (decimal), `mensagem` (string)
- [ ] Para evento pago com 23 ingressos vendidos a R$ 49,90 cada: `reembolsoNecessario = true`, `valorTotalReembolso = 1147.70`, `mensagem` contém "23 ingressos vendidos. O cancelamento exigirá reembolso de R$ 1.147,70. Deseja continuar?"
- [ ] Para evento gratuito com reservas: `gratuito = true`, `reembolsoNecessario = false`, `valorTotalReembolso = 0`, `mensagem` contém "Nenhum ingresso vendido. O cancelamento não exige reembolso."
- [ ] Para evento sem nenhum ingresso vendido: `totalIngressosVendidos = 0`, `reembolsoNecessario = false`, `valorTotalReembolso = 0`
- [ ] Para evento inexistente: retorna `404 Not Found` com `{ "message": "Evento não encontrado." }`
- [ ] Para vendedor não dono do evento: retorna `403 Forbidden` com `{ "message": "Você não tem permissão para acessar este evento." }`
- [ ] Admin pode consultar o status de qualquer evento, independentemente do VendedorCpf
- [ ] CPF do vendedor é extraído da claim `cpf` do JWT — nunca do body, query string ou rota

### FR-002: Cancelamento de Evento com Reembolso Atômico

**What:** `DELETE /api/evento/{id}` deve executar cancelamento lógico com reembolso obrigatório de todos os ingressos vendidos, em uma única transação atômica SQL. O verbo HTTP `DELETE` é mantido, mas a semântica muda de exclusão física para cancelamento lógico.

**Why:** ADR-012 — "Cancelamento lógico com flags explícitas e transações atômicas." Garantir que compradores nunca percam dinheiro quando um vendedor cancela um evento.

**Acceptance Criteria:**
- [ ] Evento pago com ingressos vendidos: após `DELETE`, `Eventos.Cancelado = 1`, todos `Ingressos.Status = 3` (Reembolsado) para ingressos com `Status = 2` (Vendido), todas `Reservas.Reembolsada = 1`, todos `ItensReserva.Reembolsado = 1`, todos `Pagamentos.Status = 3` (Reembolsado) para pagamentos com `Status = 1` (Confirmado)
- [ ] Evento gratuito com reservas: após `DELETE`, `Eventos.Cancelado = 1`, todas `Reservas.Reembolsada = 1`, todos `ItensReserva.Reembolsado = 1`. Nenhum UPDATE em `Pagamentos` ou `Ingressos.Status`
- [ ] Evento sem ingressos vendidos: após `DELETE`, apenas `Eventos.Cancelado = 1`
- [ ] Evento já cancelado (`Cancelado = true`): retorna `409 Conflict` com `{ "message": "Evento já foi cancelado." }`
- [ ] Vendedor não dono do evento: retorna `403 Forbidden` com `{ "message": "Você não tem permissão para cancelar este evento." }`
- [ ] Admin pode cancelar qualquer evento, independentemente do VendedorCpf
- [ ] Evento inexistente: retorna `404 Not Found` com `{ "message": "Evento não encontrado." }`
- [ ] CPF do vendedor é extraído da claim `cpf` do JWT — nunca do body, query string ou rota
- [ ] Retorna `200 OK` com `{ "message": "Evento cancelado com sucesso." }`
- [ ] Todas as operações de UPDATE nas 5 tabelas (`Eventos`, `Ingressos`, `Reservas`, `ItensReserva`, `Pagamentos`) executam dentro de `BEGIN TRANSACTION` / `COMMIT` / `ROLLBACK`

### FR-003: Filtro de Eventos Cancelados na Listagem Pública

**What:** `GET /api/evento` (listagem pública) não deve retornar eventos com `Cancelado = 1`. A listagem "Meus Eventos" do vendedor (`GET /api/evento/meus`) continua retornando todos os eventos, incluindo cancelados.

**Why:** Compradores não devem ver eventos que não serão mais realizados. Vendedores precisam ver o histórico completo dos seus eventos, incluindo cancelados.

**Acceptance Criteria:**
- [ ] `GET /api/evento` retorna apenas eventos com `Cancelado = 0` na query SQL
- [ ] `GET /api/evento/meus` retorna todos os eventos do vendedor, independentemente do valor de `Cancelado`
- [ ] Após cancelar um evento via `DELETE /api/evento/{id}`, o evento não aparece mais em `GET /api/evento`

---

## Non-Functional Requirements

### NFR-001: Atomicidade da Transação

**What:** A operação de cancelamento de evento deve ser atômica (ou todas as tabelas são atualizadas, ou nenhuma é).

**Acceptance Criteria:**
- [ ] Se qualquer UPDATE na transação falhar, `ROLLBACK` é executado e nenhuma tabela é alterada
- [ ] A transação usa `BEGIN TRANSACTION` / `COMMIT` / `ROLLBACK` explícitos, não `TransactionScope`
- [ ] Em caso de `ROLLBACK`, a exceção original é propagada após o rollback

### NFR-002: Performance da Consulta de Status

**What:** O endpoint `GET /api/evento/{id}/status-cancelamento` deve executar em uma única query SQL agregada, sem locks de tabela.

**Acceptance Criteria:**
- [ ] A query SQL usa apenas `SELECT` com `LEFT JOIN` e `GROUP BY` — sem `UPDATE`, `INSERT`, ou `DELETE`
- [ ] A query não utiliza `WITH (NOLOCK)` ou hints de lock
- [ ] Tempo de resposta < 200ms para eventos com até 1000 ingressos

### NFR-003: Idempotência da Migration

**What:** A migration não deve falhar se executada múltiplas vezes.

**Acceptance Criteria:**
- [ ] A migration verifica `IF NOT EXISTS` antes de criar qualquer coluna ou tabela
- [ ] Executar a migration 2 vezes seguidas não gera erro

### NFR-004: Segurança — Identidade via JWT

**What:** A identidade do vendedor deve ser sempre extraída do token JWT, nunca de parâmetros de rota ou body.

**Acceptance Criteria:**
- [ ] Nenhum parâmetro de rota ou body aceita `vendedorCpf`, `adminId` ou similar
- [ ] O CPF é lido de `User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value`
- [ ] Se a claim `cpf` estiver ausente, retorna `401 Unauthorized`

---

## Constraints

- **Clean Architecture:** Domain não referencia camadas externas. Application depende só de Domain. Infraestructure implementa interfaces do Domain. Api orquestra. (ADR-001)
- **Dapper:** Todas as queries SQL são parametrizadas com `@param` — sem concatenação de strings. (ADR-002)
- **DbUp:** Migrations seguem padrão `ScriptNNNN_Descricao.sql` e são idempotentes (`IF NOT EXISTS`). (ADR-008)
- **Cancelamento lógico:** Nunca exclusão física de registros. Flags `Cancelado`, `Reembolsada`, `Reembolsado`, `Status` indicam estado. (ADR-012)
- **Nenhuma migration adicional é necessária** — o campo `Reservas.Reembolsada` já foi adicionado pela spec 40 (`Script0013_AdicionarReembolsadaReservas.sql`)
- **Ingresso.Status = 3** é o valor para "Reembolsado" — consistente com spec 170 (`StatusPagamento.Reembolsado = 3`)
- **O método `DeleteAsync` existente** em `IEventoRepository`, `IEventoService`, `EventoService` e `EventoController` deve ser removido e substituído por `CancelarEvento`

---

## Edge Cases & Error States

| # | Caso | Comportamento esperado |
|---|------|----------------------|
| B1 | Evento tipo Palestra sem ingressos na tabela `Ingressos` | UPDATE em `Ingressos` afeta 0 linhas — não gera erro |
| B2 | Reservas pendentes de pagamento (`Pago = false`) | São marcadas como `Reembolsada = true` mesmo sem pagamento realizado |
| B3 | `Pagamentos.Status` já é diferente de 1 (ex: já reembolsado, pendente) | UPDATE `WHERE Status = 1` garante que apenas pagamentos confirmados são afetados |
| B4 | Evento com centenas de reservas | A transação atômica garante consistência independentemente do volume |
| B5 | Cancelamento concorrente (duas requests simultâneas para o mesmo evento) | A transação atômica garante que apenas um cancelamento é efetivado; a segunda request encontra `Cancelado = true` e retorna `409 Conflict` |
| B6 | Evento que já começou (`DataEvento <= DateTime.UtcNow`) | Permitir cancelamento — o vendedor pode cancelar a qualquer momento |
| B7 | Vendedor tenta acessar `status-cancelamento` de evento que não existe | `404 Not Found` |
| B8 | Usuário sem claim `cpf` no JWT acessa qualquer endpoint | `401 Unauthorized` |
| B9 | Usuário com role `Comprador` tenta acessar os endpoints | `403 Forbidden` (atributo `[Authorize(Roles = "Vendedor,Admin")]`) |
| B10 | `Ingresso.Status` já é 3 (reembolsado por outro motivo) | UPDATE `WHERE Status = 2` garante que apenas ingressos vendidos são reembolsados |

---

## Dependencies

- **Spec 40 (implemented):** Campo `Reservas.Reembolsada` (entidade + migration `Script0013`). Método `Reserva.MarcarReembolsada()`. Sem esta spec, a transação de cancelamento de evento não pode marcar `Reservas.Reembolsada = 1`.
- **Spec 170 (implemented):** Entidade `Pagamento` com `Status` enum e método `MarcarReembolsado()`. Define `StatusPagamento.Reembolsado = 3`.
- **Spec 180 (implemented):** Serviço de e-mail transacional — a spec 50 não envia e-mails (isso é responsabilidade da spec 180).
- **SQL Server** via Dapper + `ConnectionFactory`
- **DbUp** para versionamento de migrations
- **BCrypt.Net-Next** (já configurado, ADR-003)
- **JWT Bearer** (já configurado, ADR-004)

---

## Out of Scope

- Gateway de pagamento real (reembolso é simulado via status no banco)
- Envio de e-mail de notificação de cancelamento para compradores (spec 180)
- Cancelamento parcial de evento (ex: cancelar apenas um setor)
- Reabertura de evento cancelado
- Política de reembolso proporcional (ex: reembolsar 50% se evento já começou)
- Rollback de migration
