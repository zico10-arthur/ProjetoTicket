## ADR-011: Global Exception Handler como middleware

**Status:** ✅ Aceito

**Contexto:** Exceções não tratadas retornam 500 com stack trace. Precisamos de respostas de erro padronizadas em português.

**Alternativas consideradas:**

| Opção | Prós | Contras |
|-------|------|---------|
| **Middleware customizado** | Captura tudo, formato consistente, sem dependência | Código próprio |
| `IExceptionFilter` do ASP.NET | Nativo, por controller | Não cobre erros fora do pipeline MVC |
| Try-catch em cada endpoint | Controle granular | Duplicação de código, inconsistente |

**Decisão:** `GlobalExceptionHandlerMiddleware` como primeiro item do pipeline. Mapeia exceções de domínio para HTTP status codes com mensagens em português.

**Consequências:**
- Toda resposta de erro segue o formato `{ "error": "mensagem" }`
- Exceções de domínio mapeadas: `CredenciaisInvalidasException → 401`, `ConflitoException → 409`, etc.
- `500` nunca expõe stack trace em produção
- Registrado antes de `UseAuthentication()` para capturar todos os erros

---

