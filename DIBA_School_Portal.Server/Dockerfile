# ==========================================
# STAGE 1 — Base runtime
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# ==========================================
# STAGE 2 — SDK + Node.js Installation
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /src

# Install Node.js (v20) and npm so the JavaScript SDK can build the client project
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl ca-certificates gnupg \
    && mkdir -p /etc/apt/keyrings \
    && curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg \
    && echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_20.x nodistro main" | tee /etc/apt/sources.list.d/nodesource.list \
    && apt-get update \
    && apt-get install -y nodejs \
    && rm -rf /var/lib/apt/lists/*

# Verify Node is accessible
RUN node --version && npm --version

# Copy solution and project files
COPY ["DIBA_School_Portal.slnx", "./"]
COPY ["DIBA_School_Portal.Server/DIBA_School_Portal.Server.csproj", "DIBA_School_Portal.Server/"]
COPY ["DIBA_School_Portal.ServiceDefaults/DIBA_School_Portal.ServiceDefaults.csproj", "DIBA_School_Portal.ServiceDefaults/"]
COPY ["diba_school_portal.client/diba_school_portal.client.esproj", "diba_school_portal.client/"]
COPY ["diba_school_portal.client/package.json", "diba_school_portal.client/"]
COPY ["diba_school_portal.client/package-lock.json", "diba_school_portal.client/"]

# Restore NuGet packages
RUN dotnet restore "DIBA_School_Portal.Server/DIBA_School_Portal.Server.csproj"

# Copy the rest of the source code
COPY . .

# Build and Publish
WORKDIR "/src/DIBA_School_Portal.Server"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# ==========================================
# STAGE 3 — Final Runtime Image
# ==========================================
FROM base AS final
WORKDIR /app
COPY --from=build-env /app/publish .

ENV ASPNETCORE_URLS=http://+:${PORT:-8080} \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

ENTRYPOINT ["dotnet", "DIBA_School_Portal.Server.dll"]