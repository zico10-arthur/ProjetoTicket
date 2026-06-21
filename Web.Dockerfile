# ===== Web Dockerfile (Frontend Blazor Server) =====
# Contexto de build: raiz do ProjetoTicket/ (contendo Web/, Application/, Domain/)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar todos os csproj para restaurar dependências em cache layer
COPY Web/Web.csproj Web/
COPY Application/Application.csproj Application/
COPY Domain/Domain.csproj Domain/
COPY Infraestructure/Infraestructure.csproj Infraestructure/
RUN dotnet restore Web/Web.csproj

# Copiar código fonte completo
COPY Web/ Web/
COPY Application/ Application/
COPY Domain/ Domain/
COPY Infraestructure/ Infraestructure/

# Publicar o frontend
WORKDIR /src/Web
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 5057
ENV ASPNETCORE_URLS=http://+:5057
ENTRYPOINT ["dotnet", "Web.dll"]