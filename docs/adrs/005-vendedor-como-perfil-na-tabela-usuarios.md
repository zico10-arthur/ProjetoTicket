## ADR-005: Vendedor como perfil na tabela Usuarios

**Status:** ✅ Aceito

**Contexto:** O sistema anterior tinha uma tabela `Empresas` separada para vendedores. A v2.0 unifica tudo.

**Alternativas consideradas:**

| Opção | Prós | Contras |
|-------|------|---------|
| **Vendedor na tabela Usuarios** | Login unificado, schema simples, FK direta | Colunas nullable para Admin/Comprador |
| Tabela Empresas separada | Separação clara PF vs PJ | Login duplicado, joins extras, dois endpoints de auth |
| Herança (table-per-type) | Modelagem "pura" de OO | Complexo com Dapper, queries com UNION |

**Decisão:** Vendedor como perfil na tabela `Usuarios`. Colunas específicas (`Cnpj`, `NomeFantasia`, `LogoUrl`, etc.) são preenchidas apenas quando `PerfilId = B2B2...`.

**Consequências:**
- `POST /api/usuario/login` funciona para Admin, Vendedor e Comprador
- `Eventos.VendedorId` referencia `Usuarios.Cpf`
- Migração SQL: `ALTER TABLE Usuarios ADD Cnpj, NomeFantasia, ...`
- Campo `Cpf` armazena CPF (PF) ou CNPJ (Vendedor) como string de 14 caracteres

---

