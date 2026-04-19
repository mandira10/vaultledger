# syntax=docker/dockerfile:1.7

# Stage 1: build and publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files first so nuget restore is cached when deps don't change
COPY src/VaultLedger.Domain/*.csproj            src/VaultLedger.Domain/
COPY src/VaultLedger.Application/*.csproj       src/VaultLedger.Application/
COPY src/VaultLedger.Infrastructure/*.csproj    src/VaultLedger.Infrastructure/
COPY src/VaultLedger.AI/*.csproj                src/VaultLedger.AI/
COPY src/VaultLedger.API/*.csproj               src/VaultLedger.API/

RUN dotnet restore src/VaultLedger.API/VaultLedger.API.csproj

# Copy remaining source and publish
COPY src/ src/
RUN dotnet publish src/VaultLedger.API/VaultLedger.API.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# Stage 2: runtime image (no SDK, no source)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Run as a non-root user (uid 1000 matches the default 'app' user in the aspnet image)
USER app

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

ENTRYPOINT ["dotnet", "VaultLedger.API.dll"]
