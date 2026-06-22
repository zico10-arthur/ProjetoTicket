---
name: "Cupons de Desconto — AdminId via JWT"
status: "audited"
references:
  - "requirements.md (Spec 160)"
  - "design.md (Spec 160)"
---

# Spec 160: Cupons de Desconto — Task Breakdown

## Task Graph

```
T1 (Remove AdminId from DTOs)
 ├─► T2 (Update CupomController)
 │    └─► T5 (Build verification)
 ├─► T3 (Update GlobalExceptionHandler)
 └─► T4 (Verify Web frontend)
```

---

## Tasks

### [ ] Task 1: Remove AdminId from DTOs

**Files:**
- `Application/DTOs/AlterarValorMinimoDTO.cs` — remove `public Guid AdminId { get; set; }`
- `Application/DTOs/AlterarDataExpiracaoDTO.cs` — remove `public Guid AdminId { get; set; }`
- `Application/DTOs/AlterarDescontoDTO.cs` — remove `public Guid AdminId { get; set; }`

**Verify:** `dotnet build Application/Application.csproj` — 0 errors

**Dependencies:** None

---

### [ ] Task 2: Update CupomController — Extract AdminId from JWT

**File:** `Api/Controllers/CupomController.cs`

**Changes:**
1. Add helper method `GetAdminIdFromJwt()`:
   ```csharp
   private Guid GetAdminIdFromJwt()
   {
       var perfilIdStr = User.Claims.FirstOrDefault(c => c.Type == "perfilId")?.Value;
       if (string.IsNullOrEmpty(perfilIdStr) || !Guid.TryParse(perfilIdStr, out var perfilId))
           throw new UnauthorizedAccessException("Token inválido: perfil não identificado.");
       return perfilId;
   }
   ```

2. `CadastrarCupom`: Remove `[FromRoute] Guid Id` parameter; change route from `CadastrarCupom/{Id}` to `CadastrarCupom`; call `GetAdminIdFromJwt()` to pass AdminLogado

3. `DeletarCupom`: Remove `[FromBody] Guid adminId` parameter; call `GetAdminIdFromJwt()`

4. `AlterarValorMinimo`: Replace `dto.AdminId` with `GetAdminIdFromJwt()`

5. `AlterarDataExpiracao`: Replace `dto.AdminId` with `GetAdminIdFromJwt()`

6. `AlternarStatus`: Remove `[FromBody] Guid adminId` parameter; call `GetAdminIdFromJwt()`

7. `AlterarDesconto`: Replace `dto.AdminId` with `GetAdminIdFromJwt()`

8. `ListarTodosCupons`: Replace `Guid.Empty` with `GetAdminIdFromJwt()`

**Verify:** `dotnet build Api/Api.csproj` — 0 errors

**Dependencies:** Task 1 (DTOs must have AdminId removed first)

---

### [ ] Task 3: Update GlobalExceptionHandler — Handle UnauthorizedAccessException

**File:** `Api/Middlewares/GlobalExceptionHandlerMiddleware.cs`

**Pattern:** O middleware usa switch expression, não try/catch.

**Change:** Adicionar `UnauthorizedAccessException` ao switch expression, antes do `_` default:
```csharp
    UnauthorizedAccessException
        => (StatusCodes.Status401Unauthorized, ex.Message),
```

**Verify:** `dotnet build Api/Api.csproj` — 0 errors

**Dependencies:** Task 2 (exception is thrown by GetAdminIdFromJwt)

---

### [ ] Task 4: Update Web Frontend — Remove AdminId from API calls

**Files:** 4 arquivos Blazor + 1 ViewModel para verificar

1. **`Web/Components/Pages/Admin/CriarCupom.razor`** — Remove `{adminId}` da URL:
   - Linha ~56: `$"api/Cupom/CadastrarCupom/{adminId}"` → `"api/Cupom/CadastrarCupom"`

2. **`Web/Components/Pages/Admin/Cupons.razor`** — Remove `adminId` do body (2 ocorrências):
   - Linha ~147: Remove `request.Content = JsonContent.Create(adminId);` (DeletarCupom)
   - Linha ~183: Remove `request.Content = JsonContent.Create(adminId);` (AlternarStatus)

3. **`Web/Components/Pages/Admin/EditarCupom.razor`** — Remove `AdminId` de DTOs anônimos (3 ocorrências):
   - Linhas ~100-109: Remove `AdminId = adminId` dos objetos `new { }`

4. **`Web/Models/CupomViewModel.cs`** — Verify: confirmar que não há campo `AdminId`

**Verify:** `dotnet build Web/Web.csproj` — 0 errors

**Dependencies:** None (can run in parallel)

---

### [ ] Task 5: Build Verification

**Command:** `dotnet build ProjetoTicket.sln`

**Expected:** 0 errors. Warnings pré-existentes aceitáveis.

**Dependencies:** Tasks 1-4

---

### [ ] Task 6: Run Existing Tests

**Command:** `dotnet test`

**Expected:** All existing tests pass. CupomTests (8 tests) pass. No regressions.

**Dependencies:** Task 5

---

### [ ] Task 7: Update Roadmap

**File:** `docs/agents/roadmap.md`

**Change:** Update Spec 160 status from `❌ pendente` to `✅ implementada` and evidence line.

**Evidence line:** `160 | CupomController: AdminId extraído do JWT (claim perfilId), removido de rota/body/DTOs | ✅ implementada`

**Dependencies:** Task 6

---

## Summary

| # | Task | Files Modified | Verification |
|---|------|---------------|-------------|
| 1 | Remove AdminId from DTOs | 3 DTO files | `dotnet build` |
| 2 | Update CupomController | 1 controller file | `dotnet build` |
| 3 | Update GlobalExceptionHandler | 1 middleware file | `dotnet build` |
| 4 | Update Web frontend | 3 razor files | `dotnet build` (Web) |
| 5 | Build verification | 0 (check only) | 0 errors |
| 6 | Run tests | 0 (check only) | All tests pass |
| 7 | Update roadmap | 1 doc file | Confirmation |

**Total files modified: 9** (3 DTOs + 1 controller + 1 middleware + 3 razor + 1 roadmap)
**Total files verified: 6** (CupomViewModel, ICupomService, CupomService, Cupom, CupomRepository, CupomTests)

## Requirements Covered

| FR | Task(s) |
|----|---------|
| FR-001 (Extrair AdminId do JWT) | T2 |
| FR-002 (Remover AdminId dos DTOs) | T1 |
| FR-003 (Manter ICupomService compatível) | T5 (verificação de build) |
| FR-004 (Validação de Role mantida) | T6 (verificação de testes) |
| EC-001, EC-002 (perfilId ausente/inválido) | T2, T3 |
