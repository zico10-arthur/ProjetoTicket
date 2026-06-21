# Spec 190 — Tasks: Substituir BackgroundService por Hangfire

> **Requirements:** [`requirements.md`](./requirements.md)
> **Design:** [`design.md`](./design.md)
> **Ordem:** Cada task depende das anteriores. Seguir a sequência numérica.

---

## Task 1 — Adicionar pacotes NuGet do Hangfire

**Objetivo:** Adicionar as dependências `Hangfire`, `Hangfire.SqlServer` e `Hangfire.AspNetCore` ao projeto `Api`.

| Campo | Valor |
|-------|-------|
| Arquivo | `Api/Api.csproj` (editar) |
| Dependências | Nenhuma |

**Conteúdo:**

```bash
cd Api
dotnet add package Hangfire
dotnet add package Hangfire.SqlServer
dotnet add package Hangfire.AspNetCore
```

**Verificação:**
- `dotnet restore Api` executa sem erros
- `Api/Api.csproj` contém `<PackageReference Include="Hangfire"`, `Hangfire.SqlServer` e `Hangfire.AspNetCore`

---

## Task 2 — Criar filtro de autorização do dashboard Hangfire

**Objetivo:** Criar classe `HangfireAdminAuthorizationFilter` que restringe o dashboard `/hangfire` a usuários com role `Admin`.

| Campo | Valor |
|-------|-------|
| Arquivo | `Api/Middlewares/HangfireAdminAuthorizationFilter.cs` (criar) |
| Dependências | Task 1 |

**Conteúdo:**

```csharp
using Hangfire.Dashboard;

namespace Api.Middlewares;

public class HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.IsInRole("Admin");
    }
}
```

**Verificação:**
- Arquivo compila: `dotnet build Api` sem erros
- Classe implementa `IDashboardAuthorizationFilter` corretamente

---

## Task 3 — Criar classe `LiberacaoAssentosJob`

**Objetivo:** Criar a classe que contém a lógica de liberação, substituindo o `LiberacaoAssentosWorker`.

| Campo | Valor |
|-------|-------|
| Arquivo | `Api/BackgroundTasks/LiberacaoAssentosJob.cs` (criar) |
| Dependências | Task 1 |

**Conteúdo:**

```csharp
using Domain.Interface;
using Hangfire;

namespace Api.BackgroundTasks;

public class LiberacaoAssentosJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LiberacaoAssentosJob> _logger;

    public LiberacaoAssentosJob(
        IServiceProvider serviceProvider,
        ILogger<LiberacaoAssentosJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 120)]
    public async Task ExecutarAsync(CancellationToken ct)
    {
        _logger.LogInformation(
            "Hangfire: Iniciando liberação de assentos expirados.");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var ingressoRepository = scope.ServiceProvider
                .GetRequiredService<IIngressoRepository>();
            var reservaRepository = scope.ServiceProvider
                .GetRequiredService<IReservaRepository>();

            int minutosParaExpirar = 15;

            await reservaRepository.DeletarReservasNaoPagasExpiradas(
                minutosParaExpirar, ct);

            await ingressoRepository.LiberarAssentosExpirados(
                minutosParaExpirar, ct);

            _logger.LogInformation(
                "[{Time}] Hangfire: Varredura concluída. " +
                "Reservas e Assentos expirados foram limpos.",
                DateTime.Now);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Hangfire: Erro ao liberar assentos expirados.");
            throw;
        }
    }
}
```

**Verificação:**
- `dotnet build Api` compila sem erros
- A assinatura do método `ExecutarAsync` bate com o delegate esperado pelo Hangfire: `Expression<Func<T, Task>>`

---

## Task 4 — Configurar Hangfire no `Program.cs`

**Objetivo:** Registrar Hangfire, remover o `BackgroundService` antigo, configurar recurring job e dashboard.

| Campo | Valor |
|-------|-------|
| Arquivo | `Api/Program.cs` (editar) |
| Dependências | Task 2, Task 3 |

**Conteúdo:**

Adicionar os `using` no topo:
```csharp
using Api.BackgroundTasks;
using Hangfire;
using Hangfire.SqlServer;
```

**ANTES** de `var builder = ...` manter os usings existentes. Após `builder.Services.AddAuthorization();`, adicionar:

```csharp
// Hangfire
builder.Services.AddHangfire(config =>
{
    var cs = builder.Configuration.GetConnectionString("DefaultConnection")!;
    config.UseSqlServerStorage(cs, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        PrepareSchemaIfNecessary = true
    });
});

builder.Services.AddHangfireServer();
```

**REMOVER** a linha:
```csharp
builder.Services.AddHostedService<LiberacaoAssentosWorker>();
```

Após `DatabaseSeeder.Seed(seederFactory);` e antes de `app.UseSwagger();`, adicionar:

```csharp
// Registrar recurring job do Hangfire
using (var hangfireScope = app.Services.CreateScope())
{
    var recurringJobManager = hangfireScope.ServiceProvider
        .GetRequiredService<IRecurringJobManager>();

    recurringJobManager.AddOrUpdate<LiberacaoAssentosJob>(
        "liberacao-assentos-expirados",
        job => job.ExecutarAsync(default),
        Cron.Minutely);
}
```

Após `app.UseAuthorization();`, adicionar:

```csharp
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new Api.Middlewares.HangfireAdminAuthorizationFilter() }
});
```

**Verificação:**
- `dotnet build Api` compila sem erros (importante: o using de `Api.BackgroundTasks` resolve `LiberacaoAssentosJob`)
- `dotnet build Api` **NÃO** referencia `LiberacaoAssentosWorker` (confirma que a remoção foi completa)
- `dotnet run` inicia sem erros, Hangfire cria tabelas no banco

---

## Task 5 — Remover `LiberacaoAssentosWorker.cs`

**Objetivo:** Deletar o arquivo do worker antigo.

| Campo | Valor |
|-------|-------|
| Arquivo | `Api/BackgroundTasks/LiberacaoAssentosWorker.cs` (deletar) |
| Dependências | Task 4 |

**Conteúdo:** Deletar o arquivo completamente.

**Verificação:**
- `dotnet build Api` compila sem erros (sem referências quebradas)
- `rg "LiberacaoAssentosWorker" Api/` retorna zero resultados
- O diretório `Api/BackgroundTasks/` contém apenas `LiberacaoAssentosJob.cs`

---

## Task 6 — Testar o job recorrente

**Objetivo:** Verificar que o recurring job executa corretamente e libera assentos expirados.

| Campo | Valor |
|-------|-------|
| Arquivo | Nenhum (teste manual + verificação no banco) |
| Dependências | Task 5 |

**Conteúdo:**

1. Rodar a API: `cd Api && dotnet run`
2. Acessar `http://localhost:5007/hangfire` → logar como Admin → verificar que o dashboard carrega
3. No dashboard, acessar **Recurring Jobs** → confirmar que `liberacao-assentos-expirados` aparece com cron `* * * * *`
4. Criar uma reserva (via Swagger) e aguardar 15+ minutos sem pagar
5. Verificar no dashboard (aba **Succeeded**) que o job executou e liberou os assentos
6. Confirmar no banco que `Ingressos.Status` voltou a `0` e as `Reservas` foram deletadas

**Verificação:**
- Dashboard acessível apenas como Admin (usuário Comprador/Vendedor recebe 401)
- Job aparece como **Succeeded** após cada execução
- Assentos e reservas expirados são corretamente liberados/deletados
- O log contém `"Hangfire: Varredura concluída."` com timestamp

---

## Task 7 — Atualizar `docs/visao.md` e `docs/agents/roadmap.md`

**Objetivo:** Atualizar documentação do projeto refletindo a migração.

| Campo | Valor |
|-------|-------|
| Arquivos | `docs/visao.md` (editar), `docs/agents/roadmap.md` (editar) |
| Dependências | Task 6 |

**Conteúdo:**

### `docs/visao.md`
- Em `§6.8`, substituir "Background Worker de Liberação de Assentos" por "Hangfire Recurring Job de Liberação de Assentos"
- Atualizar a descrição da tabela: "Worker executa a cada 60s" → "Hangfire recurring job executa a cada 60s com persistência em banco e dashboard de monitoramento"
- Em `§8.1` (Stack Tecnológica), adicionar linha: `Hangfire | Agendamento e monitoramento de jobs em background`
- Em `§10` (Escopo), substituir "Background Worker" por "Hangfire recurring job para liberação de assentos expirados"

### `docs/agents/roadmap.md`
- Incrementar contador no header: `(27 arquivos)` → `(30 arquivos)`
- Adicionar na tabela da Sprint 3:
  ```
  | 190 | Substituir BackgroundService por Hangfire | ❌ `pendente` | **Gerenciar vagas** — confiabilidade na liberação | [`spec-190/`](./roadmap/spec-190/) |
  ```
- Atualizar tabela de "Resumo por Status": incrementar `pendente` de 9 para 10
- Atualizar "Evidências no Código" adicionando:
  ```
  | 190 | `LiberacaoAssentosWorker.cs` existe com PeriodicTimer — spec completa em [`spec-190/`](./roadmap/spec-190/) | ❌ `pendente` |
  ```

**Verificação:**
- `docs/visao.md` contém "Hangfire" na stack (§8.1)
- `docs/agents/roadmap.md` contém spec 190 na tabela
- Contagem de arquivos no header do roadmap reflete `(30 arquivos)`

---

## Resumo de Tarefas

| # | Camada | Arquivo(s) | Ação |
|---|--------|-----------|------|
| 1 | Api | `Api/Api.csproj` | Editar (adicionar pacotes NuGet) |
| 2 | Api | `Api/Middlewares/HangfireAdminAuthorizationFilter.cs` | Criar |
| 3 | Api | `Api/BackgroundTasks/LiberacaoAssentosJob.cs` | Criar |
| 4 | Api | `Api/Program.cs` | Editar (registrar Hangfire, remover worker) |
| 5 | Api | `Api/BackgroundTasks/LiberacaoAssentosWorker.cs` | Deletar |
| 6 | — | Teste manual | Verificar |
| 7 | docs | `docs/visao.md`, `docs/agents/roadmap.md` | Editar |

---

## Dependências entre Tasks

```
Task 1 (NuGet packages)
  ├── Task 2 (Authorization filter)
  ├── Task 3 (LiberacaoAssentosJob)
  └── Task 4 (Program.cs) ── depende de Task 2 e Task 3
        └── Task 5 (Remover Worker) ── depende de Task 4
              └── Task 6 (Testar) ── depende de Task 5
                    └── Task 7 (Atualizar docs) ── depende de Task 6
```

---

## Estimativa de Esforço

| Bloco | Tasks | Esforço |
|-------|-------|---------|
| NuGet | 1 | 0.1h |
| API (novos arquivos) | 2, 3 | 0.5h |
| API (configuração) | 4, 5 | 1h |
| Testes | 6 | 0.5h |
| Docs | 7 | 0.5h |
| **Total** | **7 tasks** | **~2.6h** |
