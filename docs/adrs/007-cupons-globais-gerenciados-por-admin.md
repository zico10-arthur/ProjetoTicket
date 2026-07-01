## ADR-007: Cupons globais gerenciados por Admin

**Status:** ✅ Aceito

**Contexto:** Cupons de desconto precisam ser criados e gerenciados. Inicialmente considerou-se vincular cupons a vendedores.

**Alternativas consideradas:**

| Opção | Prós | Contras |
|-------|------|---------|
| **Cupons globais (Admin)** | Simples, Admin tem controle total, comprador usa em qualquer evento | Vendedor não pode criar seus próprios cupons |
| Cupons por vendedor (VendedorId) | Vendedor autônomo para marketing | Complexidade de isolamento, Admin precisa gerenciar per-vendedor |
| Ambos (Admin global + Vendedor local) | Flexibilidade máxima | Duas tabelas ou lógica condicional complexa |

**Decisão:** Cupons são globais. Apenas Admin cria e gerencia. Qualquer comprador aplica em qualquer evento pago.

**Consequências:**
- Tabela `Cupons` sem coluna `VendedorId`
- `CupomController` com `[Authorize(Roles = "Admin")]`
- Validação no uso: ativo, não expirado, valor mínimo atingido, evento não gratuito
- Sem restrição de "cupom do mesmo vendedor"

---

