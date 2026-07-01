# Fluxo de Manutenção — SoldOut Tickets

> Classificação de tickets segundo a taxonomia de Swanson (Corretiva, Adaptativa, Perfectiva, Preventiva).

---

## Parte 1: Classificação dos 12 Tickets

| # | Ticket | Descrição | Classificação |
|---|---|---|---|
| **Ticket 1** | Erro ao cadastrar vendedor: `JsonDocument.Parse` quebra com resposta não-JSON da API | O frontend espera `{ "message": "..." }` mas a API retorna string pura `"texto do erro"` em alguns endpoints. O `JsonDocument.Parse` lança exceção e o usuário vê "Erro ao conectar com o servidor". | **Corretiva** |
| **Ticket 2** | Migrar projeto de .NET 9 para .NET 10 quando disponível | O runtime .NET 10 trará melhorias de performance e segurança. É necessário atualizar todos os `.csproj`, testar pacotes NuGet (Dapper, BCrypt, MailKit) e validar breaking changes. | **Adaptativa** |
| **Ticket 3** | Adicionar página de dashboard com gráficos de vendas para o Vendedor | O Vendedor precisa visualizar métricas: ingressos vendidos por evento, faturamento mensal, taxa de ocupação. Usar gráficos de barras/linha com dados já disponíveis nas tabelas `Reservas` e `Pagamentos`. | **Perfectiva** |
| **Ticket 4** | Adicionar health check com validação de conexão ao banco | Atualmente não há monitoramento de saúde da aplicação. Implementar `IHealthCheck` no ASP.NET Core para verificar conectividade com SQL Server, latência de resposta e disponibilidade do Hangfire. Expor em `/health`. | **Preventiva** |
| **Ticket 5** | Corrigir: colunas `Tipo`, `Descricao`, `Local`, `Cancelado` não existem na tabela `Eventos` | O script de migração `Script0009` na pasta correta (`Infraestructure/DataBase/Scripts/`) não contém as colunas que a entidade `Evento` espera. O DbUp executa o script errado. O cadastro de evento quebra com `Invalid column name`. | **Corretiva** |
| **Ticket 6** | Atualizar pacote `BCrypt.Net-Next` da versão 4.2.0 para 5.x | A versão 5.x corrige vulnerabilidade de timing attack e melhora performance em 30%. Necessário validar breaking changes na API de hash e regravar testes do `SegurancaTests.cs`. | **Adaptativa** |
| **Ticket 7** | Melhorar usabilidade do mapa de assentos em dispositivos móveis | O painel de seleção de assentos (SeatMap) não é responsivo: container duplo rouba padding, `touch-action: none` bloqueia scroll, botões de zoom são muito pequenos (36px vs 44px WCAG). | **Perfectiva** |
| **Ticket 8** | Implementar rate limiting nos endpoints de criação de reserva | O endpoint `POST /api/reserva` atualmente não tem proteção contra abuso. Um bot pode criar milhares de reservas e esgotar assentos falsamente. Adicionar limite de 10 requisições/minuto por IP. | **Preventiva** |
| **Ticket 9** | Corrigir: vendedor com `Cpf = string.Empty` causa conflito no índice único `UQ_Usuarios_Cpf` | O factory `Usuario.CriarVendedor()` define `Cpf = string.Empty`. O banco tem índice filtrado `WHERE Cpf IS NOT NULL`, mas `""` não é `NULL`. Dois vendedores sem CPF conflitam. | **Corretiva** |
| **Ticket 10** | Adaptar validação de CPF/CNPJ para novo formato da Receita Federal | A Receita Federal anunciou mudança no algoritmo de dígitos verificadores para 2027. É necessário atualizar `CpfValidator` e `CnpjValidator` no Domain com o novo cálculo e criar período de transição aceitando ambos os formatos. | **Adaptativa** |
| **Ticket 11** | Adicionar modo escuro (dark mode) no frontend Blazor | Usuários solicitaram tema escuro para uso noturno. O MudBlazor já suporta `Theme.Dark`. Implementar toggle no layout principal com persistência da preferência em `localStorage`. | **Perfectiva** |
| **Ticket 12** | Adicionar logging estruturado com Serilog e armazenamento em arquivo rotativo | Atualmente o log é apenas console via `ILogger<T>` padrão. Em produção, logs são perdidos no restart. Implementar Serilog com sinks: console (dev), arquivo JSON rotativo (produção) e nível mínimo Warning para erros de domínio. | **Preventiva** |

---

## Parte 2: Resumo por Categoria (Swanson)

| Categoria | Quantidade | Tickets | Característica |
|---|---|---|---|
| **Corretiva** | 3 | T1, T5, T9 | Correção de defeitos/bugs existentes no sistema |
| **Adaptativa** | 3 | T2, T6, T10 | Mudanças por fatores externos (novo runtime, biblioteca, legislação) |
| **Perfectiva** | 3 | T3, T7, T11 | Melhorias visíveis ao usuário (nova funcionalidade, usabilidade) |
| **Preventiva** | 3 | T4, T8, T12 | Mudanças para prevenir falhas futuras (monitoramento, proteção) |
| **Total** | **12** | | |

---

## Parte 3: Fluxo de Triagem

```
Ticket recebido
      │
      ▼
┌─────────────────────────────┐
│ 1. CLASSIFICAR (Swanson)    │
│ Corretiva / Adaptativa /    │
│ Perfectiva / Preventiva     │
└─────────────┬───────────────┘
              │
              ▼
┌─────────────────────────────┐
│ 2. AVALIAR IMPACTO          │
│ Alto / Médio / Baixo        │
└─────────────┬───────────────┘
              │
              ▼
┌─────────────────────────────┐
│ 3. PRIORIZAR                │
│ P1 (Imediato) /             │
│ P2 (Próx. Sprint) /         │
│ P3 (Backlog)                │
└─────────────┬───────────────┘
              │
              ▼
┌─────────────────────────────┐
│ 4. CORRIGIR (se corretiva)  │
│ ou PLANEJAR (demais)        │
└─────────────────────────────┘
```

**Regra geral:** Tickets Corretivos com impacto Alto entram como P1. Tickets
Adaptativos são agendados por prazo externo (ex: data da mudança na Receita
Federal). Perfectivos e Preventivos entram no backlog e são priorizados por valor
entregue ao usuário × esforço.

---

## Parte 4: Pipeline de Liberação Segura — Ticket Corretivo

> **Ticket exemplo:** Ticket 1 — Erro ao cadastrar vendedor: `JsonDocument.Parse`
> quebra com resposta não-JSON da API.

---

### 1. Análise de Impacto

Antes de qualquer linha de código, mapeia-se o escopo da correção para evitar
efeitos colaterais.

| Item | Resposta |
|---|---|
| **Raiz do problema** | `EventoController.cs` (4 catch blocks) e `UsuarioController.cs` (1 catch block) retornam `BadRequest("texto puro")` em vez de `BadRequest(new { message = "..." })`. O frontend (`CriarEvento.razor`, `CadastroVendedor.razor`) chama `JsonDocument.Parse(response)` que quebra com string não-JSON. |
| **Arquivos afetados** | `Api/Controllers/EventoController.cs` (linhas 29, 58, 80, 104), `Api/Controllers/UsuarioController.cs` (linha 45), `Web/.../CriarEvento.razor` (linha 81), `Web/.../CadastroVendedor.razor` (linha 57) |
| **Módulos dependentes** | Qualquer controller que use `catch (Exception ex) { return BadRequest(ex.Message); }` — é um anti-padrão que pode existir em outros endpoints |
| **Risco da correção** | **Baixo** — a mudança é cirúrgica (trocar string por objeto anônimo). O formato JSON `{ "message": "..." }` já é usado em outros endpoints, então não quebra consumidores existentes |
| **Rollback** | Simples — reverter o commit. Não há migração de banco envolvida |

---

### 2. Teste como Instrumento Cirúrgico

Testes são o bisturi do pipeline. Garantem que a correção resolve o problema
**sem introduzir regressões** nos fluxos adjacentes.

#### 2.1 Teste do defeito (prova de que existia)

```csharp
[Fact]
public async Task CadastrarVendedor_ComDadosInvalidos_DeveRetornarJsonNaoTextoPuro()
{
    // Arrange
    var dto = new CadastrarVendedorDTO { Cnpj = "00000000000000", /* ... */ };

    // Act
    var response = await _client.PostAsJsonAsync("/api/usuario/cadastrar-vendedor", dto);
    var body = await response.Content.ReadAsStringAsync();

    // Assert
    Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
    Assert.DoesNotThrow(() => JsonDocument.Parse(body)); // ← não pode quebrar
}
```

#### 2.2 Teste de regressão (prova de que não quebrou o que já funcionava)

```csharp
[Fact]
public async Task CadastrarVendedor_ComSucesso_DeveRetornarCreated()
{
    var dto = new CadastrarVendedorDTO { Cnpj = "11222333000181", /* dados válidos */ };
    var response = await _client.PostAsJsonAsync("/api/usuario/cadastrar-vendedor", dto);
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
}
```

#### 2.3 Teste de contrato (garante que o formato de resposta é estável)

```csharp
[Theory]
[InlineData("evento", "POST")]   // CreateAsync → BadRequest
[InlineData("evento", "PUT")]    // UpdateAsync → BadRequest
[InlineData("usuario", "POST")]  // CadastrarVendedor → BadRequest
public async Task TodosControllers_Erro_DevemRetornarJsonComPropriedadeMessage(
    string controller, string method)
{
    // ... verifica que TODAS as respostas de erro têm { "message": "..." }
}
```

---

### 3. Feature Toggle

Embora esta correção seja um bug fix (não uma feature nova), o **padrão de resposta
de erro** pode ser encapsulado atrás de um toggle para rollout seguro.

#### 3.1 Toggle no `appsettings.json`

```json
{
  "FeatureFlags": {
    "UsarRespostaErroPadronizada": true
  }
}
```

#### 3.2 Middleware de resposta padronizada (alternativa ao toggle por controller)

Em vez de alterar cada catch block manualmente, cria-se um **Action Filter**
global que intercepta exceções e garante o formato `{ "message": "..." }`:

```csharp
public class ErroPadronizadoFilter : IActionFilter
{
    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Exception != null)
        {
            context.Result = new BadRequestObjectResult(
                new { message = context.Exception.Message });
            context.ExceptionHandled = true;
        }
    }
}
```

**Toggle:** O filtro é registrado condicionalmente com base na flag, permitindo
desligá-lo se houver impacto inesperado em produção:

```csharp
if (featureFlags.UsarRespostaErroPadronizada)
    services.AddControllers(o => o.Filters.Add<ErroPadronizadoFilter>());
```

#### 3.3 Kill Switch

Se o novo formato de erro quebrar algum cliente não mapeado (ex: app mobile),
basta setar `"UsarRespostaErroPadronizada": false` e reiniciar. O comportamento
volta ao anterior sem deploy.

---

### 4. Estratégia de Release e Regressão

#### 4.1 Etapas de release

```
┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
│ 1. LOCAL │ → │ 2. STAGE │ → │ 3. CANÁRIO│ → │ 4. TOTAL │
│ dev +    │   │ banco    │   │ 10% dos  │   │ 100%     │
│ testes   │   │ staging  │   │ usuários │   │ usuários │
└──────────┘   └──────────┘   └──────────┘   └──────────┘
```

| Etapa | O que fazer | Critério de sucesso |
|---|---|---|
| **1. LOCAL** | Rodar `dotnet test` na solution completa (128 testes AAA). Rodar `dotnet build` do Web + Api. | Todos os testes passam. Build sem warnings. |
| **2. STAGE** | Deploy em ambiente de staging com banco clone de produção. Testar manualmente: cadastrar vendedor com CNPJ inválido → verificar que a resposta é `{ "message": "CNPJ inválido" }` e o frontend mostra o snackbar com a mensagem. | Resposta sempre JSON. Mensagem de erro aparece corretamente na tela. |
| **3. CANÁRIO** | Deploy em 1 instância (10% dos usuários). Monitorar logs de erro por 30 minutos. | Zero exceções `JsonDocument.Parse`. Zero erros 500 novos. |
| **4. TOTAL** | Deploy nas instâncias restantes. Monitorar por 24h. | Sem regressões. Tickets similares (T5, T7) fechados. |

#### 4.2 Plano de regressão

Se o canário detectar falha:

1. **Reverter imediatamente** — rollback para a versão anterior via CI/CD
   (tempo alvo: < 5 minutos)
2. **Congelar deploy** — impedir que a versão problemática chegue às outras
   instâncias
3. **Post-mortem leve** — documentar o que falhou no canal da equipe em até 24h:
   - O que foi detectado? (ex: `JsonDocument.Parse` ainda quebrando em endpoint X)
   - Por que o teste de contrato não pegou? (ex: endpoint X não estava coberto)
   - Ação: adicionar endpoint X ao teste de contrato antes do próximo deploy
4. **Reaplicar correção** com o teste adicional, repetindo o pipeline desde a
   etapa LOCAL

---

### Resumo do Pipeline

| Passo | Duração estimada | Responsável |
|---|---|---|
| 1. Análise de Impacto | 15 min | Dev + Tech Lead |
| 2. Teste Cirúrgico | 30 min | Dev |
| 3. Feature Toggle (se aplicável) | 15 min | Dev |
| 4. Release (LOCAL → STAGE → CANÁRIO → TOTAL) | 2h (com monitoramento) | DevOps + Dev |
| **Total** | **~3h** | |
