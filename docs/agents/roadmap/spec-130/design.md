---
name: "Isolamento Multi-Tenant (VendedorId)"
status: "audited"
---

# Spec 130 — Design

## 1. Componentes

### Component 1 — Migração SQL (Script0012_AdicionarVendedorCpfReservas.sql)

Adiciona a coluna `VendedorCpf` à tabela `Reservas` com foreign key e índice.

```sql
-- Script0012: Adicionar VendedorCpf à tabela Reservas
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Reservas' AND COLUMN_NAME = 'VendedorCpf'
)
BEGIN
    ALTER TABLE Reservas ADD VendedorCpf NVARCHAR(11) NOT NULL DEFAULT '';

    ALTER TABLE Reservas ADD CONSTRAINT FK_Reservas_Usuarios_Vendedor
        FOREIGN KEY (VendedorCpf) REFERENCES Usuarios(Cpf);

    CREATE INDEX IX_Reservas_VendedorCpf ON Reservas(VendedorCpf);
END
```

### Component 2 — Entidade Reserva (Domain/Entities/Reserva.cs)

Adicionar propriedade `VendedorCpf` e aceitá-la no factory method `Criar()`.

**Antes:**
```csharp
public string UsuarioCpf { get; private set; }
public Guid EventoId { get; private set; }
// ... sem VendedorCpf

public static Reserva Criar(string usuarioCpf, Guid eventoId, List<ItemReserva> itens, Cupom? cupom = null)
```

**Depois:**
```csharp
public string UsuarioCpf { get; private set; }
public Guid EventoId { get; private set; }
public string VendedorCpf { get; private set; } = string.Empty;

public static Reserva Criar(string usuarioCpf, Guid eventoId, List<ItemReserva> itens, Cupom? cupom = null, string vendedorCpf = "")
```

No retorno de `Criar()`:
```csharp
return new Reserva(usuarioCpf, eventoId, codigoCupom, valorFinal)
{
    Itens = itens,
    VendedorCpf = vendedorCpf
};
```

### Component 3 — Repository (Infraestructure/Repository/ReservaRepository.cs)

**3a. Modificar INSERT em `CadastrarReservaComItens`:**
```csharp
const string sqlReserva = @"
    INSERT INTO Reservas (Id, UsuarioCpf, EventoId, CupomUtilizado, ValorFinalPago, VendedorCpf)
    VALUES (@Id, @UsuarioCpf, @EventoId, @CupomUtilizado, @ValorFinalPago, @VendedorCpf)";

await conn.ExecuteAsync(new CommandDefinition(sqlReserva, new
{
    reserva.Id,
    reserva.UsuarioCpf,
    reserva.EventoId,
    reserva.CupomUtilizado,
    reserva.ValorFinalPago,
    reserva.VendedorCpf
}, transacao, cancellationToken: ct));
```

**3b. Novo método `ListarReservasDetalhadasPorVendedor`:**
```csharp
public async Task<IEnumerable<ReservaVendedorDTO>> ListarReservasDetalhadasPorVendedor(
    string vendedorCpf, CancellationToken ct)
{
    using var connection = _factory.CreateConnection();

    const string sql = @"
        SELECT 
            r.Id,
            e.Nome AS NomeEvento,
            e.DataEvento,
            r.ValorFinalPago,
            r.Pago,
            u.Nome AS NomeComprador,
            u.Cpf AS CpfComprador
        FROM Reservas r
        INNER JOIN Eventos e ON r.EventoId = e.Id
        INNER JOIN Usuarios u ON r.UsuarioCpf = u.Cpf
        WHERE r.VendedorCpf = @VendedorCpf
        ORDER BY e.Nome, r.Id";

    return await connection.QueryAsync<ReservaVendedorDTO>(
        new CommandDefinition(sql, new { VendedorCpf = vendedorCpf }, cancellationToken: ct));
}
```

### Component 4 — DTO de Saída (Domain/DTOs/ReservaVendedorDTO.cs)

Novo DTO para retornar vendas do vendedor:
```csharp
namespace Domain.DTOs;

public class ReservaVendedorDTO
{
    public Guid Id { get; set; }
    public string NomeEvento { get; set; } = string.Empty;
    public DateTime DataEvento { get; set; }
    public decimal ValorFinalPago { get; set; }
    public bool Pago { get; set; }
    public string NomeComprador { get; set; } = string.Empty;
    public string CpfComprador { get; set; } = string.Empty;
}
```

### Component 5 — Interface do Repository (Domain/Interface/IReservaRepository.cs)

Adicionar assinatura:
```csharp
Task<IEnumerable<ReservaVendedorDTO>> ListarReservasDetalhadasPorVendedor(
    string vendedorCpf, CancellationToken ct);
```

### Component 6 — Service (Application/Service/ReservaService.cs)

**6a. Modificar `FazerReserva` para passar `VendedorCpf`:**
```csharp
// Após obter o evento (linha 40)
Evento? evento = await _repositoryEvento.GetByIdAsync(dto.EventoId);

// ... validações existentes ...

// Criar reserva com VendedorCpf
Reserva novaReserva = Reserva.Criar(usuarioCpf, dto.EventoId, itens, cupom, evento.VendedorCpf);
```

**6b. Novo método `ListarVendasDoVendedor`:**
```csharp
public async Task<IEnumerable<ReservaVendedorDTO>> ListarVendasDoVendedor(
    string vendedorCpf, CancellationToken ct)
{
    return await _repositoryReserva.ListarReservasDetalhadasPorVendedor(vendedorCpf, ct);
}
```

### Component 7 — Interface do Service (Application/Interfaces/IReservaService.cs)

Adicionar assinatura:
```csharp
Task<IEnumerable<ReservaVendedorDTO>> ListarVendasDoVendedor(string vendedorCpf, CancellationToken ct);
```

### Component 8 — Controller (Api/Controllers/ReservaController.cs)

Novo endpoint:
```csharp
[HttpGet("minhas-vendas")]
[Authorize(Roles = "Vendedor")]
public async Task<IActionResult> ListarMinhasVendas(CancellationToken ct)
{
    var cpf = User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value;
    if (string.IsNullOrEmpty(cpf))
        return Unauthorized(new { message = "Não foi possível identificar o vendedor." });

    var vendas = await _service.ListarVendasDoVendedor(cpf, ct);
    return Ok(vendas);
}
```

## 2. Data Flow

### Flow 1 — Criação de Reserva (com VendedorCpf)
```
POST /api/reserva/criar
  → ReservaController.CriarReserva(dto)
    → Extrai cpfUsuarioLogado do JWT claim "cpf"
    → ReservaService.FazerReserva(usuarioCpf, dto)
      → Busca Evento (contém VendedorCpf)
      → Reserva.Criar(..., evento.VendedorCpf)
      → ReservaRepository.CadastrarReservaComItens(reserva)
        → INSERT Reservas (... VendedorCpf ...)
        → INSERT ItensReserva
        → UPDATE Ingressos (bloqueio)
```

### Flow 2 — Vendedor lista suas vendas
```
GET /api/reserva/minhas-vendas [Authorize(Roles="Vendedor")]
  → ReservaController.ListarMinhasVendas()
    → Extrai cpf do JWT claim "cpf"
    → ReservaService.ListarVendasDoVendedor(cpf)
      → ReservaRepository.ListarReservasDetalhadasPorVendedor(cpf)
        → SELECT ... FROM Reservas WHERE VendedorCpf = @vendedorCpf
```

### Flow 3 — Admin lista todas as reservas (sem alteração)
```
GET /api/reserva/Admin/Todas [Authorize(Roles="Admin")]
  → ReservaController.ListarTodasAdmin()
    → ReservaRepository.ListarTodasDetalhadasAdmin()
      → SELECT ... FROM Reservas (sem filtro VendedorCpf)
```

### Flow 4 — Comprador lista suas reservas (sem alteração)
```
GET /api/reserva/minhas [Authorize]
  → ReservaController.ListarMinhasReservas()
    → Extrai cpf do JWT claim "cpf"
    → ReservaService.ListarMinhasReservas(cpf)
      → ReservaRepository.ListarReservasDetalhadasPorCpf(cpf)
        → SELECT ... WHERE UsuarioCpf = @Cpf
```

## 3. Files Summary

| Arquivo | Tipo | Descrição |
|---------|------|-----------|
| `db/Script0012_AdicionarVendedorCpfReservas.sql` | **Criado** | Migration SQL |
| `Domain/Entities/Reserva.cs` | **Modificado** | +`VendedorCpf`, `Criar()` aceita parâmetro |
| `Domain/DTOs/ReservaVendedorDTO.cs` | **Criado** | DTO de saída para vendas |
| `Domain/Interface/IReservaRepository.cs` | **Modificado** | +`ListarReservasDetalhadasPorVendedor` |
| `Infraestructure/Repository/ReservaRepository.cs` | **Modificado** | INSERT inclui VendedorCpf + novo método |
| `Application/Interfaces/IReservaService.cs` | **Modificado** | +`ListarVendasDoVendedor` |
| `Application/Service/ReservaService.cs` | **Modificado** | Passa VendedorCpf na criação + novo método |
| `Api/Controllers/ReservaController.cs` | **Modificado** | +`GET minhas-vendas` |

**Total: 8 files (2 criados, 6 modificados)**

## 4. Testing Strategy

| # | Teste | Tipo | Cobertura |
|---|-------|------|-----------|
| T1 | Criar reserva → verificar que VendedorCpf foi persistido | Automatizado | FR-001, FR-002 |
| T2 | Vendedor A lista vendas → não vê vendas do Vendedor B | Automatizado | FR-003, FR-004 |
| T3 | Admin lista todas → vê vendas de todos os vendedores | Automatizado | FR-005 |
| T4 | Comprador lista minhas → vê apenas suas reservas | Automatizado | FR-006 |
| T5 | Worker libera assentos → opera sem filtro VendedorCpf | Manual | FR-007 |
| T6 | Vendedor sem eventos → retorna lista vazia | Automatizado | EC-002 |
| T7 | Admin acessa minhas-vendas → 403 Forbidden | Automatizado | EC-004 |

## 5. Migration & Rollback

**Migration (Script0012):**
1. Adicionar coluna `VendedorCpf NVARCHAR(11) NOT NULL DEFAULT ''`
2. Adicionar FK `FK_Reservas_Usuarios_Vendedor`
3. Criar índice `IX_Reservas_VendedorCpf`

**Rollback:**
```sql
DROP INDEX IF EXISTS IX_Reservas_VendedorCpf ON Reservas;
ALTER TABLE Reservas DROP CONSTRAINT IF EXISTS FK_Reservas_Usuarios_Vendedor;
ALTER TABLE Reservas DROP COLUMN IF EXISTS VendedorCpf;
```

## 6. Breaking Changes

| Item | Impacto | Mitigação |
|------|---------|-----------|
| Coluna NOT NULL | Reservas existentes ganham `VendedorCpf = ''` | `DEFAULT ''` evita erro de NOT NULL |
| `Reserva.Criar()` aceita novo parâmetro | Chamadas existentes precisam passar `vendedorCpf` | Parâmetro opcional com default `""` mantém compatibilidade |
| INSERT inclui nova coluna | Transações existentes não quebram | Coluna tem default `''` no banco |
