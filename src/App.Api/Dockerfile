# ------------------------------------------------------------
# App.Api — Multi-stage build
#
# Build context is the repository root (see docker-compose.yml)
# because App.Api references sibling projects (Domain, Application,
# Integration, Persistence, Infrastructure) via ProjectReference.
#
# Layer order: project files are copied and restored first so that
# `docker compose build` reuses the restore layer whenever only
# source files (not dependencies) change.
# ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/App.Api/App.Api.csproj src/App.Api/
COPY src/App.Domain/App.Domain.csproj src/App.Domain/
COPY src/App.Application/App.Application.csproj src/App.Application/
COPY src/App.Integration/App.Integration.csproj src/App.Integration/
COPY src/App.Persistence/App.Persistence.csproj src/App.Persistence/
COPY src/App.Infrastructure/App.Infrastructure.csproj src/App.Infrastructure/

RUN dotnet restore src/App.Api/App.Api.csproj

COPY src/App.Api/ src/App.Api/
COPY src/App.Domain/ src/App.Domain/
COPY src/App.Application/ src/App.Application/
COPY src/App.Integration/ src/App.Integration/
COPY src/App.Persistence/ src/App.Persistence/
COPY src/App.Infrastructure/ src/App.Infrastructure/

RUN dotnet publish src/App.Api/App.Api.csproj -c Release -o /app/publish --no-restore

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

ENTRYPOINT ["dotnet", "App.Api.dll"]
