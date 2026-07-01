# Operação

---

## 📊 Matriz de Riscos

| # | Risco | Probabilidade | Impacto | Estratégia | Gatilho | Ação Planejada |
|:---:|:---|:---:|:---:|:---|:---|:---|
| 1 | **Atraso na entrega por subestimação de complexidade** — tarefas se mostram mais complexas do que o estimado, comprometendo o prazo da iteração. | Alto | Alto | Mitigar | Tarefa estimada em 2 dias ultrapassa 3 dias consecutivos sem conclusão e sem previsão realista de término reportada na daily. | Dividir tarefas grandes em entregas menores, realizar checkpoints diários (daily) e reavaliar o escopo semanalmente com o time. |
| 2 | **Indisponibilidade de integrante do grupo** — ausência prolongada de um membro por motivos pessoais, saúde ou conflitos de agenda. | Médio | Alto | Mitigar | Membro ausente por mais de 48 horas consecutivas sem qualquer comunicação ou atualização no grupo do projeto. | Manter documentação atualizada para facilitar o compartilhamento de conhecimento; adotar programação em par nas tarefas críticas; rearranjar o quadro conforme necessidade. |
| 3 | **Falha na integração com gateway de pagamento** — erros de comunicação, timeouts ou mudanças na API externa durante a implementação de reembolsos. | Médio | Alto | Mitigar | Testes de integração retornam erro HTTP 5xx ou timeout em 3 ou mais execuções consecutivas no ambiente de homologação. | Implementar camada de abstração (adapter) para isolar a dependência externa, utilizar mocks nos testes e prever política de retry com backoff exponencial. |
| 4 | **Mudança de escopo durante a iteração** — novos requisitos ou alterações solicitadas que impactam o que foi planejado. | Médio | Médio | Mitigar | Solicitação formal de nova funcionalidade ou alteração de requisito é registrada após o kickoff da iteração já ter sido concluído. | Congelar o escopo da iteração após o planejamento; novas demandas entram no backlog para a próxima iteração, salvo decisão unânime do time. |
| 5 | **Vazamento de dados sensíveis** — exposição acidental de dados de usuários, chaves de API ou credenciais no repositório. | Baixo | Alto | Evitar | Ferramenta automatizada de scan ou revisão manual detecta chave de API, senha ou token em arquivo commitado no repositório remoto. | Utilizar variáveis de ambiente e `.env` (nunca commitado), revisar código antes do merge com atenção a secrets, configurar `.gitignore` adequado e usar ferramentas de detecção de secrets. |
| 6 | **Débito técnico acumulado** — atalhos e concessões feitos para cumprir prazos que comprometem a qualidade e manutenibilidade do código. | Alto | Médio | Mitigar | Code review identifica código duplicado, ausência de testes ou violações de padrão de projeto em 3 ou mais pull requests consecutivos. | Reservar tempo no final da iteração para refatoração; manter cobertura de testes; code review obrigatório; documentar débitos técnicos conhecidos como tarefas no backlog. |

---

### Legenda

| Nível | Probabilidade | Impacto |
|:---:|:---|:---|
| **Alto** | Provável de ocorrer durante a iteração | Compromete gravemente a entrega |
| **Médio** | Pode ocorrer sob certas condições | Afeta parcialmente o andamento |
| **Baixo** | Pouco provável de ocorrer | Impacto mínimo, contornável |

---

## 📈 Métrica de Fluxo (DORA)

### Ficha de Definição Operacional

| Campo | Definição |
|:---|:---|
| **Nome da Métrica** | Lead Time for Changes (Tempo de Entrega de Mudanças) |
| **O que Mede** | O tempo total decorrido desde o primeiro commit de uma funcionalidade até sua implantação bem-sucedida em produção, medindo a velocidade do pipeline de entrega de ponta a ponta. |
| **Fórmula** | `Lead Time = Data/Hora do Deploy em Produção − Data/Hora do Primeiro Commit da Tarefa` |
| **Fonte de Dados** | Histórico de commits no Git (timestamp do primeiro commit da branch) e log de deploys do Docker/CI (timestamp do deploy concluído). |
| **Frequência de Coleta** | A cada deploy concluído (por release); consolidação semanal às segundas-feiras. |
| **Limites de Saúde** | 🟢 **Elite:** ≤ 1 hora \| 🟡 **Bom:** ≤ 1 dia \| 🟠 **Regular:** 1 dia a 1 semana \| 🔴 **Ruim:** > 1 semana |
| **Ação se Violado** | Se Lead Time > 1 dia por duas releases consecutivas: realizar retrospectiva focada no pipeline, revisar tamanho das tarefas (quebrar em entregas menores) e automatizar etapas manuais do processo de deploy. |

---

## 🛡️ Métrica de Qualidade

### Ficha de Definição Operacional

| Campo | Definição |
|:---|:---|
| **Nome da Métrica** | Change Failure Rate (Taxa de Falha em Mudanças) |
| **O que Mede** | A proporção de deploys em produção que resultam em falha (incidente, rollback, hotfix ou degradação de serviço), indicando a estabilidade e qualidade das entregas. |
| **Fórmula** | `Change Failure Rate = (Nº de Deploys com Falha nos Últimos 30 Dias / Nº Total de Deploys nos Últimos 30 Dias) × 100` |
| **Fonte de Dados** | Log de deploys do Docker/CI (contagem de deploys) e registro de incidentes/rollbacks no repositório Git (tags de hotfix, branches `hotfix/*`). |
| **Frequência de Coleta** | Consolidação semanal às segundas-feiras, considerando janela móvel dos últimos 30 dias. |
| **Limites de Saúde** | 🟢 **Elite:** 0–15% \| 🟡 **Bom:** 16–30% \| 🟠 **Regular:** 31–45% \| 🔴 **Ruim:** > 45% |
| **Ação se Violado** | Se Change Failure Rate > 30% por duas semanas consecutivas: congelar novos deploys, realizar análise de causa raiz dos incidentes, reforçar cobertura de testes automatizados e exigir aprovação dupla no code review antes de novos merges. |

---

## 🎯 SLO (Service Level Objective)

### Rota Crítica: `POST /api/eventos/{id}/cancelar` — Cancelamento de Evento com Reembolso

| Campo | Definição |
|:---|:---|
| **SLI (Indicador)** | Disponibilidade da rota de cancelamento de evento — percentual de requisições `POST /api/eventos/{id}/cancelar` que retornam código HTTP 2xx (sucesso) ou 4xx (erro do cliente), excluindo falhas internas 5xx e timeouts. |
| **Fórmula de Coleta** | `SLI = (Requisições 2xx + Requisições 4xx) / Total de Requisições à Rota × 100` |
| **Fonte do Dado** | Logs de aplicação do ASP.NET Core (middleware de logging) agregados via Application Insights / Serilog, filtrados pelo endpoint `/api/eventos/*/cancelar`. |
| **Janela de Medição** | **30 dias** (janela móvel) |
| **Alvo (SLO)** | **99,5%** de disponibilidade |

---

### 📉 Error Budget Policy (Política de Orçamento de Erro)

**Orçamento de Erro Total:** 100% − 99,5% = **0,5%** de falhas permitidas na janela de 30 dias.

| Nível | Gatilho (% do Orçamento Consumido) | Resposta Graduada |
|:---:|:---|:---|
| **Nível 1 🟡** | **> 50%** do orçamento consumido (equivalente a > 0,25% de falhas) | **Alerta preventivo.** Notificar o time no canal de comunicação. Aumentar a cadência de monitoramento para diário. Priorizar correção de bugs sobre novas tarefas na sprint atual. |
| **Nível 2 🟠** | **> 80%** do orçamento consumido (equivalente a > 0,40% de falhas) | **Intervenção ativa.** Convocar war room com todo o time para análise de causa raiz. Suspender deploys de novas funcionalidades. Todo esforço do time é redirecionado para correção de incidentes e melhorias de estabilidade. |
| **Nível 3 🔴** | **100%** do orçamento consumido (equivalente a ≥ 0,50% de falhas) | **Feature Freeze — Congelamento total de novas funcionalidades.** Zero novas funcionalidades são desenvolvidas ou implantadas. O time entra em regime de exclusiva correção de confiabilidade até que o SLO volte ao limiar verde por pelo menos 7 dias consecutivos na janela móvel. Nenhum deploy de feature é permitido enquanto o orçamento de erro permanecer esgotado. |
