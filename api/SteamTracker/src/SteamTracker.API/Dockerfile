FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /workspace/api/SteamTracker/src
COPY SteamTracker.Domain/SteamTracker.Domain.csproj ./SteamTracker.Domain/
COPY SteamTracker.Application/SteamTracker.Application.csproj ./SteamTracker.Application/
COPY SteamTracker.Infrastructure/SteamTracker.Infrastructure.csproj ./SteamTracker.Infrastructure/
COPY SteamTracker.API/SteamTracker.API.csproj ./SteamTracker.API/
RUN dotnet restore SteamTracker.API/SteamTracker.API.csproj
COPY . .
WORKDIR /workspace/api/SteamTracker/src/SteamTracker.API
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "SteamTracker.API.dll"]
