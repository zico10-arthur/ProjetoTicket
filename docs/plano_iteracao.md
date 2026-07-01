# Plano de Iteração — SoldOut Tickets

> **Iteração:** Sprint 3  
> **Duração:** 2 semanas (01/07/2026 a 14/07/2026)  
> **Time:** 2 desenvolvedores + 1 QA

---

## Objetivo da Iteração:

Corrigir as dívidas técnicas de Prioridade 1 (Imediato) identificadas no
[registro de dívida técnica](registro_divida_tecnica.md) — especificamente
**DT-003** (reflection no `EventoService`) e **DT-006** (scripts DbUp
duplicados) — e entregar a funcionalidade pendente de **filtro de eventos por
data e categoria** no frontend de compra de ingressos. Paralelamente, elevar a
cobertura de testes de 0 para pelo menos 3 testes end-to-end simulando o fluxo
completo de compra, pagamento e cancelamento.

---

## Escopo (Backlog Selecionado):

| ID | Tipo | Descrição | Esforço | Responsável |
|---|---|---|---|---|
| **DT-003** | Correção (P1) | Remover `typeof(Evento).GetProperty("VendedorId")?.SetValue(...)` do `EventoService.CriarEventoAsync`. Configurar AutoMapper Profile ou usar factory method. | 3h | Dev 1 |
| **DT-006** | Correção (P1) | Consolidar scripts DbUp: unificar `db/Script0009_AdicionarCamposEvento.sql` com `Infraestructure/DataBase/Scripts/Script0009_*.sql`. Remover duplicação entre as pastas `db/` e `Infraestructure/DataBase/Scripts/`. | 2h | Dev 1 |
| **FEAT-010** | Feature | Adicionar filtro de eventos por data (calendário) e categoria (palestra/teatro) na página `ComprarIngressos.razor`. Incluir `DatePicker` e `Select` do MudBlazor com debounce de 300ms. | 8h | Dev 2 |
| **TEST-001** | Teste | Criar 3 testes end-to-end com `WebApplicationFactory`: (1) compra de ingresso com sucesso, (2) tentativa de pagamento duplicado, (3) cancelamento de reserva com reembolso. | 6h | QA |
| **DT-007** | Correção (P2) | Padronizar respostas de erro: remover `NotFound(string)` e `BadRequest(string)` sem wrapper JSON. Garantir `{ "message": "..." }` em todos os controllers (`EventoController.cs`, `UsuarioController.cs`). | 2h | Dev 1 |
| **CHORE-005** | Infra | Adicionar `IUserContext` injetável para extração de `userId` dos claims JWT e substituir as 5 ocorrências duplicadas no `EventoController`. | 4h | Dev 2 |

---

## Entregáveis (Evidências):

| ID | Entregável | Evidência de conclusão |
|---|---|---|
| **DT-003** | `EventoService.cs` sem reflection | Código revisado: `VendedorId` setado via AutoMapper Profile ou parâmetro no construtor da entidade. `EventoTests.cs` com teste específico validando que `VendedorId` é atribuído corretamente sem reflection. |
| **DT-006** | Scripts DbUp unificados | Pasta `Infraestructure/DataBase/Scripts/` como única fonte de scripts. `db/` removida ou com `README.md` apontando para a localização correta. `dotnet run` da API executa todas as migrations sem erros. |
| **FEAT-010** | Filtro de eventos no frontend | Vídeo/print do `ComprarIngressos.razor` com filtros de data e categoria funcionais. Teste manual: selecionar data futura + categoria "Teatro" → apenas eventos correspondentes exibidos. |
| **TEST-001** | 3 testes E2E | `dotnet test` output com 3 testes passando. Relatório de cobertura mostrando os fluxos: reserva → pagamento → confirmação; pagamento duplicado → 409 Conflict; cancelamento → reembolso → status Reembolsada. |
| **DT-007** | Erros padronizados | Todos os catch blocks retornam `new { message = ... }`. Teste de contrato (`ResilicienciaTests.cs`) cobre todos os controllers verificando Content-Type `application/json` e presença da propriedade `message`. |
| **CHORE-005** | `UserContext` injetável | Interface `IUserContext` no `Application` com método `GetUserId()`. Implementação `HttpUserContext` no `Api`. Injetada nos controllers. Métodos de extração duplicados removidos. |

---

## Risco Principal do Ciclo:

**Risco:** A unificação dos scripts DbUp (DT-006) pode quebrar o banco de
desenvolvimento se um script renumerado já tiver sido executado. O DbUp registra
cada script pelo nome na tabela `SchemaVersions` — renomear ou consolidar scripts
fará o DbUp tentar reexecutar scripts que já rodaram, potencialmente causando
erros de `ALTER TABLE` em colunas que já existem.

**Mitigação:**
1. Criar backup do banco de desenvolvimento antes da alteração
2. Testar a consolidação primeiro em um banco limpo (`docker-compose down -v &&
   docker-compose up`) para validar a ordem correta
3. Se necessário, inserir manualmente registros na `SchemaVersions` para os
   scripts renomeados, evitando reexecução
4. O ambiente de staging será implantado primeiro, servindo de canário antes de
   produção

---

## Definição de Pronto (DoD):

Uma tarefa é considerada **PRONTA** somente quando todos os critérios abaixo
forem atendidos:

- [ ] Código revisado por outro desenvolvedor (Pull Request aprovado)
- [ ] Todos os testes existentes passam (`dotnet test` com 0 falhas)
- [ ] Testes novos (AAA) cobrem o código alterado (mínimo 1 teste por bug fix,
  2 por feature)
- [ ] Sem warnings de compilação no `dotnet build` da solution completa
- [ ] Código segue o padrão de nomenclatura `Metodo_Cenario_ResultadoEsperado`
  nos testes
- [ ] Sem desvios condicionais (`if`, `for`, `foreach`, `switch`, `while`) nos
  métodos de teste
- [ ] Migrações de banco testadas em banco limpo (Docker SQL Server)
- [ ] Feature flag (se aplicável) configurada e testada nos modos ON/OFF
- [ ] Documentação atualizada (ADR se for decisão arquitetural, registro de
  dívida técnica se o débito for quitado)
- [ ] Branch mergeada em `dev` (nunca direto em `main`)
- [ ] Deploy em staging validado manualmente (smoke test: fluxo principal)

---

## Cronograma da Iteração

| Dia | Atividade | Marco |
|---|---|---|
| **Dia 1** | Planning + setup: criar branches `fix/dt-003`, `fix/dt-006`, `feat/filtro-eventos`, `test/e2e` | Backlog revisado |
| **Dia 2-3** | DT-003 (reflection) + DT-006 (scripts) | PR aberto para review |
| **Dia 3-5** | FEAT-010 (filtro de eventos) + CHORE-005 (UserContext) | Demo interna do filtro |
| **Dia 5-7** | TEST-001 (3 E2E) + DT-007 (padronizar erros) | Testes E2E passando |
| **Dia 8** | Code review + merge em `dev` | `dev` build verde |
| **Dia 9** | Deploy em staging + smoke test manual | Staging validado |
| **Dia 10** | Correções de bugs encontrados no staging | Buffer |
| **Dia 11** | Deploy canário (10% usuários) + monitoramento | 30 min sem erros |
| **Dia 12** | Deploy total (100%) | Release concluída |
| **Dia 13-14** | Retrospectiva + atualização de docs (ADR, dívida técnica) | Iteração fechada |
