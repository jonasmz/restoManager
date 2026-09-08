# Imagen de desarrollo para las APIs .NET. El código se monta como volumen;
# el contenedor solo aporta el SDK y hot-reload (`dotnet watch`).
FROM mcr.microsoft.com/dotnet/sdk:10.0

# curl para el healthcheck de compose.
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_USE_POLLING_FILE_WATCHER=1 \
    ASPNETCORE_URLS=http://0.0.0.0:8080

WORKDIR /src
EXPOSE 8080

# El proyecto concreto y el comando los define docker-compose (`command:`).
CMD ["bash", "-lc", "dotnet restore && dotnet watch run --project \"$API_PROJECT\" --non-interactive"]
