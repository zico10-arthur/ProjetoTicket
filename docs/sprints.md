# Planejamento de Sprints — SoldOut Tickets (v2.0) — Backend

## Pontos Críticos Encontrados na Análise do Código

| # | Criticidade | Problema | Arquivo | Impacto |
|---|------------|----------|---------|---------|
| 1 | 🔴 CRÍTICO | Senhas em texto plano (sem BCrypt) | `UsuarioRepository.cs`, `UsuarioService.cs` | Vazamento de senhas |
| 2 | 🔴 CRÍTICO | `CupomController` aceita `AdminId` via rota | `CupomController.cs` | Falsificação de identidade |
| 3 | 🔴 CRÍTICO | `Login()` compara senha sem hash | `UsuarioService.cs` | Autenticação insegura |
| 4 | 🟠 ALTO | `appsettings.json` com `Jwt:Key` exposto | `appsettings.json` | Exposição de chave |
| 5 | 🟠 ALTO | `ReservaController` só aceita "Comprador" | `ReservaController.cs` | Admin/Vendedor não reservam |
| 6 | 🟠 ALTO | `CadastrarVendedor` exige Admin logado | `UsuarioController.cs`, `UsuarioService.cs` | Contraria ST-01 |
| 7 | 🟡 MÉDIO | `IngressoController` sem `[Authorize]` | `IngressoController.cs` | Endpoint público |
| 8 | 🟡 MÉDIO | Connection string sem `Max Pool Size` | `appsettings.json` | Esgotamento de conexões |
| 9 | 🟡 MÉDIO | Background Worker sem `VendedorId` | `LiberacaoAssentosWorker.cs` | Isolamento de dados |

---

## Visão Geral

| Sprint | Duração | Foco |
|--------|---------|------|
| **Sprint 1** | 2 semanas | 🔒 Segurança + Fundação (Domain, Application, Infra) |
| **Sprint 2** | 2 semanas | Reembolso + API + Testes + Deploy |

**Time:** 4 desenvolvedores (apenas backend)

---

## Sprint 1 — Segurança e Fundação (2 semanas)

---

### DEV 1: 🔒 BCrypt + Auto Cadastro de Vendedor

| ID | Tarefa | Corrige # | Arquivos | História | h |
|----|--------|----------|----------|----------|----|
| 1.1 | Instalar **BCrypt.Net-Next** via NuGet | 1, 3 | `Application.csproj` | ST-01 | 0.5 |
| 1.2 | `UsuarioRepository.CadastrarUsuario()`: aplicar `BCrypt.HashPassword(senha)` antes do INSERT | 1 | `UsuarioRepository.cs` | ST-01 | 1.5 |
| 1.3 | `UsuarioService.Login()`: verificar senha com `BCrypt.Verify()` | 1, 3 | `UsuarioService.cs` | ST-08 | 1 |
| 1.4 | Script SQL seed: hash BCrypt na senha do Admin | 1 | SQL seed | ST-01 | 0.5 |
| 1.5 | `Usuario.cs`: adicionar Cnpj, NomeFantasia, Telefone, LogoUrl, Descricao, Site, Ativo, DataCriacao | - | `Usuario.cs` | ST-09 | 2.5 |
| 1.6 | `CadastrarVendedorDTO.cs` (CNPJ, RazãoSocial, NomeFantasia, Email, Senha, Telefone) | - | `Application/DTOs/` | ST-01 | 1 |
| 1.7 | `UsuarioService.CadastrarVendedor()`: **remover Admin logado**, validar CNPJ (dígitos), BCrypt | 6 | `UsuarioService.cs` | ST-01 | 4 |
| 1.8 | `CnpjValidator.Validar()` no Domain (dígitos verificadores) | - | `Domain/` | ST-01 | 2 |
| 1.9 | `UsuarioController`: endpoint `POST /api/usuario/cadastrar-vendedor` (público) | 6 | `UsuarioController.cs` | ST-01 | 1 |
| 1.10 | Script SQL: `ALTER TABLE Usuarios` (Cnpj, NomeFantasia, etc.) | - | SQL migration | ST-09 | 1 |
| 1.11 | `UsuarioRepository`: `BuscarCnpj()`, atualizar `CadastrarUsuario()` | - | `UsuarioRepository.cs` | ST-01, ST-09 | 3 |
| 1.12 | Script SQL seed: 3 perfis (Admin A1A1..., Vendedor B2B2..., Comprador C3C3...) | - | SQL seed | ST-10 | 0.5 |
| 1.13 | **Mover `Jwt:Key` para `dotnet user-secrets`** | 4 | `appsettings.json` | - | 0.5 |
| 1.14 | Testes unitários: BCrypt, CNPJ, auto cadastro, unicidade | - | `tests/` | ST-01 | 4 |

**Total DEV 1:** ~23h

---

### DEV 2: 🔒 CupomController + ItemReserva + Banco

| ID | Tarefa | Corrige # | Arquivos | História | h |
|----|--------|----------|----------|----------|----|
| 2.1 | **Corrigir `CupomController`**: AdminId via JWT (`User.Claims`), não rota | 2 | `CupomController.cs` | - | 2 |
| 2.2 | `CupomService`: extrair AdminId do JWT | 2 | `CupomService.cs` | - | 2 |
| 2.3 | `ItemReserva.cs` (Id, ReservaId, CpfParticipante, IngressoId?, PrecoUnitario, Reembolsado) | - | `Domain/Entities/` | ST-04 | 2 |
| 2.4 | `Reserva.cs`: remover IngressoId/Quantidade, adicionar `Itens`, `PodeAdicionarMaisItens` | - | `Domain/Entities/` | ST-04 | 3 |
| 2.5 | `Reserva.Criar()`: factory method com lista de `ItemReserva` | - | `Domain/Entities/` | ST-04 | 2 |
| 2.6 | SQL: `ItensReserva` + `ALTER TABLE Reservas` + `ALTER TABLE Cupons ADD VendedorId` | - | SQL migration | ST-04 | 2.5 |
| 2.7 | `IReservaRepository` + `ReservaRepository`: ItemReserva, `CancelarReserva()`, `ReembolsarReserva()` | - | `Infraestructure/` | ST-04, ST-05 | 3.5 |
| 2.8 | `ItemReservaDTO`, `ReservaRequestDTO`, `ReservaProfile` (AutoMapper) | - | `Application/` | ST-04 | 1.5 |
| 2.9 | Registrar `ReservaProfile` no `Program.cs` | - | `Api/Program.cs` | ST-04 | 0.5 |
| 2.10 | Testes: CupomController JWT, ItemReserva 1-4 itens | - | `tests/` | ST-04 | 4 |

**Total DEV 2:** ~23h

---

### DEV 3: TipoEvento + Evento Gratuito + VendedorId + Isolamento

| ID | Tarefa | Corrige # | Arquivos | História | h |
|----|--------|----------|----------|----------|----|
| 3.1 | `TipoEvento.cs` (enum: Teatro=0, Palestra=1) | - | `Domain/Entities/` | ST-03, ST-11 | 1 |
| 3.2 | `Evento.cs`: VendedorId, Tipo, Descricao, Local, ImagemUrl, Cancelado, DataCriacao, `Gratuito` | - | `Domain/Entities/` | ST-03, ST-11 | 3 |
| 3.3 | `GerarLoteIngressos()`: só se `Tipo == Teatro` | - | `Domain/Entities/` | ST-03, ST-11 | 2 |
| 3.4 | SQL: `ALTER TABLE Eventos` (VendedorId, Descricao, Local, ImagemUrl, Tipo, DataCriacao, Cancelado) | - | SQL migration | ST-03 | 2 |
| 3.5 | `EventoService`: filtrar por VendedorId, suportar Tipo, lógica gratuito | - | `Application/` | ST-03, ST-11 | 3 |
| 3.6 | `LiberacaoAssentosWorker`: filtro por VendedorId | 9 | `Api/BackgroundTasks/` | - | 2 |
| 3.7 | `EventoRequestDTO` + `EventoResponseDTO`: Tipo, Descricao, Local, ImagemUrl | - | `Application/DTOs/` | ST-03, ST-11 | 1 |
| 3.8 | `IEventoRepository` + `EventoRepository`: queries com VendedorId | - | `Infraestructure/` | ST-03 | 3 |
| 3.9 | `Max Pool Size=100` na connection string | 8 | `appsettings.json` | - | 0.5 |
| 3.10 | Sanitização de inputs (Trim + validação XSS) | - | DTOs | - | 1.5 |
| 3.11 | Testes: Palestra sem ingressos, Teatro com, gratuito, VendedorId | - | `tests/` | ST-03, ST-11 | 4 |

**Total DEV 3:** ~23h

---

### DEV 4: 🔒 ReservaController + ReservaService Multi-Participante

| ID | Tarefa | Corrige # | Arquivos | História | h |
|----|--------|----------|----------|----------|----|
| 4.1 | `ReservaController`: `[Authorize(Roles = "Comprador")]` → `[Authorize]` | 5 | `ReservaController.cs` | ST-07, ST-12 | 1 |
| 4.2 | `ReservaService.FazerReserva()`: lista de ItemReserva, validar CPFs, limite 4 | - | `Application/` | ST-04 | 4 |
| 4.3 | `CpfValidator` no Domain (estático, reutilizável) | - | `Domain/` | ST-04 | 2 |
| 4.4 | Lógica Palestra: IngressoId=null, vagas por `SUM(Quantidade)` | - | `Application/` | ST-03 | 3 |
| 4.5 | Cupom sobre valor total (soma PrecoUnitario) | - | `Application/` | ST-04 | 2 |
| 4.6 | Anti-cambista: duplicidade por CpfParticipante no evento | - | `Application/` | ST-07 | 2 |
| 4.7 | **Rate limiting**: middleware `POST /api/usuario/login` (5/min/IP) | - | `Api/Middlewares/` | ST-01 | 3 |
| 4.8 | `[Authorize]` no `IngressoController` | 7 | `IngressoController.cs` | - | 0.5 |
| 4.9 | `ReservaController`: `POST /api/reserva/criar` com lista de itens | - | `ReservaController.cs` | ST-04 | 2 |
| 4.10 | Testes: multi-item, limite 4, cupom total, anti-cambista, rate limit | - | `tests/` | ST-04 | 4 |

**Total DEV 4:** ~23.5h

---

### Resumo Sprint 1

| Dev | Carga | Principais Entregas |
|-----|-------|---------------------|
| DEV 1 | ~23h | BCrypt, Auto cadastro Vendedor, CNPJ, user-secrets |
| DEV 2 | ~23h | CupomController JWT, ItemReserva, banco, AutoMapper |
| DEV 3 | ~23h | TipoEvento, Evento Gratuito, Worker, Max Pool, sanitização |
| DEV 4 | ~23.5h | ReservaController, Reserva multi-item, rate limiting, anti-cambista |

**Total Sprint 1:** ~92.5h

---

## Sprint 2 — Reembolso, API e Testes (2 semanas)

---

### DEV 1: Cancelamento de Evento + Reembolso

| ID | Tarefa | Arquivos | História | h |
|----|--------|----------|----------|----|
| 5.1 | `EventoService.CancelarEvento()`: validar ingressos vendidos, alerta, transação | `Application/` | ST-06 | 4 |
| 5.2 | `Evento.Cancelar()`: método de domínio (Cancelado=true) | `Domain/Entities/` | ST-06 | 1 |
| 5.3 | `ReservaService.ReembolsarPorCancelamentoEvento()`: Ingressos Status=3, Reservas/Itens reembolsados | `Application/` | ST-06 | 3 |
| 5.4 | `IngressoRepository.ReembolsarIngressosPorEvento()` | `Infraestructure/` | ST-06 | 2 |
| 5.5 | `EventoController`: `DELETE /api/evento/{id}` com verificação reembolso | `Api/Controllers/` | ST-06 | 2 |
| 5.6 | Testes: cancelamento com/sem ingressos, gratuito vs pago, transação | `tests/` | ST-06 | 4 |

**Total DEV 1:** ~16h

---

### DEV 2: Cancelamento de Reserva + Reembolso por Item

| ID | Tarefa | Arquivos | História | h |
|----|--------|----------|----------|----|
| 6.1 | `ReservaService.CancelarReserva()`: validar DataEvento > agora, já reembolsada? | `Application/` | ST-05 | 3 |
| 6.2 | Reembolso por item: ItemReserva Reembolsado=true, Ingresso Status=0 | `Application/` | ST-05 | 3 |
| 6.3 | `ReservaController`: `DELETE /api/reserva/{id}` (dono, qualquer perfil) | `Api/Controllers/` | ST-05, ST-12 | 2 |
| 6.4 | `ReservaRepository`: `CancelarReserva()`, `BuscarReservaPorId()` | `Infraestructure/` | ST-05 | 2 |
| 6.5 | Evento gratuito: cancelamento sem reembolso | `Application/` | ST-05 | 1 |
| 6.6 | Testes: antes/depois evento, gratuito vs pago, múltiplos itens | `tests/` | ST-05 | 4 |

**Total DEV 2:** ~15h

---

### DEV 3: API — Endpoints e Controllers (Refatoração Final)

| ID | Tarefa | Arquivos | História | h |
|----|--------|----------|----------|----|
| 7.1 | Revisar todos os `[Authorize]` nos Controllers | `Api/Controllers/` | - | 2 |
| 7.2 | `EventoController`: endpoint `GET /api/evento/meus` (VendedorId do JWT) | `Api/Controllers/` | ST-02 | 2 |
| 7.3 | `UsuarioController`: `PUT /api/usuario/atualizar-vendedor` (LogoUrl, Descricao, Site) | `Api/Controllers/` | ST-02 | 2 |
| 7.4 | `UsuarioController`: `GET /api/admin/vendedores` (listar todos) | `Api/Controllers/` | ST-02 | 1.5 |
| 7.5 | `UsuarioController`: `PUT /api/admin/vendedor/{cpf}/ativar` | `Api/Controllers/` | ST-02 | 1.5 |
| 7.6 | `CupomController`: garantir que endpoints de Admin funcionam com JWT | `Api/Controllers/` | - | 2 |
| 7.7 | `ReservaController`: `GET /api/reserva/evento/{eventoId}` (Vendedor/Admin) | `Api/Controllers/` | ST-02 | 2 |
| 7.8 | `Program.cs`: registrar novos serviços + rate limit middleware | `Api/Program.cs` | - | 1 |
| 7.9 | Testes de integração: todos os endpoints com JWT | `tests/` | - | 4 |

**Total DEV 3:** ~18h

---

### DEV 4: Testes + Deploy + Documentação

| ID | Tarefa | Arquivos | História | h |
|----|--------|----------|----------|----|
| 8.1 | Script SQL de migração consolidado (Sprint 1 + 2) | SQL | - | 2 |
| 8.2 | Atualizar `DatabaseMigration.cs`: ordem dos scripts | `Infraestructure/` | - | 1 |
| 8.3 | Testes de integração: fluxo completo compra multi-item + cancelamento | `tests/` | ST-04, ST-05 | 4 |
| 8.4 | Testes E2E: cadastro Vendedor → evento → compra → cancelamento | `tests/` | Todos | 4 |
| 8.5 | Atualizar `README.md`: BCrypt, user-secrets, novos endpoints | `README.md` | - | 1 |
| 8.6 | `docker-compose.yml` para API + SQL Server | Raiz | - | 2 |
| 8.7 | Verificar cobertura de testes (todos os Services e Controllers) | `tests/` | - | 2 |

**Total DEV 4:** ~16h

---

### 📊 Infraestrutura Compartilhada (Sprint 2)

| ID | Tarefa | Responsável | h |
|----|--------|-------------|----|
| INF-01 | Gerar hash BCrypt para senha Admin existente (script UPDATE) | DEV 1 | 0.5 |
| INF-02 | Verificar `dotnet user-secrets` configurado em todas as máquinas | DEV 1 | 0.5 |

---

### Resumo Sprint 2

| Dev | Carga | Principais Entregas |
|-----|-------|---------------------|
| DEV 1 | ~17h | Cancelamento evento, reembolso obrigatório, transações |
| DEV 2 | ~15h | Cancelamento reserva, reembolso por item, endpoint DELETE |
| DEV 3 | ~18h | Refatoração controllers, endpoints Vendedor, Program.cs |
| DEV 4 | ~16h | Testes integração/E2E, deploy, documentação |

**Total Sprint 2:** ~66h

---

## Resumo Geral

| Sprint | Duração | Carga Total | Foco |
|--------|---------|-------------|------|
| Sprint 1 | Semanas 1-2 | ~92.5h | 🔒 Segurança + Fundação (9 pontos críticos) |
| Sprint 2 | Semanas 3-4 | ~66h | Reembolso + API + Testes + Deploy |
| **Total** | **4 semanas** | **~158.5h** | **Backend v2.0 completo** |

---

## Checklist de Pontos Críticos

| # | Ponto Crítico | Sprint | Dev | Status |
|---|---------------|--------|-----|--------|
| 1 | Senhas em texto plano → BCrypt | Sprint 1 | DEV 1 | ✅ |
| 2 | CupomController AdminId via rota → JWT | Sprint 1 | DEV 2 | ✅ |
| 3 | Login sem hash → BCrypt.Verify() | Sprint 1 | DEV 1 | ✅ |
| 4 | Jwt:Key exposto → user-secrets | Sprint 1 | DEV 1 | ✅ |
| 5 | ReservaController só Comprador → todos perfis | Sprint 1 | DEV 4 | ✅ |
| 6 | CadastrarVendedor exige Admin → auto cadastro | Sprint 1 | DEV 1 | ✅ |
| 7 | IngressoController sem [Authorize] | Sprint 1 | DEV 4 | ✅ |
| 8 | Sem Max Pool Size → 100 | Sprint 1 | DEV 3 | ✅ |
| 9 | Background Worker sem VendedorId | Sprint 1 | DEV 3 | ✅ |

---

> **Documento v3.0** — Sprints apenas do backend. Sem frontend.
>
> ⚠️ **Nota:** Carga estimada para 4 devs backend em regime acadêmico/part-time (~12h/semana).
> Cupons sempre gerenciados por Admins. Sem planos de Vendedor.