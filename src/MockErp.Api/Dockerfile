# ------------------------------------------------------------
# MockErp.Api — Multi-stage build
#
# Build context is the repository root (see docker-compose.yml)
# for consistency with the other .NET services; MockErp.Api has
# no ProjectReferences of its own.
# ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/MockErp.Api/MockErp.Api.csproj src/MockErp.Api/

RUN dotnet restore src/MockErp.Api/MockErp.Api.csproj

COPY src/MockErp.Api/ src/MockErp.Api/

RUN dotnet publish src/MockErp.Api/MockErp.Api.csproj -c Release -o /app/publish --no-restore

# ------------------------------------------------------------
# Runtime stage
# curl is installed only so the container's own HEALTHCHECK /
# docker-compose healthcheck can call GET /health without
# adding any application dependency.
# ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "MockErp.Api.dll"]
