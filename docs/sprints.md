# Planejamento de Sprints — SoldOut Tickets (v2.0)

> **Fonte:** [`storytelling.md`](./storytelling.md) — 11 specs backend  
> **Specs detalhadas:** [`agents/roadmap/`](./agents/roadmap/)  
> **Status:** [`agents/roadmap.md`](./agents/roadmap.md)  
> **Time:** 4 desenvolvedores backend (~12h/semana cada)

---

## Visão Geral

| Sprint | Duração | Specs | Foco |
|--------|---------|-------|------|
| **Sprint 1** | 2 semanas | ST-01, ST-03, ST-04, ST-08, ST-09, ST-10, ST-11 | Fundação: cadastro, eventos, reservas |
| **Sprint 2** | 2 semanas | ST-05, ST-06, ST-07, ST-12 | Transações: cancelamento e reembolso |
| **Total** | **4 semanas** | **11 specs** | **Backend v2.0 completo** |

---

## Pontos Críticos (corrigidos nas sprints)

| # | Criticidade | Problema | Spec que resolve |
|---|------------|----------|-----------------|
| 1 | 🔴 CRÍTICO | Senhas em texto plano (sem BCrypt) | [120](./agents/roadmap/120-seguranca-autenticacao.md) |
| 2 | 🔴 CRÍTICO | `CupomController` aceita `AdminId` via rota | [10](./agents/roadmap/10-st01-auto-cadastro-vendedor.md) |
| 3 | 🔴 CRÍTICO | `Login()` compara senha sem hash | [120](./agents/roadmap/120-seguranca-autenticacao.md) |
| 4 | 🟠 ALTO | `appsettings.json` com `Jwt:Key` exposto | [120](./agents/roadmap/120-seguranca-autenticacao.md) |
| 5 | 🟠 ALTO | `ReservaController` só aceita "Comprador" | [60](./agents/roadmap/60-st07-admin-vendedor-reservas.md) |
| 6 | 🟠 ALTO | `CadastrarVendedor` exige Admin logado | [10](./agents/roadmap/10-st01-auto-cadastro-vendedor.md) |
| 7 | 🟡 MÉDIO | `IngressoController` sem `[Authorize]` | [150](./agents/roadmap/150-resiliencia-erros.md) |
| 8 | 🟡 MÉDIO | Connection string sem `Max Pool Size` | [140](./agents/roadmap/140-infraestrutura-deploy.md) |
| 9 | 🟡 MÉDIO | Background Worker sem `VendedorId` | [130](./agents/roadmap/130-isolamento-multi-tenant.md) |

---

## Sprint 1 — Fundação (2 semanas)

> **7 specs:** cadastro, login, entidades, eventos, reserva multi-participante

### ST-08 — Login Unificado `em revisão`

| # | Tarefa | Arquivos | Corrige | h |
|---|--------|----------|---------|----|
| 8.1 | Instalar BCrypt.Net-Next via NuGet | `Application.csproj` | #1, #3 | 0.5 |
| 8.2 | `UsuarioService.Login()`: verificar senha com `BCrypt.Verify()` | `UsuarioService.cs` | #1, #3 | 1 |
| 8.3 | `UsuarioRepository`: aplicar `BCrypt.HashPassword()` no INSERT | `UsuarioRepository.cs` | #1 | 1.5 |
| 8.4 | Script SQL seed Admin com hash BCrypt | SQL seed | #1 | 0.5 |
| 8.5 | Mover `Jwt:Key` para `dotnet user-secrets` | `appsettings.json` | #4 | 0.5 |
| 8.6 | `TokenService`: gerar JWT com role (Admin/Vendedor/Comprador) | `TokenService.cs` | — | 1.5 |
| 8.7 | Remover endpoint `/api/empresa/login` (se existir) | `UsuarioController.cs` | — | 0.5 |

**Subtotal:** ~6h

### ST-09 — Vendedor como Perfil na Tabela Usuarios `em revisão`

| # | Tarefa | Arquivos | Corrige | h |
|---|--------|----------|---------|----|
| 9.1 | `Usuario.cs`: adicionar Cnpj, NomeFantasia, Telefone, LogoUrl, Descricao, Site, Plano, DataCriacao | `Domain/Entities/Usuario.cs` | — | 2.5 |
| 9.2 | Script SQL: `ALTER TABLE Usuarios` (8 colunas novas) | SQL migration | — | 1 |
| 9.3 | Migrar dados da tabela `Empresas` → `Usuarios` (se aplicável) | SQL migration | — | 1 |
| 9.4 | Atualizar `UsuarioRepository` com novos campos | `UsuarioRepository.cs` | — | 2 |

**Subtotal:** ~6.5h

### ST-10 — Perfis Simplificados `implementada`

| # | Tarefa | Arquivos | Corrige | h |
|---|--------|----------|---------|----|
| 10.1 | Script SQL seed: 3 perfis com GUIDs fixos | SQL seed | — | 0.5 |
| 10.2 | Garantir `PerfilId` correto no cadastro (Comprador → C3C3..., Vendedor → B2B2...) | `UsuarioService.cs` | — | 1 |
| 10.3 | Factory methods: `CriarComprador()`, `CriarVendedor()` | `Domain/Entities/Usuario.cs` | — | 1 |

**Subtotal:** ~2.5h

### ST-01 — Auto Cadastro de Vendedor `pendente`

| # | Tarefa | Arquivos | Corrige | h |
|---|--------|----------|---------|----|
| 1.1 | `CadastrarVendedorDTO.cs` (CNPJ, RazaoSocial, NomeFantasia, Email, Senha, Telefone) | `Application/DTOs/` | — | 1 |
| 1.2 | `CnpjValidator.Validar()` no Domain (dígitos verificadores) | `Domain/` | — | 2 |
| 1.3 | `UsuarioService.CadastrarVendedor()`: validar CNPJ, BCrypt, PerfilId=Vendedor, Plano=Gratuito | `UsuarioService.cs` | #6 | 4 |
| 1.4 | `UsuarioRepository`: `BuscarPorCnpj()`, validar unicidade CNPJ e Email | `UsuarioRepository.cs` | — | 2 |
| 1.5 | `UsuarioController`: `POST /api/usuario/cadastrar-vendedor` (público) | `UsuarioController.cs` | #6 | 1 |
| 1.6 | Rate limiting: middleware `POST /api/usuario/login` (5/min/IP) | `Api/Middlewares/` | — | 3 |
| 1.7 | Testes: BCrypt, CNPJ, auto cadastro, unicidade | `tests/` | — | 4 |

**Subtotal:** ~17h

### ST-03 + ST-11 — Tipo de Evento + Palestras com Assentos + Gratuito `pendente` / `em revisão`

| # | Tarefa | Arquivos | Corrige | h |
|---|--------|----------|---------|----|
| 3.1 | `TipoEvento.cs` (enum: Teatro=0, Palestra=1) | `Domain/Entities/` | — | 0.5 |
| 3.2 | `Evento.cs`: VendedorId, Tipo, Descricao, Local, Cancelado, `Gratuito` (PrecoPadrao==0) | `Domain/Entities/` | — | 3 |
| 3.3 | `GerarLoteIngressos()`: **ambos os tipos** — Palestra (Assento 1..N, Geral), Teatro (VIP/Geral, filas de 20) | `Domain/Entities/` | — | 3 |
| 3.4 | SQL: `ALTER TABLE Eventos` (VendedorId, Tipo, Descricao, Local, Cancelado) | SQL migration | — | 2 |
| 3.5 | `EventoService.CriarEvento()`: suportar Tipo, lógica gratuito (pula pagamento) | `Application/Service/` | — | 3 |
| 3.6 | `EventoRequestDTO` + `EventoResponseDTO`: Tipo, Descricao, Local | `Application/DTOs/` | — | 1 |
| 3.7 | `IEventoRepository` + `EventoRepository`: queries com VendedorId | `Infraestructure/` | — | 3 |
| 3.8 | `Max Pool Size=100` na connection string | `appsettings.json` | #8 | 0.5 |
| 3.9 | Testes: Palestra e Teatro com assentos, gratuito vs pago, VendedorId | `tests/` | — | 4 |

**Subtotal:** ~20h

### ST-04 — Reserva Multi-Participante (ItemReserva) `pendente`

| # | Tarefa | Arquivos | Corrige | h |
|---|--------|----------|---------|----|
| 4.1 | `ItemReserva.cs`: Id, ReservaId, CpfParticipante, IngressoId (NOT NULL), PrecoUnitario, Reembolsado | `Domain/Entities/` | — | 2 |
| 4.2 | `Reserva.cs`: remover IngressoId/Quantidade, adicionar `Itens` (List<ItemReserva>), `PodeAdicionarMaisItens` | `Domain/Entities/` | — | 3 |
| 4.3 | SQL: `CREATE TABLE ItensReserva` + `ALTER TABLE Reservas` | SQL migration | — | 2.5 |
| 4.4 | `ReservaService.FazerReserva()`: lista de ItemReserva, validar CPFs (não precisam ser cadastrados), limite 4 | `Application/Service/` | — | 4 |
| 4.5 | `CpfValidator` no Domain (estático, reutilizável) | `Domain/` | — | 1.5 |
| 4.6 | Anti-cambista: um CPF não pode ter mais de uma reserva ativa no mesmo evento | `Application/Service/` | — | 2 |
| 4.7 | Cupom sobre valor total: `SUM(PrecoUnitario)` de todos os itens | `Application/Service/` | — | 1.5 |
| 4.8 | Cupom não aplicável em evento gratuito | `Application/Service/` | — | 0.5 |
| 4.9 | `ReservaController`: `POST /api/reserva/criar` com lista de itens | `Api/Controllers/` | — | 2 |
| 4.10 | `ItemReservaDTO`, `ReservaRequestDTO`, `ReservaProfile` (AutoMapper) | `Application/` | — | 1.5 |
| 4.11 | Testes: multi-item, limite 4, cupom total, anti-cambista, CPFs não cadastrados | `tests/` | — | 4 |

**Subtotal:** ~24.5h

---

### Resumo Sprint 1

| Spec | Status | Carga |
|------|--------|-------|
| ST-08 Login Unificado | `em revisão` | ~6h |
| ST-09 Vendedor na tabela Usuarios | `em revisão` | ~6.5h |
| ST-10 Perfis Simplificados | `implementada` | ~2.5h |
| ST-01 Auto Cadastro de Vendedor | `pendente` | ~17h |
| ST-03 + ST-11 Tipo Evento + Gratuito | `pendente` / `em revisão` | ~20h |
| ST-04 ItemReserva | `pendente` | ~24.5h |
| **Total Sprint 1** | | **~76.5h** |

---

## Sprint 2 — Transações (2 semanas)

> **4 specs:** cancelamento de reserva, cancelamento de evento, expansão de autorização, visão unificada

### ST-05 — Cancelamento de Reserva com Reembolso `pendente`

| # | Tarefa | Arquivos | Corrige | h |
|---|--------|----------|---------|----|
| 5.1 | `ReservaService.CancelarReserva()`: validar DataEvento > agora, já reembolsada? | `Application/Service/` | — | 3 |
| 5.2 | Lógica de reembolso: Evento.Gratuito → sem reembolso; Evento pago → libera ingressos (Status=0), marca Reembolsada=true | `Application/Service/` | — | 3 |
| 5.3 | `ItemReserva`: cada item marcado Reembolsado=true individualmente | `Application/Service/` | — | 1.5 |
| 5.4 | `ReservaRepository`: `CancelarReserva()`, `BuscarReservaPorId()` | `Infraestructure/` | — | 2 |
| 5.5 | `IngressoRepository.LiberarIngressosDaReserva()` | `Infraestructure/` | — | 1.5 |
| 5.6 | `ReservaController`: `DELETE /api/reserva/{id}` (dono, qualquer perfil) | `Api/Controllers/` | — | 2 |
| 5.7 | Testes: antes/depois evento, gratuito vs pago, múltiplos itens | `tests/` | — | 4 |

**Subtotal:** ~17h

### ST-06 — Cancelamento de Evento com Reembolso Obrigatório `pendente`

| # | Tarefa | Arquivos | Corrige | h |
|---|--------|----------|---------|----|
| 6.1 | `EventoService.CancelarEvento()`: validar ingressos vendidos, alerta de reembolso, transação atômica | `Application/Service/` | — | 4 |
| 6.2 | `Evento.Cancelar()`: método de domínio (Cancelado=true) | `Domain/Entities/` | — | 1 |
| 6.3 | `GET /api/evento/{id}/status-cancelamento`: retorna contagem de ingressos vendidos + valor total de reembolso | `Api/Controllers/` | — | 1.5 |
| 6.4 | `ReservaService.ReembolsarPorCancelamentoEvento()`: Status=3, Reservas/Itens reembolsados | `Application/Service/` | — | 2 |
| 6.5 | `IngressoRepository.ReembolsarIngressosPorEvento()` | `Infraestructure/` | — | 1.5 |
| 6.6 | `EventoController`: `DELETE /api/evento/{id}` com verificação de reembolso | `Api/Controllers/` | — | 2 |
| 6.7 | `LiberacaoAssentosWorker`: filtro por VendedorId | `Api/BackgroundTasks/` | #9 | 2 |
| 6.8 | Testes: cancelamento com/sem ingressos, gratuito vs pago, transação atômica | `tests/` | — | 4 |

**Subtotal:** ~18h

### ST-07 — Admin e Vendedor Podem Fazer Reservas `pendente`

| # | Tarefa | Arquivos | Corrige | h |
|---|--------|----------|---------|----|
| 7.1 | `ReservaController`: `[Authorize(Roles = "Comprador")]` → `[Authorize]` (todos os perfis) | `ReservaController.cs` | #5 | 1 |
| 7.2 | `ReservaController`: `GET /api/reserva/minhas` — filtrar por `UsuarioCpf` do JWT (funciona para Admin, Vendedor, Comprador) | `ReservaController.cs` | #5 | 1.5 |
| 7.3 | `[Authorize]` no `IngressoController` | `IngressoController.cs` | #7 | 0.5 |
| 7.4 | `CupomController`: AdminId via JWT (`User.Claims`), não rota | `CupomController.cs` | #2 | 2 |
| 7.5 | `CupomService`: extrair VendedorId do JWT | `CupomService.cs` | #2 | 1.5 |
| 7.6 | Testes: Admin e Vendedor fazendo reservas, CupomController JWT | `tests/` | — | 3 |

**Subtotal:** ~9.5h

### ST-12 — Cancelamento de Reserva — Visão Unificada `pendente`

| # | Tarefa | Arquivos | Corrige | h |
|---|--------|----------|---------|----|
| 12.1 | `ReservaController`: `DELETE /api/reserva/{id}` — qualquer perfil cancela suas próprias reservas | `ReservaController.cs` | #5 | 1.5 |
| 12.2 | `ReservaController`: `GET /api/reserva/minhas` — retorna itens com flag `Reembolsado` individual | `ReservaController.cs` | — | 1.5 |
| 12.3 | `ReservaController`: `GET /api/reserva/evento/{eventoId}` (Vendedor dono ou Admin) | `ReservaController.cs` | — | 1.5 |
| 12.4 | Testes de integração: fluxo completo compra multi-item + cancelamento unificado | `tests/` | — | 3 |

**Subtotal:** ~7.5h

---

### Resumo Sprint 2

| Spec | Status | Carga |
|------|--------|-------|
| ST-05 Cancelamento de Reserva | `pendente` | ~17h |
| ST-06 Cancelamento de Evento | `pendente` | ~18h |
| ST-07 Admin/Vendedor fazem reserva | `pendente` | ~9.5h |
| ST-12 Cancelamento Unificado | `pendente` | ~7.5h |
| **Total Sprint 2** | | **~52h** |

---

## Infraestrutura Compartilhada

| # | Tarefa | Sprint | h |
|---|--------|--------|----|
| INF-01 | Script SQL de migração consolidado | Sprint 2 | 2 |
| INF-02 | `docker-compose.yml` para API + SQL Server | Sprint 2 | 2 |
| INF-03 | Atualizar `README.md` com novos endpoints e configurações | Sprint 2 | 1 |
| INF-04 | Verificar cobertura de testes (>80%) | Sprint 2 | 2 |
| **Total Infra** | | | **~7h** |

---

## Resumo Geral

| Sprint | Specs | Carga |
|--------|-------|-------|
| Sprint 1 — Fundação | ST-01, ST-03, ST-04, ST-08, ST-09, ST-10, ST-11 | ~76.5h |
| Sprint 2 — Transações | ST-05, ST-06, ST-07, ST-12 | ~52h |
| Infraestrutura | — | ~7h |
| **Total** | **11 specs** | **~135.5h** |

---

## Status por Spec

| # | Spec | Status | Sprint |
|---|------|--------|--------|
| ST-01 | Auto Cadastro de Vendedor | `pendente` | 1 |
| ST-03 | Palestras com assentos numerados | `pendente` | 1 |
| ST-04 | ItemReserva (até 4 CPFs) | `pendente` | 1 |
| ST-05 | Cancelamento de Reserva com Reembolso | `pendente` | 2 |
| ST-06 | Cancelamento de Evento com Reembolso | `pendente` | 2 |
| ST-07 | Admin e Vendedor fazem reservas | `pendente` | 2 |
| ST-08 | Login Unificado | `em revisão` | 1 |
| ST-09 | Vendedor na tabela Usuarios | `em revisão` | 1 |
| ST-10 | Perfis Simplificados (3 perfis) | `implementada` | 1 |
| ST-11 | Tipo Evento + Gratuito | `em revisão` | 1 |
| ST-12 | Cancelamento Unificado | `pendente` | 2 |

---

> **Nota:** Carga estimada para 4 devs backend em regime acadêmico/part-time (~12h/semana).  
> ST-02 (Painel do Vendedor) é frontend puro e está fora do escopo backend.  
> Specs detalhadas em [`agents/roadmap/`](./agents/roadmap/).
