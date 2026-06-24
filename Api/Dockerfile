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

# DbUp scripts como embedded resources precisam estar na DLL
ENTRYPOINT ["dotnet", "Api.dll"]