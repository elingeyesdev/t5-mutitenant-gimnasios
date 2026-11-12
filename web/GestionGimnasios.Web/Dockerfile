# Etapa 1: compilación y publicación
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY web/GestionGimnasios.Web/GestionGimnasios.Web.csproj web/GestionGimnasios.Web/
RUN dotnet restore web/GestionGimnasios.Web/GestionGimnasios.Web.csproj

COPY web/GestionGimnasios.Web/ web/GestionGimnasios.Web/
RUN dotnet publish web/GestionGimnasios.Web/GestionGimnasios.Web.csproj \
    -c Release -o /app/publish --no-restore

# Etapa 2: ejecución
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "GestionGimnasios.Web.dll"]
