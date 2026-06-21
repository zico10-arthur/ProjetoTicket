# ===== API Dockerfile =====
# Contexto de build: raiz do ProjetoTicket/ (contendo Api/, Domain/, Application/, Infraestructure/)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar todos os csproj para restaurar dependências em cache layer
COPY Api/Api.csproj Api/
COPY Domain/Domain.csproj Domain/
COPY Application/Application.csproj Application/
COPY Infraestructure/Infraestructure.csproj Infraestructure/
RUN dotnet restore Api/Api.csproj

# Copiar código fonte completo
COPY Api/ Api/
COPY Domain/ Domain/
COPY Application/ Application/
COPY Infraestructure/ Infraestructure/

# Publicar a API
WORKDIR /src/Api
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 5007
ENV ASPNETCORE_URLS=http://+:5007
ENTRYPOINT ["dotnet", "Api.dll"]