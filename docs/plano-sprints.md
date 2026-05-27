# Plano de Sprints — Refatoração SaaS (Backend)

> **Equipe:** 4 desenvolvedores (Dev A, Dev B, Dev C, Dev D)
> **Duração por sprint:** 2 semanas
> **Total:** 4 sprints (~8 semanas)
> **Escopo:** Exclusivamente backend (Domain, Application, API, Infrastructure)
> **Nenhuma tarefa de frontend está incluída neste plano.**

---

## Critérios de Priorização

| Prioridade | Critério |
|------------|----------|
| 🔴 **Alta** | Bloqueante para outras tarefas (ex: entidades de domínio, SQL, interfaces) |
| 🟡 **Média** | Dependente de tarefas anteriores, mas sem bloqueio crítico |
| 🟢 **Baixa** | Melhorias, segurança, testes |

---

## Sprint 1 — Fundação: Domain + Infraestrutura

**Objetivo:** Criar todas as novas entidades de domínio, enums, interfaces de repositório e scripts de migração SQL. Atualizar entidades existentes.

### Tarefas

#### Dev A — Criação de Novas Entidades de Domínio

| # | Tarefa | Arquivo | Esforço | Dependências |
|---|--------|---------|---------|--------------|
| 1.1 | Criar entidade `Empresa` com propriedades, factory e validações (CNPJ, email, senha) | [`Domain/Entities/Empresa.cs`](Domain/Entities/Empresa.cs) | 4h | Nenhuma |
| 1.2 | Criar enum `TipoEvento` (Teatro=0, Palestra=1) | `Domain/Entities/TipoEvento.cs` | 30min | Nenhuma |
| 1.3 | Criar enum `PlanoEmpresa` (Gratuito=0, Basico=1, Profissional=2) | `Domain/Entities/PlanoEmpresa.cs` | 30min | Nenhuma |
| 1.4 | Criar exceções de domínio para Empresa (CnpjInvalidoException, EmpresaInativaException, etc.) | `Domain/Exceptions/` | 1h | Tarefa 1.1 |
| 1.5 | Criar interface `IEmpresaRepository` | [`Domain/Interface/IEmpresaRepository.cs`](Domain/Interface/IEmpresaRepository.cs) | 1h | Tarefa 1.1 |
| 1.6 | Criar DTOs de Empresa (CadastrarEmpresaDTO, LoginEmpresaDTO, EmpresaResponseDTO, AlterarEmpresaDTO, AlterarPlanoDTO) | `Application/DTOs/Empresa*.cs` (5 arquivos) | 2h | Tarefa 1.1 |

#### Dev B — Atualização de Entidades Existentes

| # | Tarefa | Arquivo | Esforço | Dependências |
|---|--------|---------|---------|--------------|
| 1.7 | Atualizar [`Evento.cs`](Domain/Entities/Evento.cs): add `EmpresaId`, `Tipo`, `Descricao`, `Local`, `ImagemUrl`, `DataCriacao`, `Gratuito`; remover `VendedorCpf` | [`Domain/Entities/Evento.cs`](Domain/Entities/Evento.cs) | 3h | Nenhuma |
| 1.8 | Atualizar construtor/factory de `Evento` para aceitar novos parâmetros | [`Domain/Entities/Evento.cs`](Domain/Entities/Evento.cs:33) | 1h | Tarefa 1.7 |
| 1.9 | Atualizar [`Usuario.cs`](Domain/Entities/Usuario.cs): add propriedade `Ativo` (bool, default true) | [`Domain/Entities/Usuario.cs`](Domain/Entities/Usuario.cs) | 30min | Nenhuma |
| 1.10 | Atualizar [`Reserva.cs`](Domain/Entities/Reserva.cs): add `EmpresaId`, `Quantidade`; tornar `IngressoId` nullable; criar factories `CriarParaTeatro` e `CriarParaPalestra` | [`Domain/Entities/Reserva.cs`](Domain/Entities/Reserva.cs) | 3h | Nenhuma |
| 1.11 | Atualizar [`Cupom.cs`](Domain/Entities/Cupom.cs): add `EmpresaId`; atualizar factory | [`Domain/Entities/Cupom.cs`](Domain/Entities/Cupom.cs) | 1h | Nenhuma |
| 1.12 | Atualizar [`Ingresso.cs`](Domain/Entities/Ingresso.cs): `Posicao` e `Setor` nullable | [`Domain/Entities/Ingresso.cs`](Domain/Entities/Ingresso.cs) | 30min | Nenhuma |
| 1.13 | Atualizar enum de Perfil (remover referência a Vendedor no código — não na entidade, mas em usages) | [`Domain/Entities/Perfil.cs`](Domain/Entities/Perfil.cs) + usages | 30min | Nenhuma |

#### Dev C — Scripts de Migração SQL

| # | Tarefa | Arquivo | Esforço | Dependências |
|---|--------|---------|---------|--------------|
| 1.14 | Criar `Script0009_CriarEmpresas.sql` — CREATE TABLE Empresas | `Infraestructure/DataBase/Scripts/Script0009_CriarEmpresas.sql` | 1h | Tarefa 1.1 |
| 1.15 | Criar `Script0010_AlterarEventos.sql` — ADD colunas (EmpresaId FK, Tipo, Descricao, Local, ImagemUrl, DataCriacao), DROP VendedorCpf, indices | `Infraestructure/DataBase/Scripts/Script0010_AlterarEventos.sql` | 1h | Tarefa 1.7 |
| 1.16 | Criar `Script0011_AlterarCupons.sql` — ADD EmpresaId FK, índice | `Infraestructure/DataBase/Scripts/Script0011_AlterarCupons.sql` | 30min | Tarefa 1.11 |
| 1.17 | Criar `Script0012_AlterarReservas.sql` — ADD EmpresaId, Quantidade; ALTER IngressoId para nullable; FK, índices | `Infraestructure/DataBase/Scripts/Script0012_AlterarReservas.sql` | 1h | Tarefa 1.10 |
| 1.18 | Criar `Script0013_AlterarUsuarios.sql` — ADD Ativo (BIT, NOT NULL, DEFAULT 1) | `Infraestructure/DataBase/Scripts/Script0013_AlterarUsuarios.sql` | 30min | Tarefa 1.9 |
| 1.19 | Criar `Script0014_RemoverPerfilVendedor.sql` — DELETE FROM Perfis WHERE Nome='Vendedor' | `Infraestructure/DataBase/Scripts/Script0014_RemoverPerfilVendedor.sql` | 15min | Nenhuma |
| 1.20 | Criar `Script0015_MigrarVendedores.sql` — migrar dados existentes de Vendedor para Empresa | `Infraestructure/DataBase/Scripts/Script0015_MigrarVendedores.sql` | 2h | Tarefa 1.14 |

#### Dev D — Repositório de Empresa

| # | Tarefa | Arquivo | Esforço | Dependências |
|---|--------|---------|---------|--------------|
| 1.21 | Criar [`EmpresaRepository`](Infraestructure/Repository/EmpresaRepository.cs) com métodos: | `Infraestructure/Repository/EmpresaRepository.cs` | 6h | Tarefa 1.5, 1.14 |
| | `Cadastrar(Empresa empresa)` — INSERT | | | |
| | `BuscarPorEmail(string email)` — SELECT para login | | | |
| | `BuscarPorCnpj(string cnpj)` — SELECT para validação unicidade | | | |
| | `BuscarPorId(Guid id)` — SELECT detalhado | | | |
| | `ListarTodas()` — SELECT para admin | | | |
| | `AtualizarDados(Empresa empresa)` — UPDATE | | | |
| | `AlterarPlano(Guid id, PlanoEmpresa plano)` — UPDATE | | | |
| | `AtivarDesativar(Guid id, bool ativo)` — UPDATE | | | |
| 1.22 | Adicionar script SQL de Empresa no `DatabaseMigration.cs` — registrar novo script na ordem | [`Infraestructure/DataBase/DatabaseMigration.cs`](Infraestructure/DataBase/DatabaseMigration.cs) | 30min | Tarefas 1.14 a 1.20 |

---

## Sprint 2 — Application Layer + API (Cadastro/Login de Empresa)

**Objetivo:** Criar serviços de aplicação para Empresa, atualizar serviços existentes para suportar multitenancy, atualizar controllers e Program.cs.

### Tarefas

#### Dev A — EmpresaService + EmpresaController

| # | Tarefa | Arquivo | Esforço | Dependências |
|---|--------|---------|---------|--------------|
| 2.1 | Criar interface `IEmpresaService` | `Application/Interfaces/IEmpresaService.cs` | 1h | Sprint 1 |
| 2.2 | Criar [`EmpresaService`](Application/Service/EmpresaService.cs) com métodos: | `Application/Service/EmpresaService.cs` | 8h | Tarefa 2.1, 1.21 |
| | `Cadastrar(CadastrarEmpresaDTO dto)` — validar CNPJ, email, senha; hash BCrypt; criar Empresa com Plano=Gratuito | | | |
| | `Login(LoginEmpresaDTO dto)` — buscar por email, verificar BCrypt, verificar Ativo, gerar JWT | | | |
| | `BuscarPorId(Guid id)` — retornar DTO | | | |
| | `ListarTodas()` — para admin | | | |
| | `AtualizarDados(Guid id, AlterarEmpresaDTO dto)` — editar perfil | | | |
| | `AlterarPlano(Guid id, PlanoEmpresa plano)` — admin apenas | | | |
| | `AtivarDesativar(Guid id, bool ativo)` — admin apenas | | | |
| | `ConsultarPlano(Guid id)` — retornar limites do plano | | | |
| 2.3 | Criar [`EmpresaController`](Api/Controllers/EmpresaController.cs) com endpoints: | `Api/Controllers/EmpresaController.cs` | 4h | Tarefa 2.2 |
| | `POST /api/empresa/cadastrar` — público | | | |
| | `POST /api/empresa/login` — público | | | |
| | `GET /api/empresa/dados` — `[Authorize(Roles = "Empresa")]` | | | |
| | `PUT /api/empresa/atualizar` — `[Authorize(Roles = "Empresa")]` | | | |
| | `GET /api/empresa/plano` — `[Authorize(Roles = "Empresa")]` | | | |
| 2.4 | Criar AutoMapper Profile para Empresa | `Application/Mappings/EmpresaProfile.cs` | 1h | Tarefa 1.6 |

#### Dev B — EventoService + EventoController Refatorados

| # | Tarefa | Arquivo | Esforço | Dependências |
|---|--------|---------|---------|--------------|
| 2.5 | Atualizar [`IEventoService`](Application/Interfaces/IEventoService.cs): | `Application/Interfaces/IEventoService.cs` | 1h | Sprint 1 |
| | Renomear `GetAllByVendedorAsync` → `GetAllByEmpresaAsync(Guid empresaId)` | | | |
| | Alterar `CriarEventoAsync` para receber `Guid empresaId` (não string vendedorCpf) | | | |
| | Alterar `UpdateAsync` e `DeleteAsync` para receber `Guid empresaId` | | | |
| | Adicionar método `ContarEventosFuturos(Guid empresaId)` (para plano) | | | |
| 2.6 | Atualizar [`EventoService`](Application/Service/EventoService.cs): | `Application/Service/EventoService.cs` | 6h | Tarefa 2.5 |
| | Implementar `GetAllByEmpresaAsync` — filtrar por `EmpresaId` | | | |
| | Implementar `CriarEventoAsync` — usar `empresaId` do parâmetro, suportar `Tipo`, validar plano, criar ingressos só se Teatro | | | |
| | Implementar `UpdateAsync` — verificar dono do evento por `EmpresaId` | | | |
| | Implementar `DeleteAsync` — verificar dono, Admin pode excluir qualquer | | | |
| | Implementar `ContarEventosFuturos` — consultar COUNT por empresa | | | |
| | `GetAllAsync` — filtrar empresas inativas (JOIN com Empresas) | | | |
| 2.7 | Atualizar [`EventoController`](Api/Controllers/EventoController.cs): | `Api/Controllers/EventoController.cs` | 3h | Tarefa 2.6 |
| | `[Authorize(Roles = "Empresa")]` no lugar de `[Authorize(Roles = "Vendedor")]` | | | |
| | Extrair `empresaId` do JWT (claim) — `User.FindFirst("empresaId")` | | | |
| | Endpoint `GET /api/evento/empresa` — listar eventos da empresa logada | | | |
| | Endpoint `POST /api/evento/criar` — remover `VendedorCpf` do body | | | |
| 2.8 | Atualizar [`EventoRequestDTO`](Application/DTOs/EventoRequestDTO.cs): add Tipo, Descricao, Local, ImagemUrl; remover VendedorCpf; alterar Range do Preco | `Application/DTOs/EventoRequestDTO.cs` | 1h | Sprint 1 |
| 2.9 | Atualizar [`EventoResponseDTO`](Application/DTOs/EventoResponseDTO.cs): add novas propriedades, remover VendedorCpf | `Application/DTOs/EventoResponseDTO.cs` | 1h | Sprint 1 |
| 2.10 | Atualizar [`EventoProfile`](Application/Mappings/EventoProfile.cs) do AutoMapper para novas propriedades | `Application/Mappings/EventoProfile.cs` | 30min | Tarefa 2.8, 2.9 |
| 2.11 | Atualizar [`EventoRepository`](Infraestructure/Repository/EventoRepository.cs): | `Infraestructure/Repository/EventoRepository.cs` | 4h | Sprint 1 |
| | INSERT com novas colunas | | | |
| | `GetAllByEmpresaAsync` (substitui `GetAllByVendedorAsync`) | | | |
| | `ContarEventosAtivos(Guid empresaId)` — COUNT para plano Gratuito | | | |
| | `ContarEventosNoMes(Guid empresaId, DateTime mes)` — COUNT para plano Básico | | | |
| | `GetAllAsync` com JOIN em Empresas para filtrar inativas | | | |
| | UPDATE/DELETE com verificação de EmpresaId | | | |

#### Dev C — CupomService + CupomController Refatorados

| # | Tarefa | Arquivo | Esforço | Dependências |
|---|--------|---------|---------|--------------|
| 2.12 | Atualizar [`ICupomService`](Application/Interfaces/ICupomService.cs): | `Application/Interfaces/ICupomService.cs` | 1h | Sprint 1 |
| | Substituir `Guid AdminLogado` por `Guid empresaId` em todos os métodos | | | |
| | Remover `ListarCuponsValidos` (não era específico de empresa) ou adaptar | | | |
| 2.13 | Atualizar [`CupomService`](Application/Service/CupomService.cs): | `Application/Service/CupomService.cs` | 6h | Tarefa 2.12 |
| | Substituir `AdminLogado` por `empresaId` em todas as assinaturas | | | |
| | `CadastrarCupom`: validar plano (Gratuito não pode), associar `EmpresaId` ao cupom | | | |
| | Todos os métodos: filtrar/alterar apenas cupons da própria empresa | | | |
| | Remover `Task.Run` para chamadas síncronas — tornar tudo async | | | |
| 2.14 | Atualizar [`CupomController`](Api/Controllers/CupomController.cs): | `Api/Controllers/CupomController.cs` | 4h | Tarefa 2.13 |
| | Alterar `[Authorize(Roles = "Admin")]` → `[Authorize(Roles = "Empresa")]` | | | |
| | Extrair `empresaId` do JWT em vez de receber do body/rota | | | |
| | Remover endpoint `DebugClaims` | | | |
| 2.15 | Atualizar [`CupomRepository`](Infraestructure/Repository/CupomRepository.cs): | `Infraestructure/Repository/CupomRepository.cs` | 4h | Sprint 1 |
| | Tornar `CadastrarCupom` assíncrono (`void` → `async Task`) | | | |
| | Adicionar `EmpresaId` em todas as queries INSERT/UPDATE/SELECT/DELETE | | | |
| | Filtrar por `EmpresaId` em `ListarTodosCupons` | | | |
| | Adicionar `BuscarCupomPorCodigoEEmpresa(string codigo, Guid empresaId)` | | | |

#### Dev D — TokenService + UsuarioService + DI

| # | Tarefa | Arquivo | Esforço | Dependências |
|---|--------|---------|---------|--------------|
| 2.16 | Atualizar [`ITokenService`](Application/Interfaces/ITokenService.cs): adicionar método `GerarTokenEmpresa(Empresa empresa)` | `Application/Interfaces/ITokenService.cs` | 30min | Sprint 1 |
| 2.17 | Atualizar [`TokenService`](Application/Service/TokenService.cs): | `Application/Service/TokenService.cs` | 3h | Tarefa 2.16 |
| | Remover case "Vendedor" do switch de PerfilId | | | |
| | Adicionar método `GerarTokenEmpresa` — gerar JWT com claims: role=Empresa, empresaId, cnpj, email | | | |
| 2.18 | Atualizar [`IUsuarioService`](Application/Interfaces/IUsuarioService.cs): remover `CadastrarVendedor` | `Application/Interfaces/IUsuarioService.cs` | 15min | Sprint 1 |
| 2.19 | Atualizar [`UsuarioService`](Application/Service/UsuarioService.cs): | `Application/Service/UsuarioService.cs` | 4h | Tarefa 2.18 |
| | Remover método `CadastrarVendedor` | | | |
| | `CadastrarComprador`: implementar BCrypt hash (`BCrypt.Net.BCrypt.HashPassword`) | | | |
| | `Login`: implementar BCrypt.Verify, verificar `Usuario.Ativo` | | | |
| 2.20 | Atualizar [`UsuarioController`](Api/Controllers/UsuarioController.cs): | `Api/Controllers/UsuarioController.cs` | 2h | Tarefa 2.19 |
| | Remover endpoint `CadastrarVendedor/{Id}` | | | |
| | Adicionar `[Authorize(Roles = "Admin")]` no endpoint `ListarUsuarioEspecifico/{cpf}` | | | |
| 2.21 | Atualizar [`UsuarioRepository`](Infraestructure/Repository/UsuarioRepository.cs): adicionar `Ativo` no INSERT e SELECT | `Infraestructure/Repository/UsuarioRepository.cs` | 1h | Sprint 1 |
| 2.22 | Atualizar [`Program.cs`](Api/Program.cs): | `Api/Program.cs` | 1h | Tarefa 2.2, 2.21 |
| | Registrar `IEmpresaService`, `IEmpresaRepository` no DI | | | |
| | Adicionar `EmpresaProfile` no AutoMapper | | | |
| | Adicionar pacote NuGet `BCrypt.Net-Next` ao projeto | `Application/Application.csproj` | | |

---

## Sprint 3 — Reserva com Palestra + Segurança

**Objetivo:** Implementar suporte a Palestra no fluxo de reserva, aplicar BCrypt, corrigir vulnerabilidades de segurança.

### Tarefas

#### Dev A — ReservaService + ReservaController (Palestra)

| # | Tarefa | Arquivo | Esforço | Dependências |
|---|--------|---------|---------|--------------|
| 3.1 | Atualizar [`IReservaService`](Application/Interfaces/IReservaService.cs): | `Application/Interfaces/IReservaService.cs` | 30min | Sprint 2 |
| | Adicionar `FazerReservaPalestra(string usuarioCpf, Guid eventoId, int quantidade, string? cupom)` | | | |
| | Ou adaptar `FazerReserva` existente para aceitar `Guid? ingressoId` e `int quantidade` | | | |
| 3.2 | Atualizar [`ReservaService`](Application/Service/ReservaService.cs): | `Application/Service/ReservaService.cs` | 8h | Tarefa 3.1 |
| | `FazerReserva`: lógica condicional — se `IngressoId` for null, é Palestra | | | |
| | Para Palestra: validar `Quantidade >= 1`, validar vagas disponíveis (`CapacidadeTotal - SUM reservas < Quantidade`) | | | |
| | Para Teatro: manter lógica existente (1 ingresso, bloquear assento) | | | |
| | Validar cupom pertence à mesma empresa do evento | | | |
| | Se evento gratuito: pular pagamento, confirmar imediatamente | | | |
| | Extrair `EmpresaId` do evento e associar à reserva | | | |
| 3.3 | Atualizar [`ReservarDTO`](Application/DTOs/ReservarDTO.cs): `IngressoId` nullable, add `Quantidade` | `Application/DTOs/ReservarDTO.cs` | 15min | Sprint 1 |
| 3.4 | Atualizar [`ReservaController`](Api/Controllers/ReservaController.cs): | `Api/Controllers/ReservaController.cs` | 2h | Tarefa 3.2 |
| | Adaptar `FazerReserva` para usar novo DTO | | | |
| | Extrair `cpf` do JWT (não receber do body) | | | |
| 3.5 | Atualizar [`ReservaRepository`](Infraestructure/Repository/ReservaRepository.cs): | `Infraestructure/Repository/ReservaRepository.cs` | 4h | Sprint 1 |
| | INSERT com `EmpresaId`, `Quantidade`, `IngressoId` nullable | | | |
| | `BuscarTotalVagasReservadas(Guid eventoId)` — SUM(Quantidade) para Palestra | | | |
| | Filtrar reservas por EmpresaId quando necessário | | | |
| | `DeletarReservasNaoPagasExpiradas` — também para Palestra (sem IngressoId) | | | |

#### Dev B — BCrypt + Correções de Segurança

| # | Tarefa | Arquivo | Esforço | Dependências |
|---|--------|---------|---------|--------------|
| 3.6 | Adicionar pacote NuGet `BCrypt.Net-Next` | `Application/Application.csproj` | 15min | Nenhuma |
| 3.7 | Implementar BCrypt no cadastro de comprador: | [`UsuarioService.cs`](Application/Service/UsuarioService.cs:22) | 1h | Sprint 2 |
| | `BCrypt.Net.BCrypt.HashPassword(dto.Senha)` antes de salvar | | | |
| 3.8 | Implementar BCrypt no login: | [`UsuarioService.cs`](Application/Service/UsuarioService.cs:48) | 1h | Tarefa 3.7 |
| | Substituir `if (usuario.Senha != dto.Senha)` por `if (!BCrypt.Net.BCrypt.Verify(dto.Senha, usuario.Senha))` | | | |
| 3.9 | Implementar BCrypt no cadastro de empresa: | [`EmpresaService.cs`](Application/Service/EmpresaService.cs) | 1h | Tarefa 3.7 |
| | Hash da senha antes de persistir | | | |
| 3.10 | Implementar BCrypt no login de empresa: | [`EmpresaService.cs`](Application/Service/EmpresaService.cs) | 1h | Tarefa 3.8 |
| | `BCrypt.Verify` no login | | | |
| 3.11 | Atualizar registros existentes no banco (Admin seed) para usar BCrypt | Script SQL ou migration | 1h | Tarefa 3.7 |
| 3.12 | Verificar se todas as senhas em seed/scripts usam BCrypt | `db/Script0000_Setup_Completo.sql` | 1h | Tarefa 3.11 |

#### Dev C — Remoção de AdminLogado + JWT Extraction

| # | Tarefa | Arquivo | Esforço | Dependências |
|---|--------|---------|---------|--------------|
| 3.13 | Refatorar [`CupomService`](Application/Service/CupomService.cs) — remover completamente o padrão de receber `AdminLogado` | `Application/Service/CupomService.cs` | 3h | Sprint 2 |
| | Todos os métodos devem usar `empresaId` passado pela controller (extraído do JWT) | | | |
| 3.14 | Refatorar [`CupomController`](Api/Controllers/CupomController.cs) — extrair `empresaId` do JWT: | `Api/Controllers/CupomController.cs` | 2h | Tarefa 3.13 |
| | `var empresaId = Guid.Parse(User.FindFirst("empresaId")?.Value!)` | | | |
| | Passar para o service em vez de receber do body | | | |
| 3.15 | Refatorar [`EventoController`](Api/Controllers/EventoController.cs) — extrair `empresaId` do JWT em todos os endpoints protegidos | `Api/Controllers/EventoController.cs` | 1h | Sprint 2 |
| 3.16 | Remover endpoint `DebugClaims` do [`CupomController`](Api/Controllers/CupomController.cs:119) | `Api/Controllers/CupomController.cs` | 15min | Nenhuma |
| 3.17 | Adicionar `[Authorize(Roles = "Admin")]` no endpoint `ListarUsuarioEspecifico` do [`UsuarioController`](Api/Controllers/UsuarioController.cs:52) | `Api/Controllers/UsuarioController.cs` | 15min | Nenhuma |
| 3.18 | Verificar se `cpf` do comprador é extraído do JWT (não do body) em todos os endpoints de reserva | `ReservaController.cs` | 1h | Tarefa 3.4 |

#### Dev D — LiberacaoAssentosWorker + Validações de Palestra

| # | Tarefa | Arquivo | Esforço | Dependências |
|---|--------|---------|---------|--------------|
| 3.19 | Atualizar [`LiberacaoAssentosWorker`](Api/BackgroundTasks/LiberacaoAssentosWorker.cs): | `Api/BackgroundTasks/LiberacaoAssentosWorker.cs` | 4h | Sprint 1 |
| | Adicionar filtro por `EmpresaId` no SQL de liberação de assentos | | | |
| | Adicionar chamada a `DeletarReservasNaoPagasExpiradas` para reservas de Palestra (que não têm IngressoId) | | | |
| | Garantir que o worker itere empresas individualmente ou use queries parametrizadas | | | |
| 3.20 | Implementar validação de vagas para Palestra no [`ReservaRepository`](Infraestructure/Repository/ReservaRepository.cs): | `Infraestructure/Repository/ReservaRepository.cs` | 2h | Sprint 2 |
| | Método `ObterVagasDisponiveis(Guid eventoId)` = `CapacidadeTotal - SUM(Quantidade)` | | | |
| 3.21 | Implementar validação de cupom por empresa no fluxo de reserva | [`ReservaService.cs`](Application/Service/ReservaService.cs:32) | 2h | Tarefa 3.2 |
| | Buscar cupom por código + empresaId | | | |
| | Verificar `cupom.EmpresaId == evento.EmpresaId` | | | |
| 3.22 | Atualizar [`GlobalExceptionHandlerMiddleware`](Api/Middlewares/GlobalExceptionHandlerMiddleware.cs): | `Api/Middlewares/GlobalExceptionHandlerMiddleware.cs` | 1h | Nenhuma |
| | Adicionar tratamento para `UnauthorizedAccessException` | | | |
| | Adicionar tratamento para `ArgumentException` (validações de domínio) | | | |
| 3.23 | Criar testes unitários para validação de CNPJ | `tests/SoldOutTickets.Tests.csproj` | 2h | Sprint 1 |

---

## Sprint 4 — Admin (Gestão de Plataforma) + Finalização

**Objetivo:** Criar endpoints de Admin para gestão de empresas, implementar validação de planos, auditoria de segurança e testes finais.

### Tarefas

#### Dev A — AdminController + Admin Flows

| # | Tarefa | Arquivo | Esforço | Dependências |
|---|--------|---------|---------|--------------|
| 4.1 | Criar [`AdminController`](Api/Controllers/AdminController.cs) com endpoints: | `Api/Controllers/AdminController.cs` | 6h | Sprint 3 |
| | `GET /api/admin/empresas` — listar todas empresas (`IEmpresaService.ListarTodas`) | | | |
| | `GET /api/admin/empresa/{id}` — dados de uma empresa | | | |
| | `PUT /api/admin/empresa/{id}/plano` — alterar plano (body: `AlterarPlanoDTO`) | | | |
| | `PUT /api/admin/empresa/{id}/ativar` — ativar/desativar (body: `{ "ativo": true/false }`) | | | |
| | `GET /api/admin/usuarios` — listar compradores | | | |
| | `GET /api/admin/eventos` — listar eventos de todas empresas | | | |
| | `DELETE /api/admin/evento/{id}` — excluir qualquer evento | | | |
| 4.2 | Todos os endpoints com `[Authorize(Roles = "Admin")]` | `AdminController.cs` | — | Tarefa 4.1 |
| 4.3 | Atualizar [`Program.cs`](Api/Program.cs) — registrar `AdminController` | `Api/Program.cs` | 15min | Tarefa 4.1 |

#### Dev B — Validação de Planos + Limites

| # | Tarefa | Arquivo | Esforço | Dependências |
|---|--------|---------|---------|--------------|
| 4.4 | Implementar método `ValidarLimiteDeEventos(Guid empresaId)` no [`EventoService`](Application/Service/EventoService.cs): | `Application/Service/EventoService.cs` | 3h | Sprint 2 |
| | Buscar empresa + plano | | | |
| | Se Gratuito: `ContarEventosAtivos >= 3` → rejeitar | | | |
| | Se Básico: `ContarEventosNoMes >= 10` → rejeitar | | | |
| | Se Profissional: permitir sempre | | | |
| 4.5 | Integrar `ValidarLimiteDeEventos` no fluxo de `CriarEventoAsync` | `EventoService.CriarEventoAsync` | 1h | Tarefa 4.4 |
| 4.6 | Implementar validação de plano para cupons no [`CupomService`](Application/Service/CupomService.cs): | `Application/Service/CupomService.cs` | 2h | Sprint 2 |
| | Se empresa for plano Gratuito → rejeitar criação de cupom | | | |
| 4.7 | Criar endpoint `GET /api/empresa/plano` — retornar plano atual + limites + eventos restantes | [`EmpresaController`](Api/Controllers/EmpresaController.cs) | 2h | Tarefa 4.4 |
| 4.8 | Criar DTO `PlanoResponseDTO` com: Plano, LimiteEventos, EventosCriados, EventosRestantes, CuponsPermitidos | `Application/DTOs/PlanoResponseDTO.cs` | 1h | Nenhuma |

#### Dev C — Auditoria de Segurança + Refinamentos

| # | Tarefa | Arquivo | Esforço | Dependências |
|---|--------|---------|---------|--------------|
| 4.9 | Auditoria geral: verificar se TODOS os endpoints têm `[Authorize]` correto | Todos os controllers | 2h | Sprint 3 |
| | `UsuarioController`: listar, alterar dados — verificar role | | | |
| | `EventoController`: criar/editar/excluir — `[Authorize(Roles = "Empresa")]` | | | |
| | `CupomController`: todos — `[Authorize(Roles = "Empresa")]` | | | |
| | `ReservaController`: fazer reserva — `[Authorize(Roles = "Comprador")]` | | | |
| | `AdminController`: todos — `[Authorize(Roles = "Admin")]` | | | |
| 4.10 | Remover qualquer menção a "Vendedor" no código backend (strings, comments) | Todo o projeto | 1h | Nenhuma |
| 4.11 | Verificar se claims do JWT estão sendo extraídas corretamente em todos os controllers | Todos os controllers | 2h | Sprint 3 |
| | `empresaId` nos controllers de Empresa e Evento | | | |
| | `cpf` nos controllers de Comprador | | | |
| 4.12 | Garantir que `MapInboundClaims = false` está configurado (já existe em [`Program.cs:14`](Api/Program.cs:14)) | `Api/Program.cs` | 15min | Nenhuma |
| 4.13 | Verificar se `RoleClaimType = "role"` está correto (já existe em [`Program.cs:65`](Api/Program.cs:65)) | `Api/Program.cs` | 15min | Nenhuma |

#### Dev D — Testes + Documentação Final

| # | Tarefa | Arquivo | Esforço | Dependências |
|---|--------|---------|---------|--------------|
| 4.14 | Criar testes de unidade para `Empresa` (validação de CNPJ, factory) | `tests/SoldOutTickets.Tests.csproj` | 3h | Sprint 1 |
| 4.15 | Criar testes de unidade para `Evento` (criação Teatro vs Palestra, validações) | `tests/SoldOutTickets.Tests.csproj` | 3h | Sprint 2 |
| 4.16 | Criar testes de unidade para `Reserva` (Teatro, Palestra, cupom, gratuito) | `tests/SoldOutTickets.Tests.csproj` | 3h | Sprint 3 |
| 4.17 | Criar testes de integração para `EmpresaController` (cadastro, login, plano) | `tests/SoldOutTickets.Tests.csproj` | 4h | Sprint 4 Dev A |
| 4.18 | Criar testes de integração para fluxo completo: Empresa cria evento → Comprador reserva | `tests/SoldOutTickets.Tests.csproj` | 4h | Sprint 3 |
| 4.19 | Atualizar [`docs/operacao.md`](docs/operacao.md) — novos endpoints, novos perfis | `docs/operacao.md` | 1h | Nenhuma |
| 4.20 | Atualizar [`docs/analise.md`](docs/analise.md) — marcar problemas de segurança como corrigidos | `docs/analise.md` | 1h | Nenhuma |

---

## Resumo de Esforço por Sprint

| Sprint | Dev A | Dev B | Dev C | Dev D | Total (h) |
|--------|-------|-------|-------|-------|-----------|
| **Sprint 1** — Fundação | 9h | 9h30 | 5h45 | 6h30 | ~31h |
| **Sprint 2** — Application + API | 14h | 15h30 | 15h | 11h45 | ~56h |
| **Sprint 3** — Reserva/Palestra + Segurança | 14h30 | 5h15 | 7h30 | 11h | ~38h |
| **Sprint 4** — Admin + Finalização | 6h15 | 8h | 5h30 | 19h | ~39h |
| **Total** | ~44h | ~39h | ~34h | ~48h | **~164h** |

---

## Gráfico de Dependências

```
Sprint 1                    Sprint 2                    Sprint 3                 Sprint 4
─────────                   ─────────                   ─────────                ─────────
Domain/Entities     ──►     EmpresaService       ──►    BCrypt Security   ──►    AdminController
  (Dev A, B)                  (Dev A)                    (Dev B)                  (Dev A)
       │                          │                          │
       ▼                          ▼                          ▼
SQL Scripts             ──►     EventoService       ──►    ReservaService   ──►    Plan Validation
  (Dev C)                        (Dev B)                    (Dev A)                  (Dev B)
       │                          │                          │
       ▼                          ▼                          ▼
EmpresaRepository       ──►     CupomService        ──►    JWT Extraction    ──►    Security Audit
  (Dev D)                        (Dev C)                    (Dev C)                  (Dev C)
       │                          │                          │
       ▼                          ▼                          ▼
Interfaces              ──►     TokenService +      ──►    Worker +          ──►    Tests + Docs
  (Dev A)                        UsuarioService             Palestra Valid.          (Dev D)
                                 (Dev D)                    (Dev D)
```

---

> **Observações importantes:**
>
> 1. **Sprints 1 e 2 têm maior dependência entre tarefas** — recomenda-se daily sync de 15min para alinhamento.
> 2. **Sprint 3 é a mais crítica** — envolve correções de segurança que impactam todo o sistema.
> 3. **Sprint 4 tem tasks mais independentes** — pode ser acelerada se houver folga.
> 4. **Nenhuma tarefa de frontend (Blazor/Web) está neste plano.** O frontend deve ser planejado separadamente.
> 5. **Consulte [`docs/requisitos-saas.md`](docs/requisitos-saas.md) para detalhes completos de cada requisito.**
