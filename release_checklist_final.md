# Release Checklist Final — SoldOut Tickets

---

- [x] **Fundamentos** — Arquitetura limpa em 4 camadas (Domain, Application, Infrastructure, API), .NET 9, Dapper + SQL Server, containerização com Docker e migrations via DbUp.
- [x] **Produto Mínimo** — Funcionalidades core implementadas: cadastro de eventos e ingressos, reserva com checkout, cancelamento de evento com reembolso atômico, visão unificada de reservas multi-perfil, cupons de desconto e autenticação JWT.
- [x] **Evidência de Qualidade** — Métricas DORA definidas (Lead Time for Changes, Change Failure Rate), SLO de 99,5% para rota crítica, Error Budget Policy com 3 níveis, code review obrigatório e cobertura de testes para cenários de ataque.
- [x] **Decisões Documentadas** — Plano de iteração com quadro Kanban e WIP definido, matriz de riscos com 6 riscos mapeados (probabilidade, impacto, gatilhos e ações), topologia de times Team Topologies e plano operacional com métricas e SLO.
- [x] **Evidência de Requisitos** — Specs documentadas em `management/specs/`: 11 especificações com design, requirements e tasks, cobrindo cancelamento, segurança, multi-tenant, infraestrutura, pagamentos, e-mail e Hangfire.
- [x] **Governança** — Quadro Kanban com limite de WIP (4 tarefas), regras de Pull, Code Review obrigatório, Definição de Pronto (DoD), gates de segurança no ciclo de desenvolvimento e Error Budget Policy com Feature Freeze.
- [x] **Segurança** — Threat model documentado para a rota crítica (`POST /api/eventos/{id}/cancelar`), 3 gates de segurança (identidade/autorização, anti-injeção, revisão pré-merge), BCrypt + JWT + Rate Limiting, SSDF verificado (zero credenciais hardcoded).
