---
name: Cancelamento de Reserva — Visão Unificada
status: verified
---

# Spec 110: Cancelamento de Reserva — Visão Unificada — Implementation Tasks

## Task Order

Tasks MUST be executed sequentially in numerical order.

```
Spec 40 (pré-requisito)
  ├→ Task 1 (DTO ReservaDetalhada)
  │    └→ Task 3 (query refactor) ──→ Task 4 (PodeCancelar) ──→ Task 6 (testes)
  └→ Task 5 (verificar [Authorize]) ──→ Task 6 (testes)

Task 2 (ItemReservaResponseDTO) ──→ Task 3 (query refactor)
```

---

## Tasks

### Task 1: Atualizar `ReservaDetalhadaDTO` (Domain)

**Objective:** Adicionar campos `Itens` e `PodeCancelar` ao DTO de resposta de Minhas Reservas.

**Requirements Covered:** FR-002, FR-003

**Design References:** `design.md §Component 1`, `design.md §Data Structures — ReservaDetalhadaDTO`

**Actions:**
1. Abrir `Domain/DTOs/ReservaDetalhadaDTO.cs`
2. Adicionar ao final da classe, antes da chave de fechamento `}`:
```csharp
    public List<ItemReservaResponseDTO> Itens { get; set; } = new();
    public bool PodeCancelar { get; set; }
```
3. Adicionar `using Application.DTOs;` no topo (necessário para `ItemReservaResponseDTO`)
4. NÃO remover campos existentes (`PosicaoIngresso`, `SetorIngresso`, etc.) — manter para compatibilidade

**Validation:**
- Projeto `Domain` compila sem erros: `dotnet build Domain/Domain.csproj`
- `ReservaDetalhadaDTO` contém propriedades `Itens` e `PodeCancelar`

**Status:** [x] done

---

### Task 2: Atualizar `ItemReservaResponseDTO` (Application)

**Objective:** Adicionar campos `Id` e `Reembolsado` ao DTO de item de reserva.

**Requirements Covered:** FR-002

**Design References:** `design.md §Component 2`, `design.md §Data Structures — ItemReservaResponseDTO`

**Actions:**
1. Abrir `Application/DTOs/ItemReservaResponseDTO.cs`
2. Adicionar campo `Id` antes de `CpfParticipante`:
```csharp
    public Guid Id { get; set; }
```
3. Adicionar campo `Reembolsado` após `PrecoUnitario`:
```csharp
    public bool Reembolsado { get; set; }
```
4. Manter campos existentes (`CpfParticipante`, `IngressoId`, `PrecoUnitario`)

**Validation:**
- Projeto `Application` compila sem erros: `dotnet build Application/Application.csproj`
- `ItemReservaResponseDTO` contém propriedades `Id` e `Reembolsado`

**Status:** [x] done

---

### Task 3: Refatorar Query de Minhas Reservas no Repository (Infrastructure)

**Objective:** Substituir query com `STRING_AGG` por query com `splitOn` do Dapper que retorna itens individuais com flag `Reembolsado`.

**Requirements Covered:** FR-002, FR-005, NFR-001

**Design References:** `design.md §Component 3`

**Actions:**
1. Abrir `Infraestructure/Repository/ReservaRepository.cs`
2. Localizar o método `ListarReservasDetalhadasPorCpf`
3. Substituir TODO o conteúdo do método pela implementação abaixo:
```csharp
public async Task<IEnumerable<ReservaDetalhadaDTO>> ListarReservasDetalhadasPorCpf(string cpf, CancellationToken ct)
{
    using var conn = _connectionFactory.CreateConnection();

    var reservaDict = new Dictionary<Guid, ReservaDetalhadaDTO>();

    const string sql = @"
        SELECT
            r.Id,
            e.Nome AS NomeEvento,
            e.DataEvento,
            r.ValorFinalPago,
            r.Pago,
            r.Reembolsada,
            ir.Id AS ItemId,
            ir.CpfParticipante,
            ir.PrecoUnitario,
            ir.Reembolsado
        FROM dbo.Reservas r
        INNER JOIN dbo.Eventos e ON r.EventoId = e.Id
        INNER JOIN dbo.ItensReserva ir ON ir.ReservaId = r.Id
        WHERE r.UsuarioCpf = @cpf
        ORDER BY r.DataReserva DESC, ir.CpfParticipante";

    await conn.QueryAsync<ReservaDetalhadaDTO, ItemReservaResponseDTO, ReservaDetalhadaDTO>(
        new CommandDefinition(sql, new { cpf }, cancellationToken: ct),
        (reserva, item) =>
        {
            if (!reservaDict.TryGetValue(reserva.Id, out var existing))
            {
                existing = reserva;
                existing.Itens = new List<ItemReservaResponseDTO>();
                reservaDict[reserva.Id] = existing;
            }
            existing.Itens.Add(item);
            return existing;
        },
        splitOn: "ItemId"
    );

    return reservaDict.Values;
}
```
4. Adicionar `using Application.DTOs;` e `using Domain.DTOs;` se ainda não existirem

**Validation:**
- Projeto `Infraestructure` compila sem erros: `dotnet build Infraestructure/Infraestructure.csproj`
- Query NÃO contém `STRING_AGG`
- Query contém `splitOn: "ItemId"`
- Método usa `Dictionary<Guid, ReservaDetalhadaDTO>` para agrupamento

**Status:** [x] done

---

### Task 4: Adicionar Cálculo de `PodeCancelar` no Service (Application)

**Objective:** Adicionar loop que calcula a flag `PodeCancelar` para cada reserva retornada.

**Requirements Covered:** FR-003

**Design References:** `design.md §Component 4`

**Actions:**
1. Abrir `Application/Service/ReservaService.cs`
2. Localizar o método `ListarMinhasReservas`
3. Adicionar o loop `foreach` após obter as reservas do repository e antes do `return`:
```csharp
foreach (var reserva in reservas)
{
    reserva.PodeCancelar = !reserva.Reembolsada && reserva.DataEvento > DateTime.UtcNow;
}
```
4. O método completo deve ter esta estrutura:
```csharp
public async Task<IEnumerable<ReservaDetalhadaDTO>> ListarMinhasReservas(string cpf, CancellationToken ct)
{
    var reservas = await _repositoryReserva.ListarReservasDetalhadasPorCpf(cpf, ct);

    foreach (var reserva in reservas)
    {
        reserva.PodeCancelar = !reserva.Reembolsada && reserva.DataEvento > DateTime.UtcNow;
    }

    return reservas;
}
```

**Validation:**
- Projeto `Application` compila sem erros: `dotnet build Application/Application.csproj`
- Método `ListarMinhasReservas` contém o loop `foreach` com cálculo de `PodeCancelar`

**Status:** [x] done

---

### Task 5: Verificar `[Authorize]` no Controller (API)

**Objective:** Confirmar que os endpoints `GET /api/reserva/minhas` e `DELETE /api/reserva/{id}` não têm restrição de role.

**Requirements Covered:** FR-001, FR-004, NFR-002

**Design References:** `design.md §Component 5`

**Actions:**
1. Abrir `Api/Controllers/ReservaController.cs`
2. Verificar o atributo do método `ListarMinhasReservas` (ou `GetMinhas`):
   - Deve ser `[Authorize]` (sem parâmetro `Roles`)
   - Se for `[Authorize(Roles = "Comprador")]` ou similar → **alterar para** `[Authorize]`
3. Verificar o atributo do método `CancelarReserva` (ou `DeleteReserva`):
   - Deve ser `[Authorize]` (sem parâmetro `Roles`)
   - Se for `[Authorize(Roles = "...")]` → **alterar para** `[Authorize]`
4. Se ambos já estiverem com `[Authorize]` sem restrição, nenhuma alteração é necessária

**Validation:**
- Ambos os endpoints têm `[Authorize]` sem parâmetro `Roles`
- Projeto `Api` compila sem erros: `dotnet build Api/Api.csproj`

**Status:** [x] done

---

### Task 6: Criar Testes Automatizados

**Objective:** Garantir cobertura de testes para listagem unificada e cancelamento por perfil.

**Requirements Covered:** FR-001, FR-002, FR-003, FR-004

**Design References:** `design.md §Testing Strategy`

**Actions:**
1. Abrir `tests/ReservaTests.cs` (adicionar ao final do arquivo)
2. Implementar os seguintes testes unitários para `ReservaService`:

| # | Nome do teste | Cenário | Assert |
|---|---------------|---------|--------|
| T1 | `ListarMinhasReservas_Comprador_VeApenasSuas` | Comprador autenticado | Lista contém apenas reservas com `UsuarioCpf == cpf` |
| T2 | `ListarMinhasReservas_Vendedor_VeApenasSuas` | Vendedor autenticado | Lista contém apenas reservas com `UsuarioCpf == cpf` |
| T3 | `ListarMinhasReservas_Admin_VeApenasSuas` | Admin autenticado | Lista contém apenas reservas com `UsuarioCpf == cpf` |
| T4 | `ListarMinhasReservas_ComItensReembolsados` | Reserva cancelada | Todos os itens com `Reembolsado = true` |
| T5 | `ListarMinhasReservas_PodeCancelar_EventoFuturo` | Evento futuro, não reembolsada | `PodeCancelar = true` |
| T6 | `ListarMinhasReservas_PodeCancelar_JaReembolsada` | Reserva reembolsada | `PodeCancelar = false` |
| T7 | `ListarMinhasReservas_PodeCancelar_EventoPassado` | Evento já começou | `PodeCancelar = false` |
| T8 | `CancelarReserva_Vendedor_CancelaPropria` | Vendedor dono da reserva | Sucesso (sem exceção) |
| T9 | `CancelarReserva_Admin_CancelaPropria` | Admin dono da reserva | Sucesso (sem exceção) |

3. Usar Moq para mockar `IReservaRepository`
4. Nos testes T1-T7, mockar `ListarReservasDetalhadasPorCpf` para retornar dados de teste
5. Nos testes T8-T9, mockar `GetByIdAsync` e `CancelarReserva` (método do repositório)

**Validation:**
- `dotnet test --filter "FullyQualifiedName~ReservaTests"` passa com os novos 9 testes verdes
- Cobertura dos cenários unificados ≥ 80%

**Status:** [x] done

---

## Resumo de Tarefas

| # | Camada | Arquivo(s) | Ação | Esforço |
|---|--------|-----------|------|---------|
| 1 | Domain | `DTOs/ReservaDetalhadaDTO.cs` | **Editar** (+Itens, +PodeCancelar) | 10 min |
| 2 | Application | `DTOs/ItemReservaResponseDTO.cs` | **Editar** (+Id, +Reembolsado) | 5 min |
| 3 | Infraestructure | `Repository/ReservaRepository.cs` | **Refatorar** (query splitOn) | 25 min |
| 4 | Application | `Service/ReservaService.cs` | **Editar** (+PodeCancelar) | 10 min |
| 5 | Api | `Controllers/ReservaController.cs` | **Verificar** ([Authorize]) | 5 min |
| 6 | tests | `ReservaTests.cs` | **Editar** (+9 testes) | 45 min |
| **Total** | **4 camadas** | **5 arquivos** | **6 tasks** | **~1h40** |
