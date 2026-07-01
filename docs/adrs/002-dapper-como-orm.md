## ADR-002: Dapper como ORM

**Status:** ✅ Aceito

**Contexto:** Precisamos acessar dados relacionais com performance e controle sobre o SQL gerado.

**Alternativas consideradas:**

| Opção | Prós | Contras |
|-------|------|---------|
| **Dapper** | Leve, rápido, SQL explícito, curva baixa | Sem tracking de mudanças, queries manuais |
| Entity Framework Core | Mudanças automáticas, migrations integradas, LINQ | Overhead, SQL gerado menos previsível, N+1 silencioso |
| ADO.NET puro | Máximo controle | Muito código boilerplate |

**Decisão:** Dapper + DbUp para migrations.

**Consequências:**
- SQL escrito manualmente com parâmetros (`@param`) — proteção contra SQL Injection
- DbUp executa scripts SQL versionados na inicialização
- Sem lazy loading — queries são explícitas e previsíveis
- Menos "mágica" = mais fácil para iniciantes entenderem o que acontece

---

