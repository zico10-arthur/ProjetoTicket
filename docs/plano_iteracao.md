# Plano de Iteração

---

## 📋 Quadro Visual Kanban

### **WIP Máximo: 4 tarefas** (grupo de 5 integrantes)

| 🔙 **Backlog** | 🚧 **Em Desenvolvimento** | 🔍 **Code Review** | ✅ **Concluído** |
|:---:|:---:|:---:|:---:|
| **Notificações push** — Enviar notificações em tempo real sobre status de reserva e eventos | | | **Spec 50: Vendedor cancela evento** — Transação atômica com reembolso obrigatório (5 tabelas) |
| **Relatórios de vendas** — Dashboard com gráficos de ingressos vendidos e receita por evento | | | **Spec 110: Cancelamento — Visão Unificada** — Query multi-perfil refatorada com Dapper `splitOn` |
| **Integração PIX** — Suporte a pagamento via PIX com confirmação automática | | | **Spec 120: Segurança** — BCrypt + JWT Bearer + Rate Limiting |
| **Check-in por QR Code** — Validação de ingresso no dia do evento via leitura de QR Code | | | **Spec 130: Isolamento Multi-Tenant** — VendedorId para separação de dados entre vendedores |
| | | | **Spec 140: Infraestrutura e Deploy** — Containerização com Docker + DbUp migrations |
| | | | **Spec 150: Resiliência e Erros** — Tratamento global de exceções + políticas de retry |
| | | | **Spec 160: Cupons** — CRUD de cupons de desconto com AdminId via JWT |
| | | | **Spec 190: Hangfire** — Substituição de BackgroundService por Hangfire para jobs recorrentes |

---

### Regras do Quadro

| Regra | Descrição |
|:---|:---|
| **Limite de WIP** | No máximo **4 tarefas** simultâneas na coluna "Em Desenvolvimento". |
| **Pull, não Push** | Cada integrante puxa uma nova tarefa do Backlog somente quando tiver capacidade livre, respeitando o limite de WIP. |
| **Code Review obrigatório** | Toda tarefa deve passar pela coluna de Code Review antes de ser movida para Concluído. |
| **Definição de Pronto (DoD)** | Uma tarefa só vai para "Concluído" se: código revisado, testado e integrado na branch principal. |

---

### Fluxo de Trabalho

```
Backlog  ──▶  Em Desenvolvimento  ──▶  Code Review  ──▶  Concluído
  (fila)        (WIP ≤ 4)              (revisão)         (done)
```

1. O Product Owner / time prioriza as tarefas no **Backlog**.
2. Um desenvolvedor puxa a tarefa do topo do Backlog para **Em Desenvolvimento** (se WIP < 4).
3. Ao concluir a implementação, move para **Code Review**.
4. Após aprovação no Code Review, move para **Concluído**.
