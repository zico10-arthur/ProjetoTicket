# Spec 50 — Requirements: Vendedor Cancela Evento com Reembolso Obrigatório

> **Projeto:** SoldOut Tickets
> **Contexto:** [`storytelling.md#st-06-vendedor-cancela-evento`](../../../storytelling.md#st-06-vendedor-cancela-evento) | [`visao.md §6.5`](../../../visao.md#65-cancelamento-com-reembolso-atômico)
> **Status:** `pendente`

---

## 1. Objetivo

Permitir que um vendedor (ou Admin) cancele um evento que não será mais realizado. Se o evento for pago e tiver ingressos vendidos, o sistema deve forçar o reembolso de todas as reservas e pagamentos associados. Se o evento for gratuito, apenas cancela sem reembolso. Antes de confirmar o cancelamento, o vendedor deve ser alertado sobre o impacto financeiro da operação.

---

## 2. Histórias de Usuário

### HU-CE01: Cancelar Evento

**Como** um vendedor,
**Quero** cancelar um evento que não será mais realizado,
**Para** liberar a grade de eventos e garantir que os compradores sejam reembolsados.

### HU-CE02: Verificar Impacto do Cancelamento

**Como** um vendedor,
**Quero** ver quantos ingressos foram vendidos e qual o valor total de reembolso antes de cancelar,
**Para** tomar uma decisão informada sobre o cancelamento.

---

## 3. Requisitos Funcionais

| ID | Descrição |
|----|-----------|
| RF-CE01 | O sistema deve expor `GET /api/evento/{id}/status-cancelamento` para consulta prévia ao cancelamento |
| RF-CE02 | O endpoint de status deve retornar: nome do evento, gratuito/pago, total de ingressos vendidos, total de reservas ativas, se reembolso é necessário, e valor total estimado de reembolso |
| RF-CE03 | O sistema deve modificar `DELETE /api/evento/{id}` para executar cancelamento com reembolso (não mais exclusão física) |
| RF-CE04 | Apenas o vendedor dono do evento ou Admin pode cancelar |
| RF-CE05 | O sistema deve impedir cancelamento de evento já cancelado (`Cancelado == true`) |
| RF-CE06 | Ao cancelar evento pago com ingressos vendidos: `Evento.Cancelado = true`, `Ingressos.Status = 3` (Reembolsado), `Reservas.Reembolsada = true`, `ItensReserva.Reembolsado = true`, `Pagamentos.Status = 3` (Reembolsado) |
| RF-CE07 | Ao cancelar evento gratuito: `Evento.Cancelado = true`, `Reservas.Reembolsada = true` (sem reembolso financeiro — não houve cobrança) |
| RF-CE08 | Ao cancelar evento sem ingressos vendidos: apenas `Evento.Cancelado = true` |
| RF-CE09 | Todas as operações de cancelamento de evento devem executar em uma única transação atômica |
| RF-CE10 | Eventos cancelados não devem aparecer na listagem pública (`GET /api/evento`) |
| RF-CE11 | CPF do vendedor deve ser extraído do JWT — nunca do body da requisição |

---

## 4. Requisitos Não Funcionais

| ID | Descrição |
|----|-----------|
| RNF-CE01 | A operação de cancelamento deve ser atômica (transaction SQL com `BEGIN/COMMIT/ROLLBACK`) |
| RNF-CE02 | O endpoint de status-cancelamento deve ser rápido (consulta de agregação sem lock) |
| RNF-CE03 | A migration deve seguir o padrão DbUp e ser idempotente |

---

## 5. Critérios de Aceitação (BDD)

### HU-CE01: Cancelar Evento

**Cenário 1 — Cancelamento de evento pago com ingressos vendidos**
- **Dado** que o vendedor possui um evento pago com 23 ingressos vendidos
- **Quando** ele acessa `DELETE /api/evento/{id}`
- **Então** o sistema marca `Evento.Cancelado = true`, todos os `Ingressos.Status = 3`, todas as `Reservas.Reembolsada = true`, todos os `ItensReserva.Reembolsado = true`, todos os `Pagamentos.Status = 3`, e retorna `200 OK`

**Cenário 2 — Cancelamento de evento gratuito**
- **Dado** que o vendedor possui um evento gratuito com reservas
- **Quando** ele acessa `DELETE /api/evento/{id}`
- **Então** o sistema marca `Evento.Cancelado = true` e `Reservas.Reembolsada = true` — sem alterar `Pagamentos` (não houve pagamento)

**Cenário 3 — Cancelamento de evento sem ingressos vendidos**
- **Dado** que o evento não tem nenhum ingresso vendido
- **Quando** o vendedor cancela
- **Então** o sistema apenas marca `Evento.Cancelado = true`

**Cenário 4 — Evento já cancelado**
- **Dado** que o evento já foi cancelado (`Cancelado = true`)
- **Quando** o vendedor tenta cancelar novamente
- **Então** o sistema retorna `409 Conflict` com a mensagem "Evento já foi cancelado."

**Cenário 5 — Vendedor não é dono do evento**
- **Dado** que o `VendedorCpf` do evento não corresponde ao CPF do JWT
- **Quando** o vendedor tenta cancelar
- **Então** o sistema retorna `403 Forbidden`

**Cenário 6 — Admin cancela evento de qualquer vendedor**
- **Dado** que o usuário tem role `Admin`
- **Quando** ele acessa `DELETE /api/evento/{id}`
- **Então** o sistema permite o cancelamento independentemente do `VendedorCpf`

**Cenário 7 — Evento inexistente**
- **Dado** que o `id` não corresponde a nenhum evento
- **Quando** o vendedor tenta cancelar
- **Então** o sistema retorna `404 Not Found`

### HU-CE02: Verificar Impacto do Cancelamento

**Cenário 1 — Consulta de status de cancelamento (evento pago com vendas)**
- **Dado** que o vendedor acessa `GET /api/evento/{id}/status-cancelamento`
- **Quando** o evento tem 23 ingressos vendidos e valor total de R$ 1.147,70
- **Então** o sistema retorna `200 OK` com `reembolsoNecessario: true` e `valorTotalReembolso: 1147.70`

**Cenário 2 — Consulta de status (evento gratuito)**
- **Dado** que o evento é gratuito
- **Quando** o vendedor consulta o status
- **Então** o sistema retorna `gratuito: true`, `reembolsoNecessario: false`, `valorTotalReembolso: 0`

---

## 6. Casos de Borda

| # | Caso | Comportamento esperado |
|---|------|----------------------|
| B1 | Evento tipo Palestra sem ingressos na tabela `Ingressos` | UPDATE em `Ingressos` afeta 0 linhas — seguro |
| B2 | Reservas pendentes de pagamento (`Pago = false`) | São marcadas como `Reembolsada = true` mesmo sem pagamento realizado |
| B3 | Evento com centenas de reservas | A transação atômica garante consistência independentemente do volume |
| B4 | Cancelamento concorrente (duas requests simultâneas) | A transação atômica garante que apenas um cancelamento é efetivado |
| B5 | Evento que já começou (`DataEvento <= agora`) | Permitir cancelamento mesmo assim — o vendedor pode cancelar a qualquer momento |

---

## 7. Escopo

### Dentro do escopo
- Endpoint `GET /api/evento/{id}/status-cancelamento`
- Modificação de `DELETE /api/evento/{id}` para cancelamento com reembolso
- Transação atômica: Evento + Ingressos + Reservas + ItensReserva + Pagamentos
- Filtro de eventos cancelados na listagem pública

### Fora do escopo
- Gateway de pagamento real (reembolso é simulado via status)
- Notificação por e-mail aos compradores (coberto pela spec 180)
- Cancelamento parcial de evento (ex: cancelar apenas um setor)
- Reabertura de evento cancelado
- Política de reembolso proporcional (ex: reembolsar 50% se evento já começou)
