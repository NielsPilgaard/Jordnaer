# https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md
FROM mcr.microsoft.com/dotnet/sdk:10.0-azurelinux3.0 AS build
WORKDIR /source

ARG PROJECT=src/web/Jordnaer/Jordnaer.csproj

# Copy solution file and all project files for restore
COPY --link Jordnaer.sln .
COPY --link Directory.Build.props .
COPY --link src/web/Jordnaer/*.csproj src/web/Jordnaer/
COPY --link src/shared/Jordnaer.Shared/*.csproj src/shared/Jordnaer.Shared/

# Restore dependencies
RUN dotnet restore "${PROJECT}"

# Copy all source files
COPY --link src/ src/

# Build argument for version
ARG VERSION=1.0.0

# Publish app
RUN dotnet publish "${PROJECT}" --no-restore -c Release -o /app -p:InformationalVersion="${VERSION}"

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-azurelinux3.0

RUN tdnf install -y curl && tdnf clean all

EXPOSE 8080
WORKDIR /app
COPY --link --from=build /app .

HEALTHCHECK --interval=10s --timeout=5s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:8080/alive || exit 1

ENTRYPOINT ["dotnet", "Jordnaer.dll"]
