## ADR-010: Rate limiting no endpoint de login

**Status:** ✅ Aceito

**Contexto:** Sem proteção, o endpoint de login pode sofrer força bruta para descobrir senhas.

**Alternativas consideradas:**

| Opção | Prós | Contras |
|-------|------|---------|
| **Middleware customizado** | Simples, sem dependência, controle total | Código próprio para manter |
| AspNetCoreRateLimit (NuGet) | Pronto, configurável, políticas avançadas | Dependência externa, config complexa |
| Sem rate limit | Zero esforço | Vulnerável a força bruta |

**Decisão:** Middleware customizado limitando `POST /api/usuario/login` a 5 requisições por minuto por IP.

**Consequências:**
- `ConcurrentDictionary<IP, SlidingWindow>` em memória
- Resposta `429 Too Many Requests` ao estourar o limite
- Não persiste entre reinicializações (aceitável para esta funcionalidade)
- Aplicado apenas no endpoint de login

---

