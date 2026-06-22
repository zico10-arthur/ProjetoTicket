---
name: "Cupons de Desconto — AdminId via JWT"
status: "audited"
references:
  - "requirements.md (Spec 160)"
  - "ADR-004 (JWT)"
---

# Spec 160: Cupons de Desconto — Design

## Summary

Esta spec remove as 7 ocorrências de `AdminId` passado via rota/corpo/DTO, substituindo-as por extração da claim `perfilId` do JWT. Apenas 2 camadas são afetadas: Api (Controller) e Application (DTOs). O service e o Domain permanecem inalterados.

## Components

### Component 1: CupomController — Extração de AdminId do JWT

**File:** `Api/Controllers/CupomController.cs`

**Current state (anti-pattern):**
```csharp
// AdminId via rota
[HttpPost("CadastrarCupom/{Id}")]
public async Task<IActionResult> CadastrarCupom([FromBody] CadastrarCupomDTO dto, [FromRoute] Guid Id, CancellationToken ct)
{
    await _service.CadastrarCupom(dto, ct, Id);
    ...
}

// AdminId via body
[HttpDelete("DeletarCupom/{codigo}")]
public async Task<IActionResult> DeletarCupom([FromRoute] string codigo, [FromBody] Guid adminId, CancellationToken ct)
{
    await _service.DeletarCupom(adminId, codigo, ct);
    ...
}

// AdminId via DTO
[HttpPatch("{codigo}/ValorMinimo")]
public async Task<IActionResult> AlterarValorMinimo([FromRoute] string codigo, [FromBody] AlterarValorMinimoDTO dto, CancellationToken ct)
{
    await _service.AlterarValorMinimo(dto.AdminId, codigo, dto.NovoValor, ct);
    ...
}
```

**New state (JWT extraction):**
```csharp
// Helper method
private Guid GetAdminIdFromJwt()
{
    var perfilIdStr = User.Claims.FirstOrDefault(c => c.Type == "perfilId")?.Value;
    if (string.IsNullOrEmpty(perfilIdStr) || !Guid.TryParse(perfilIdStr, out var perfilId))
        throw new UnauthorizedAccessException("Token inválido: perfil não identificado.");
    return perfilId;
}

// AdminId do JWT
[HttpPost("CadastrarCupom")]  // rota sem {Id}
public async Task<IActionResult> CadastrarCupom([FromBody] CadastrarCupomDTO dto, CancellationToken ct)
{
    var adminId = GetAdminIdFromJwt();
    await _service.CadastrarCupom(dto, ct, adminId);
    ...
}

// AdminId do JWT
[HttpDelete("DeletarCupom/{codigo}")]
public async Task<IActionResult> DeletarCupom([FromRoute] string codigo, CancellationToken ct)
{
    var adminId = GetAdminIdFromJwt();
    await _service.DeletarCupom(adminId, codigo, ct);
    ...
}
```

**Changes per method:**

| Method | Change |
|--------|--------|
| `CadastrarCupom` | Remove `[FromRoute] Guid Id`; rota muda de `CadastrarCupom/{Id}` para `CadastrarCupom`; extrai `perfilId` do JWT |
| `DeletarCupom` | Remove `[FromBody] Guid adminId`; extrai `perfilId` do JWT |
| `AlterarValorMinimo` | Remove `dto.AdminId`; extrai `perfilId` do JWT |
| `AlterarDataExpiracao` | Remove `dto.AdminId`; extrai `perfilId` do JWT |
| `AlternarStatus` | Remove `[FromBody] Guid adminId`; extrai `perfilId` do JWT |
| `AlterarDesconto` | Remove `dto.AdminId`; extrai `perfilId` do JWT |
| `ListarTodosCupons` | Substitui `Guid.Empty` por `perfilId` do JWT |
| `ListarCuponsValidos` | Sem alteração (endpoint público) |
| `DebugClaims` | Sem alteração |

### Component 2: DTOs — Remoção de AdminId

**Files:**
- `Application/DTOs/AlterarValorMinimoDTO.cs`
- `Application/DTOs/AlterarDataExpiracaoDTO.cs`
- `Application/DTOs/AlterarDescontoDTO.cs`

**Change:** Remover campo `public Guid AdminId { get; set; }` de cada DTO.

**AlterarValorMinimoDTO — antes:**
```csharp
public class AlterarValorMinimoDTO
{
    public decimal NovoValor { get; set; }
    public Guid AdminId { get; set; } 
}
```

**AlterarValorMinimoDTO — depois:**
```csharp
public class AlterarValorMinimoDTO
{
    public decimal NovoValor { get; set; }
}
```

Mesma transformação para `AlterarDataExpiracaoDTO` e `AlterarDescontoDTO`.

### Component 3: Exception Handling (GlobalExceptionHandler)

**File:** `Api/Middlewares/GlobalExceptionHandlerMiddleware.cs`

**Current pattern:** O middleware usa switch expression (não try/catch) para mapear exceções → HTTP status codes.

**Change:** Adicionar `UnauthorizedAccessException` ao switch expression (lançada quando `perfilId` não está no JWT) → HTTP 401.

**Before:**
```csharp
var (statusCode, message) = ex switch
{
    Application.Exceptions.LoginErro
        => (StatusCodes.Status401Unauthorized, "Email ou senha inválidos."),
    Application.Exceptions.CnpjJaCadastrado or ...
        => (StatusCodes.Status409Conflict, ex.Message),
    DomainException => (StatusCodes.Status400BadRequest, ex.Message),
    KeyNotFoundException => (StatusCodes.Status404NotFound, "Recurso não encontrado."),
    _ => (StatusCodes.Status500InternalServerError, "Ocorreu um erro interno no servidor.")
};
```

**After (add before `_` default):**
```csharp
    UnauthorizedAccessException
        => (StatusCodes.Status401Unauthorized, ex.Message),
```

### Component 4: Web Frontend — AdminId Removal

**Files encontrados com `AdminId`:**

1. **`Web/Components/Pages/Admin/CriarCupom.razor:56`** — Envia `{adminId}` na URL:
   ```csharp
   // Antes:
   var response = await Http.PostAsJsonAsync($"api/Cupom/CadastrarCupom/{adminId}", novoCupom);
   // Depois:
   var response = await Http.PostAsJsonAsync("api/Cupom/CadastrarCupom", novoCupom);
   ```

2. **`Web/Components/Pages/Admin/Cupons.razor:147`** — Envia `adminId` no body do DELETE:
   ```csharp
   // Antes:
   request.Content = JsonContent.Create(adminId);
   // Depois: remover esta linha (não enviar body)
   ```

3. **`Web/Components/Pages/Admin/Cupons.razor:183`** — Envia `adminId` no body do PATCH AlternarStatus:
   ```csharp
   // Antes:
   request.Content = JsonContent.Create(adminId);
   // Depois: remover esta linha (não enviar body)
   ```

4. **`Web/Components/Pages/Admin/EditarCupom.razor:100-109`** — Envia `AdminId` em DTOs anônimos:
   ```csharp
   // Antes:
   var dtoDesconto = new { AdminId = adminId, novoDesconto = cupom.PorcentagemDesconto };
   var dtoValor = new { AdminId = adminId, NovoValor = cupom.ValorMinimo };
   var dtoData = new { AdminId = adminId, novaData = dataTemp ?? DateTime.Now };
   // Depois:
   var dtoDesconto = new { novoDesconto = cupom.PorcentagemDesconto };
   var dtoValor = new { NovoValor = cupom.ValorMinimo };
   var dtoData = new { novaData = dataTemp ?? DateTime.Now };
   ```

5. **`Web/Models/CupomViewModel.cs`** — Verificar se há campo `AdminId`.

---

## Data Flow

### Antes (vulnerável):
```
Client → POST /api/cupom/CadastrarCupom/{adminId-forjado}
         Body: { codigo, desconto, valorMinimo, dataExpiracao }
       → Controller usa adminId da rota
       → Service recebe AdminLogado forjado
```

### Depois (seguro):
```
Client → POST /api/cupom/CadastrarCupom
         Header: Authorization: Bearer <JWT>
         Body: { codigo, desconto, valorMinimo, dataExpiracao }
       → Controller extrai perfilId das claims do JWT
       → Service recebe AdminLogado autêntico
```

---

## Files Summary

| File | Action | Description |
|------|--------|-------------|
| `Api/Controllers/CupomController.cs` | Edit | Remove AdminId de rota/body; adiciona `GetAdminIdFromJwt()`; atualiza 7 métodos |
| `Application/DTOs/AlterarValorMinimoDTO.cs` | Edit | Remove campo `AdminId` |
| `Application/DTOs/AlterarDataExpiracaoDTO.cs` | Edit | Remove campo `AdminId` |
| `Application/DTOs/AlterarDescontoDTO.cs` | Edit | Remove campo `AdminId` |
| `Api/Middlewares/GlobalExceptionHandlerMiddleware.cs` | Edit | Adiciona `UnauthorizedAccessException` ao switch expression → 401 |
| `Web/Components/Pages/Admin/CriarCupom.razor` | Edit | Remove `{adminId}` da URL de CadastrarCupom |
| `Web/Components/Pages/Admin/Cupons.razor` | Edit | Remove `adminId` do body em DeletarCupom e AlternarStatus |
| `Web/Components/Pages/Admin/EditarCupom.razor` | Edit | Remove `AdminId` de DTOs anônimos (3 ocorrências) |
| `Web/Models/CupomViewModel.cs` | Verify | Confirmar ausência de campo AdminId |
| `Application/Interfaces/ICupomService.cs` | No change | Interface mantida |
| `Application/Service/CupomService.cs` | No change | Implementação mantida |
| `Domain/Entities/Cupom.cs` | No change | Entidade mantida |
| `Infraestructure/Repository/CupomRepository.cs` | No change | Repositório mantido |
| `tests/CupomTests.cs` | No change | Testes de domínio mantidos |

**Total: 7 arquivos editados, 6 arquivos verificados (sem alteração)**

---

## Breaking Change

A rota `POST /api/cupom/CadastrarCupom/{Id}` muda para `POST /api/cupom/CadastrarCupom`. Clientes que usavam a rota antiga (passando AdminId na URL) precisam ser atualizados para não passar o `{Id}`.

**Mitigação:** O frontend Blazor (`Web/`) deve ser verificado e atualizado se necessário.

---

## Testing Strategy

| FR | Test Type | Test Description |
|----|-----------|-----------------|
| FR-001 | Manual (framework) | Verificar que cada endpoint extrai `perfilId` do JWT |
| FR-002 | Compile-time | Build verifica que DTOs sem `AdminId` não quebram |
| FR-003 | Compile-time | Build verifica compatibilidade com `ICupomService` |
| FR-004 | Manual (framework) | `[Authorize(Roles = "Admin")]` já testado pelo framework |
| EC-001 | Manual | Token sem `perfilId` → 401 |
| EC-002 | Manual | `perfilId` inválido → 401 |

**Nota:** Testes de integração automatizados (com WebApplicationFactory + JWT mock) exigiriam infra adicional significativa. A cobertura atual (build + verificação manual + testes de domínio existentes) é suficiente para o escopo desta spec.
