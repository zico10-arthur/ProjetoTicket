---
name: "Isolamento Multi-Tenant (VendedorId)"
status: "audited"
---

# Spec 130 — Task Breakdown

## Task Graph

```
Task 1 (Migration SQL)
  └→ Task 2 (Reserva entity + DTO)
       └→ Task 3 (Interface IReservaRepository)
            └→ Task 4 (ReservaRepository impl)
                 └→ Task 6 (ReservaService) ← Task 5 (Interface IReservaService)
                      └→ Task 7 (ReservaController)
                           └→ Task 8 (Build)
                                └→ Task 9 (Tests)
```

## Tasks

### Task 1 — Criar Script0012 de migração
- [ ] Criar `db/Script0012_AdicionarVendedorCpfReservas.sql`
- [ ] `ALTER TABLE Reservas ADD VendedorCpf NVARCHAR(11) NOT NULL DEFAULT ''`
- [ ] `ALTER TABLE Reservas ADD CONSTRAINT FK_Reservas_Usuarios_Vendedor FOREIGN KEY (VendedorCpf) REFERENCES Usuarios(Cpf)`
- [ ] `CREATE INDEX IX_Reservas_VendedorCpf ON Reservas(VendedorCpf)`
- [ ] Tudo dentro de `IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS ...)`
- **Arquivo:** `db/Script0012_AdicionarVendedorCpfReservas.sql` (criado)

### Task 2 — Adicionar VendedorCpf na entidade Reserva + novo DTO
- [ ] Adicionar propriedade `public string VendedorCpf { get; private set; } = string.Empty;` em `Reserva.cs`
- [ ] Adicionar parâmetro `string vendedorCpf = ""` ao método `Criar()`
- [ ] Setar `VendedorCpf = vendedorCpf` no objeto retornado por `Criar()`
- [ ] Criar `Domain/DTOs/ReservaVendedorDTO.cs` com campos: `Id`, `NomeEvento`, `DataEvento`, `ValorFinalPago`, `Pago`, `NomeComprador`, `CpfComprador`
- **Arquivos:** `Domain/Entities/Reserva.cs` (modificado), `Domain/DTOs/ReservaVendedorDTO.cs` (criado)

### Task 3 — Adicionar assinatura na interface IReservaRepository
- [ ] `Task<IEnumerable<ReservaVendedorDTO>> ListarReservasDetalhadasPorVendedor(string vendedorCpf, CancellationToken ct);`
- **Arquivo:** `Domain/Interface/IReservaRepository.cs` (modificado)

### Task 4 — Implementar no ReservaRepository
- [ ] Modificar `CadastrarReservaComItens()`: adicionar `VendedorCpf` no INSERT e nos parâmetros
- [ ] Implementar `ListarReservasDetalhadasPorVendedor()` com SQL que faz JOIN com Eventos e Usuarios filtrando por `r.VendedorCpf = @VendedorCpf`
- **Arquivo:** `Infraestructure/Repository/ReservaRepository.cs` (modificado)

### Task 5 — Adicionar assinatura na interface IReservaService
- [ ] `Task<IEnumerable<ReservaVendedorDTO>> ListarVendasDoVendedor(string vendedorCpf, CancellationToken ct);`
- **Arquivo:** `Application/Interfaces/IReservaService.cs` (modificado)

### Task 6 — Implementar no ReservaService
- [ ] Modificar `FazerReserva()`: passar `evento.VendedorCpf` para `Reserva.Criar()`
- [ ] Implementar `ListarVendasDoVendedor()` delegando para `_repositoryReserva.ListarReservasDetalhadasPorVendedor()`
- **Arquivo:** `Application/Service/ReservaService.cs` (modificado)

### Task 7 — Adicionar endpoint no ReservaController
- [ ] Novo endpoint `[HttpGet("minhas-vendas")]` com `[Authorize(Roles = "Vendedor")]`
- [ ] Extrair `cpf` do claim JWT (tipo `"cpf"`)
- [ ] Se vazio → `Unauthorized()`
- [ ] Delegar para `_service.ListarVendasDoVendedor(cpf, ct)`
- [ ] Retornar `Ok(vendas)`
- **Arquivo:** `Api/Controllers/ReservaController.cs` (modificado)

### Task 8 — Build
- [ ] `dotnet build` — 0 erros
- **Verificação:** build limpo

### Task 9 — Testes
- [ ] `dotnet test` — verificar que testes existentes continuam passando
- [ ] Verificar que `Reserva.Criar()` com novo parâmetro não quebra testes existentes
- [ ] Verificar que `FazerReserva` passa `evento.VendedorCpf` corretamente
- **Verificação:** sem regressões

### Task 10 — Testes manuais/verificação
- [ ] Verificar que `VendedorCpf` é populado no INSERT (inspeção de código)
- [ ] Verificar que o endpoint `minhas-vendas` está funcional (build limpo)
- [ ] Verificar que Admin ainda vê todas as reservas (sem filtro)
- **Verificação:** inspeção de código + build

## Requirements Coverage

| FR | Tasks |
|----|-------|
| FR-001 (Coluna VendedorCpf na tabela) | Task 1 |
| FR-002 (Preenchimento automático) | Task 2, Task 4, Task 6 |
| FR-003 (Endpoint minhas-vendas) | Task 5, Task 6, Task 7 |
| FR-004 (Isolamento nas queries) | Task 3, Task 4 |
| FR-005 (Admin visão global) | Task 9 (testes de regressão), Task 10 (verificação — sem alteração) |
| FR-006 (Comprador isolamento) | Task 9 (testes de regressão), Task 10 (verificação — sem alteração) |
| FR-007 (Worker global) | Task 10 (verificação — sem alteração) |

## Summary

| Métrica | Valor |
|---------|-------|
| Total tasks | 10 |
| Arquivos criados | 2 (`Script0012`, `ReservaVendedorDTO.cs`) |
| Arquivos modificados | 6 (`Reserva.cs`, `IReservaRepository.cs`, `ReservaRepository.cs`, `IReservaService.cs`, `ReservaService.cs`, `ReservaController.cs`) |
| Testes automatizados | 6 (T1-T4, T6-T7 do design.md) |
| Verificações manuais | 3 (FR-005, FR-006, FR-007) + 1 (T5 do design.md) |
