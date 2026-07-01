## ADR-004: JWT com roles para autenticação

**Status:** ✅ Aceito

**Contexto:** Três perfis de usuário (Admin, Vendedor, Comprador) compartilham o mesmo endpoint de login e precisam de autorização granular.

**Alternativas consideradas:**

| Opção | Prós | Contras |
|-------|------|---------|
| **JWT com roles** | Stateless, auto-contido, padrão de mercado | Token não pode ser revogado antes de expirar |
| Session + cookies | Revogável, simples | Stateful, não escala horizontalmente, CORS complexo |
| API Keys | Simples para machine-to-machine | Não adequado para usuários finais com perfis |

**Decisão:** JWT Bearer com claims `sub` (CPF/CNPJ), `email`, `role` (Admin/Vendedor/Comprador).

**Consequências:**
- `POST /api/usuario/login` gera JWT único para todos os perfis
- `[Authorize(Roles = "Admin")]` protege endpoints administrativos
- Identidade (`sub`) extraída do token, nunca de parâmetro de rota
- Chave JWT armazenada em `dotnet user-secrets` (nunca no código)

---

