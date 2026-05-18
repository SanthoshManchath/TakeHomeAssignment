# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj/slnx files first to leverage Docker layer caching on restore
COPY ["WifiProvisioning.slnx", "./"]
COPY ["src/WifiProvisioning.Api/WifiProvisioning.Api.csproj", "src/WifiProvisioning.Api/"]
COPY ["src/WifiProvisioning.Core/WifiProvisioning.Core.csproj", "src/WifiProvisioning.Core/"]
COPY ["tests/WifiProvisioning.Tests/WifiProvisioning.Tests.csproj", "tests/WifiProvisioning.Tests/"]
RUN dotnet restore "src/WifiProvisioning.Api/WifiProvisioning.Api.csproj"

# Copy the rest of the source and publish
COPY . .
RUN dotnet publish "src/WifiProvisioning.Api/WifiProvisioning.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install curl (for healthcheck), clean apt cache to keep image small
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Run as non-root user for security
USER app

# Copy the published output from build stage
COPY --from=build --chown=app:app /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "WifiProvisioning.Api.dll"]