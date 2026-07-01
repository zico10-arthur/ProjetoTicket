## ADR-003: BCrypt para hash de senhas

**Status:** ✅ Aceito

**Contexto:** O código atual armazena e compara senhas em texto plano. Precisamos de hash seguro com salt automático.

**Alternativas consideradas:**

| Opção | Prós | Contras |
|-------|------|---------|
| **BCrypt (BCrypt.Net-Next)** | Salt automático, work factor configurável, amplamente auditado | 100ms por hash (aceitável) |
| SHA-256 com salt manual | Rápido, nativo do .NET | Precisamos implementar salt, vulnerável a GPU/força bruta |
| Argon2 | Mais moderno que BCrypt, resistente a GPU | Ecossistema .NET menor, menos maduro |
| PBKDF2 | Nativo do .NET (`Rfc2898DeriveBytes`) | Configuração mais complexa, não é padrão para senhas web |

**Decisão:** BCrypt.Net-Next com work factor 11 (padrão da lib).

**Consequências:**
- `BCrypt.HashPassword(senha)` no cadastro
- `BCrypt.Verify(senha, hash)` no login
- Nunca comparar strings de senha diretamente
- Senha do Admin no seed SQL usa hash pré-gerado
- Coluna `Senha` precisa suportar 60+ caracteres (VARCHAR(100))

---

