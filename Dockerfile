# https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
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

# Publish app
RUN dotnet publish "${PROJECT}" --no-restore -c Release -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0

EXPOSE 8080
WORKDIR /app
COPY --link --from=build /app .

ENTRYPOINT ["dotnet", "Jordnaer.dll"]
