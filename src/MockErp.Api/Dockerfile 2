# ------------------------------------------------------------
# MockErp.Api — Placeholder Dockerfile (T-104 Day-1 skeleton)
#
# TODO (T-103): Replace this placeholder with the real multi-
# stage build once the MockErp.Api project is complete:
#
#   Stage 1 (build):
#     FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
#     COPY . .
#     RUN dotnet publish src/MockErp.Api/MockErp.Api.csproj \
#         -c Release -o /app/publish
#
#   Stage 2 (runtime):
#     FROM mcr.microsoft.com/dotnet/aspnet:8.0
#     COPY --from=build /app/publish .
#     ENTRYPOINT ["dotnet", "MockErp.Api.dll"]
# ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

CMD ["sh", "-c", "echo '[mock-erp] Placeholder container — real build not yet implemented (T-103). Sleeping.' && sleep infinity"]
