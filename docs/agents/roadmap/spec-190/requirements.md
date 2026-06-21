# Spec 190 — Requirements: Substituir BackgroundService por Hangfire

> **Projeto:** SoldOut Tickets
> **Contexto:** [`visao.md §6.8`](../../../visao.md#68-background-worker-de-liberação-de-assentos) | Análise de escalabilidade do `LiberacaoAssentosWorker`
> **Status:** `implementada`

---

## 1. Objetivo

Substituir o `LiberacaoAssentosWorker` (BackgroundService com `PeriodicTimer` de 60 segundos) pelo **Hangfire** com job recorrente, eliminando polling ineficiente e garantindo persistência e monitoramento das execuções de liberação de assentos e exclusão de reservas expiradas.

---

## 3. Requisitos Funcionais

| ID | Descrição |
|----|-----------|
| RF-19001 | O job de liberação deve executar a cada **60 segundos**, mesma cadência do worker atual |
| RF-19002 | O job deve executar as mesmas duas operações do worker atual: `DeletarReservasNaoPagasExpiradas(15)` e `LiberarAssentosExpirados(15)` |
| RF-19003 | O job deve usar o mesmo `CancellationToken` via `IJobCancellationToken` para respeitar shutdown graceful |
| RF-19004 | O dashboard do Hangfire deve estar disponível em `/hangfire` apenas para usuários autenticados com role `Admin` |
| RF-19005 | O `LiberacaoAssentosWorker.cs` deve ser **removido** do projeto e seu registro `AddHostedService<LiberacaoAssentosWorker>()` removido do `Program.cs` |
| RF-19006 | As tabelas do Hangfire devem ser criadas automaticamente na inicialização, usando o mesmo banco SQL Server do projeto |

---

## 4. Requisitos Não Funcionais

| ID | Descrição |
|----|-----------|
| RNF-19001 | **Confiabilidade:** jobs são persistidos em banco — se o servidor reiniciar, o job continua executando na próxima iteração sem perda de estado |
| RNF-19002 | **Observabilidade:** dashboard do Hangfire exibe histórico de execuções, falhas e tempo de cada job |
| RNF-19003 | **Performance:** a migração não deve degradar o tempo de inicialização da aplicação em mais de 2 segundos |
| RNF-19004 | **Compatibilidade:** o mesmo `ConnectionFactory` existente deve ser reutilizado para o storage do Hangfire |
| RNF-19005 | **Idempotência:** as queries SQL executadas pelo job permanecem idempotentes (eram antes, continuam sendo) |

---

## 7. Escopo

### Dentro do escopo
- Adicionar pacotes NuGet `Hangfire` e `Hangfire.AspNetCore`
- Configurar Hangfire com storage SQL Server usando a connection string existente
- Criar classe `LiberacaoAssentosJob` com método `ExecutarAsync` contendo a lógica atual do worker
- Registrar recurring job com cron `* * * * *` (a cada minuto)
- Configurar dashboard do Hangfire protegido por autorização Admin
- Remover `LiberacaoAssentosWorker.cs` e seu registro no `Program.cs`

### Fora do escopo
- Alterar a lógica de negócio das queries de liberação/expiração
- Alterar o timeout de 15 minutos
- Substituir outros BackgroundServices (ex: email worker)
- Usar Hangfire para filas de e-mail ou outros jobs que não o de liberação
- Migrar para outro storage que não SQL Server (ex: Redis)
