# Spec 190 — Design: Substituir BackgroundService por Hangfire

> **Requirements:** [`requirements.md`](./requirements.md)
> **Contexto:** Clean Architecture (.NET 9 + Dapper + SQL Server + DbUp)

---

## 2. Banco de Dados

### 2.1 Tabelas do Hangfire (criação automática)

As tabelas do Hangfire são criadas automaticamente pelo método `SqlServerStorage` na inicialização. Não é necessário script DbUp manual. As tabelas criadas são:

```
HangFire.AggregatedCounter
HangFire.Counter
HangFire.Hash
HangFire.Job
HangFire.JobParameter
HangFire.JobQueue
HangFire.List
HangFire.Schema
HangFire.Server
HangFire.Set
HangFire.State
```

### 2.2 Diagrama de Relacionamentos

```
dbo.Ingressos                HangFire.Job
┌──────────────────┐        ┌──────────────────┐
│ Id (PK)          │        │ Id (PK)          │
│ EventoId (FK)    │        │ StateId          │
│ Status           │        │ InvocationData   │
│ DataBloqueio     │        │ Arguments        │
└──────────────────┘        │ CreatedAt        │
        │                   └──────────────────┘
        │                            │
dbo.Reservas                HangFire.State
┌──────────────────┐        ┌──────────────────┐
│ Id (PK)          │        │ Id (PK)          │
│ UsuarioCpf       │        │ JobId (FK)       │
│ Pago             │        │ Name             │
│ DataBloqueio     │        │ CreatedAt        │
└──────────────────┘        └──────────────────┘
```

> As tabelas do Hangfire residem no mesmo banco `ProjetoTicketDB`, sem interferir nas tabelas de domínio.

---

## 5. Camada de Infraestrutura

### 5.1 Classe `LiberacaoAssentosJob` (nova)

```csharp
// Api/BackgroundTasks/LiberacaoAssentosJob.cs
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
        _logger.LogInformation("Hangfire: Iniciando liberação de assentos expirados.");

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

### 5.2 Configuração no `Program.cs` (trechos alterados)

```csharp
// Adicionar no topo do arquivo:
using Api.BackgroundTasks;
using Hangfire;
using Hangfire.SqlServer;

// Registrar Hangfire (ANTES de builder.Build()):
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

// REMOVER esta linha:
// builder.Services.AddHostedService<LiberacaoAssentosWorker>();

// Após app.Build(), ANTES de DatabaseMigration.Initialize(...):

// Registrar o recurring job
using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider
        .GetRequiredService<IRecurringJobManager>();

    recurringJobManager.AddOrUpdate<LiberacaoAssentosJob>(
        "liberacao-assentos-expirados",
        job => job.ExecutarAsync(default),
        Cron.Minutely);
}

// Configurar dashboard do Hangfire (após UseAuthorization):
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAdminAuthorizationFilter() }
});
```

### 5.3 Filtro de Autorização do Dashboard

```csharp
// Api/Middlewares/HangfireAdminAuthorizationFilter.cs
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

### 5.4 Remoção do Worker Antigo

O arquivo `Api/BackgroundTasks/LiberacaoAssentosWorker.cs` deve ser **deletado** completamente.

---

## 6. Integração com Fluxos Existentes

| Componente existente | Impacto da spec 190 |
|---|---|
| `ReservaRepository.DeletarReservasNaoPagasExpiradas()` | **Nenhum** — método permanece igual, chamado pelo job Hangfire |
| `IngressoRepository.LiberarAssentosExpirados()` | **Nenhum** — método permanece igual, chamado pelo job Hangfire |
| `ConnectionFactory` | **Nenhum** — Hangfire usa a mesma connection string `DefaultConnection` configurada em `appsettings.json` |
| `Program.cs` | **Alterado** — remove `AddHostedService<LiberacaoAssentosWorker>()`, adiciona configuração Hangfire e recurring job |
| `DatabaseMigration.Initialize()` | **Nenhum** — Hangfire cria suas próprias tabelas via `PrepareSchemaIfNecessary = true` |
| Middlewares existentes (`RateLimiting`, `GlobalExceptionHandler`) | **Nenhum** — dashboard Hangfire fica em pipeline separado |
| Autenticação JWT | **Reutilizada** — dashboard Hangfire protegido por `IDashboardAuthorizationFilter` que verifica `User.IsInRole("Admin")` |
| Logging (`ILogger<T>`) | **Reutilizado** — `LiberacaoAssentosJob` injeta `ILogger<LiberacaoAssentosJob>` |

---

## 8. Decisões de Design

| Decisão | Justificativa |
|---------|---------------|
| Hangfire com storage SQL Server (mesmo banco) | Elimina dependência externa (Redis). Para o volume atual, o mesmo banco suporta tanto os dados de domínio quanto os jobs sem degradação |
| `PrepareSchemaIfNecessary = true` | Tabelas do Hangfire criadas automaticamente na primeira execução, sem script DbUp manual. Idempotente: se já existirem, não recria |
| `DisableConcurrentExecution(timeoutInSeconds: 120)` | Impede que duas execuções do mesmo job rodem em paralelo. Timeout de 120s cobre folgadamente o pior caso (as queries levam <1s atualmente) |
| `Cron.Minutely` | Equivalente ao `PeriodicTimer(TimeSpan.FromMinutes(1))` atual. Mantém a mesma cadência |
| `IRecurringJobManager.AddOrUpdate` em vez de atributo `[RecurringJob]` | Permite configurar o job em um único ponto central (`Program.cs`), mantendo a classe de job limpa e testável |
| Dashboard apenas para Admin | Segurança: expor dashboard para todos os perfis vazaria informações operacionais (logs de execução, falhas). Admin é o operador da plataforma |
| `IServiceProvider` como dependência (não os repositórios direto) | O job é singleton, mas os repositórios são scoped. `CreateScope()` replica o padrão do worker atual |
| Remoção completa do `LiberacaoAssentosWorker` em vez de manter ambos | Evita duplicidade de execução (dois jobs liberando os mesmos assentos). Simplifica o código |
| Hangfire.SqlServer — não Hangfire.MemoryStorage | MemoryStorage perde jobs no restart. Para produção e confiabilidade, SQL Server é necessário |
