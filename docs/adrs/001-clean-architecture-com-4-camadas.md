## ADR-001: Clean Architecture com 4 camadas

**Status:** ✅ Aceito

**Contexto:** O sistema precisa ser testável, manutenível e permitir troca de componentes externos (banco, framework) sem reescrever regras de negócio.

**Alternativas consideradas:**

| Opção | Prós | Contras |
|-------|------|---------|
| Clean Architecture (Domain/Application/Infra/Api) | Isolamento de regras de negócio, testabilidade, independência de frameworks | Mais camadas = mais arquivos |
| Monolito com pastas por feature | Simples, rápido de começar | Regras de negócio misturadas com infra, difícil testar |
| Vertical Slice | Features independentes, baixo acoplamento | Difícil para iniciantes, padrão menos conhecido |

**Decisão:** Clean Architecture com 4 camadas.

**Consequências:**
- `Domain/` não importa nenhuma camada externa — entidades com regras de negócio encapsuladas
- `Application/` depende apenas do Domain — serviços, DTOs, interfaces
- `Infraestructure/` implementa interfaces do Domain — repositórios, conexões, migrations
- `Api/` orquestra tudo — controllers, middlewares, injeção de dependências

---

