## ADR-008: DbUp para migrations versionadas

**Status:** ✅ Aceito

**Contexto:** O schema do banco evolui com as specs. Precisamos versionar e automatizar alterações.

**Alternativas consideradas:**

| Opção | Prós | Contras |
|-------|------|---------|
| **DbUp** | Scripts SQL puros, ordem explícita, idempotente, roda na inicialização | Sem rollback automático, sem "estado" do schema |
| EF Core Migrations | Snapshots do modelo C#, rollback, ferramenta visual | Acoplado ao EF, se não usamos EF pra queries não faz sentido |
| Scripts manuais | Controle total | Esquecível, erro humano, sem automação |

**Decisão:** DbUp com scripts SQL numerados (`Script0001_...sql`) executados na inicialização da API.

**Consequências:**
- Scripts em `Infraestructure/DataBase/Scripts/` com numeração sequencial
- Toda migração usa `IF NOT EXISTS` para ser idempotente
- `DatabaseMigration.cs` configura e executa na inicialização
- Ordem dos scripts é explícita — sem "mágica"

---

