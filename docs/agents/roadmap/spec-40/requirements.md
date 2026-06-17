# Spec 40 — Requirements: Comprador Cancela Reserva com Reembolso

> **Projeto:** SoldOut Tickets
> **Contexto:** [`storytelling.md#st-05-comprador-cancela-reserva`](../../../storytelling.md#st-05-comprador-cancela-reserva) | [`visao.md §6.5`](../../../visao.md#65-cancelamento-com-reembolso-atômico)
> **Status:** `pendente`

---

## 1. Objetivo

Permitir que o dono de uma reserva (qualquer perfil) cancele sua reserva antes do início do evento. O cancelamento reverte todos os status para o estado anterior à compra: ingressos voltam a Livre, a reserva é marcada como reembolsada, e o pagamento associado (se existir) transiciona para `Reembolsado`. O reembolso é **simulado** — opera apenas sobre status no banco, sem gateway de pagamento real.

---

## 2. Histórias de Usuário

### HU-CR01: Cancelar Minha Reserva

**Como** um comprador (ou Admin, ou Vendedor),
**Quero** cancelar minha reserva antes do evento começar e ter o reembolso registrado,
**Para** não perder dinheiro se não puder mais comparecer.

### HU-CR02: Ver Status de Reembolso nas Minhas Reservas

**Como** um comprador,
**Quero** ver se minha reserva foi reembolsada e quais itens foram reembolsados,
**Para** ter certeza de que o cancelamento foi processado.

---

## 3. Requisitos Funcionais

| ID | Descrição |
|----|-----------|
| RF-CR01 | O sistema deve expor `DELETE /api/reserva/{id}` para cancelamento de reserva |
| RF-CR02 | O cancelamento deve ser autorizado apenas para o dono da reserva (`Reserva.UsuarioCpf == cpf do JWT`) |
| RF-CR03 | O sistema deve impedir cancelamento se o evento já começou (`DataEvento <= DateTime.UtcNow`) |
| RF-CR04 | O sistema deve impedir cancelamento de reserva já reembolsada (`Reembolsada == true`) |
| RF-CR05 | Ao cancelar, a reserva deve ser marcada como `Reembolsada = true` |
| RF-CR06 | Ao cancelar, todos os `ItensReserva` devem ser marcados como `Reembolsado = true` |
| RF-CR07 | Ao cancelar, os ingressos vinculados devem voltar a `Status = 0` (Livre) |
| RF-CR08 | Ao cancelar, se existir `Pagamento` associado, seu `Status` deve ser alterado para `Reembolsado` |
| RF-CR09 | Todas as operações de cancelamento devem executar em uma única transação atômica |
| RF-CR10 | O response de `GET /api/reserva/minhas` deve incluir os campos `reembolsada` e `reembolsado` (por item) |
| RF-CR11 | CPF do usuário deve ser extraído do JWT — nunca do body da requisição |

---

## 4. Requisitos Não Funcionais

| ID | Descrição |
|----|-----------|
| RNF-CR01 | A operação de cancelamento deve ser atômica (transaction SQL com `BEGIN/COMMIT/ROLLBACK`) |
| RNF-CR02 | O endpoint deve seguir o padrão de autorização existente (JWT + claims) |
| RNF-CR03 | A migration deve seguir o padrão DbUp (`Script00XX_...`) e ser idempotente (`IF NOT EXISTS`) |

---

## 5. Critérios de Aceitação (BDD)

### HU-CR01: Cancelar Minha Reserva

**Cenário 1 — Cancelamento bem-sucedido (evento pago)**
- **Dado** que o usuário está logado e possui uma reserva paga (`Pago = true`) em um evento futuro
- **Quando** ele acessa `DELETE /api/reserva/{id}`
- **Então** o sistema marca `Reserva.Reembolsada = true`, `ItensReserva.Reembolsado = true`, `Ingressos.Status = 0`, `Pagamento.Status = Reembolsado`, e retorna `200 OK`

**Cenário 2 — Cancelamento de evento gratuito**
- **Dado** que o usuário possui uma reserva de evento gratuito (`ValorFinalPago = 0`)
- **Quando** ele cancela
- **Então** o sistema marca `Reembolsada = true` e libera os ingressos — sem alterar `Pagamento` (não houve pagamento)

**Cenário 3 — Cancelamento de reserva não paga**
- **Dado** que o usuário possui uma reserva pendente de pagamento (`Pago = false`)
- **Quando** ele cancela
- **Então** o sistema marca `Reembolsada = true`, libera os ingressos, sem alterar `Pagamento` (não existe)

**Cenário 4 — Reserva já reembolsada**
- **Dado** que a reserva já foi cancelada (`Reembolsada = true`)
- **Quando** o usuário tenta cancelar novamente
- **Então** o sistema retorna `409 Conflict` com a mensagem "Reserva já foi cancelada."

**Cenário 5 — Evento já começou**
- **Dado** que `DataEvento <= DateTime.UtcNow`
- **Quando** o usuário tenta cancelar
- **Então** o sistema retorna `400 Bad Request` com a mensagem "Não é possível cancelar. O evento já começou."

**Cenário 6 — Reserva de outro usuário**
- **Dado** que o `UsuarioCpf` da reserva não corresponde ao CPF do JWT
- **Quando** o usuário tenta cancelar
- **Então** o sistema retorna `403 Forbidden`

**Cenário 7 — Reserva inexistente**
- **Dado** que o `id` não corresponde a nenhuma reserva
- **Quando** o usuário tenta cancelar
- **Então** o sistema retorna `404 Not Found`

---

## 6. Casos de Borda

| # | Caso | Comportamento esperado |
|---|------|----------------------|
| B1 | Reserva com múltiplos itens (até 4 CPFs) | Todos os `ItensReserva` são marcados `Reembolsado = true` |
| B2 | Reserva de evento tipo Palestra (sem IngressoId) | A liberação de ingressos não afeta linhas — comportamento seguro |
| B3 | Reserva sem pagamento associado | O UPDATE em `Pagamentos` afeta 0 linhas — comportamento seguro |
| B4 | Cancelamento concorrente (duas requests simultâneas) | A transação atômica garante que apenas um cancelamento é efetivado |

---

## 7. Escopo

### Dentro do escopo
- Endpoint `DELETE /api/reserva/{id}`
- Campo `Reembolsada` na entidade e tabela `Reservas`
- Transação atômica: Reserva + ItensReserva + Ingressos + Pagamento
- Atualização do response de `GET /api/reserva/minhas` com flags de reembolso

### Fora do escopo
- Gateway de pagamento real (reembolso é simulado via status)
- Reembolso parcial (ItemReserva individual)
- Envio de e-mail de confirmação de reembolso (coberto pela spec 180)
- Cancelamento de evento pelo vendedor (spec 50)
- Política de prazo mínimo para cancelamento (ex: até 24h antes)
- Estorno financeiro real
