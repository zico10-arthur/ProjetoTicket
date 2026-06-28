FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar arquivos de projeto e restaurar dependências
COPY Web/Web.csproj Web/
COPY Application/Application.csproj Application/
COPY Domain/Domain.csproj Domain/

RUN dotnet restore Web/Web.csproj

# Copiar todo o código fonte
COPY . .

# Publicar o frontend Blazor
RUN dotnet publish Web/Web.csproj -c Release -o /app

# Imagem final
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 5057

ENTRYPOINT ["dotnet", "Web.dll"]