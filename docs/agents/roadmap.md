# Roadmap — SoldOut Tickets

> **Fonte:** [`storytelling.md`](../storytelling.md) | **Specs:** [`roadmap/`](./roadmap/) (16 arquivos) | **Data:** 04/06/2026

---

## Fase 1 (Concluída)

- Sistema base com Clean Architecture
- CRUD de usuários, eventos, ingressos, reservas e cupons
- Autenticação JWT com 3 perfis (Admin, Vendedor, Comprador)
- Frontend Blazor Server + MudBlazor

---

## Fase 2 — v2.0 (ordenado por prioridade)

> Cada spec aponta para qual dos [5 problemas do usuário](../visao.md#2-problema) ela resolve.
> Ordem definida por dependência: o que está em cima precisa existir antes do que está embaixo.

### 🔴 Prioridade 1 — Fundação

| # | Spec | Status | Problema ([visao.md §2](../visao.md#2-problema)) | Arquivo |
|---|------|--------|-------------------------------------------------|---------|
| 90 | ST-10 Perfis Simplificados (3 perfis) | ✅ `implementada` | **Autonomia** — controle de acesso claro | [`90-st10-perfis-simplificados.md`](./roadmap/90-st10-perfis-simplificados.md) |
| 120 | Segurança (BCrypt, JWT, rate limit) | ❌ `pendente` | **Autonomia** — segurança para todos os perfis | [`120-seguranca-autenticacao.md`](./roadmap/120-seguranca-autenticacao.md) |
| 70 | ST-08 Login Unificado | ⚠️ `em revisão` | **Autonomia** — login único simplificado | [`70-st08-login-unificado.md`](./roadmap/70-st08-login-unificado.md) |
| 80 | ST-09 Vendedor na tabela Usuarios | ❌ `pendente` | **Emitir ingressos** — arquitetura unificada | [`80-st09-vendedor-perfil.md`](./roadmap/80-st09-vendedor-perfil.md) |
| 150 | Resiliência e Tratamento de Erros | ⚠️ `em revisão` | **Emitir ingressos** — sistema profissional | [`150-resiliencia-erros.md`](./roadmap/150-resiliencia-erros.md) |

### 🟠 Prioridade 2 — Core

| # | Spec | Status | Problema ([visao.md §2](../visao.md#2-problema)) | Arquivo |
|---|------|--------|-------------------------------------------------|---------|
| 10 | ST-01 Auto Cadastro de Vendedor | ❌ `pendente` | **Autonomia** — vender sem depender de Admin | [`10-st01-auto-cadastro-vendedor.md`](./roadmap/10-st01-auto-cadastro-vendedor.md) |
| 100 | ST-11 Tipo de Evento + Gratuito | ✅ `implementada` | **Criar eventos** — com tipo e sem barreira financeira | [`100-st11-tipo-evento-gratuito.md`](./roadmap/100-st11-tipo-evento-gratuito.md) |
| 20 | ST-03 Palestras com assentos numerados | ✅ `implementada` | **Criar eventos** — lugares marcados | [`20-st03-palestras.md`](./roadmap/20-st03-palestras.md) |
| 130 | Isolamento Multi-Tenant (VendedorId) | ⚠️ `em revisão` | **Autonomia** — privacidade entre vendedores | [`130-isolamento-multi-tenant.md`](./roadmap/130-isolamento-multi-tenant.md) |
| 30 | ST-04 ItemReserva (até 4 CPFs) | ❌ `pendente` | **Gerenciar vagas** — compra para até 4 pessoas | [`30-st04-item-reserva.md`](./roadmap/30-st04-item-reserva.md) |
| 160 | Cupons de Desconto | ⚠️ `em revisão` | **Emitir ingressos** — desconto por Admin | [`160-cupons.md`](./roadmap/160-cupons.md) |

### 🟡 Prioridade 3 — Transações

| # | Spec | Status | Problema ([visao.md §2](../visao.md#2-problema)) | Arquivo |
|---|------|--------|-------------------------------------------------|---------|
| 60 | ST-07 Admin/Vendedor fazem reserva | ❌ `pendente` | **Autonomia** — todos os perfis compram | [`60-st07-admin-vendedor-reservas.md`](./roadmap/60-st07-admin-vendedor-reservas.md) |
| 40 | ST-05 Cancelamento de Reserva c/ Reembolso | ❌ `pendente` | **Processar cancelamentos** — comprador reembolsado | [`40-st05-cancelamento-reserva.md`](./roadmap/40-st05-cancelamento-reserva.md) |
| 50 | ST-06 Cancelamento de Evento c/ Reembolso | ⚠️ `em revisão` | **Processar cancelamentos** — evento cancelado | [`50-st06-cancelamento-evento.md`](./roadmap/50-st06-cancelamento-evento.md) |
| 110 | ST-12 Cancelamento — Visão Unificada | ❌ `pendente` | **Processar cancelamentos** — qualquer perfil | [`110-st12-cancelamento-unificado.md`](./roadmap/110-st12-cancelamento-unificado.md) |

### 🟢 Prioridade 4 — Entrega

| # | Spec | Status | Problema ([visao.md §2](../visao.md#2-problema)) | Arquivo |
|---|------|--------|-------------------------------------------------|---------|
| 140 | Infraestrutura e Deploy (Docker) | ⚠️ `em revisão` | **Criar eventos** — sistema disponível | [`140-infraestrutura-deploy.md`](./roadmap/140-infraestrutura-deploy.md) |

---

## Cobertura dos 5 Problemas

| Problema ([visao.md §2](../visao.md#2-problema)) | Specs que resolvem | Total |
|---------------------------------------------------|-------------------|-------|
| **Criar e divulgar eventos** de forma simples | 20, 100, 140 | 3 |
| **Gerenciar inscrições e vagas** sem planilhas | 30 | 1 |
| **Emitir e validar ingressos** profissionalmente | 80, 150, 160 | 3 |
| **Processar cancelamentos e reembolsos** | 40, 50, 110 | 3 |
| **Ter autonomia** (cadastrar e vender sem Admin) | 10, 60, 70, 90, 120, 130 | 6 |

---

## Resumo por Status

| Status | Quantidade | Specs |
|--------|-----------|-------|
| ✅ `implementada` | 3 | ST-03, ST-10, ST-11 |
| ⚠️ `em revisão` | 6 | ST-06, ST-08, 130, 140, 150, 160 |
| ❌ `pendente` | 7 | ST-01, ST-04, ST-05, ST-07, ST-09, ST-12, 120 |

---

## Fase 3 (Futuro)

- Gateway de pagamento real (Stripe/PagSeguro)
- Notificações por e-mail
- Check-in digital (QR Code)
- Aplicativo mobile
- Relatórios avançados e dashboards analíticos

---

## Evidências no Código (04/06/2026)

| # | O que foi encontrado | Status |
|---|---------------------|--------|
| ST-10 | `Script0003`: 3 perfis com GUIDs fixos | ✅ `implementada` |
| ST-06 | `EventoController`: `DELETE /api/evento/{id}` existe | ⚠️ `em revisão` (falta reembolso) |
| ST-08 | `UsuarioService.Login()`: senha em texto plano | ⚠️ `em revisão` (sem BCrypt) |
| 130 | `Evento.cs`: campo `VendedorCpf`; `Script0008` | ⚠️ `em revisão` (só Evento) |
| 140 | `DatabaseMigration.cs` + 8 scripts DbUp | ⚠️ `em revisão` (sem Docker) |
| 150 | `GlobalExceptionHandlerMiddleware.cs` existe | ⚠️ `em revisão` (sem sanitização) |
| 160 | `Cupom.cs` + CRUD no `CupomController` | ⚠️ `em revisão` (AdminId via rota) |
| ST-01 | `CadastrarVendedor(AdminLogado)` — exige Admin | ❌ `pendente` |
| ST-03 | `Evento.cs` com `Tipo`, `Descricao`, `Local`, `Cancelado`, `DataCriacao`; `GerarLoteIngressos()` por tipo (Palestra/Teatro); `Script0010`; testes 14/14 ✅ | ✅ `implementada` |
| ST-04 | `Reserva.cs`: `IngressoId` único, sem `ItemReserva` | ❌ `pendente` |
| ST-05 | Sem `DELETE /api/reserva/{id}` | ❌ `pendente` |
| ST-07 | `ReservaController`: `[Authorize(Roles="Comprador")]` | ❌ `pendente` |
| ST-09 | `Usuario.cs` sem Cnpj, NomeFantasia, etc. | ❌ `pendente` |
| ST-11 | `TipoEvento.cs` (enum Teatro/Palestra); `Evento.Gratuito` (PrecoPadrao==0); `POST /api/evento/criar` com 201; `ReservaRepository` com transação atômica p/ gratuito | ✅ `implementada` |
| ST-12 | Sem endpoint de cancelamento pelo usuário | ❌ `pendente` |
| 120 | Sem BCrypt; `Jwt:Key` exposto; sem rate limit | ❌ `pendente` |

---

> **Nota:** ST-02 (Painel do Vendedor) é frontend puro.  
> Para planejamento detalhado de tarefas, consulte [`sprints.md`](../sprints.md).
