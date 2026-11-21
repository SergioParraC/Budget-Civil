# syntax=docker/dockerfile:1

# ---------- Etapa 1: compilación ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /origen

COPY backend/src/BudgetCivil.Domain/BudgetCivil.Domain.csproj backend/src/BudgetCivil.Domain/
COPY backend/src/BudgetCivil.Application/BudgetCivil.Application.csproj backend/src/BudgetCivil.Application/
COPY backend/src/BudgetCivil.Infrastructure/BudgetCivil.Infrastructure.csproj backend/src/BudgetCivil.Infrastructure/
COPY backend/src/BudgetCivil.Api/BudgetCivil.Api.csproj backend/src/BudgetCivil.Api/

RUN dotnet restore backend/src/BudgetCivil.Api/BudgetCivil.Api.csproj

COPY backend/src/ backend/src/

RUN dotnet publish backend/src/BudgetCivil.Api/BudgetCivil.Api.csproj \
    -c Release -o /app/publicado --no-restore

# ---------- Etapa 2: ejecución sin privilegios ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publicado .

# La imagen oficial ya incluye el usuario sin privilegios `app` (uid/gid 1654).
USER app
EXPOSE 8080

ENTRYPOINT ["dotnet", "BudgetCivil.Api.dll"]
