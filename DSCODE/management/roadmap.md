# Roadmap — SoldOut Tickets

> **Projeto:** SoldOut Tickets — Plataforma SaaS de venda de ingressos para eventos de pequeno porte
> **Stack:** .NET 9 + Dapper + SQL Server + Clean Architecture

---

## Tabela de Specs

| # | Nome | Status | Referências |
|---|------|--------|-------------|
| 40 | Comprador Cancela Reserva com Reembolso | implemented | `docs/agents/roadmap/spec-40/` |
| 50 | Vendedor Cancela Evento com Reembolso Obrigatório | audited | `docs/agents/roadmap/spec-50/`, `docs/visao.md §6.5`, `docs/ADR.md §ADR-012` |
| 110 | Cancelamento — Visão Unificada | audited | `docs/agents/roadmap/spec-110/` |
| 120 | Segurança (BCrypt, JWT, Rate Limit) | audited | `docs/agents/roadmap/spec-120/` |
| 130 | Isolamento Multi-Tenant (VendedorId) | audited | `docs/agents/roadmap/spec-130/` |
| 140 | Infraestrutura e Deploy (Docker) | audited | `docs/agents/roadmap/140-infraestrutura-deploy.md` |
| 150 | Resiliência e Tratamento de Erros | audited | `docs/agents/roadmap/spec-150/` |
| 160 | Cupons — AdminId via JWT | audited | `docs/agents/roadmap/spec-160/` |
| 170 | Pagamentos (Status + Reembolso) | implemented | `docs/agents/roadmap/spec-170/` |
| 180 | Serviço de E-mail Transacional + Redefinição de Senha | implemented | `docs/agents/roadmap/spec-180/` |
| 190 | Substituir BackgroundService por Hangfire | audited | `docs/agents/roadmap/spec-190/` |

---

## Grafo de Dependências

```
120 (JWT Key) ──┐
160 (Cupons)   ──┤
130 (Isolamento)──┤─ Sprint 1: Segurança
150 (Resiliência)─┤
180 (E-mail)     ─┘
                  │
                  ▼
              Spec 40 (Cancelamento Reserva) ──┬── Spec 50 (Cancelamento Evento)
                  │                            │
                  └──────────────┬─────────────┘
                                 ▼
                          Spec 110 (Visão Unificada)
                                 │
                                 ▼
                       140 (Docker) + 190 (Hangfire)
```

---

## Status por Sprint

| Sprint | Specs | Status |
|--------|-------|--------|
| Sprint 1 — Segurança | 120, 160, 130, 150, 180 | 4/5 auditadas, 1 implementada |
| Sprint 2 — Cancelamento e Reembolso | 40, 50, 110 | 1 implementada, 1 verificada, 1 planejada |
| Sprint 3 — Infraestrutura | 140, 190 | 2/2 auditadas |
