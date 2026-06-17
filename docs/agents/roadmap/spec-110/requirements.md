# Spec 110 — Requirements: Cancelamento de Reserva — Visão Unificada

> **Projeto:** SoldOut Tickets
> **Contexto:** [`storytelling.md#st-12-cancelamento-de-reserva`](../../../storytelling.md#st-12-cancelamento-de-reserva) | [`visao.md §6.5`](../../../visao.md#65-cancelamento-com-reembolso-atômico)
> **Status:** `pendente`

---

## 1. Objetivo

Garantir que **todos os perfis** (Comprador, Admin e Vendedor) possam cancelar suas próprias reservas com as mesmas regras, através dos mesmos endpoints. Cada `ItemReserva` deve exibir seu flag `Reembolsado` individual, permitindo visibilidade granular do status de reembolso. A lógica de cancelamento deve ser unificada no Service, sem duplicação por perfil.

---

## 2. Histórias de Usuário

### HU-CU01: Cancelamento Unificado por Perfil

**Como** qualquer usuário logado (Comprador, Admin ou Vendedor),
**Quero** acessar Minhas Reservas e cancelar qualquer reserva minha,
**Para** ter controle total sobre minhas compras, independentemente do meu perfil.

### HU-CU02: Visibilidade Granular de Reembolso

**Como** um comprador,
**Quero** ver quais itens da minha reserva foram reembolsados individualmente,
**Para** saber exatamente o status de cada participante da compra.

---

## 3. Requisitos Funcionais

| ID | Descrição |
|----|-----------|
| RF-CU01 | O endpoint `DELETE /api/reserva/{id}` deve aceitar qualquer perfil autenticado (não apenas Comprador) |
| RF-CU02 | A autorização deve ser unificada: `Reserva.UsuarioCpf == cpf do JWT`, independentemente do perfil |
| RF-CU03 | O endpoint `GET /api/reserva/minhas` deve retornar reservas de qualquer perfil (não apenas Comprador) |
| RF-CU04 | O response de `GET /api/reserva/minhas` deve incluir o campo `reembolsado` por `ItemReserva` |
| RF-CU05 | O response de `GET /api/reserva/minhas` deve incluir o campo `reembolsada` por `Reserva` |
| RF-CU06 | O response de `GET /api/reserva/minhas` deve incluir o campo `podeCancelar` (calculado: `!Reembolsada && DataEvento > agora`) |
| RF-CU07 | A lógica de cancelamento deve residir em um único método no Service (`CancelarReserva`), sem duplicação por perfil |
| RF-CU08 | Admin também pode cancelar QUALQUER reserva via endpoints admin — mas em "Minhas Reservas" aparecem apenas as dele |

---

## 4. Requisitos Não Funcionais

| ID | Descrição |
|----|-----------|
| RNF-CU01 | O endpoint `GET /api/reserva/minhas` deve usar `[Authorize]` sem restrição de role |
| RNF-CU02 | A query SQL de Minhas Reservas deve retornar itens com seus flags individuais em uma única consulta (evitar N+1) |

---

## 5. Critérios de Aceitação (BDD)

### HU-CU01: Cancelamento Unificado por Perfil

**Cenário 1 — Comprador cancela sua reserva**
- **Dado** que um Comprador está logado e possui uma reserva
- **Quando** ele acessa `DELETE /api/reserva/{id}`
- **Então** o sistema cancela a reserva e retorna `200 OK`

**Cenário 2 — Vendedor cancela sua própria reserva**
- **Dado** que um Vendedor está logado e comprou ingressos para um evento
- **Quando** ele acessa `DELETE /api/reserva/{id}` com o id da sua reserva
- **Então** o sistema cancela a reserva e retorna `200 OK`

**Cenário 3 — Admin cancela sua própria reserva**
- **Dado** que um Admin está logado e comprou ingressos para um evento
- **Quando** ele acessa `DELETE /api/reserva/{id}` com o id da sua reserva
- **Então** o sistema cancela a reserva e retorna `200 OK`

**Cenário 4 — Vendedor tenta cancelar reserva de outro usuário**
- **Dado** que um Vendedor tenta cancelar uma reserva cujo `UsuarioCpf` não é o seu
- **Quando** ele acessa `DELETE /api/reserva/{id}`
- **Então** o sistema retorna `403 Forbidden`

**Cenário 5 — Minhas Reservas mostra apenas reservas próprias**
- **Dado** que um Vendedor acessa `GET /api/reserva/minhas`
- **Quando** a consulta é executada
- **Então** o sistema retorna apenas reservas onde `UsuarioCpf == cpf do JWT`

### HU-CU02: Visibilidade Granular de Reembolso

**Cenário 1 — Reserva com itens reembolsados**
- **Dado** que uma reserva foi cancelada
- **Quando** o usuário acessa `GET /api/reserva/minhas`
- **Então** cada `ItemReserva` exibe `"reembolsado": true` e a reserva exibe `"reembolsada": true`

**Cenário 2 — Reserva ativa (não cancelada)**
- **Dado** que uma reserva está ativa
- **Quando** o usuário acessa `GET /api/reserva/minhas`
- **Então** a reserva exibe `"reembolsada": false`, `"podeCancelar": true` e itens com `"reembolsado": false`

---

## 6. Casos de Borda

| # | Caso | Comportamento esperado |
|---|------|----------------------|
| B1 | Admin acessa Minhas Reservas pela primeira vez | Retorna array vazio (Admin pode não ter reservas próprias) |
| B2 | Vendedor sem reservas acessa Minhas Reservas | Retorna array vazio |
| B3 | Reserva com evento cancelado (spec 50) | `reembolsada: true`, `podeCancelar: false` |

---

## 7. Escopo

### Dentro do escopo
- Unificação do `DELETE /api/reserva/{id}` para todos os perfis
- Unificação do `GET /api/reserva/minhas` para todos os perfis
- Inclusão de flags `reembolsada`, `reembolsado` (por item) e `podeCancelar` no response
- Método único de cancelamento no Service

### Fora do escopo
- Admin cancelar reserva de terceiros via endpoint público (Admin usa endpoints admin separados)
- Paginação em Minhas Reservas
- Filtros avançados em Minhas Reservas (por status, data, evento)
- Ordenação customizável
