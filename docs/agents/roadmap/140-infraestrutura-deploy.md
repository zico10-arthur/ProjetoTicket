# 140 — Infraestrutura e Deploy ✅

> **Status:** `audited`
> **Origem:** `sprints.md` — infraestrutura compartilhada
>
> **Problema:** [`visao.md#2`](../../visao.md#2-problema) — Criar e divulgar eventos com sistema disponível, escalável e de fácil configuração por qualquer desenvolvedor do time.

---

## 140.1 O que resolve

| Item | Sem spec | Com spec |
|------|----------|----------|
| Ambiente local | Cada dev configura manualmente | `docker-compose up -d` sobe API + SQL Server |
| Connection pool | Conexões esgotam sob carga | `Max Pool Size=100` |
| Migrations | Scripts SQL manuais | DbUp executa migrations automaticamente ao iniciar a API |
| Novo dev onboarding | Configuração obscura | `README.md` + `docker-compose` padronizados |

---

## 140.2 Docker Compose (implementado)

```yaml
version: '3.8'

services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      SA_PASSWORD: "ProjetoTicket@2026!"
      MSSQL_PID: "Express"
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql
    healthcheck:
      test: /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "ProjetoTicket@2026!" -Q "SELECT 1" || exit 1
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 30s

  api:
    build:
      context: .
      dockerfile: Api/Dockerfile
    ports:
      - "5007:5007"
    environment:
      ConnectionStrings__DefaultConnection: "Server=sqlserver;Database=ProjetoTicketDB;User=sa;Password=ProjetoTicket@2026!;TrustServerCertificate=True;Max Pool Size=100;"
      Jwt__Key: "chave-desenvolvimento-local-nao-usar-em-producao"
      Jwt__Issuer: "ProjetoTicket"
      Jwt__Audience: "ProjetoTicket"
    depends_on:
      sqlserver:
        condition: service_healthy
    healthcheck:
      test: curl --fail http://localhost:5007/swagger/index.html || exit 1
      interval: 15s
      timeout: 5s
      retries: 3
      start_period: 20s

  web:
    build:
      context: .
      dockerfile: Web/Dockerfile
    ports:
      - "5057:5057"
    environment:
      ApiBaseUrl: "http://api:5007"
    depends_on:
      - api

volumes:
  sqlserver_data:
```

> **Nota:** O serviço `web` (Blazor Server) e os health checks foram adicionados durante a implementação. O dashboard Hangfire (spec 190) fica em `/hangfire` na API.

---

## 140.3 Dockerfiles (implementados)

### API (`Api/Dockerfile`)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar arquivos de projeto e restaurar dependências
COPY Api/Api.csproj Api/
COPY Application/Application.csproj Application/
COPY Domain/Domain.csproj Domain/
COPY Infraestructure/Infraestructure.csproj Infraestructure/

RUN dotnet restore Api/Api.csproj

# Copiar todo o código fonte
COPY . .

# Publicar a API
RUN dotnet publish Api/Api.csproj -c Release -o /app

# Imagem final
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 5007
ENTRYPOINT ["dotnet", "Api.dll"]
```

### Web (`Web/Dockerfile`)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Web/Web.csproj Web/
COPY Application/Application.csproj Application/
COPY Domain/Domain.csproj Domain/

RUN dotnet restore Web/Web.csproj

COPY . .

RUN dotnet publish Web/Web.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 5057
ENTRYPOINT ["dotnet", "Web.dll"]
```

---

## 140.4 Connection Pool

```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=ProjetoTicketDB;Trusted_Connection=True;TrustServerCertificate=True;Max Pool Size=100;"
  }
}
```

| Configuração | Valor | Motivo |
|---|---|---|
| `Max Pool Size` | 100 | Evita esgotamento com múltiplos usuários simultâneos |
| `TrustServerCertificate` | True | Dev local — produção usa certificado real |

---

## 140.5 Migrations (DbUp)

```csharp
// Program.cs
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

EnsureDatabase.For.SqlDatabase(connectionString);

var upgrader = DeployChanges.To
    .SqlDatabase(connectionString)
    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
    .LogToConsole()
    .Build();

var result = upgrader.PerformUpgrade();
if (!result.Successful)
    throw new Exception("Falha na migração", result.Error);
```

### Ordem dos scripts

```
Infraestructure/DataBase/Scripts/
├── 001_CreatePerfis.sql
├── 002_CreateUsuarios.sql
├── 003_AlterUsuarios_Vendedor.sql    # ST-09
├── 004_CreateEventos.sql
├── 005_AlterEventos_VendedorId.sql   # ST-03, ST-11
├── 006_CreateIngressos.sql
├── 007_CreateReservas.sql
├── 008_CreateCupons.sql
├── 009_AlterCupons_VendedorId.sql    # ST-01
├── 010_CreateItensReserva.sql        # ST-04
└── 011_SeedData.sql                  # ST-10 (perfis + admin)
```

---

## 140.6 Comandos (implementados)

```bash
# Subir ambiente completo
docker-compose up -d

# Subir reconstruindo imagens (após mudanças no código)
docker-compose up -d --build

# Ver logs da API
docker-compose logs -f api

# Ver status
docker-compose ps

# Parar
docker-compose down

# Recriar do zero (limpa banco)
docker-compose down -v && docker-compose up -d

# Swagger
http://localhost:5007/swagger

# Hangfire Dashboard (Admin)
http://localhost:5007/hangfire
```
