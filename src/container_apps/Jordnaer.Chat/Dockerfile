#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["src/container_apps/Jordnaer.Chat/Jordnaer.Chat.csproj", "src/container_apps/Jordnaer.Chat/"]
RUN dotnet restore "src/container_apps/Jordnaer.Chat/Jordnaer.Chat.csproj"
COPY . .
WORKDIR "/src/src/container_apps/Jordnaer.Chat"
RUN dotnet build "Jordnaer.Chat.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Jordnaer.Chat.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Jordnaer.Chat.dll"]
