# spec

Skill para geração determinística e detalhada de especificações técnicas a partir de storytellings. A IA autônoma lê o `storytelling.md`, extrai cada história (ST-01, ST-03...) e gera uma **pasta** por história dentro de `docs/agents/roadmap/`, contendo exatamente 3 arquivos: `requirements.md`, `design.md` e `tasks.md`.

## Gatilhos de Uso

- "Criar as specs do storytelling"
- "Gerar especificações a partir das histórias"
- "Transformar storytelling em specs"
- "Criar arquivos de spec na pasta roadmap"

## Princípios

1. **Uma spec por storytelling**: cada pasta `spec-{numero}/` corresponde a exatamente uma história (ST-XX).
2. **3 arquivos obrigatórios por spec**: toda pasta contém `requirements.md` (o quê), `design.md` (como) e `tasks.md` (passos).
3. **Somente backend**: stories puramente de frontend (ex: painéis, dashboards UI) são ignoradas — a pasta `roadmap/` contém apenas specs implementáveis no backend.
4. **Determinístico**: mesma entrada produz sempre a mesma estrutura de saída nos 3 arquivos.
5. **Implementável**: o conjunto dos 3 arquivos contém endpoint, request/response, validações, regras, SQL, modelo de domínio, tasks sequenciais e verificações — suficiente para um dev implementar sem abrir outro documento.
6. **Rastreável**: cada arquivo referencia o storytelling de origem e o problema resolvido no `visao.md`.
7. **Numerado de 10 em 10**: pastas seguem o padrão `spec-10/`, `spec-20/`, `spec-30/`... com seções internas numeradas como `10.1`, `10.2` etc.

## Fluxo de Execução

### Fase 1 — Coleta de Contexto

1. Ler `docs/storytelling.md` por completo.
2. Ler `docs/visao.md` seções 2 (Problema) e 6 (Especificações-Chave e Valor).
3. Ler `docs/especificacoes.md` se existir (para referência de entidades e endpoints já definidos).
4. Opcional: ler código em `Domain/Entities/`, `Api/Controllers/`, `Application/DTOs/` para precisão técnica.

### Fase 2 — Filtragem

1. Extrair todas as histórias (ST-01 a ST-N).
2. **Remover histórias puramente de frontend** — stories cujo escopo é apenas UI (ex: "Painel do Vendedor" com seções de navegação, sem endpoint novo).
3. Manter histórias de backend — stories que introduzem endpoint, entidade, regra de negócio, migração de banco ou fluxo de dados.

### Fase 3 — Geração

Para cada história de backend, criar uma pasta `docs/agents/roadmap/spec-{numero}/` contendo exatamente 3 arquivos:

```
docs/agents/roadmap/
├── spec-10/
│   ├── requirements.md
│   ├── design.md
│   └── tasks.md
├── spec-20/
│   ├── requirements.md
│   ├── design.md
│   └── tasks.md
└── spec-30/
    ├── requirements.md
    ├── design.md
    └── tasks.md
```

---

## Template: `requirements.md`

```markdown
# Spec [NÚMERO] — Requirements: [TÍTULO]

> **Projeto:** SoldOut Tickets
> **Contexto:** [`storytelling.md#st-xx-...`](../../../storytelling.md#st-xx-...) | [`visao.md §X`](../../../visao.md#X-...)
> **Status:** `pendente`

---

## 1. Objetivo

[1-2 frases descrevendo o que esta spec entrega e por quê]

---

## 2. Histórias de Usuário

### HU-XX01: [Título da história]

**Como** [ator],
**Quero** [ação],
**Para** [benefício].

### HU-XX02: [Título da história] (se houver mais de uma)

**Como** [ator],
**Quero** [ação],
**Para** [benefício].

---

## 3. Requisitos Funcionais

| ID | Descrição |
|----|-----------|
| RF-XX01 | [Descrição do requisito funcional] |
| RF-XX02 | [Descrição do requisito funcional] |

---

## 4. Requisitos Não Funcionais

| ID | Descrição |
|----|-----------|
| RNF-XX01 | [Descrição do requisito não funcional] |
| RNF-XX02 | [Descrição do requisito não funcional] |

---

## 5. Critérios de Aceitação (BDD)

### HU-XX01: [Título]

**Cenário 1 — [Nome do cenário]**
- **Dado** que [pré-condição]
- **Quando** [ação]
- **Então** [resultado esperado]

**Cenário 2 — [Nome do cenário]**
- **Dado** que [pré-condição]
- **Quando** [ação]
- **Então** [resultado esperado]

---

## 6. Casos de Borda

| # | Caso | Comportamento esperado |
|---|------|----------------------|
| B1 | [Caso de borda] | [Comportamento] |
| B2 | [Caso de borda] | [Comportamento] |

---

## 7. Escopo

### Dentro do escopo
- [Item incluído]
- [Item incluído]

### Fora do escopo
- [Item excluído]
- [Item excluído]
```

---

## Template: `design.md`

```markdown
# Spec [NÚMERO] — Design: [TÍTULO]

> **Requirements:** [`requirements.md`](./requirements.md)
> **Contexto:** Clean Architecture (.NET 9 + Dapper + SQL Server + DbUp)

---

## 1. Modelo de Domínio

### 1.1 Enum `[Nome]` (se aplicável)

```csharp
// Domain/Enums/[Nome].cs
namespace Domain.Enums;

public enum [Nome]
{
    [Valor1] = 0,
    [Valor2] = 1
}
```

### 1.2 Entidade `[Nome]`

```csharp
// Domain/Entities/[Nome].cs
namespace Domain.Entities;

public class [Nome]
{
    // Propriedades, construtor privado, métodos de domínio
}
```

---

## 2. Banco de Dados

### 2.1 Migration: `ScriptXXXX_[Descricao].sql`

```sql
-- ScriptXXXX: [Descricao]

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '[Tabela]')
BEGIN
    CREATE TABLE dbo.[Tabela] (
        -- colunas
    );
END
```

### 2.2 Diagrama de Relacionamentos

```
[Diagrama ASCII das tabelas e relacionamentos]
```

---

## 3. API

### 3.1 `MÉTODO /api/recurso/ação`

```
MÉTODO /api/recurso/ação
Auth: JWT (role=XXX) | Público
Content-Type: application/json
```

#### Request Body

```json
{
    "campo": "valor"
}
```

#### Response `200 OK`

```json
{
    "campo": "valor"
}
```

#### Response `400 Bad Request`

```json
{
    "message": "Mensagem de erro"
}
```

#### Response `404 Not Found`

```json
{
    "message": "Recurso não encontrado."
}
```

### 3.2 DTOs

```csharp
// Application/DTOs/[Nome]RequestDTO.cs
public record [Nome]RequestDTO(...);

// Application/DTOs/[Nome]ResponseDTO.cs
public record [Nome]ResponseDTO(...);
```

---

## 4. Camada de Aplicação

### 4.1 Interface `I[Nome]Service`

```csharp
// Application/Interfaces/I[Nome]Service.cs
namespace Application.Interfaces;

public interface I[Nome]Service
{
    Task<[ResponseDTO]> [Metodo](...);
}
```

### 4.2 Lógica de `[Nome]Service`

```csharp
// Application/Service/[Nome]Service.cs
public async Task<[ResponseDTO]> [Metodo](...)
{
    // 1. Buscar entidade
    // 2. Validar
    // 3. Operação
    // 4. Retornar DTO
}
```

---

## 5. Camada de Infraestrutura

### 5.1 Interface `I[Nome]Repository`

```csharp
// Domain/Interface/I[Nome]Repository.cs
namespace Domain.Interface;

public interface I[Nome]Repository
{
    Task [Metodo](...);
}
```

### 5.2 Transação SQL (Repository)

```sql
-- Executado dentro de transação no Repository:
BEGIN TRANSACTION;
    -- operações
COMMIT;
```

---

## 6. Integração com Fluxos Existentes

[Descrever como esta spec se conecta com endpoints/tabelas/fluxos já existentes]

---

## 7. Fluxo de Dados (Sequência)

```
[Diagrama ASCII de sequência: Cliente → Controller → Service → Repository → DB]
```

---

## 8. Decisões de Design

| Decisão | Justificativa |
|---------|---------------|
| [Decisão] | [Por quê] |
```

---

## Template: `tasks.md`

```markdown
# Spec [NÚMERO] — Tasks: [TÍTULO]

> **Requirements:** [`requirements.md`](./requirements.md)
> **Design:** [`design.md`](./design.md)
> **Ordem:** Cada task depende das anteriores. Seguir a sequência numérica.

---

## Task 1 — [Título da task]

**Objetivo:** [1 frase descrevendo o que esta task entrega]

| Campo | Valor |
|-------|-------|
| Arquivo | `[Caminho]/[Arquivo].cs` (criar/editar/sobrescrever) |
| Dependências | [Task N ou "Nenhuma"] |

**Conteúdo:** (código ou descrição do que implementar)

**Verificação:**
- [Critério objetivo para considerar a task concluída]

---

## Task 2 — [Título da task]

...

---

## Resumo de Tarefas

| # | Camada | Arquivo(s) | Ação |
|---|--------|-----------|------|
| 1 | Domain | `[Arquivo]` | Criar/Editar |
| 2 | Application | `[Arquivo]` | Criar/Editar |

---

## Dependências entre Tasks

```
[Diagrama ASCII de dependências]
```

---

## Estimativa de Esforço

| Bloco | Tasks | Esforço |
|-------|-------|---------|
| Domain | X, Y | Xh |
| Database | Z | Xh |
| Application | ... | Xh |
| Infrastructure | ... | Xh |
| API | ... | Xh |
| Tests | ... | Xh |
| **Total** | **N tasks** | **~Xh** |
```

---

## Seções por Tipo de Spec

### `requirements.md`

| Tipo de spec | Seções obrigatórias |
|---|---|
| **Novo endpoint** | 1 (Objetivo), 2 (Histórias), 3 (Req. Funcionais), 5 (BDD), 6 (Bordas), 7 (Escopo) |
| **Nova entidade/banco** | 1 (Objetivo), 2 (Histórias), 3 (Req. Funcionais), 4 (Req. Não Funcionais), 6 (Bordas), 7 (Escopo) |
| **Mudança de regra** | 1 (Objetivo), 2 (Histórias), 3 (Req. Funcionais com Antes/Depois), 5 (BDD), 7 (Escopo) |
| **Migração/refatoração** | 1 (Objetivo), 3 (Req. Funcionais), 4 (Req. Não Funcionais), 7 (Escopo) |

### `design.md`

| Tipo de spec | Seções obrigatórias |
|---|---|
| **Novo endpoint** | 1 (Domínio), 2 (Banco), 3 (API), 4 (Aplicação), 5 (Infra), 6 (Integração), 7 (Fluxo), 8 (Decisões) |
| **Nova entidade/banco** | 1 (Domínio), 2 (Banco), 6 (Integração), 8 (Decisões) |
| **Mudança de regra** | 1 (Domínio — apenas entidade alterada), 3 (API — apenas endpoint afetado), 4 (Aplicação — lógica alterada), 8 (Decisões) |
| **Migração/refatoração** | 2 (Banco — migration SQL), 5 (Infra — mudanças no repositório), 6 (Integração), 8 (Decisões) |

### `tasks.md`

- **Sempre obrigatório** para qualquer tipo de spec.
- Tasks seguem a ordem natural de dependência (Domain → Database → Application → Infrastructure → API → Tests).
- Cada task contém: Objetivo, Arquivo, Dependências, Conteúdo, Verificação.

Seções não aplicáveis são omitidas (não colocar "N/A" ou seções vazias).

## Numeração

- Pastas: `spec-10/`, `spec-20/`, `spec-30/`... (incremento de 10).
- O número pula quando uma história de frontend é removida (ex: ST-02 removida → spec-10, spec-20, spec-30... sem spec-20 que seria ST-02).
- Seções internas de cada arquivo: numeração natural (`1.`, `2.`, `3.`...).
- Se no futuro for necessário criar uma sub-spec (ex: correção de bug), usar numeração interna como `10.1`, `10.2` etc. dentro do mesmo número base.

## Regras de Qualidade

### Gerais (todos os arquivos)

1. **Nunca inventar**: todo conteúdo deve ser rastreável ao `storytelling.md` ou dedutível de regras de negócio já documentadas.
2. **Consistência entre arquivos**: os 3 arquivos da mesma spec devem ser coerentes — o que está no `requirements.md` é atendido pelo `design.md` e implementado pelas `tasks.md`.
3. **CPFs CNPJs**: usar números com dígitos válidos nos exemplos.
4. **IDs**: usar GUIDs placeholder como `"a1b2c3d4-..."` consistentemente.
5. **Consistência cross-spec**: se duas specs referenciam o mesmo endpoint, os detalhes devem bater.

### `requirements.md`

6. **BDD completo**: cada história de usuário tem pelo menos 1 cenário de sucesso e cenários de erro relevantes.
7. **Casos de borda explícitos**: cobrir reserva cancelada, evento passado, conflito de concorrência, etc.
8. **Escopo claro**: a seção "Fora do escopo" evita que a spec cresça indefinidamente.

### `design.md`

9. **Código real, não pseudocódigo**: entidades, DTOs, serviços e repositories usam sintaxe C# real.
10. **JSON real**: request/response usa valores de exemplo realistas, não placeholders.
11. **Códigos HTTP corretos**: 400 para validação, 409 para conflito, 404 para não encontrado, 403 para permissão, 401 para auth.
12. **Migration SQL idempotente**: usar `IF NOT EXISTS` para tabelas e colunas.
13. **Transações documentadas**: quando houver operação multi-tabela, documentar `BEGIN/COMMIT/ROLLBACK`.

### `tasks.md`

14. **Tasks sequenciais e verificáveis**: cada task tem dependências claras e critério de conclusão objetivo.
15. **Verificações objetivas**: cada task tem critério verificável (ex: "Projeto compila", "Teste X passa").
16. **Ordem topológica**: tasks respeitam Domain → Database → Application → Infrastructure → API → Tests.

## Anti-Padrões

- ❌ Criar apenas 1 ou 2 arquivos dos 3 obrigatórios.
- ❌ Copiar o storytelling sem adicionar detalhe técnico (endpoint, validações, SQL, modelo de domínio).
- ❌ Incluir stories de frontend puro na pasta `roadmap/`.
- ❌ Usar `Guid?` ou `null` onde o domínio exige valor obrigatório.
- ❌ Deixar seções com "N/A" ou vazias — se não se aplica, omitir a seção inteira.
- ❌ Inventar endpoints ou regras não mencionados no storytelling.
- ❌ Usar linguagem vaga ("etc", "dentre outros", "e assim por diante").
- ❌ Tasks sem verificação objetiva.
- ❌ `requirements.md` sem BDD ou com cenários vagos.
- ❌ `design.md` com pseudocódigo em vez de código C# real.
- ❌ `tasks.md` com dependências circulares ou ordem incorreta.

## Pós-Geração

Após gerar todas as specs:

1. Listar as pastas criadas com número e título (ex: `spec-10/ — ST-01: Auto Cadastro de Vendedor`).
2. Verificar que **cada pasta contém exatamente 3 arquivos**: `requirements.md`, `design.md`, `tasks.md`.
3. Verificar que a numeração está correta e sem saltos indevidos.
4. Confirmar que cada spec referencia corretamente o `storytelling.md` e `visao.md`.
5. Fazer verificação cruzada: requisitos do `requirements.md` têm cobertura no `design.md` e tasks correspondentes no `tasks.md`.

### Atualização obrigatória do `visao.md` e `roadmap.md`

**Sempre que uma nova spec é criada**, os dois arquivos de visão do projeto devem ser atualizados:

**6. Atualizar `docs/visao.md`:**
- Adicionar uma nova subseção em `§6` (Especificações-Chave e Valor) descrevendo o que a spec entrega e **por que entrega valor ao usuário final** (tabela Especificação → Valor).
- Mover a funcionalidade da lista "fora do escopo" para "incluído" na `§10`, se aplicável.
- Se a spec introduzir nova tecnologia na stack (ex: MailKit), adicionar na tabela de stack em `§8.1`.

**7. Atualizar `docs/agents/roadmap.md`:**
- Adicionar a spec na tabela da sprint correspondente (ou criar nova linha/seção) com: número, título, status (`pendente`), **qual dos 5 problemas do [`visao.md §2`](../visao.md#2-problema) ela resolve**, e link para a pasta.
- Atualizar a tabela "Cobertura dos 5 Problemas" incluindo o número da spec no problema correspondente.
- Atualizar a tabela "Resumo por Status" (incrementar contagem de `pendente` ou `implementada`).
- Adicionar linha na tabela "Evidências no Código" com o status atual.
- Atualizar o contador de arquivos no header (ex: `(17 arquivos)` → `(18 arquivos)`).
- Se a spec implementa algo que estava listado na "Fase 3 (Futuro)", remover o item da lista de futuro.
