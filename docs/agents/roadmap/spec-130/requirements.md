---
name: "Isolamento Multi-Tenant (VendedorId)"
status: "audited"
references:
  - "130-isolamento-multi-tenant.md"
  - "especificacoes.md#42-regras-de-isolamento"
  - "visao.md#2-problema (Autonomia)"
dependencies:
  - "Spec 120 (Segurança: BCrypt, JWT, rate limit) — JWT claims disponíveis"
  - "Spec 160 (Cupons de Desconto — AdminId via JWT) — padrão de extração de identidade"
---

# Spec 130 — Isolamento Multi-Tenant (VendedorId)

## 1. Visão Geral

**Problema:** Atualmente a tabela `Reservas` não possui coluna `VendedorCpf`, o que significa que um vendedor pode, em teoria, acessar reservas de eventos que não são seus — seja via API ou via queries diretas. Além disso, não existe endpoint para o vendedor visualizar as reservas dos seus próprios eventos.

**Solução:** Adicionar coluna `VendedorCpf` à tabela `Reservas`, propagar o valor durante a criação da reserva, e garantir que toda query feita por um vendedor filtre por `VendedorCpf` extraído do JWT.

## 2. Requisitos Funcionais (FR)

### FR-001 — Coluna VendedorCpf na tabela Reservas
A tabela `Reservas` deve ter uma coluna `VendedorCpf NVARCHAR(11) NOT NULL` com:
- Foreign Key para `Usuarios(Cpf)`
- Índice `IX_Reservas_VendedorCpf`
- Valor preenchido automaticamente durante a criação da reserva (extraído do evento)

### FR-002 — Preenchimento automático ao criar reserva
Ao criar uma reserva via `ReservaService.FazerReserva()`, o `VendedorCpf` deve ser extraído do `Evento.VendedorCpf` e armazenado na reserva.

### FR-003 — Endpoint de vendas para Vendedor
Deve existir um endpoint `GET /api/reserva/minhas-vendas` acessível apenas ao perfil `Vendedor` que retorna todas as reservas dos eventos do vendedor logado. O `VendedorCpf` deve ser extraído do JWT (claim `"cpf"`).

### FR-004 — Isolamento nas queries de vendedor
Toda query de reservas feita no contexto de um vendedor deve incluir `WHERE VendedorCpf = @vendedorCpf`, onde `vendedorCpf` vem do JWT.

### FR-005 — Admin mantém visão global
O endpoint `GET /api/reserva/Admin/Todas` (Admin) continua retornando todas as reservas sem filtro por vendedor.

### FR-006 — Comprador mantém isolamento próprio
Comprador continua vendo apenas suas reservas (`WHERE UsuarioCpf = @cpf`), sem alteração.

### FR-007 — Worker de liberação opera globalmente
O `LiberacaoAssentosWorker` continua operando sem filtro de vendedor (processo de sistema).

## 3. Requisitos Não-Funcionais (NFR)

### NFR-001 — Performance
Índice `IX_Reservas_VendedorCpf` garante que queries por vendedor não degradam com volume de dados.

### NFR-002 — Segurança
`VendedorCpf` nunca é aceito via parâmetro de rota ou corpo de requisição. Sempre extraído do JWT.

### NFR-003 — Retrocompatibilidade
Migração SQL usa `NOT NULL DEFAULT ''` para não quebrar reservas existentes.

### NFR-004 — Consistência de arquitetura
Segue o mesmo padrão da Spec 160 (AdminId via JWT) e do `EventoController` (cpf via claim).

## 4. Edge Cases (EC)

### EC-001 — Reserva existente sem VendedorCpf
Após a migração, reservas antigas terão `VendedorCpf = ''`. O sistema deve tratar string vazia como "sem vendedor associado" e não expor essas reservas para nenhum vendedor.

### EC-002 — Vendedor sem eventos
Se o vendedor logado não tem eventos, `GET /api/reserva/minhas-vendas` retorna lista vazia (200 OK com `[]`).

### EC-003 — Vendedor tenta acessar vendas de outro vendedor
Impossível — o `VendedorCpf` é extraído do JWT, não da rota.

### EC-004 — Admin acessa endpoint de vendedor
Admin autenticado acessando `GET /api/reserva/minhas-vendas` deve receber 403 Forbidden (restrito a `Vendedor`).

### EC-005 — Evento cancelado
Reservas de eventos cancelados continuam visíveis para o vendedor (histórico).

### EC-006 — Transação de criação de reserva falha
Se a criação da reserva falhar após obter o evento, o rollback da transação garante que nenhum dado parcial persiste.

## 5. Out of Scope

- Isolamento de Cupons — cupons são globais (gerenciados pelo Admin), sem vínculo com vendedor
- Isolamento de Ingressos — ingressos pertencem a eventos que já têm `VendedorCpf`; o isolamento é herdado via JOIN com Eventos
- Isolamento de Pagamentos — pagamentos são vinculados a reservas; o isolamento é herdado via JOIN com Reservas
- CRUD de vendedor — endpoints de cadastro/login de vendedor já existem (Specs ST-01, ST-08)
- Rate limiting — já implementado na Spec 120
- Filtro no frontend Blazor — esta spec trata apenas da API; frontend é tratado separadamente
