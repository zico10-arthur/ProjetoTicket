# Planejamento de Sprints — SoldOut Tickets (v2.0)

> **Fonte:** [`storytelling.md`](./storytelling.md) | **Roadmap:** [`agents/roadmap.md`](./agents/roadmap.md)
> **Specs detalhadas:** [`agents/roadmap/`](./agents/roadmap/)
> **Data:** 16/06/2026

---

## Visão Geral

> 9 specs já implementadas. **8 specs pendentes.** Apenas o que falta fazer.

| Sprint | Duração | Specs | Foco |
|--------|---------|-------|------|
| **Sprint 1** | 1 semana | 120, 160, 130, 150 | 🔴 Segurança e correções críticas |
| **Sprint 2** | 2 semanas | ST-05, ST-06, ST-12 | 🟠 Cancelamento e reembolso |
| **Sprint 3** | 1 semana | 140 | 🟢 Infraestrutura e deploy |
| **Total** | **4 semanas** | **8 specs** | **v2.0 completo** |

---

## Sprint 1 — Segurança e Correções Críticas (1 semana)

> **4 specs:** bugs de segurança que precisam ser corrigidos antes de qualquer feature nova.

---

### 120 — Segurança (BCrypt, JWT, rate limit) 🔴

> **Spec:** [`120-seguranca-autenticacao.md`](./agents/roadmap/120-seguranca-autenticacao.md)
> **Problema:** `Jwt:Key` exposta em `appsettings.json` — qualquer um com acesso ao repositório forja tokens de Admin.

| # | Tarefa | Arquivos | h |
|---|--------|----------|----|
| 120.1 | Mover `Jwt:Key` para `dotnet user-secrets` | `Api/appsettings.json`, `Api/Program.cs` | 0.5 |
| 120.2 | Remover hardcode de `Jwt:Issuer`, `Jwt:Audience` para user-secrets | `Api/appsettings.json` | 0.5 |
| 120.3 | Adicionar `builder.Configuration.AddUserSecrets()` (se não existir) | `Api/Program.cs` | 0.5 |
| 120.4 | Verificar que BCrypt já está funcional e cobrindo todas as operações de senha | `UsuarioService.cs`, `UsuarioRepository.cs` | 1 |
| 120.5 | Testes: login com senha errada, token inválido, chave em produção | `tests/` | 1.5 |

**Subtotal:** ~4h

---

### 160 — Cupons: AdminId via JWT (não rota) 🔴

> **Spec:** [`160-cupons.md`](./agents/roadmap/160-cupons.md)
> **Problema:** `CupomController` recebe `AdminId` como parâmetro de rota. Qualquer usuário logado pode passar um GUID e se passar por Admin.

| # | Tarefa | Arquivos | h |
|---|--------|----------|----|
| 160.1 | `CupomController`: remover `adminId` da rota, extrair CPF do `User.Claims` do JWT | `Api/Controllers/CupomController.cs` | 1.5 |
| 160.2 | `CupomService`: substituir `adminId` por CPF extraído do JWT em todos os métodos | `Application/Service/CupomService.cs` | 2 |
| 160.3 | `ICupomService`: atualizar assinaturas para remover `adminId` dos parâmetros | `Application/Interfaces/ICupomService.cs` | 0.5 |
| 160.4 | `ICupomRepository`: ajustar queries para usar CPF do JWT onde necessário | `Domain/Interface/ICupomRepository.cs` | 1 |
| 160.5 | Testes: verificar que cupons não podem ser criados por não-Admin via JWT | `tests/` | 2 |

**Subtotal:** ~7h

---

### 130 — Isolamento Multi-Tenant (VendedorId) 🔴

> **Spec:** [`130-isolamento-multi-tenant.md`](./agents/roadmap/130-isolamento-multi-tenant.md)
> **Problema:** `ReservaRepository` não filtra por `VendedorCpf`. Um vendedor vê reservas de eventos de outros vendedores.

| # | Tarefa | Arquivos | h |
|---|--------|----------|----|
| 130.1 | `ReservaRepository`: adicionar `WHERE VendedorCpf = @vendedorCpf` nas queries de listagem | `Infraestructure/Repository/ReservaRepository.cs` | 2 |
| 130.2 | `IngressoRepository`: adicionar filtro por `VendedorCpf` nas queries | `Infraestructure/Repository/IngressoRepository.cs` | 1.5 |
| 130.3 | `IReservaRepository`: adicionar `vendedorCpf` nos métodos que listam dados | `Domain/Interface/IReservaRepository.cs` | 1 |
| 130.4 | `ReservaService`: propagar `VendedorCpf` do JWT para queries | `Application/Service/ReservaService.cs` | 1.5 |
| 130.5 | `LiberacaoAssentosWorker`: adicionar filtro por `VendedorId` | `Api/BackgroundTasks/LiberacaoAssentosWorker.cs` | 1 |
| 130.6 | Testes: vendedor X não vê reservas do vendedor Y | `tests/` | 2 |

**Subtotal:** ~9h

---

### 150 — Resiliência e Tratamento de Erros 🟡

> **Spec:** [`150-resiliencia-erros.md`](./agents/roadmap/150-resiliencia-erros.md)
> **Problema:** `GlobalExceptionHandlerMiddleware` expõe `ex.Message` diretamente — stack traces e informações internas vazam em produção.

| # | Tarefa | Arquivos | h |
|---|--------|----------|----|
| 150.1 | `GlobalExceptionHandlerMiddleware`: sanitizar resposta — em produção retornar mensagem genérica, logar detalhes internamente | `Api/Middlewares/GlobalExceptionHandlerMiddleware.cs` | 1.5 |
| 150.2 | Adicionar `ILogger` no middleware para logging server-side | `Api/Middlewares/GlobalExceptionHandlerMiddleware.cs` | 0.5 |
| 150.3 | `ConnectionFactory`: adicionar `Max Pool Size=100` na connection string | `Infraestructure/DataBase/ConnectionFactory.cs` | 0.5 |
| 150.4 | Verificar que todos os controllers têm tratamento adequado de exceções | `Api/Controllers/` | 1 |
| 150.5 | Testes: respostas de erro não expõem dados internos | `tests/` | 1 |

**Subtotal:** ~4.5h

---

### Resumo Sprint 1

| Spec | Carga |
|------|-------|
| 120 — Segurança | ~4h |
| 160 — Cupons | ~7h |
| 130 — Isolamento | ~9h |
| 150 — Resiliência | ~4.5h |
| **Total Sprint 1** | **~24.5h** |

---

## Sprint 2 — Cancelamento e Reembolso (2 semanas)

> **3 specs:** funcionalidades core de cancelamento com reembolso. ST-05 é pré-requisito para ST-06 e ST-12.

---

### ST-05 — Cancelamento de Reserva com Reembolso 🟠

> **Spec:** [`40-st05-cancelamento-reserva.md`](./agents/roadmap/40-st05-cancelamento-reserva.md)
> **Problema:** Comprador não consegue cancelar reserva nem receber reembolso. Funcionalidade ausente.

| # | Tarefa | Arquivos | h |
|---|--------|----------|----|
| 5.1 | `Reserva.cs`: adicionar campo `Reembolsada` (bool) + `MarcarReembolsada()` | `Domain/Entities/Reserva.cs` | 1 |
| 5.2 | `Pagamento.cs`: integrar `MarcarReembolsado()` no fluxo de cancelamento | `Domain/Entities/Pagamento.cs` | 0.5 |
| 5.3 | `ItemReserva.cs`: já tem `Reembolsado` e `MarcarReembolsado()` — verificar | `Domain/Entities/ItemReserva.cs` | 0.5 |
| 5.4 | Script SQL: `ALTER TABLE Reservas ADD Reembolsada BIT DEFAULT 0` | `db/Script0012_*.sql` | 0.5 |
| 5.5 | `ReservaService.CancelarReserva()`: validar DataEvento > agora, verificar se já reembolsada, transação atômica | `Application/Service/ReservaService.cs` | 3 |
| 5.6 | Lógica: evento gratuito → cancela sem reembolso; evento pago → libera ingressos (Status=0), marca Reembolsada, marca Pagamento como Reembolsado | `Application/Service/ReservaService.cs` | 2 |
| 5.7 | `IReservaRepository`: adicionar `CancelarReserva()`, `MarcarReembolsada()` | `Domain/Interface/IReservaRepository.cs` | 1 |
| 5.8 | `ReservaRepository`: implementar transação SQL (UPDATE Reservas + ItensReserva + Ingressos + Pagamentos) | `Infraestructure/Repository/ReservaRepository.cs` | 2.5 |
| 5.9 | `ReservaController`: `DELETE /api/reserva/{id}` com verificação de dono + Admin | `Api/Controllers/ReservaController.cs` | 2 |
| 5.10 | Testes: cancelamento antes/depois do evento, gratuito vs pago, múltiplos itens, reembolso do Pagamento | `tests/` | 4 |

**Subtotal:** ~17h

---

### ST-06 — Cancelamento de Evento com Reembolso Obrigatório 🟠

> **Spec:** [`50-st06-cancelamento-evento.md`](./agents/roadmap/50-st06-cancelamento-evento.md)
> **Problema:** `DELETE /api/evento/{id}` existe mas **não processa reembolso**. Vendedor cancela evento e compradores perdem o dinheiro.

| # | Tarefa | Arquivos | h |
|---|--------|----------|----|
| 6.1 | `GET /api/evento/{id}/status-cancelamento`: endpoint que retorna quantos ingressos vendidos + valor total de reembolso | `Api/Controllers/EventoController.cs` | 1.5 |
| 6.2 | `EventoService.CancelarEvento()`: transação atômica — Cancelado=true, reembolsar todos os ingressos vendidos, marcar Reservas e Pagamentos | `Application/Service/EventoService.cs` | 3 |
| 6.3 | `EventoRepository`: adicionar `MarcarCancelado()`, query de contagem de ingressos vendidos | `Infraestructure/Repository/EventoRepository.cs` | 2 |
| 6.4 | `ReservaRepository`: adicionar `MarcarReembolsadasPorEvento()` | `Infraestructure/Repository/ReservaRepository.cs` | 1.5 |
| 6.5 | `PagamentoRepository`: adicionar `MarcarReembolsadosPorEvento()` | `Infraestructure/Repository/PagamentoRepository.cs` | 1.5 |
| 6.6 | Integrar alerta de confirmação: "X ingressos vendidos. O cancelamento exigirá reembolso." | `Api/Controllers/EventoController.cs` | 1 |
| 6.7 | Testes: cancelamento com/sem ingressos vendidos, gratuito vs pago, transação atômica | `tests/` | 4 |

**Subtotal:** ~14.5h

---

### ST-12 — Cancelamento de Reserva — Visão Unificada 🟠

> **Spec:** [`110-st12-cancelamento-unificado.md`](./agents/roadmap/110-st12-cancelamento-unificado.md)
> **Problema:** Apenas Comprador cancela reserva. Admin e Vendedor também precisam poder cancelar as próprias.

| # | Tarefa | Arquivos | h |
|---|--------|----------|----|
| 12.1 | `ReservaController`: garantir que `DELETE /api/reserva/{id}` aceita qualquer perfil autenticado (já feito em ST-05) | `Api/Controllers/ReservaController.cs` | 0.5 |
| 12.2 | `ReservaController`: `GET /api/reserva/minhas` — retornar itens com flag `Reembolsado` e `Pago` | `Api/Controllers/ReservaController.cs` | 1 |
| 12.3 | `ReservaController`: `GET /api/reserva/evento/{eventoId}` — Vendedor dono do evento ou Admin visualizam todas as reservas | `Api/Controllers/ReservaController.cs` | 1.5 |
| 12.4 | `ReservaDetalhadaDTO`: garantir que `Pago` e `Reembolsada` estão nos responses | `Domain/DTOs/ReservaDetalhadaDTO.cs` | 0.5 |
| 12.5 | Testes: Admin cancela própria reserva, Vendedor cancela própria reserva, Vendedor vê reservas do seu evento | `tests/` | 3 |

**Subtotal:** ~6.5h

---

### Resumo Sprint 2

| Spec | Carga |
|------|-------|
| ST-05 — Cancelamento Reserva | ~17h |
| ST-06 — Cancelamento Evento | ~14.5h |
| ST-12 — Cancelamento Unificado | ~6.5h |
| **Total Sprint 2** | **~38h** |

---

## Sprint 3 — Infraestrutura e Deploy (1 semana)

> **1 spec:** containerização para deploy em produção.

---

### 140 — Infraestrutura e Deploy (Docker) 🟢

> **Spec:** [`140-infraestrutura-deploy.md`](./agents/roadmap/140-infraestrutura-deploy.md)
> **Problema:** Sistema só roda via `dotnet run`. Sem containerização para produção.

| # | Tarefa | Arquivos | h |
|---|--------|----------|----|
| 140.1 | Criar `Dockerfile` para a API (multi-stage build) | `Api/Dockerfile` | 1.5 |
| 140.2 | Criar `Dockerfile` para o Frontend Blazor | `Web/Dockerfile` | 1 |
| 140.3 | Criar `docker-compose.yml` (API + Web + SQL Server) | `docker-compose.yml` | 1.5 |
| 140.4 | Configurar health checks nos containers | `docker-compose.yml` | 0.5 |
| 140.5 | Atualizar `README.md` com instruções de deploy Docker | `README.md` | 1 |
| 140.6 | Testar build e execução dos containers localmente | — | 2 |

**Subtotal:** ~7.5h

---

## Resumo Geral

| Sprint | Specs | Carga |
|--------|-------|-------|
| Sprint 1 — Segurança | 120, 160, 130, 150 | ~24.5h |
| Sprint 2 — Transações | ST-05, ST-06, ST-12 | ~38h |
| Sprint 3 — Infra | 140 | ~7.5h |
| **Total** | **8 specs** | **~70h** |

---

## Ordem de dependência

```
120 (JWT Key) ──┐
160 (Cupons)   ──┤─ Sprint 1: sem dependências entre si
130 (Isolamento)──┤
150 (Resiliência)─┘
                  │
                  ▼
              ST-05 (Cancelamento Reserva) ──┬── ST-06 (Cancelamento Evento)
                  │                          │
                  └──────────────┬───────────┘
                                 ▼
                          ST-12 (Unificado)
                                 │
                                 ▼
                          140 (Docker)
```

---

## Status por Spec

| # | Spec | Status | Sprint |
|---|------|--------|--------|
| 120 | Segurança (BCrypt, JWT, rate limit) | ❌ `pendente` | 1 |
| 160 | Cupons — AdminId via JWT | ❌ `pendente` | 1 |
| 130 | Isolamento Multi-Tenant | ❌ `pendente` | 1 |
| 150 | Resiliência e Tratamento de Erros | ❌ `pendente` | 1 |
| ST-05 | Cancelamento de Reserva c/ Reembolso | ❌ `pendente` | 2 |
| ST-06 | Cancelamento de Evento c/ Reembolso | ❌ `pendente` | 2 |
| ST-12 | Cancelamento — Visão Unificada | ❌ `pendente` | 2 |
| 140 | Infraestrutura e Deploy (Docker) | ❌ `pendente` | 3 |

---

> **Nota:** 9 specs já estão ✅ implementadas (ST-01, ST-03, ST-04, ST-07, ST-08, ST-09, ST-10, ST-11, 170).
> ST-02 (Painel do Vendedor) é frontend puro e está fora do escopo backend.
