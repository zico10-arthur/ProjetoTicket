## ADR-009: Background Worker para liberação de assentos (ATUALIZADO — spec 190)

**Status:** ✅ Aceito (atualizado em 17/06/2026)

**Contexto:** Quando um comprador inicia o checkout, o assento fica com `Status=1` (Reservado). Se ele abandonar, o assento precisa voltar ao estado Livre. A v1 usava `BackgroundService` com `PeriodicTimer`, mas a ausência de persistência e monitoramento levou à substituição pelo Hangfire na v2.

**Alternativas consideradas:**

| Opção | Prós | Contras |
|-------|------|---------|
| BackgroundService (timer) | Nativo do .NET, simples, sem dependência externa | Executa em memória, para se a API parar, sem dashboard, sem retry |
| **Hangfire** | Jobs persistentes em banco, dashboard com histórico, retry automático, mesma connection string | Dependência externa (NuGet), tabelas extras no banco |
| Quartz.NET | Scheduling cron-like, persistente | Mais complexo de configurar que Hangfire, sem dashboard built-in |

**Decisão:** **Hangfire** com storage SQL Server (mesmo banco do projeto). Job `LiberacaoAssentosJob.ExecutarAsync()` executado a cada 60 segundos via `Cron.Minutely`. O `BackgroundService` anterior (`LiberacaoAssentosWorker.cs`) foi removido.

**Consequências:**
- `LiberacaoAssentosJob.cs` em `Api/BackgroundTasks/` — mesmo diretório, implementação diferente
- `[DisableConcurrentExecution(timeoutInSeconds: 120)]` impede execuções paralelas do mesmo job
- Dashboard em `/hangfire` protegido por `HangfireAdminAuthorizationFilter` (apenas Admin)
- Tabelas do Hangfire (`HangFire.Job`, `HangFire.State`, etc.) criadas automaticamente via `PrepareSchemaIfNecessary = true`
- Mesmas queries SQL do worker anterior — `DeletarReservasNaoPagasExpiradas(15)` e `LiberarAssentosExpirados(15)` — preservadas
- Se a API reiniciar, o Hangfire retoma os jobs do banco sem perda de estado

---

