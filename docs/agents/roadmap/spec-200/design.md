---
name: "Guid Id como PK de Usuarios (substituir Cpf)"
status: "audited"
references:
  - "requirements.md (este diretório)"
  - "ADR-001 (Clean Architecture)"
  - "Spec 130 — Isolamento Multi-Tenant"
  - "Spec 120 — Segurança JWT"
---

# Spec 200: Guid Id como PK de Usuarios — Design

## Design Approach

**Estratégia:** Migration SQL script + refatoração em cascata (Domain → Infrastructure → Application → Api). A migration cria a nova coluna `Id`, migra dados, atualiza FKs e remove colunas antigas. A refatoração de código segue a ordem natural de dependência: entidades primeiro, depois repositórios, depois serviços, depois controllers.

**Princípios:**
1. **Database First:** A migration SQL é o ponto de partida — o código é refatorado para refletir o novo schema.
2. **Surgical Changes:** Apenas o necessário muda. Arquivos não relacionados (ex: `CupomService`, `PagamentoService`, DTOs de cupom/pagamento) não são tocados.
3. **Fallback JWT:** O claim `cpf` é mantido no JWT por tempo limitado para não quebrar tokens existentes.
4. **Transaction Safety:** A migration SQL usa transações para garantir atomicidade.

---

## Architecture Decisions

| Decisão | Justificativa | Referência |
|---------|---------------|------------|
| Guid em vez de int/Identity | Consistência com todas as outras tabelas do sistema | ADR-001 |
| Manter `Cpf` como coluna nullable | Vendedores não têm CPF, compradores sim. Filtered unique index previne duplicatas. | KISS |
| Claim `cpf` mantido (transição) | Tokens existentes não quebram. Removido em spec futura. | NFR-003 |
| `VendedorCpf` → `VendedorId` em Eventos | Eventos já tinham `VendedorCpf` sem FK. Agora ganham FK opcional. | Spec 130 |
| Atualizar testes existentes em vez de criar novos | O escopo é migração, não feature nova. Cobrir regressões é suficiente. | KISS |

---

## Component / Module Breakdown

### Component 1: Migration Script SQL (NEW)

**Purpose:** Script DbUp que executa a migração do banco de `Cpf` PK para `Id` PK.

**File:** `db/Script0013_MigrarPkGuidUsuarios.sql` (NEW)

**Operations (em ordem, dentro de transação):**

```sql
BEGIN TRANSACTION;

-- 1. Adicionar coluna Id se não existir
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Usuarios' AND COLUMN_NAME = 'Id')
    ALTER TABLE Usuarios ADD Id UNIQUEIDENTIFIER NULL;

-- 2. Preencher Id com NEWID() para registros existentes
UPDATE Usuarios SET Id = NEWID() WHERE Id IS NULL;

-- 3. Tornar Id NOT NULL
ALTER TABLE Usuarios ALTER COLUMN Id UNIQUEIDENTIFIER NOT NULL;

-- 4. Remover PK antiga (Cpf)
ALTER TABLE Usuarios DROP CONSTRAINT IF EXISTS PK_Usuarios;

-- 5. Criar nova PK em Id
ALTER TABLE Usuarios ADD CONSTRAINT PK_Usuarios PRIMARY KEY (Id);

-- 6. Tornar Cpf nullable
ALTER TABLE Usuarios ALTER COLUMN Cpf NVARCHAR(11) NULL;

-- 7. Criar filtered unique index em Cpf
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Usuarios_Cpf')
    CREATE UNIQUE INDEX UQ_Usuarios_Cpf ON Usuarios(Cpf) WHERE Cpf IS NOT NULL;

-- 8. Reservas: adicionar UsuarioId e VendedorId
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Reservas' AND COLUMN_NAME = 'UsuarioId')
BEGIN
    ALTER TABLE Reservas ADD UsuarioId UNIQUEIDENTIFIER NULL;
    ALTER TABLE Reservas ADD VendedorId UNIQUEIDENTIFIER NULL;

    -- Migrar dados
    UPDATE r SET r.UsuarioId = u.Id
    FROM Reservas r INNER JOIN Usuarios u ON r.UsuarioCpf = u.Cpf;

    UPDATE r SET r.VendedorId = u.Id
    FROM Reservas r INNER JOIN Usuarios u ON r.VendedorCpf = u.Cpf;

    -- Tornar NOT NULL
    ALTER TABLE Reservas ALTER COLUMN UsuarioId UNIQUEIDENTIFIER NOT NULL;
END

-- 9. Remover FKs antigas de Reservas
ALTER TABLE Reservas DROP CONSTRAINT IF EXISTS FK_Reservas_Usuarios;
ALTER TABLE Reservas DROP CONSTRAINT IF EXISTS FK_Reservas_Usuarios_Vendedor;

-- 10. Criar novas FKs em Reservas
ALTER TABLE Reservas ADD CONSTRAINT FK_Reservas_Usuarios 
    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id);
ALTER TABLE Reservas ADD CONSTRAINT FK_Reservas_Vendedor 
    FOREIGN KEY (VendedorId) REFERENCES Usuarios(Id);

-- 11. Remover colunas antigas de Reservas
ALTER TABLE Reservas DROP COLUMN IF EXISTS UsuarioCpf;
ALTER TABLE Reservas DROP COLUMN IF EXISTS VendedorCpf;

-- 12. Eventos: adicionar VendedorId
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Eventos' AND COLUMN_NAME = 'VendedorId')
BEGIN
    ALTER TABLE Eventos ADD VendedorId UNIQUEIDENTIFIER NULL;

    UPDATE e SET e.VendedorId = u.Id
    FROM Eventos e INNER JOIN Usuarios u ON e.VendedorCpf = u.Cpf;
END

-- 13. Remover FK antiga de Eventos (se existir)
--    e coluna VendedorCpf
ALTER TABLE Eventos DROP COLUMN IF EXISTS VendedorCpf;

-- 14. Criar FK em Eventos (opcional)
--    VendedorId pode ser NULL (eventos criados por Admin ou sem vendedor)
ALTER TABLE Eventos ADD CONSTRAINT FK_Eventos_Vendedor 
    FOREIGN KEY (VendedorId) REFERENCES Usuarios(Id);

COMMIT;
```

**Dependencies:** Scripts 0000 a 0012 já executados.

---

### Component 2: Domain Entity — Usuario (EDIT)

**File:** `Domain/Entities/Usuario.cs`

**Changes:**
1. Adicionar `public Guid Id { get; private set; } = Guid.NewGuid();`
2. `Cpf` muda de `string.Empty` para `null` (nullable)
3. `CriarVendedor` seta `Id = Guid.NewGuid()` e `Cpf = null`
4. `CriarComprador` seta `Id = Guid.NewGuid()`
5. Construtor principal atualizado para aceitar `Guid id`

**New constructor:**
```csharp
public Usuario(Guid id, string cpf, string nome, string email, Guid perfilid, string senha)
{
    Id = id;
    Cpf = cpf;
    Nome = nome;
    Email = email;
    PerfilId = perfilid;
    Senha = senha;
}
```

**CriarComprador updated:**
```csharp
public static Usuario CriarComprador(string cpf, string nome, string email, string senhaHash)
{
    cpf = (cpf ?? string.Empty).Replace(".", "").Replace("-", "").Trim();
    
    var usuario = new Usuario
    {
        Id = Guid.NewGuid(),
        Cpf = cpf,
        Nome = nome.Trim(),
        Email = email,
        PerfilId = Guid.Parse("C3C3C3C3-C3C3-C3C3-C3C3-C3C3C3C3C3C3"),
        Senha = senhaHash
    };
    // ... validações mantidas
    return usuario;
}
```

---

### Component 3: Domain Entities — Reserva & Evento (EDIT)

**Files:** `Domain/Entities/Reserva.cs`, `Domain/Entities/Evento.cs`

**Reserva changes:**
- `UsuarioCpf` → `UsuarioId` (Guid)
- `VendedorCpf` → `VendedorId` (Guid?)
- Construtor e factory `Criar` atualizados
- `Reserva.Criar(string usuarioCpf, ...)` → `Reserva.Criar(Guid usuarioId, ...)`

**Evento changes:**
- `VendedorCpf` → `VendedorId` (Guid?)
- Construtor atualizado

---

### Component 4: Repository Interfaces (EDIT)

**File:** `Domain/Interface/IUsuarioRepository.cs`

**New/Updated methods:**
```csharp
Task<Usuario?> BuscarPorId(Guid id, CancellationToken ct);        // NEW
Task<Usuario?> BuscarCpf(string cpf, CancellationToken ct);        // mantido
void CadastrarUsuario(Usuario usuario);                             // Id via usuario.Id
int CadastrarVendedor(Usuario vendedor);                            // Id via vendedor.Id
Task RemoverUsuario(Guid id, CancellationToken ct);                 // Guid, não Usuario
Task AtualizarSenha(Guid id, string novaSenha, CancellationToken ct); // Guid, não string cpf
Task AtualizarEmailAsync(Guid id, string novoEmail, CancellationToken ct);
Task AtualizarNomeAsync(Guid id, string novoNome, CancellationToken ct);
```

**File:** `Domain/Interface/IReservaRepository.cs`

**Updated methods:**
```csharp
Task<IEnumerable<Reserva>> ListarPorUsuarioId(Guid usuarioId, CancellationToken ct);
Task<IEnumerable<ReservaDetalhadaDTO>> ListarReservasDetalhadasPorUsuarioId(Guid usuarioId, CancellationToken ct);
Task<IEnumerable<ReservaVendedorDTO>> ListarReservasDetalhadasPorVendedorId(Guid vendedorId, CancellationToken ct);
```

---

### Component 5: UsuarioRepository (EDIT)

**File:** `Infraestructure/Repository/UsuarioRepository.cs`

**All SQL queries updated:**
- INSERTs incluem `Id` column
- WHERE clauses usam `Id = @Id` para operações de identificação única
- `BuscarCpf` mantido (busca por CPF ainda é necessária no cadastro de comprador)
- `RemoverUsuario(Guid id)` — DELETE por Id
- `AtualizarSenha(Guid id, ...)` — UPDATE por Id
- `AtualizarEmailAsync(Guid id, ...)` — UPDATE por Id
- `AtualizarNomeAsync(Guid id, ...)` — UPDATE por Id
- `BuscarPorId(Guid id, ...)` — SELECT por Id com JOIN de Perfil

---

### Component 6: ReservaRepository (EDIT)

**File:** `Infraestructure/Repository/ReservaRepository.cs`

**SQL changes:**
- `CadastrarReservaComItens`: INSERT usa `UsuarioId`/`VendedorId` em vez de `UsuarioCpf`/`VendedorCpf`
- `ListarPorCpf` → `ListarPorUsuarioId(Guid usuarioId)` — WHERE `UsuarioId = @UsuarioId`
- `ReservaExistenteParaCpfNoEvento` — mantém check por CpfParticipante (coluna `ItensReserva` não muda)
- `ListarReservasDetalhadasPorCpf` → `ListarReservasDetalhadasPorUsuarioId`
  - JOIN muda: `INNER JOIN Usuarios u ON r.UsuarioId = u.Id`
  - SELECT muda: `u.Id AS UsuarioId` em vez de `u.Cpf AS CpfUsuario`
- `ListarTodasDetalhadasAdmin`: JOIN atualizado, seleciona `u.Id AS UsuarioId`
- `ListarReservasDetalhadasPorVendedor(string vendedorCpf)` → `(Guid vendedorId)`:
  - WHERE `r.VendedorId = @VendedorId`
  - JOIN: `INNER JOIN Usuarios u ON r.UsuarioId = u.Id`

---

### Component 7: UsuarioService (EDIT)

**File:** `Application/Service/UsuarioService.cs`

**Changes:**
- `UsuarioEspecifico(Guid id, ...)` — busca por Id
- `RemoverUsuario(Guid id, ...)` — remove por Id
- `AlterarSenha(Guid id, AlterarSenhaDTO dto, ...)` — parâmetro Guid
- `AlterarEmailAsync(Guid id, AlterarEmailDTO dto, ...)` — parâmetro Guid
- `AlterarNomeAsync(Guid id, AlterarNomeDTO dto, ...)` — parâmetro Guid
- `CadastrarComprador`: sem alteração na assinatura, busca por CPF mantida
- `Login`: sem alteração (busca por Email)
- `ListarUsuariosAsync`: mapeia `u.Id` nos DTOs de resposta

---

### Component 8: IUsuarioService (EDIT)

**File:** `Application/Interfaces/IUsuarioService.cs`

**Updated signatures:**
```csharp
Task<UsuarioSaidaDTO> UsuarioEspecifico(Guid id, CancellationToken ct);
Task RemoverUsuario(Guid id, CancellationToken ct);
Task AlterarSenha(AlterarSenhaDTO dto, Guid id, CancellationToken ct);
Task AlterarEmailAsync(Guid id, AlterarEmailDTO dto, CancellationToken ct);
Task AlterarNomeAsync(Guid id, AlterarNomeDTO dto, CancellationToken ct);
```

---

### Component 9: TokenService (EDIT)

**File:** `Application/Service/TokenService.cs`

**Change:** Adicionar claim `userId`:
```csharp
var claimsIdentity = new ClaimsIdentity(new[]
{
    new Claim("userId", usuario.Id.ToString()),
    new Claim("cpf", usuario.Cpf ?? ""),  // opcional
    new Claim("perfilId", usuario.PerfilId.ToString()),
    new Claim("email", usuario.Email),
    new Claim("role", role),
    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
});
```

---

### Component 10: UsuarioController (EDIT)

**File:** `Api/Controllers/UsuarioController.cs`

**Route changes (string cpf → Guid id):**
- `GET "ListarUsuarioEspecifico/{id:guid}"` — `[FromRoute] Guid id`
- `DELETE "DeletarUsuario/{id:guid}"` — `[FromRoute] Guid id`
- `PUT "alterarsenha/{id:guid}"` — `[FromRoute] Guid id`
- `PUT "alterarnome/{id:guid}"` — `[FromRoute] Guid id`
- `PUT "alteraremail/{id:guid}"` — `[FromRoute] Guid id`

---

### Component 11: ReservaController (EDIT)

**File:** `Api/Controllers/ReservaController.cs`

**Change:** Extrair `userId` do JWT em vez de `cpf`:
```csharp
var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value
                  ?? User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value;

if (string.IsNullOrEmpty(userIdClaim))
    return Unauthorized(new { message = "Não foi possível identificar o usuário." });

// Para endpoints que ainda precisam de Guid:
if (!Guid.TryParse(userIdClaim, out var userId))
    // Fallback: buscar por CPF
```

**Updated methods:**
- `CriarReserva`: usa `userId` Guid
- `ListarMinhasReservas`: usa `userId` Guid
- `ListarMinhasVendas`: usa `userId` Guid

---

### Component 12: ReservaService (EDIT)

**File:** `Application/Service/ReservaService.cs`

**Changes:**
- `FazerReserva(Guid usuarioId, ...)` — Guid, não string
- `ListarMinhasReservas(Guid usuarioId, ...)` — Guid
- `ListarVendasDoVendedor(Guid vendedorId, ...)` — Guid

---

### Component 13: DTOs (EDIT)

**Files to update:**

| DTO | Change |
|-----|--------|
| `UsuarioSaidaDTO.cs` | Adicionar `Guid Id` |
| `UsuarioResponseDTO.cs` | Adicionar `Guid Id` |
| `LoginResponseDTO.UsuarioLoginDTO` | Adicionar `Guid Id` |
| `VendedorCadastradoDTO.cs` | Adicionar `Guid Id`, remover `Cpf` (ou manter vazio) |
| `EventoRequestDTO.cs` | `VendedorCpf` (string) → remover (não usado pelo controller; service seta via JWT) |
| `EventoResponseDTO.cs` | `VendedorCpf` (string) → `VendedorId` (Guid?) |
| `PagamentoAdminDTO.cs` | `string UsuarioCpf` → `string UsuarioId` (Guid como string para exibição) |
| `ReservaAdminDTO.cs` | `string CpfUsuario` → `string UsuarioId` (Guid como string) |
| `ReservaVendedorDTO.cs` | `string CpfComprador` → `string CompradorId` (Guid como string) |

---

### Component 14: DatabaseSeeder (EDIT)

**File:** `Infraestructure/DataBase/DatabaseSeeder.cs`

**Change:** Admin com `Guid Id`:
```csharp
var admin = new Usuario(
    Guid.NewGuid(),           // Id
    "00000000000",            // Cpf
    "Administrador",          // Nome
    "admin@soldout.com",      // Email
    AdminPerfilId,            // PerfilId
    senhaHash                 // Senha
);

const string insertAdmin = @"
    INSERT INTO Usuarios (Id, Cpf, Nome, Email, PerfilId, Senha, Ativo, DataCriacao)
    VALUES (@Id, @Cpf, @Nome, @Email, @PerfilId, @Senha, 1, GETDATE());";
```

---

### Component 15: AutoMapper Profiles (VERIFY/EDIT)

**Files:** `Application/Mappings/UsuarioProfile.cs`, `Application/Mappings/EventoProfile.cs`

**UsuarioProfile:** AutoMapper mapeia por nome. Como DTOs ganham `Id`, funciona automaticamente.

**EventoProfile:** 
- `CreateMap<Evento, EventoResponseDTO>()` — `VendedorCpf` → `VendedorId`: precisa de `.ForMember(dest => dest.VendedorId, opt => opt.MapFrom(src => src.VendedorId))` se nomes divergirem. Como ambos viram `VendedorId`, funciona por convenção.
- `CreateMap<EventoRequestDTO, Evento>()` — `VendedorCpf` foi removido do RequestDTO. Sem impacto.

---

### Component 16: IEventoRepository + EventoRepository (EDIT)

**Files:** `Domain/Interface/IEventoRepository.cs`, `Infraestructure/Repository/EventoRepository.cs`

**IEventoRepository changes:**
- `GetAllByVendedorAsync(string vendedorCpf)` → `GetAllByVendedorAsync(Guid vendedorId)`

**EventoRepository changes:**
- `GetAllByVendedorAsync(string vendedorCpf)` → `GetAllByVendedorAsync(Guid vendedorId)`:
  - SQL: `WHERE VendedorId = @VendedorId` em vez de `WHERE VendedorCpf = @VendedorCpf`
- `CriarEventoCompletoAsync`:
  - INSERT: `VendedorCpf` → `VendedorId` na lista de colunas e valores
  - Dapper mapeia automaticamente se a entidade tiver `VendedorId`

---

### Component 17: IEventoService + EventoService (EDIT)

**Files:** `Application/Interfaces/IEventoService.cs`, `Application/Service/EventoService.cs`

**IEventoService changes:**
- `GetAllByVendedorAsync(string vendedorCpf)` → `GetAllByVendedorAsync(Guid vendedorId)`
- `CriarEventoAsync(EventoRequestDTO dto, string vendedorCpf)` → `(EventoRequestDTO dto, Guid vendedorId)`
- `UpdateAsync(Guid id, EventoRequestDTO dto, string vendedorCpf)` → `(Guid id, EventoRequestDTO dto, Guid vendedorId)`
- `DeleteAsync(Guid id, string vendedorCpf, bool isAdmin)` → `(Guid id, Guid vendedorId, bool isAdmin)`

**EventoService changes:**
- `GetAllByVendedorAsync(Guid vendedorId)` — repassa para repositório
- `CriarEventoAsync(dto, Guid vendedorId)`:
  - Remove: `eventoDto.VendedorCpf = vendedorCpf;`
  - O `vendedorId` é passado para a entidade `Evento` via AutoMapper ou diretamente
  - Como `EventoRequestDTO` não tem mais `VendedorCpf`, o service precisa setar `VendedorId` na entidade após o Map: `evento.VendedorId = vendedorId;`
- `UpdateAsync(id, dto, Guid vendedorId)`:
  - `evento.VendedorCpf != vendedorCpf` → `evento.VendedorId != vendedorId`
- `DeleteAsync(id, Guid vendedorId, isAdmin)`:
  - `evento.VendedorCpf != vendedorCpf` → `evento.VendedorId != vendedorId`

---

### Component 18: IPagamentoService + PagamentoService (EDIT)

**Files:** `Application/Interfaces/IPagamentoService.cs`, `Application/Service/PagamentoService.cs`

**IPagamentoService changes:**
- `ConfirmarCheckout(Guid reservaId, string usuarioCpf, string metodo, CancellationToken ct)` → `(Guid reservaId, Guid usuarioId, string metodo, CancellationToken ct)`

**PagamentoService changes:**
- `ConfirmarCheckout(Guid reservaId, Guid usuarioId, ...)`:
  - `reserva.UsuarioCpf != usuarioCpf` → `reserva.UsuarioId != usuarioId`
- `ListarTodosAdmin`:
  - `p.Reserva?.UsuarioCpf ?? ""` → `p.Reserva?.UsuarioId.ToString() ?? ""`

---

### Component 19: PagamentoRepository (EDIT)

**File:** `Infraestructure/Repository/PagamentoRepository.cs`

**Changes:**
- `ListarTodosAdmin` query:
  - `SELECT p.*, r.UsuarioCpf, r.EventoId` → `SELECT p.*, r.UsuarioId, r.EventoId`
  - `splitOn: "UsuarioCpf"` → `splitOn: "UsuarioId"`

---

### Component 20: EventoController + PagamentoController (EDIT)

**Files:** `Api/Controllers/EventoController.cs`, `Api/Controllers/PagamentoController.cs`

**EventoController changes (todos os métodos extraem `cpf` do JWT):**
- Extrair `userId` do JWT:
```csharp
var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
    return Unauthorized();
```
- `GetMeusEventosAsync`: `_eventoService.GetAllByVendedorAsync(userId)`
- `CreateAsync`: `_eventoService.CriarEventoAsync(evento, userId)`
- `UpdateAsync`: `_eventoService.UpdateAsync(id, evento, userId)`
- `DeleteAsync`: `_eventoService.DeleteAsync(id, userId, isAdmin)`

**PagamentoController changes:**
- `Checkout`: extrair `userId` do JWT em vez de `cpf`
- `_service.ConfirmarCheckout(reservaId, userId, dto.Metodo, ct)`

---

## Data Flow

### Flow 1: Cadastro de Vendedor (novo fluxo)

```
POST /api/usuario/cadastrar-vendedor
    │
    ▼
UsuarioController.CadastrarVendedor(dto)
    │
    ▼
UsuarioService.CadastrarVendedor(dto)
    │  Validar CNPJ
    │  Verificar unicidade (CNPJ/Email)
    │  BCrypt.HashPassword(senha)
    │  Usuario.CriarVendedor(...) → Id = Guid.NewGuid(), Cpf = null
    ▼
UsuarioRepository.CadastrarVendedor(vendedor)
    │  INSERT INTO Usuarios (Id, Nome, NomeFantasia, Email, Senha, PerfilId, Cnpj, ...)
    │  VALUES (@Id, @Nome, ...)  -- Id = vendedor.Id (Guid.NewGuid())
    ▼
HTTP 201 Created
```

### Flow 2: Reserva (novo fluxo)

```
POST /api/reserva/criar (Authorization: Bearer <JWT>)
    │
    ▼
ReservaController.CriarReserva(dto)
    │  var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
    │  var userId = Guid.Parse(userIdClaim);
    │
    ▼
ReservaService.FazerReserva(userId, dto)
    │  Busca evento, itens, cupom
    │  Reserva.Criar(userId, eventoId, itens, cupom, evento.VendedorId)
    │
    ▼
ReservaRepository.CadastrarReservaComItens(reserva)
    │  INSERT INTO Reservas (Id, UsuarioId, EventoId, VendedorId, ...)
    │  INSERT INTO ItensReserva (...)
    │  UPDATE Ingressos SET Status = 1 ...
    ▼
HTTP 200 { reservaId: "abc-123" }
```

---

## File / Module Layout

```
ProjetoTicket/
├── db/
│   └── Script0013_MigrarPkGuidUsuarios.sql              ← NEW (Component 1)
├── Domain/
│   ├── Entities/
│   │   ├── Usuario.cs                                    ← EDIT (Component 2)
│   │   ├── Reserva.cs                                    ← EDIT (Component 3)
│   │   └── Evento.cs                                     ← EDIT (Component 3)
│   ├── Interface/
│   │   ├── IUsuarioRepository.cs                         ← EDIT (Component 4)
│   │   ├── IReservaRepository.cs                         ← EDIT (Component 4)
│   │   └── IEventoRepository.cs                          ← EDIT (Component 16)
│   └── DTOs/
│       ├── ReservaAdminDTO.cs                            ← EDIT (Component 13)
│       └── ReservaVendedorDTO.cs                         ← EDIT (Component 13)
├── Infraestructure/
│   ├── Repository/
│   │   ├── UsuarioRepository.cs                          ← EDIT (Component 5)
│   │   ├── ReservaRepository.cs                          ← EDIT (Component 6)
│   │   ├── EventoRepository.cs                           ← EDIT (Component 16)
│   │   └── PagamentoRepository.cs                        ← EDIT (Component 19)
│   └── DataBase/
│       └── DatabaseSeeder.cs                             ← EDIT (Component 14)
├── Application/
│   ├── Interfaces/
│   │   ├── IUsuarioService.cs                            ← EDIT (Component 8)
│   │   ├── IReservaService.cs                            ← EDIT (Component 7)
│   │   ├── IEventoService.cs                             ← EDIT (Component 17)
│   │   └── IPagamentoService.cs                          ← EDIT (Component 18)
│   ├── Service/
│   │   ├── UsuarioService.cs                             ← EDIT (Component 7)
│   │   ├── ReservaService.cs                             ← EDIT (Component 12)
│   │   ├── EventoService.cs                              ← EDIT (Component 17)
│   │   ├── PagamentoService.cs                           ← EDIT (Component 18)
│   │   └── TokenService.cs                               ← EDIT (Component 9)
│   ├── DTOs/
│   │   ├── UsuarioSaidaDTO.cs                            ← EDIT (Component 13)
│   │   ├── UsuarioResponseDTO.cs                         ← EDIT (Component 13)
│   │   ├── LoginResponseDTO.cs                           ← EDIT (Component 13)
│   │   ├── VendedorCadastradoDTO.cs                      ← EDIT (Component 13)
│   │   ├── EventoRequestDTO.cs                           ← EDIT (Component 13)
│   │   ├── EventoResponseDTO.cs                          ← EDIT (Component 13)
│   │   └── PagamentoAdminDTO.cs                          ← EDIT (Component 13)
│   └── Mappings/
│       ├── UsuarioProfile.cs                             ← VERIFY (Component 15)
│       └── EventoProfile.cs                              ← VERIFY (Component 15)
├── Api/
│   └── Controllers/
│       ├── UsuarioController.cs                          ← EDIT (Component 10)
│       ├── ReservaController.cs                          ← EDIT (Component 11)
│       ├── EventoController.cs                           ← EDIT (Component 20)
│       └── PagamentoController.cs                        ← EDIT (Component 20)
└── tests/
    └── (testes existentes atualizados)                    ← EDIT (FR-012)
```

**Total files created:** 1 (`Script0013_MigrarPkGuidUsuarios.sql`)
**Total files modified:** 27 (ver lista acima)

---

## Testing Strategy

### Test Updates (não novos testes)

Os testes existentes serão atualizados para refletir as novas assinaturas:

| Arquivo de Teste | Mudança Esperada |
|-----------------|------------------|
| `SegurancaTests.cs` | Mock de `IUsuarioRepository` — métodos com Guid |
| `CupomTests.cs` | Sem mudança (cupons não referenciam Cpf diretamente) |
| `ResilicienciaTests.cs` | Sem mudança (testa middleware e DTOs) |
| `AutoCadastroVendedorTests.cs` | Já falham (2 falhas pré-existentes) |
| `EventoTests.cs` | Atualizar referências a `VendedorCpf` → `VendedorId` |

### Manual Verification

| # | Verificação |
|---|------------|
| V1 | Rodar migration `Script0013` — sem erros |
| V2 | Admin seed funciona após migration — `admin@soldout.com` loga |
| V3 | Cadastrar vendedor (sem CPF) — sucesso, Id gerado |
| V4 | Cadastrar comprador (com CPF) — sucesso, Id gerado |
| V5 | Login com comprador — retorna JWT com claims `userId` + `cpf` |
| V6 | Login com vendedor — retorna JWT com claims `userId` + `cpf` vazio |
| V7 | Criar reserva autenticado — sucesso |
| V8 | Listar minhas reservas — sucesso |
| V9 | `dotnet build` — 0 erros |
| V10 | `dotnet test` — sem novas falhas |

---

## Migration / Rollback

### Migration Steps

1. **Executar migration SQL** (Script0013) — altera schema do banco
2. **Atualizar Domain** (Usuario, Reserva, Evento) — reflete novo schema
3. **Atualizar Repository interfaces** — novas assinaturas
4. **Atualizar Repositories** — queries SQL
5. **Atualizar Services** — consumir novas assinaturas
6. **Atualizar TokenService** — claims expandidos
7. **Atualizar Controllers** — rotas com Guid
8. **Atualizar DTOs** — incluir Id
9. **Atualizar DatabaseSeeder** — admin com Guid
10. **Build + testes** — verificação

### Rollback Strategy

**Database:** Restaurar backup pré-migration. Não há rollback automático (migration destrutiva: remove colunas `UsuarioCpf`, `VendedorCpf`).

**Código:** `git revert` do commit da spec 200.

### Breaking Changes

1. **Rotas da API mudam:** `{cpf}` → `{id:guid}` nos endpoints do UsuarioController. O frontend Blazor e qualquer cliente HTTP precisam ser atualizados.
2. **JWT claims expandidos:** O claim `userId` é adicionado. Clientes que só buscavam `cpf` precisam buscar também `userId`.
3. **Colunas removidas:** `Reservas.UsuarioCpf`, `Reservas.VendedorCpf`, `Eventos.VendedorCpf` são removidas permanentemente.
4. **PK do banco:** A tabela `Usuarios` tem nova PK. Qualquer query raw SQL que use `Cpf` como FK quebra.
