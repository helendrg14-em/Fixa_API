# Imagen de la API para Render. Render no trae .NET, por eso se despliega con Docker.

# 1) Compilar
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS compilar
WORKDIR /src
COPY Fixa.Api/Fixa.Api.csproj Fixa.Api/
RUN dotnet restore Fixa.Api/Fixa.Api.csproj
COPY Fixa.Api/ Fixa.Api/
RUN dotnet publish Fixa.Api/Fixa.Api.csproj -c Release -o /app --no-restore

# 2) Ejecutar (solo el runtime, sin el SDK)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=compilar /app .
# Render envía el tráfico al puerto 10000. Se usa el usuario sin privilegios de la imagen.
ENV ASPNETCORE_HTTP_PORTS=10000 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 10000
USER $APP_UID
ENTRYPOINT ["dotnet", "Fixa.Api.dll"]
