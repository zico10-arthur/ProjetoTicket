# spec

Skill para geração determinística e detalhada de especificações técnicas a partir de storytellings. A IA autônoma lê o `storytelling.md`, extrai cada história (ST-01, ST-03...) e gera um arquivo de especificação por história na pasta `docs/agents/roadmap/`.

## Gatilhos de Uso

- "Criar as specs do storytelling"
- "Gerar especificações a partir das histórias"
- "Transformar storytelling em specs"
- "Criar arquivos de spec na pasta roadmap"

## Princípios

1. **Uma spec por storytelling**: cada arquivo corresponde a exatamente uma história (ST-XX).
2. **Somente backend**: stories puramente de frontend (ex: painéis, dashboards UI) são ignoradas — a pasta `roadmap/` contém apenas specs implementáveis no backend.
3. **Determinístico**: mesma entrada produz sempre a mesma estrutura de saída.
4. **Implementável**: cada spec contém endpoint, request/response, validações, regras e SQL — suficiente para um dev implementar sem abrir outro documento.
5. **Rastreável**: cada spec referencia o storytelling de origem e o problema resolvido no `visao.md`.
6. **Numerado de 10 em 10**: arquivos seguem o padrão `10-st01-..., 20-st03-..., 30-st04-...`.

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

Para cada história de backend, gerar um arquivo em `docs/agents/roadmap/` seguindo o template abaixo.

## Template Obrigatório por Spec

```markdown
# [NÚMERO] — ST-XX: [TÍTULO]

> **Origem:** [`storytelling.md#st-xx-...`](../../storytelling.md#st-xx-...)
>
> **Problema:** [`visao.md#2`](../../visao.md#2-problema) — [frase conectando ao problema do usuário]

---

## [NÚMERO].1 História

**Como** [ator],
**Quero** [ação],
**Para** [benefício].

---

## [NÚMERO].2 Endpoint (se aplicável)

```
MÉTODO /api/recurso/ação
Auth: JWT (role=XXX) | Público
Content-Type: application/json
```

### Request

```json
{
    "campo": "valor"
}
```

### Response

```json
{
    "campo": "valor"
}
```

---

## [NÚMERO].3 Validações

| Regra | Erro |
|-------|------|
| `campo` vazio | 400 "Campo é obrigatório" |
| `campo` duplicado | 409 "Campo já cadastrado" |

---

## [NÚMERO].4 Regras de Negócio

```
1. Regra:
   → ação
   → condição → código HTTP "mensagem"

2. Regra com branch:
   SE condição:
       → ação
   SENÃO:
       → outra ação
```

---

## [NÚMERO].5 SQL (se aplicável)

```sql
-- Query ou migration relevante
INSERT INTO Tabela (...) VALUES (...);
```

---

## [NÚMERO].6 Respostas HTTP

| Código | Caso |
|--------|------|
| `200 OK` | Sucesso |
| `201 Created` | Recurso criado |
| `400 Bad Request` | Erro de validação |
| `401 Unauthorized` | Token inválido ou ausente |
| `403 Forbidden` | Perfil sem permissão |
| `404 Not Found` | Recurso não encontrado |
| `409 Conflict` | Conflito (duplicado, estado inválido) |
```

## Seções por Tipo de Spec

| Tipo de spec | Seções obrigatórias |
|---|---|
| **Novo endpoint** | 1 (História), 2 (Endpoint), 3 (Validações), 4 (Regras), 6 (Respostas) |
| **Nova entidade/banco** | 1 (História), 2 (Modelo), 5 (SQL), 4 (Regras) |
| **Mudança de regra** | 1 (História), 4 (Regras com Antes/Depois) |
| **Migração/refatoração** | 1 (História), 5 (SQL da migração), 4 (Regras) |

Seções não aplicáveis são omitidas (não colocar "N/A").

## Numeração

- Arquivos: `10-st01-...md`, `20-st03-...md`, `30-st04-...md` (incremento de 10).
- O número pula quando uma história de frontend é removida (ex: ST-02 removida → 10, 20, 30... sem o 20 que seria ST-02).
- Seções internas: `10.1`, `10.2`, `10.3`... correspondendo ao número do arquivo.

## Regras de Qualidade

1. **Nunca inventar**: todo conteúdo deve ser rastreável ao `storytelling.md` ou dedutível de regras de negócio já documentadas.
2. **JSON real**: request/response usa valores de exemplo realistas, não placeholders.
3. **Códigos HTTP corretos**: 400 para validação, 409 para conflito, 404 para não encontrado, 403 para permissão, 401 para auth.
4. **CPFs CNPJs**: usar números com dígitos válidos nos exemplos.
5. **IDs**: usar GUIDs placeholder como `"a1b2c3d4-..."` consistentemente.
6. **Regras com branch**: usar `SE/SENÃO` com indentação clara e setas `→`.
7. **Consistência**: se duas specs referenciam o mesmo endpoint, os detalhes devem bater.

## Anti-Padrões

- ❌ Copiar o storytelling sem adicionar detalhe técnico (endpoint, validações, SQL).
- ❌ Incluir stories de frontend puro na pasta `roadmap/`.
- ❌ Usar `Guid?` ou `null` onde o domínio exige valor obrigatório.
- ❌ Deixar seções com "N/A" — se não se aplica, omitir a seção.
- ❌ Inventar endpoints ou regras não mencionados no storytelling.
- ❌ Usar linguagem vaga ("etc", "dentre outros", "e assim por diante").

## Pós-Geração

Após gerar todos os arquivos:

1. Listar os arquivos criados com número e título.
2. Verificar que a numeração está correta e sem saltos indevidos.
3. Confirmar que cada spec referencia corretamente o `storytelling.md` e `visao.md`.
