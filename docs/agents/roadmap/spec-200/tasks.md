---
name: "Guid Id como PK de Usuarios (substituir Cpf)"
status: "verified"
references:
  - "requirements.md (este diretório)"
  - "design.md (este diretório)"
---

# Spec 200: Guid Id como PK de Usuarios — Implementation Tasks

## Task Order

Tasks MUST be executed sequentially in numerical order. Cada task depende da conclusão de todas as tasks anteriores. A ordem segue a dependência natural: banco → domain → repository → service → controller → testes.

```
Task 1 (Migration SQL Script0013)
  │
  └──→ Task 2 (Domain: Usuario + Reserva + Evento entities)
        │
        └──→ Task 3 (Domain: IUsuarioRepository + IReservaRepository interfaces)
              │
              └──→ Task 4 (Infra: UsuarioRepository)
                    │
                    └──→ Task 5 (Infra: ReservaRepository)
                          │
                          └──→ Task 6 (Application: UsuarioService + IUsuarioService)
                                │
                                └──→ Task 7 (Application: ReservaService + TokenService)
                                      │
                                      └──→ Task 8 (Application: DTOs + Mappings)
                                            │
                                            └──→ Task 9 (Api: UsuarioController + ReservaController)
                                                  │
                                                  └──→ Task 10 (Infra: DatabaseSeeder)
                                                        │
                                                        └──→ Task 11 (Build + testes)
```

---

## Tasks

### Task 1: Criar Migration Script SQL

**Objective:** Criar script DbUp `Script0013` para migrar PK de `Cpf` para `Id`.

**Requirements Covered:** FR-001, FR-002, FR-003

**Design References:** design.md — Component 1

**Actions:**
1. Create file `db/Script0013_MigrarPkGuidUsuarios.sql`
2. Write the migration script conforme design.md Component 1
3. Garantir idempotência: todos os comandos usam `IF EXISTS`/`IF NOT EXISTS`
4. Usar transação (`BEGIN TRANSACTION ... COMMIT`)
5. Operações na ordem correta:
   - Add Id column → Preencher com NEWID() → NOT NULL → Remover PK antiga → Nova PK → Cpf nullable → Filtered unique index
   - Add UsuarioId/VendedorId em Reservas → Migrar dados → NOT NULL → Remover FKs antigas → Novas FKs → Drop colunas antigas
   - Add VendedorId em Eventos → Migrar dados → Drop VendedorCpf → Nova FK

**Validation:**
- [ ] Script executa sem erros em banco limpo
- [ ] Script executa sem erros em banco com dados existentes
- [ ] Script é idempotente (executar 2x sem erro)
- [ ] Colunas `UsuarioCpf`, `VendedorCpf` removidas de Reservas
- [ ] Coluna `VendedorCpf` removida de Eventos
- [ ] FKs novas apontam para `Usuarios(Id)`

**Status:** [ ] pending

---

### Task 2: Atualizar Domain Entities

**Objective:** Atualizar `Usuario`, `Reserva`, `Evento` para usar `Guid Id`.

**Requirements Covered:** FR-004, FR-010

**Design References:** design.md — Components 2, 3

**Actions:**

**2a. `Domain/Entities/Usuario.cs`:**
1. Adicionar `public Guid Id { get; private set; } = Guid.NewGuid();`
2. Alterar construtor para `Usuario(Guid id, string cpf, string nome, string email, Guid perfilid, string senha)`
3. `CriarComprador`: adicionar `Id = Guid.NewGuid()`
4. `CriarVendedor`: adicionar `Id = Guid.NewGuid()`, manter `Cpf = null`
5. Manter todas as validações de CPF para comprador

**2b. `Domain/Entities/Reserva.cs`:**
1. `UsuarioCpf` → `UsuarioId` (Guid)
2. `VendedorCpf` → `VendedorId` (Guid?)
3. Atualizar construtor: `Reserva(Guid usuarioId, Guid eventoId, string? cupomUtilizado, decimal valorFinalPago)`
4. Atualizar `Criar(Guid usuarioId, Guid eventoId, List<ItemReserva> itens, Cupom? cupom = null, Guid? vendedorId = null)`

**2c. `Domain/Entities/Evento.cs`:**
1. `VendedorCpf` → `VendedorId` (Guid?)
2. Atualizar construtor: `Evento(string nome, int capacidadetotal, DateTime dataevento, decimal precopadrao, Guid? vendedorId, ...)`

**Validation:**
- [ ] `Usuario` tem `Id` como Guid
- [ ] `Cpf` é nullable em `CriarVendedor`
- [ ] `Reserva` usa `UsuarioId`/`VendedorId`
- [ ] `Evento` usa `VendedorId`

**Status:** [ ] pending

---

### Task 3: Atualizar Repository Interfaces

**Objective:** Atualizar assinaturas de `IUsuarioRepository` e `IReservaRepository`.

**Requirements Covered:** FR-005, FR-009

**Design References:** design.md — Component 4

**Actions:**

**3a. `Domain/Interface/IUsuarioRepository.cs`:**
1. Adicionar `Task<Usuario?> BuscarPorId(Guid id, CancellationToken ct);`
2. `RemoverUsuario(Usuario usuario, ...)` → `RemoverUsuario(Guid id, ...)`
3. `AtualizarSenha(string cpf, ...)` → `AtualizarSenha(Guid id, ...)`
4. Adicionar `AtualizarEmailAsync(Guid id, ...)` e `AtualizarNomeAsync(Guid id, ...)`
5. Manter `BuscarCpf`, `BuscarEmail`, `BuscarCnpjOuEmail`, `BuscarPorCnpj`, `CadastrarUsuario`, `CadastrarVendedor`

**3b. `Domain/Interface/IReservaRepository.cs`:**
1. `ListarPorCpf(string cpf, ...)` → `ListarPorUsuarioId(Guid usuarioId, ...)`
2. `ListarReservasDetalhadasPorCpf` → `ListarReservasDetalhadasPorUsuarioId(Guid)`
3. `ListarReservasDetalhadasPorVendedor(string)` → `ListarReservasDetalhadasPorVendedorId(Guid)`

**Validation:**
- [ ] `IUsuarioRepository` tem `BuscarPorId(Guid)`
- [ ] Métodos de atualização usam `Guid id`
- [ ] `IReservaRepository` usa `Guid` para buscas

**Status:** [ ] pending

---

### Task 4: Atualizar UsuarioRepository

**Objective:** Refatorar todas as queries SQL de `UsuarioRepository` para usar `Id`.

**Requirements Covered:** FR-005

**Design References:** design.md — Component 5

**Actions:**

1. `CadastrarUsuario`: INSERT adiciona `Id` column → `@Id = usuario.Id`
2. `CadastrarVendedor`: INSERT adiciona `Id` column → `@Id = vendedor.Id`
3. `BuscarCpf`: mantém query com `WHERE u.Cpf = @Cpf` (ainda necessário)
4. `BuscarPorId` (NEW): `WHERE u.Id = @Id` com JOIN de Perfil
5. `BuscarCpfOuEmail`: mantém query com `WHERE Cpf = @Cpf OR Email = @Email`
6. `BuscarCnpjOuEmail`: adiciona `Id` no SELECT
7. `BuscarPorCnpj`: adiciona `Id` no SELECT
8. `BuscarId`: renomear para `BuscarPorPerfilId` e adicionar `Id` no SELECT
9. `BuscarEmail`: adiciona `Id` no SELECT
10. `RemoverUsuario(Guid id)`: `DELETE FROM Usuarios WHERE Id = @Id`
11. `AtualizarSenha(Guid id, ...)`: `UPDATE Usuarios SET Senha = @Senha WHERE Id = @Id`
12. `AtualizarEmailAsync(Guid id, ...)`: `UPDATE Usuarios SET Email = @Email WHERE Id = @Id`
13. `AtualizarNomeAsync(Guid id, ...)`: `UPDATE Usuarios SET Nome = @Nome WHERE Id = @Id`
14. `ListarTodosAsync` e `ListarTodos`: adicionam `u.Id` no SELECT, JOIN usa `u.PerfilId = p.Id`

**Validation:**
- [ ] Nenhum método usa `WHERE Cpf = @Cpf` exceto `BuscarCpf` e `BuscarCpfOuEmail`
- [ ] INSERTs incluem `Id`
- [ ] SELECTs incluem `Id`

**Status:** [ ] pending

---

### Task 5: Atualizar ReservaRepository

**Objective:** Refatorar todas as queries SQL de `ReservaRepository` para usar `UsuarioId`/`VendedorId`.

**Requirements Covered:** FR-009

**Design References:** design.md — Component 6

**Actions:**

1. `CadastrarReservaComItens`: INSERT usa `@UsuarioId`, `@VendedorId` em vez de `@UsuarioCpf`, `@VendedorCpf`
2. `ListarPorCpf` → `ListarPorUsuarioId(Guid usuarioId)`: `WHERE UsuarioId = @UsuarioId`
3. `ReservaExistenteParaCpfNoEvento`: mantém check por `ir.CpfParticipante` (coluna `ItensReserva` não muda)
4. `ListarReservasDetalhadasPorCpf` → `ListarReservasDetalhadasPorUsuarioId`:
   - WHERE `r.UsuarioId = @UsuarioId`
   - JOIN `INNER JOIN Usuarios u ON r.UsuarioId = u.Id`
   - SELECT `u.Id AS UsuarioId`
5. `ListarTodasDetalhadasAdmin`:
   - JOIN `INNER JOIN Usuarios u ON r.UsuarioId = u.Id`
   - SELECT `u.Id AS UsuarioId` em vez de `u.Cpf AS CpfUsuario`
6. `ListarReservasDetalhadasPorVendedor` → `ListarReservasDetalhadasPorVendedorId`:
   - WHERE `r.VendedorId = @VendedorId`
   - JOIN `INNER JOIN Usuarios u ON r.UsuarioId = u.Id`
   - SELECT `u.Id AS UsuarioId` em vez de `u.Cpf AS CpfComprador`

**Validation:**
- [ ] Nenhum query referencia `UsuarioCpf` ou `VendedorCpf`
- [ ] Todos os JOINs usam `UsuarioId`/`VendedorId`

**Status:** [ ] pending

---

### Task 6: Atualizar UsuarioService + IUsuarioService

**Objective:** Atualizar service de usuário para usar `Guid Id`.

**Requirements Covered:** FR-006

**Design References:** design.md — Components 7, 8

**Actions:**

**6a. `Application/Interfaces/IUsuarioService.cs`:**
1. `UsuarioEspecifico(string cpf, ...)` → `UsuarioEspecifico(Guid id, ...)`
2. `RemoverUsuario(string cpf, ...)` → `RemoverUsuario(Guid id, ...)`
3. `AlterarSenha(dto, string cpf, ...)` → `AlterarSenha(dto, Guid id, ...)`
4. `AlterarEmailAsync(string cpf, ...)` → `AlterarEmailAsync(Guid id, ...)`
5. `AlterarNomeAsync(string cpf, ...)` → `AlterarNomeAsync(Guid id, ...)`

**6b. `Application/Service/UsuarioService.cs`:**
1. `UsuarioEspecifico`: `await _repository.BuscarPorId(id, ct)` em vez de `BuscarCpf(cpf, ct)`
2. `RemoverUsuario`: `await _repository.RemoverUsuario(id, ct)` em vez de `RemoverUsuario(usuario, ct)`
3. `AlterarSenha`:
   - Buscar por Id: `await _repository.BuscarPorId(id, ct)`
   - Atualizar por Id: `await _repository.AtualizarSenha(id, senhaHash, ct)`
4. `AlterarEmailAsync`:
   - Buscar por Id: `await _repository.BuscarPorId(id, ct)`
   - Atualizar por Id: `await _repository.AtualizarEmailAsync(id, dto.NovoEmail, ct)`
5. `AlterarNomeAsync`:
   - Buscar por Id: `await _repository.BuscarPorId(id, ct)`
   - Atualizar por Id: `await _repository.AtualizarNomeAsync(id, dto.NovoNome, ct)`
6. `Login`: retorna `UsuarioLoginDTO` com `usuario.Id`
7. `ListarUsuariosAsync`: mapeia `u.Id` no `UsuarioResponseDTO`
8. `CadastrarComprador`: sem mudança na lógica (busca por CPF/Email mantida)
9. `CadastrarVendedor`: retorna `VendedorCadastradoDTO` com `vendedor.Id`

**Validation:**
- [ ] Nenhum método usa `string cpf` como parâmetro de identificação (exceto `CadastrarComprador`)
- [ ] `Login` retorna `Id` no DTO

**Status:** [ ] pending

---

### Task 7: Atualizar ReservaService + TokenService

**Objective:** Atualizar service de reserva e token para usar Guid.

**Requirements Covered:** FR-007, FR-008, FR-009

**Design References:** design.md — Components 9, 12

**Actions:**

**7a. `Application/Service/TokenService.cs`:**
1. Adicionar claim `"userId"` com `usuario.Id.ToString()`
2. Claim `"cpf"` condicional: `usuario.Cpf ?? ""`

**7b. `Application/Service/ReservaService.cs`:**
1. `FazerReserva(Guid usuarioId, ReservarDTO dto, ...)` — Guid, não string
2. `ListarMinhasReservas(Guid usuarioId, ...)` — Guid
3. `ListarVendasDoVendedor(Guid vendedorId, ...)` — Guid

**7c. `Application/Interfaces/IReservaService.cs`:**
1. `FazerReserva(Guid usuarioId, ...)`
2. `ListarMinhasReservas(Guid usuarioId, ...)`
3. `ListarVendasDoVendedor(Guid vendedorId, ...)`

**Validation:**
- [ ] `TokenService` gera claim `userId`
- [ ] `ReservaService` usa `Guid` para identificação de usuário/vendedor

**Status:** [ ] pending

---

### Task 8: Atualizar DTOs + Mappings

**Objective:** Atualizar DTOs de resposta para incluir `Id`.

**Requirements Covered:** FR-010

**Design References:** design.md — Component 13, 15

**Actions:**

1. `UsuarioSaidaDTO.cs`: adicionar `public Guid Id { get; set; }`
2. `UsuarioResponseDTO.cs`: adicionar `public Guid Id { get; set; }`
3. `LoginResponseDTO.cs` (UsuarioLoginDTO): adicionar `public Guid Id { get; set; }`
4. `VendedorCadastradoDTO.cs`: adicionar `public Guid Id { get; set; }`
5. `Application/Mappings/UsuarioProfile.cs`: verificar se AutoMapper funciona (deve funcionar por convenção)

**Validation:**
- [ ] DTOs de resposta têm `Id`
- [ ] `LoginResponseDTO.Usuario` inclui `Id`
- [ ] `VendedorCadastradoDTO` inclui `Id`

**Status:** [ ] pending

---

### Task 9: Atualizar Controllers

**Objective:** Atualizar rotas e extração de claims nos controllers.

**Requirements Covered:** FR-007, FR-009

**Design References:** design.md — Components 10, 11

**Actions:**

**9a. `Api/Controllers/UsuarioController.cs`:**
1. `ListarUsuarioEspecifico/{id:guid}` — `[FromRoute] Guid id`
2. `DeletarUsuario/{id:guid}` — `[FromRoute] Guid id`
3. `alterarsenha/{id:guid}` — `[FromRoute] Guid id`
4. `alterarnome/{id:guid}` — `[FromRoute] Guid id`
5. `alteraremail/{id:guid}` — `[FromRoute] Guid id`

**9b. `Api/Controllers/ReservaController.cs`:**
1. Extrair `userId` do JWT:
```csharp
var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
    return Unauthorized(new { message = "Não foi possível identificar o usuário." });
```
2. `CriarReserva`: usa `userId`
3. `ListarMinhasReservas`: usa `userId`
4. `ListarMinhasVendas`: usa `userId`

**Validation:**
- [ ] Rotas do `UsuarioController` usam `{id:guid}`
- [ ] `ReservaController` extrai `userId` do JWT
- [ ] `dotnet build` compila

**Status:** [ ] pending

---

### Task 10: Atualizar DatabaseSeeder

**Objective:** Atualizar seeder do Admin para usar `Guid Id`.

**Requirements Covered:** FR-011

**Design References:** design.md — Component 14

**Actions:**
1. `Infraestructure/DataBase/DatabaseSeeder.cs`:
   - Admin criado com `Guid.NewGuid()` para `Id`
   - INSERT inclui `Id` column
   - Query `checkAdmin` mantida (busca por Email)

**Validation:**
- [ ] Admin seed funciona após migration
- [ ] `admin@soldout.com` loga com sucesso

**Status:** [ ] pending

---

### Task 11: Build e Testes

**Objective:** Garantir que o projeto compila e os testes passam.

**Requirements Covered:** FR-012

**Design References:** design.md — Testing Strategy

**Actions:**
1. Run `dotnet build` — corrigir erros de compilação
2. Atualizar testes existentes que quebraram com as mudanças de assinatura
3. Run `dotnet test`
4. Verificar que não há novas falhas (além das 2 pré-existentes do `AutoCadastroVendedorTests`)

**Validation:**
- [ ] `dotnet build`: 0 errors
- [ ] `SegurancaTests`: 8/8 pass
- [ ] `CupomTests`: passando
- [ ] `ResilicienciaTests`: 8/8 pass
- [ ] `dotnet test` completo: sem novas falhas

**Status:** [ ] pending

---

## Summary

| # | Layer | File(s) | Action | FRs Covered | Est. Time |
|---|-------|---------|--------|-------------|-----------|
| 1 | db | `Script0013_MigrarPkGuidUsuarios.sql` | NEW: Migration SQL | FR-001, FR-002, FR-003 | 30 min |
| 2 | Domain | `Usuario.cs`, `Reserva.cs`, `Evento.cs` | EDIT: Entities | FR-004, FR-010 | 20 min |
| 3 | Domain | `IUsuarioRepository.cs`, `IReservaRepository.cs`, `IEventoRepository.cs` | EDIT: Interfaces | FR-005, FR-009 | 15 min |
| 4 | Infra | `UsuarioRepository.cs` | EDIT: Queries SQL | FR-005 | 30 min |
| 5 | Infra | `ReservaRepository.cs` | EDIT: Queries SQL | FR-009 | 25 min |
| 6 | App | `UsuarioService.cs`, `IUsuarioService.cs` | EDIT: Service | FR-006 | 20 min |
| 7 | App | `ReservaService.cs`, `TokenService.cs`, `IReservaService.cs`, `EventoService.cs`, `IEventoService.cs`, `PagamentoService.cs`, `IPagamentoService.cs` | EDIT: Services | FR-007, FR-008, FR-009 | 30 min |
| 8 | App | DTOs (9) + Mappings (2) | EDIT: DTOs | FR-010 | 20 min |
| 9 | Api | `UsuarioController.cs`, `ReservaController.cs`, `EventoController.cs`, `PagamentoController.cs` | EDIT: Controllers | FR-007, FR-009 | 20 min |
| 10 | Infra | `EventoRepository.cs`, `PagamentoRepository.cs`, `DatabaseSeeder.cs` | EDIT: Repos + Seeder | FR-011 | 20 min |
| 11 | All | Build + testes | VERIFY | FR-012 | 30 min |

**Total estimated time:** ~4h

**Total files created:** 1 (`Script0013_MigrarPkGuidUsuarios.sql`)

**Total files modified:** 27 (`Usuario.cs`, `Reserva.cs`, `Evento.cs`, `IUsuarioRepository.cs`, `IReservaRepository.cs`, `IEventoRepository.cs`, `UsuarioRepository.cs`, `ReservaRepository.cs`, `EventoRepository.cs`, `PagamentoRepository.cs`, `UsuarioService.cs`, `IUsuarioService.cs`, `ReservaService.cs`, `IReservaService.cs`, `EventoService.cs`, `IEventoService.cs`, `PagamentoService.cs`, `IPagamentoService.cs`, `TokenService.cs`, `UsuarioSaidaDTO.cs`, `UsuarioResponseDTO.cs`, `LoginResponseDTO.cs`, `VendedorCadastradoDTO.cs`, `EventoRequestDTO.cs`, `EventoResponseDTO.cs`, `PagamentoAdminDTO.cs`, `ReservaAdminDTO.cs`, `ReservaVendedorDTO.cs`, `EventoProfile.cs`, `UsuarioProfile.cs`, `UsuarioController.cs`, `ReservaController.cs`, `EventoController.cs`, `PagamentoController.cs`, `DatabaseSeeder.cs`)
