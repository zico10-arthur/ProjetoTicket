# Spec 170 — Requirements: Pagamento Simulado (Checkout Interno)

> **Projeto:** SoldOut Tickets
> **Contexto:** [`requisitos.md` HU30](../../../requisitos.md) | [`visao.md §10`](../../../visao.md#10-escopo-atual-v20)
> **Status:** `pendente`

---

## 1. Objetivo

Permitir que um comprador confirme o pagamento de uma reserva já criada no sistema. O pagamento é **simulado** (sem gateway externo) — o sistema apenas registra a confirmação e transiciona os ingressos para o estado "Vendido".

---

## 2. Histórias de Usuário

### HU-PAG01: Confirmar Pagamento da Reserva

**Como** um comprador,
**Quero** confirmar o pagamento da minha reserva,
**Para** garantir meus ingressos e finalizar a compra.

### HU-PAG02: Ver Status de Pagamento

**Como** um comprador,
**Quero** ver se minha reserva está paga ou pendente,
**Para** saber se ainda preciso pagar ou se já está confirmado.

---

## 3. Requisitos Funcionais

| ID | Descrição |
|----|-----------|
| RF-PAG01 | O sistema deve permitir que o comprador confirme o pagamento de uma reserva da qual é dono |
| RF-PAG02 | O sistema deve registrar um `Pagamento` vinculado à `Reserva` com valor, data e status |
| RF-PAG03 | O sistema deve marcar a `Reserva` como paga (`Pago = true`) após confirmação |
| RF-PAG04 | O sistema deve transicionar todos os `Ingressos` da reserva para `Status = 2` (Vendido) |
| RF-PAG05 | O sistema deve impedir pagamento duplicado (reserva já paga) |
| RF-PAG06 | O sistema deve impedir pagamento de reserva que não pertence ao usuário logado |
| RF-PAG07 | O sistema deve impedir pagamento após o início do evento |
| RF-PAG08 | O endpoint de checkout deve executar todas as operações em uma transação atômica |
| RF-PAG09 | O sistema deve incluir o status de pagamento (`Pago`) no response de consulta de reservas |
| RF-PAG10 | Admin deve poder visualizar todos os pagamentos do sistema |

---

## 4. Requisitos Não Funcionais

| ID | Descrição |
|----|-----------|
| RNF-PAG01 | A operação de checkout deve ser atômica (transaction SQL) |
| RNF-PAG02 | O endpoint deve extrair o CPF do JWT — nunca confiar em CPF enviado no body |
| RNF-PAG03 | Senhas e dados sensíveis não trafegam no fluxo de pagamento simulado |
| RNF-PAG04 | O método de pagamento deve ser extensível (enum/string) para futuro gateway real |
| RNF-PAG05 | A migration do banco deve seguir o padrão DbUp existente (`Script0011_...`) |

---

## 5. Critérios de Aceitação (BDD)

### HU-PAG01: Confirmar Pagamento

**Cenário 1 — Pagamento bem-sucedido (evento pago)**
- **Dado** que o comprador está logado e possui uma reserva com `ValorFinalPago > 0`
- **Quando** ele acessa `POST /api/pagamento/checkout/{reservaId}`
- **Então** o sistema cria um `Pagamento` com `Status = Confirmado`, marca `Reserva.Pago = true`, marca `Ingressos.Status = 2`, e retorna `200 OK` com os detalhes do pagamento

**Cenário 2 — Pagamento de evento gratuito**
- **Dado** que o comprador está logado e possui uma reserva de evento gratuito (`ValorFinalPago = 0`)
- **Quando** ele acessa `POST /api/pagamento/checkout/{reservaId}`
- **Então** o sistema confirma instantaneamente: cria `Pagamento` com `ValorPago = 0`, `Status = Confirmado`, e transiciona os ingressos — sem cobrança

**Cenário 3 — Reserva já paga**
- **Dado** que a reserva já foi paga (`Pago = true`)
- **Quando** o comprador tenta pagar novamente
- **Então** o sistema retorna `409 Conflict` com a mensagem "Reserva já foi paga"

**Cenário 4 — Reserva de outro usuário**
- **Dado** que o comprador tenta pagar uma reserva cujo `UsuarioCpf` não é o seu
- **Quando** ele acessa o endpoint de checkout
- **Então** o sistema retorna `403 Forbidden`

**Cenário 5 — Evento já começou**
- **Dado** que a `DataEvento <= DateTime.Now`
- **Quando** o comprador tenta pagar
- **Então** o sistema retorna `400 Bad Request` com a mensagem "Não é possível pagar. O evento já começou."

**Cenário 6 — Reserva inexistente**
- **Dado** que o `reservaId` não existe no banco
- **Quando** o comprador tenta pagar
- **Então** o sistema retorna `404 Not Found`

---

## 6. Casos de Borda

| # | Caso | Comportamento esperado |
|---|------|----------------------|
| B1 | Reserva cancelada/reembolsada | Bloquear pagamento — `409 Conflict` "Reserva foi cancelada" |
| B2 | Reserva com múltiplos ItensReserva (até 4 CPFs) | Todos os ingressos de todos os itens transitam para Status=2 |
| B3 | Reserva de evento tipo Palestra (sem IngressoId) | Confirmar pagamento sem tentar atualizar Ingressos (Palestra não gera ingressos) |
| B4 | Checkout concorrente (duas requests simultâneas) | A transação atômica garante que apenas uma confirmação é efetivada |

---

## 7. Escopo

### Dentro do escopo
- Endpoint `POST /api/pagamento/checkout/{reservaId}`
- Tabela `Pagamentos` com migration SQL
- Integração com Reserva (campo `Pago`)
- Transição de Ingressos para Status=2
- Consulta de status de pagamento via reserva
- Listagem de pagamentos para Admin

### Fora do escopo
- Gateway de pagamento real (Stripe, PagSeguro, Mercado Pago)
- Reembolso (já coberto por ST-05 e ST-06)
- Envio de comprovante por e-mail
- Conciliação financeira / extrato
- Estorno parcial (ItemReserva individual)
- Suporte a parcelamento
