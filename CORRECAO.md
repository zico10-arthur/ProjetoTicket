# Correção AV2 — ProjetoTicket (SoldOut Tickets)

**Grupo:** Arthur, Eduardo, Erick, Ian, Yuri

| # | Item de Avaliação | Nota | Justificativa |
|---|-------------------|:----:|---------------|
| 01 | Padrão AAA nos Testes | 0,5 | Todos os métodos em `EventoTests.cs` e `ReservaTests.cs` com `// Arrange`, `// Act`, `// Assert` separados; 11 arquivos de teste, 128 testes |
| 02 | Nomenclatura e Independência | 0,5 | Padrão `Metodo_Cenario_ResultadoEsperado` (ex: `Criar_ComCupomValido_DeveAplicarDesconto`); zero condicionais nos corpos de teste |
| 03 | Padrões Arquiteturais | 0,5 | 3 cenários com `Positivo:`/`Negativo:` (Clean Architecture, Repository Pattern, Dependency Injection) |
| 04 | Violações Arquiteturais | 0,5 | 5 violações reais com `**Problema:**`, `**Evidência:**` (trechos de código), `**Impacto:**`, `**Ação Recomendada:**` |
| 05 | ADR | 0,5 | 15 ADRs em `/docs/adrs/`; `ADR-001` com `## Contexto`, `## Decisão`, `## Consequências`, `Status: Aceito`, `Prós`/`Contras` |
| 06 | Dívida Técnica | 0,5 | 8 dívidas com colunas: ID, Descrição Técnica, Freq. Alteração, Risco, Esforço, Decisão |
| 07 | Priorização Dívida | 0,5 | P1 (DT-003 reflection, DT-006 scripts duplicados), P2 (5 dívidas), P3 (DT-008 string.Empty no CPF) |
| 08 | Classificação Manutenção | 0,5 | 12 tickets: 3 Corretiva, 3 Adaptativa, 3 Perfectiva, 3 Preventiva (Swanson) |
| 09 | Pipeline de Liberação | 0,5 | 4 passos detalhados: Análise de Impacto, Teste Cirúrgico (3 tipos), Feature Toggle (com Kill Switch), Estratégia de Release (4 etapas) |
| 10 | Plano de Iteração | 0,5 | Objetivo, Escopo, Entregáveis, Risco Principal, DoD — com quadro Kanban visual |
| 11 | Quadro Kanban e WIP | 0,5 | 4 colunas nomeadas (Backlog, Em Desenvolvimento, Code Review, Concluído); WIP = 4 ≤ 5 integrantes |
| 12 | Matriz de Riscos | 0,5 | 6 riscos com colunas: Risco, Probabilidade, Impacto, Estratégia, Gatilho, Ação Planejada |
| 13 | Gatilhos de Risco | 0,5 | Todos os 6 gatilhos com ≥20 caracteres descrevendo evento observável concreto |
| 14 | Métrica DORA | 0,5 | "Lead Time for Changes" com 7 campos: Nome, O que Mede, Fórmula, Fonte, Frequência, Limites (4 níveis), Ação se Violado |
| 15 | Métrica de Qualidade | 0,5 | "Change Failure Rate" com os mesmos 7 campos completos |
| 16 | SLO | 0,5 | SLI definido, Fórmula de Coleta, Fonte (Serilog/App Insights), Janela (30 dias), Alvo (99,5%) para rota crítica de cancelamento |
| 17 | Error Budget Policy | 0,5 | 3 níveis graduados: N1 (>50%, alerta), N2 (>80%, war room), N3 (100%, **"Feature Freeze — Congelamento total de novas funcionalidades"**) |
| 18 | Segurança SSDF | 0,5 | Nenhuma credencial hardcoded nos 128 arquivos `.cs`; usa variáveis de ambiente e `appsettings.json` |
| 19 | Threat Model e Gates | 0,5 | Ativos Protegidos (4), Vetor de Ataque (IDOR + race condition), Falha Arquitetural, Mitigação (4 controles) + 3 Gates com checklists `[x]` |
| 20 | Topologia Times e DoD | 0,5 | 4 tipos Team Topologies mapeados (Stream-Aligned, Platform, Enabling, Complicated-Subsystem) + diagrama de interação + `release_checklist_final.md` com 7 `[x]` |

**Nota Final: 10,0 / 10,0**

---

**Destaques:**
- Trabalho excepcionalmente completo. Todos os 20 itens atendidos com qualidade acima do esperado.
- Documentação detalhada e contextualizada: as violações arquiteturais usam trechos reais do código com número de linha; ADRs cobrem 15 decisões reais do projeto.
- Métricas DORA e de Qualidade com 4 níveis de saúde (Elite/Bom/Regular/Ruim), SLO com SLI explícito e Error Budget Policy com 3 níveis graduados e Feature Freeze.
- Pipeline de liberação completo com 4 etapas (LOCAL → STAGE → CANÁRIO → TOTAL), Feature Toggle com Kill Switch e plano de rollback.
- Testes robustos com 128 métodos, mocks (Moq/NSubstitute), testes de integração assíncronos e cenários de falha.
- Projeto com Clean Architecture de 4 camadas, 15 ADRs, DbUp migrations e 11 specs documentadas.
