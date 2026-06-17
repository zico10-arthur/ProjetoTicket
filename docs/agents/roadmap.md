# Roadmap — SoldOut Tickets

> **Fonte:** [`storytelling.md`](../storytelling.md) | **Specs:** [`roadmap/`](./roadmap/) (30 arquivos) | **Data:** 17/06/2026

---

## Fase 1 (Concluída)

- Sistema base com Clean Architecture
- CRUD de usuários, eventos, ingressos, reservas e cupons
- Autenticação JWT com 3 perfis (Admin, Vendedor, Comprador)
- Frontend Blazor Server + MudBlazor

---

## Fase 2 — v2.0

> Cada spec aponta para qual dos [5 problemas do usuário](../visao.md#2-problema) ela resolve.
> **Planejamento detalhado:** [`sprints.md`](../sprints.md) — 3 sprints, ~70h.

### 🔴 Sprint 1 — Segurança e Correções Críticas

| # | Spec | Status | Problema ([visao.md §2](../visao.md#2-problema)) | Arquivo |
|---|------|--------|-------------------------------------------------|---------|
| 120 | Segurança (BCrypt, JWT, rate limit) | ❌ `pendente` | **Autonomia** — segurança para todos os perfis | [`120-seguranca-autenticacao.md`](./roadmap/120-seguranca-autenticacao.md) |
| 160 | Cupons de Desconto | ❌ `pendente` | **Emitir ingressos** — AdminId via JWT, não rota | [`160-cupons.md`](./roadmap/160-cupons.md) |
| 130 | Isolamento Multi-Tenant (VendedorId) | ❌ `pendente` | **Autonomia** — privacidade entre vendedores | [`130-isolamento-multi-tenant.md`](./roadmap/130-isolamento-multi-tenant.md) |
| 150 | Resiliência e Tratamento de Erros | ❌ `pendente` | **Emitir ingressos** — sistema profissional | [`150-resiliencia-erros.md`](./roadmap/150-resiliencia-erros.md) |
| 180 | Serviço de E-mail + Redef. de Senha | ❌ `pendente` | **Autonomia** — confirmações por e-mail e recuperação de senha | [`spec-180/`](./roadmap/spec-180/) |

### 🟠 Sprint 2 — Cancelamento e Reembolso

| # | Spec | Status | Problema ([visao.md §2](../visao.md#2-problema)) | Arquivo |
|---|------|--------|-------------------------------------------------|---------|
| 40 | ST-05 Cancelamento de Reserva c/ Reembolso | ❌ `pendente` | **Processar cancelamentos** — comprador reembolsado | [`spec-40/`](./roadmap/spec-40/) |
| 50 | ST-06 Cancelamento de Evento c/ Reembolso | ❌ `pendente` | **Processar cancelamentos** — evento cancelado | [`spec-50/`](./roadmap/spec-50/) |
| 110 | ST-12 Cancelamento — Visão Unificada | ❌ `pendente` | **Processar cancelamentos** — qualquer perfil | [`spec-110/`](./roadmap/spec-110/) |

### 🟡 Implementadas

| # | Spec | Status | Problema ([visao.md §2](../visao.md#2-problema)) | Arquivo |
|---|------|--------|-------------------------------------------------|---------|
| 10 | ST-01 Auto Cadastro de Vendedor | ✅ `implementada` | **Autonomia** — vender sem depender de Admin | [`10-st01-auto-cadastro-vendedor.md`](./roadmap/10-st01-auto-cadastro-vendedor.md) |
| 20 | ST-03 Palestras com assentos numerados | ✅ `implementada` | **Criar eventos** — lugares marcados | [`20-st03-palestras.md`](./roadmap/20-st03-palestras.md) |
| 30 | ST-04 ItemReserva (até 4 CPFs) | ✅ `implementada` | **Gerenciar vagas** — compra para até 4 pessoas | [`30-st04-item-reserva.md`](./roadmap/30-st04-item-reserva.md) |
| 60 | ST-07 Admin/Vendedor fazem reserva | ✅ `implementada` | **Autonomia** — todos os perfis compram | [`60-st07-admin-vendedor-reservas.md`](./roadmap/60-st07-admin-vendedor-reservas.md) |
| 70 | ST-08 Login Unificado | ✅ `implementada` | **Autonomia** — login único simplificado | [`70-st08-login-unificado.md`](./roadmap/70-st08-login-unificado.md) |
| 80 | ST-09 Vendedor na tabela Usuarios | ✅ `implementada` | **Emitir ingressos** — arquitetura unificada | [`80-st09-vendedor-perfil.md`](./roadmap/80-st09-vendedor-perfil.md) |
| 90 | ST-10 Perfis Simplificados (3 perfis) | ✅ `implementada` | **Autonomia** — controle de acesso claro | [`90-st10-perfis-simplificados.md`](./roadmap/90-st10-perfis-simplificados.md) |
| 100 | ST-11 Tipo de Evento + Gratuito | ✅ `implementada` | **Criar eventos** — com tipo e sem barreira financeira | [`100-st11-tipo-evento-gratuito.md`](./roadmap/100-st11-tipo-evento-gratuito.md) |
| 170 | Pagamento Simulado (Checkout Interno) | ✅ `implementada` | **Emitir ingressos** — confirmação de pagamento | [`spec-170/`](./roadmap/spec-170/) |

### 🟢 Sprint 3 — Infraestrutura e Deploy

| # | Spec | Status | Problema ([visao.md §2](../visao.md#2-problema)) | Arquivo |
|---|------|--------|-------------------------------------------------|---------|
| 140 | Infraestrutura e Deploy (Docker) | ❌ `pendente` | **Criar eventos** — sistema disponível | [`140-infraestrutura-deploy.md`](./roadmap/140-infraestrutura-deploy.md) |
| 190 | Substituir BackgroundService por Hangfire | ❌ `pendente` | **Gerenciar vagas** — confiabilidade na liberação de assentos | [`spec-190/`](./roadmap/spec-190/) |

---

## Cobertura dos 5 Problemas

| Problema ([visao.md §2](../visao.md#2-problema)) | Specs que resolvem | Total |
|---------------------------------------------------|-------------------|-------|
| **Criar e divulgar eventos** de forma simples | 20, 100, 140 | 3 |
| **Gerenciar inscrições e vagas** sem planilhas | 30, 190 | 2 |
| **Emitir e validar ingressos** profissionalmente | 80, 150, 160 | 3 |
| **Processar cancelamentos e reembolsos** | 40, 50, 110 | 3 |
| **Ter autonomia** (cadastrar e vender sem Admin) | 10, 60, 70, 90, 120, 130, 180 | 7 |

---

## Resumo por Status

| Status | Quantidade | Specs |
|--------|-----------|-------|
| ✅ `implementada` | 9 | ST-01, ST-03, ST-04, ST-07, ST-08, ST-09, ST-10, ST-11, 170 |
| ❌ `pendente` | 10 | ST-05, ST-06, ST-12, 120, 130, 140, 150, 160, 180, 190 |

---

## Fase 3 (Futuro)

- Gateway de pagamento real (Stripe/PagSeguro)
- Check-in digital (QR Code)
- Aplicativo mobile
- Relatórios avançados e dashboards analíticos

---

## Evidências no Código (16/06/2026)

| # | O que foi encontrado | Status |
|---|---------------------|--------|
| ST-01 | `UsuarioController`: `[HttpPost("cadastrar-vendedor")]` público, sem `[Authorize]` | ✅ `implementada` |
| ST-03 | `Evento.cs`: campo `TipoEvento Tipo`, método `GerarPalestra()` | ✅ `implementada` |
| ST-04 | `ItemReserva.cs` + `Reserva.Itens` (List&lt;ItemReserva&gt;) ativo | ✅ `implementada` |
| ST-05 | `DELETE /api/reserva/{id}` não existe — spec completa em [`spec-40/`](./roadmap/spec-40/) | ❌ `pendente` |
| ST-06 | `DELETE /api/evento/{id}` existe mas sem lógica de reembolso — spec completa em [`spec-50/`](./roadmap/spec-50/) | ❌ `pendente` |
| ST-07 | `ReservaController`: `[Authorize]` sem restrição de role — todos podem | ✅ `implementada` |
| ST-08 | `UsuarioService.Login()`: BCrypt.Verify + Ativo + LoginResponseDTO | ✅ `implementada` |
| ST-09 | `Usuario.cs` com Cnpj, NomeFantasia, Telefone, Plano, Ativo, DataCriacao | ✅ `implementada` |
| ST-10 | `Script0003`: 3 perfis com GUIDs fixos | ✅ `implementada` |
| ST-11 | `Evento.cs`: `Gratuito` (PrecoPadrao == 0), `TipoEvento` (Teatro/Palestra) | ✅ `implementada` |
| ST-12 | Sem endpoint de cancelamento pelo próprio usuário — spec completa em [`spec-110/`](./roadmap/spec-110/) | ❌ `pendente` |
| 120 | BCrypt ✅, RateLimit ✅ — mas `Jwt:Key` ainda exposto em `appsettings.json` | ❌ `pendente` |
| 130 | `EventoRepository`: filtra por `VendedorCpf` — `ReservaRepository`: SEM filtro | ❌ `pendente` |
| 140 | `DatabaseMigration.cs` + 11 scripts DbUp — sem Dockerfile | ❌ `pendente` |
| 150 | `GlobalExceptionHandlerMiddleware.cs` existe — exceções expõem `ex.Message` sem sanitização | ❌ `pendente` |
| 160 | `Cupom.cs` + CRUD no `CupomController` — `AdminId` ainda via rota (7 ocorrências) | ❌ `pendente` |
| 170 | `PagamentoController` + `PagamentoService` + `PagamentoRepository` + `Script0011` | ✅ `implementada` |
| 180 | Spec criada com 3 arquivos — sem código ainda (infra SMTP + MailKit pendente) | ❌ `pendente` |
| 190 | `LiberacaoAssentosWorker.cs` existe com `PeriodicTimer` — spec completa em [`spec-190/`](./roadmap/spec-190/) | ❌ `pendente` |

---

> **Nota:** ST-02 (Painel do Vendedor) é frontend puro.  
> Para planejamento detalhado de tarefas, consulte [`sprints.md`](../sprints.md).
