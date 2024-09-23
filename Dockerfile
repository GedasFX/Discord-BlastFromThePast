# syntax=docker/dockerfile:1.7-labs

# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY --parents ./src/**/*.csproj .
RUN dotnet restore src/BlastFromThePast

# copy everything else and build app
COPY --parents ./src/**/ .
RUN dotnet publish -c Release -o /app --no-restore -r linux-x64 --self-contained -p:PublishTrimmed=true src/BlastFromThePast

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-noble-chiseled-extra
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "BlastFromThePast.dll"]