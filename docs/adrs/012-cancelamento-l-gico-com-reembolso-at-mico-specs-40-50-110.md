## ADR-012: Cancelamento Lógico com Reembolso Atômico (specs 40, 50, 110)

**Status:** ✅ Aceito

**Contexto:** O sistema precisa permitir que compradores cancelem suas reservas e que vendedores cancelem eventos inteiros. Em ambos os casos, quando há dinheiro envolvido, o reembolso precisa ser processado de forma consistente. O modelo anterior previa exclusão física — o que inviabilizaria auditoria e histórico.

**Alternativas consideradas:**

| Opção | Prós | Contras |
|-------|------|---------|
| **Cancelamento lógico + transação atômica** | Preserva histórico, reembolso garantido, idempotente | Requer colunas extras (Reembolsada, Cancelado) e transações multi-tabela |
| Exclusão física + INSERT de log | Schema mais limpo | Perde rastreabilidade, log pode divergir do estado real |
| Soft delete genérico (IsDeleted) | Simples | Não diferencia "cancelado com reembolso" de "excluído", semântica pobre |

**Decisão:** Cancelamento lógico com flags explícitas e transações atômicas cobrindo até 5 tabelas. Reserva cancelada → `Reserva.Reembolsada = true` + `ItemReserva.Reembolsado = true` + `Ingressos.Status = 0` (Livre) + `Pagamento.Status = 3` (Reembolsado). Evento cancelado → adiciona `Evento.Cancelado = true` e propaga para todas as reservas do evento.

**Consequências:**
- **Comprador cancela reserva** (spec 40): `DELETE /api/reserva/{id}` — autorização por `UsuarioCpf == cpf do JWT`, válido para qualquer perfil
- **Vendedor/Admin cancela evento** (spec 50): `DELETE /api/evento/{id}` mantém o verbo HTTP mas muda a semântica para cancelamento lógico; `GET /api/evento/{id}/status-cancelamento` consulta impacto antes de cancelar
- **Visão unificada** (spec 110): `GET /api/reserva/minhas` retorna `reembolsada`, `reembolsado` (por item) e `podeCancelar` para todos os perfis
- Coluna `Reservas.Reembolsada` (BIT, NOT NULL DEFAULT 0) — migration `Script0012`
- Eventos cancelados são filtrados da listagem pública (`WHERE Cancelado = 0`)
- Reembolso é **simulado** (status no banco) — sem gateway de pagamento real
- Transações usam `BEGIN/COMMIT/ROLLBACK` — ou tudo acontece, ou nada

---

