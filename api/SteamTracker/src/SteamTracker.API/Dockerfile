FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /workspace/api
COPY SteamTracker/src/SteamTracker.Domain/SteamTracker.Domain.csproj ./SteamTracker/src/SteamTracker.Domain/
COPY SteamTracker/src/SteamTracker.Application/SteamTracker.Application.csproj ./SteamTracker/src/SteamTracker.Application/
COPY SteamTracker/src/SteamTracker.Infrastructure/SteamTracker.Infrastructure.csproj ./SteamTracker/src/SteamTracker.Infrastructure/
COPY SteamTracker/src/SteamTracker.API/SteamTracker.API.csproj ./SteamTracker/src/SteamTracker.API/
COPY CrossService/CrossService.csproj ./CrossService/
RUN dotnet restore SteamTracker/src/SteamTracker.API/SteamTracker.API.csproj
COPY . .
WORKDIR /workspace/api/SteamTracker/src/SteamTracker.API
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "SteamTracker.API.dll"]
