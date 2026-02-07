# Consultez https://aka.ms/customizecontainer pour savoir comment personnaliser votre conteneur.

# === GESTION DES ARGUMENTS (Correction du Warning) ===
ARG LAUNCHING_FROM_VS
# 1. Si on lance depuis VS, on cible 'aotdebug'
ARG FINAL_BASE_IMAGE=${LAUNCHING_FROM_VS:+aotdebug}
# 2. Si c'est vide (Production), on force l'image Runtime par défaut ICI
# Cela supprime le warning car la variable est désormais garantie d'être définie
ARG FINAL_IMAGE=${FINAL_BASE_IMAGE:-mcr.microsoft.com/dotnet/runtime-deps:10.0}

# === ÉTAPE 1 : Runtime Base (Debug F5) ===
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

# === ÉTAPE 2 : Build Environment ===
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
# Outils natifs indispensables
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    clang zlib1g-dev

WORKDIR /src

# Copie des fichiers projets (Respect de l'arborescence)
COPY ["src/MonECC.CLI/MonECC.CLI.csproj", "src/MonECC.CLI/"]
COPY ["src/MonECC.Application/MonECC.Application.csproj", "src/MonECC.Application/"]
COPY ["src/MonECC.Infrastructure/MonECC.Infrastructure.csproj", "src/MonECC.Infrastructure/"]
COPY ["src/MonECC.Domain/MonECC.Domain.csproj", "src/MonECC.Domain/"]

# Restauration
RUN dotnet restore "src/MonECC.CLI/MonECC.CLI.csproj"

# Copie du code source
COPY . .

# Build
WORKDIR "/src/src/MonECC.CLI"
ARG BUILD_CONFIGURATION=Release
RUN dotnet build "MonECC.CLI.csproj" -c $BUILD_CONFIGURATION -o /app/build

# === ÉTAPE 3 : Publication AOT ===
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "MonECC.CLI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=true

# === ÉTAPE 4 : Debug AOT (Support VS) ===
FROM base AS aotdebug
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    gdb
USER app

# === ÉTAPE 5 : Final (Production) ===
# On utilise la variable nettoyée FINAL_IMAGE
FROM ${FINAL_IMAGE} AS final

# Installation propre dans /app/bin
WORKDIR /app/bin
COPY --from=publish /app/publish .

# Dossier de données par défaut pour les volumes
WORKDIR /data

# Point d'entrée absolu
ENTRYPOINT ["/app/bin/MonECC.CLI"]