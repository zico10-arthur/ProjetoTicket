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
| 120 | Segurança (BCrypt, JWT, rate limit) | ✅ `audited` | **Autonomia** — segurança para todos os perfis | [`spec-120/`](./roadmap/spec-120/) |
| 160 | Cupons de Desconto | ✅ `audited` | **Emitir ingressos** — AdminId via JWT, não rota | [`spec-160/`](./roadmap/spec-160/) |
| 130 | Isolamento Multi-Tenant (VendedorId) | ✅ `audited` | **Autonomia** — privacidade entre vendedores | [`spec-130/`](./roadmap/spec-130/) |
| 150 | Resiliência e Tratamento de Erros | ✅ `audited` | **Emitir ingressos** — sistema profissional | [`spec-150/`](./roadmap/spec-150/) |
| 180 | Serviço de E-mail + Redef. de Senha | ✅ `audited` | **Autonomia** — confirmações por e-mail e recuperação de senha | [`spec-180/`](./roadmap/spec-180/) |
| 200 | Guid Id PK de Usuarios (substituir Cpf) | ✅ `audited` | **Autonomia** — vendedor sem CPF, Guid como PK | [`spec-200/`](./roadmap/spec-200/) |

### 🟠 Sprint 2 — Cancelamento e Reembolso

| # | Spec | Status | Problema ([visao.md §2](../visao.md#2-problema)) | Arquivo |
|---|------|--------|-------------------------------------------------|---------|
| 40 | ST-05 Cancelamento de Reserva c/ Reembolso | ✅ `audited` | **Processar cancelamentos** — comprador reembolsado | [`spec-40/`](./roadmap/spec-40/) |
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
| 140 | Infraestrutura e Deploy (Docker) | ✅ `audited` | **Criar eventos** — sistema disponível | [`140-infraestrutura-deploy.md`](./roadmap/140-infraestrutura-deploy.md) |
| 190 | Substituir BackgroundService por Hangfire | ✅ `audited` | **Gerenciar vagas** — confiabilidade na liberação de assentos | [`spec-190/`](./roadmap/spec-190/) |

---

## Cobertura dos 5 Problemas

| Problema ([visao.md §2](../visao.md#2-problema)) | Specs que resolvem | Total |
|---------------------------------------------------|-------------------|-------|
| **Criar e divulgar eventos** de forma simples | 20, 100, 140 | 3 |
| **Gerenciar inscrições e vagas** sem planilhas | 30, 190 | 2 |
| **Emitir e validar ingressos** profissionalmente | 80, 150, 160 | 3 |
| **Processar cancelamentos e reembolsos** | 40, 50, 110 | 3 |
| **Ter autonomia** (cadastrar e vender sem Admin) | 10, 60, 70, 90, 120, 130, 180, 200 | 8 |

---

## Resumo por Status

| Status | Quantidade | Specs |
|--------|-----------|-------|
| ✅ `audited` | 9 | 120, 130, 150, 160, 200, 140, 190, 180, 40 |
| ✅ `implementada` | 9 | ST-01, ST-03, ST-04, ST-07, ST-08, ST-09, ST-10, ST-11, 170 |
| ❌ `pendente` | 2 | ST-06 (50), ST-12 (110) |

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
| ST-05 | `DELETE /api/reserva/{id}` + `CancelarReserva` + transação atômica + e-mail reembolso | ✅ `audited` |
| ST-06 | `DELETE /api/evento/{id}` existe mas sem lógica de reembolso — spec completa em [`spec-50/`](./roadmap/spec-50/) | ❌ `pendente` |
| ST-07 | `ReservaController`: `[Authorize]` sem restrição de role — todos podem | ✅ `implementada` |
| ST-08 | `UsuarioService.Login()`: BCrypt.Verify + Ativo + LoginResponseDTO | ✅ `implementada` |
| ST-09 | `Usuario.cs` com Cnpj, NomeFantasia, Telefone, Plano, Ativo, DataCriacao | ✅ `implementada` |
| ST-10 | `Script0003`: 3 perfis com GUIDs fixos | ✅ `implementada` |
| ST-11 | `Evento.cs`: `Gratuito` (PrecoPadrao == 0), `TipoEvento` (Teatro/Palestra) | ✅ `implementada` |
| ST-12 | Sem endpoint de cancelamento pelo próprio usuário — spec completa em [`spec-110/`](./roadmap/spec-110/) | ❌ `pendente` |
| 120 | BCrypt ✅, RateLimit ✅, Jwt:Key em user-secrets ✅ | ✅ `audited` |
| 130 | `EventoRepository`: filtra por `VendedorCpf` — `ReservaRepository`: `VendedorCpf` adicionado, endpoint `minhas-vendas` implementado | ✅ `audited` |
| 140 | `Api/Dockerfile` + `Web/Dockerfile` + `docker-compose.yml` com SQL Server, health checks e instruções no README — build + testes passando | ✅ `audited` |
| 150 | `GlobalExceptionHandlerMiddleware` sanitizado, `[Authorize]` no IngressoController, DTOs com data annotations + Trim, 8 testes passando | ✅ `audited` |
| 160 | CupomController: AdminId extraído do JWT (claim perfilId), removido de rota/body/DTOs | ✅ `audited` |
| 170 | `PagamentoController` + `PagamentoService` + `PagamentoRepository` + `Script0011` | ✅ `implementada` |
| 180 | Spec auditada — build ✅ 0 erros, 8 arquivos criados, 8 editados, todos FRs verificados | ✅ `audited` |
| 190 | `LiberacaoAssentosJob.cs` com Hangfire recurring job, `LiberacaoAssentosWorker.cs` removido, dashboard `/hangfire` restrito a Admin — build + 112/114 testes passando | ✅ `audited` |
| 200 | Migration Script0013, entities (Usuario/Reserva/Evento) com Guid, repositories/services/controllers atualizados, JWT com claim userId | ✅ `audited` |

---

> **Nota:** ST-02 (Painel do Vendedor) é frontend puro.  
> Para planejamento detalhado de tarefas, consulte [`sprints.md`](../sprints.md).
