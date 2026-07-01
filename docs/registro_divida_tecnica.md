# Registro de Dívida Técnica — SoldOut Tickets

> Data: 01/07/2026

---

| ID da Dívida | Descrição Técnica | Freq. Alteração | Risco | Esforço | Decisão |
|---|---|---|---|---|---|
| **DT-001** | `Application.csproj` referencia diretamente `Infraestructure.csproj`, violando a Clean Architecture. A camada de aplicação deve depender apenas do Domain. | Baixo | Alto | Alto | **Prioridade 2 (Próxima Sprint)** — Remover a referência, criar interfaces no Domain/Application para serviços de infraestrutura (hash de senha, token JWT) e movê-los para o Infraestructure. |
| **DT-002** | `UsuarioController` injeta e usa `IUsuarioRepository` diretamente (método `ListarTodos`), ignorando a camada Application. Controller com duas fontes de verdade (`_service` e `_repository`). | Baixo | Alto | Médio | **Prioridade 2 (Próxima Sprint)** — Remover `IUsuarioRepository` do controller. Criar `ListarTodosAsync` no `IUsuarioService` e delegar a chamada. |
| **DT-003** | `EventoService.CriarEventoAsync` usa reflection (`typeof(Evento).GetProperty("VendedorId")?.SetValue(...)`) para definir `VendedorId` após o AutoMapper. Se a propriedade for renomeada, quebra silenciosamente em runtime. | Médio | Alto | Baixo | **Prioridade 1 (Imediato)** — Configurar AutoMapper Profile adequadamente ou passar `VendedorId` via construtor/factory method da entidade. |
| **DT-004** | Extração de `userId` dos claims JWT duplicada em 5 métodos do `EventoController` (linhas 39-43, 71-75, 93-97, 115-119, 145-149). O fallback `"userId" ?? "cpf"` indica migração incompleta de CPF para Guid. | Alto | Médio | Baixo | **Prioridade 2 (Próxima Sprint)** — Criar método helper `GetUserId()` no controller ou um `IUserContext` injetável. Substituir as 5 ocorrências. Remover fallback `"cpf"` se a migração estiver concluída. |
| **DT-005** | `BCrypt.Net-Next` referenciado diretamente no `Application.csproj`. Hashing de senha é preocupação de infraestrutura, não de aplicação. Trocar o algoritmo exigiria alterar Application. | Baixo | Médio | Médio | **Prioridade 2 (Próxima Sprint)** — Criar `IPasswordHasher` no Domain, implementar `BCryptPasswordHasher` no Infraestructure, mover o pacote NuGet. |
| **DT-006** | Scripts de migração DbUp duplicados: pasta `db/` contém scripts com nomes diferentes dos scripts em `Infraestructure/DataBase/Scripts/` (ex: `Script0009` nas duas pastas tem conteúdo diferente). O DbUp usa apenas a pasta do Infraestructure como Embedded Resource. | Alto | Alto | Baixo | **Prioridade 1 (Imediato)** — Consolidar todos os scripts em `Infraestructure/DataBase/Scripts/`. Remover a pasta `db/` ou mantê-la apenas como referência histórica com um README de aviso. |
| **DT-007** | Tratamento de erro inconsistente entre controllers: `EventoController.GetAllAsync` (linha 29) retorna `NotFound(erro.Message)` como string pura, enquanto `EventoController.CreateAsync` (linha 80) retorna `new { message = erro.Message }` como JSON. O frontend espera JSON e quebra com string pura. | Médio | Médio | Baixo | **Prioridade 2 (Próxima Sprint)** — Padronizar todas as respostas de erro para `new { message = ... }`. Remover `NotFound(string)` puro. |
| **DT-008** | `Cpf` do vendedor armazenado como `string.Empty` em vez de `null` (Domain/Entities/Usuario.cs:84). O índice único `UQ_Usuarios_Cpf` com `WHERE Cpf IS NOT NULL` não filtra strings vazias, causando conflito se dois vendedores tentarem se cadastrar. | Baixo | Médio | Baixo | **Prioridade 3 (Aceitar/Ignorar)** — O impacto é mínimo pois a aplicação já impede cadastro duplicado via validação de CNPJ. O `string.Empty` funciona como placeholder e a constraint não gera conflito real no fluxo atual. Aceitar como débito conhecido. |

---

## Resumo

| Métrica | Valor |
|---|---|
| Total de dívidas | 8 |
| Prioridade 1 (Imediato) | 2 (DT-003, DT-006) |
| Prioridade 2 (Próxima Sprint) | 5 (DT-001, DT-002, DT-004, DT-005, DT-007) |
| Prioridade 3 (Aceitar/Ignorar) | 1 (DT-008) |
| Risco Alto | 4 (DT-001, DT-002, DT-003, DT-006) |
