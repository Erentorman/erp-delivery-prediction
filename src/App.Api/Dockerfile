# ------------------------------------------------------------
# App.Api — Placeholder Dockerfile (T-104 Day-1 skeleton)
#
# TODO (T-103): Replace this placeholder with the real multi-
# stage build once the App.Api project is complete:
#
#   Stage 1 (build):
#     FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
#     COPY . .
#     RUN dotnet publish src/App.Api/App.Api.csproj \
#         -c Release -o /app/publish
#
#   Stage 2 (runtime):
#     FROM mcr.microsoft.com/dotnet/aspnet:8.0
#     COPY --from=build /app/publish .
#     ENTRYPOINT ["dotnet", "App.Api.dll"]
# ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Placeholder: emit a clear message so `docker compose up`
# shows this service needs its real build, then sleep so
# health-dependent services have a chance to start normally.
# Replace with a real ENTRYPOINT once the publish artifact exists.
CMD ["sh", "-c", "echo '[api] Placeholder container — real build not yet implemented (T-103). Sleeping.' && sleep infinity"]
