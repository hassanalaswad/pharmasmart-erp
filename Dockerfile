# ── Stage 1: Build ──────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies (layer cache optimization)
COPY PharmaSmartWeb/*.csproj ./PharmaSmartWeb/
RUN dotnet restore "./PharmaSmartWeb/PharmaSmartWeb.csproj"

# Copy all source files
COPY PharmaSmartWeb/. ./PharmaSmartWeb/

# Publish release build
RUN dotnet publish "./PharmaSmartWeb/PharmaSmartWeb.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: Runtime ─────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production

# Render.com injects $PORT at runtime — use shell form ENTRYPOINT
ENTRYPOINT dotnet PharmaSmartWeb.dll --urls "http://+:${PORT:-8080}"
