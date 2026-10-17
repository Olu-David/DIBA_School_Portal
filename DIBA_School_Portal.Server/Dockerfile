# ==========================================================
# STAGE 1 — Base runtime
# ==========================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# ==========================================================
# STAGE 2 — SDK + Node (Node is needed for the SPA build)
# ==========================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS with-node

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl ca-certificates gnupg \
    && curl -sL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y --no-install-recommends nodejs \
    && rm -rf /var/lib/apt/lists/*

# ==========================================================
# STAGE 3 — Restore (cached layer)
# ==========================================================
FROM with-node AS restore
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["DIBA_School_Portal.Server/DIBA_School_Portal.Server.csproj", "DIBA_School_Portal.Server/"]
COPY ["diba_school_portal.client/diba_school_portal.client.esproj", "diba_school_portal.client/"]
COPY ["diba_school_portal.client/package.json", "diba_school_portal.client/"]
COPY ["diba_school_portal.client/package-lock.json", "diba_school_portal.client/"]

RUN dotnet restore "./DIBA_School_Portal.Server/DIBA_School_Portal.Server.csproj"

# ==========================================================
# STAGE 4 — Build (backend + frontend via esproj)
# ==========================================================
FROM restore AS build
ARG BUILD_CONFIGURATION=Release
COPY . .
WORKDIR "/src/DIBA_School_Portal.Server"
RUN dotnet build "./DIBA_School_Portal.Server.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build

# ==========================================================
# STAGE 5 — Publish
# ==========================================================
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DIBA_School_Portal.Server.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

# ==========================================================
# STAGE 6 — Final runtime
# ==========================================================
FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

# Non-root user (APP_UID is defined in .NET 8+ images)
USER $APP_UID

# Bind to Render's $PORT, fall back to 8080 locally
ENV ASPNETCORE_URLS=http://+:${PORT:-8080} \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_NOLOGO=true

ENTRYPOINT ["dotnet", "DIBA_School_Portal.Server.dll"]