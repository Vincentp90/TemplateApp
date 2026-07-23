# TemplateApp

This is a template for a typical business CRUD app. The goal is to have a template using technologies that are modern, stable, popular and technologies I had good success with in my career(C#) or that seem worth learning(React). Reasons for focusing on those points:
- Modern: To not fall behind with my experience and learn new technologies
- Stable: An app made from this template should still be easy to run and maintain 10 years from now
- Popular: Popular doesn't mean best in my opinion, but it greatly helps with development and diagnosing issues when you are googling/asking an LLM about a popular technology because there is a lot more information about it, people who encountered the same issues

## Installation steps
- cd api/WishlistApi
    - Get a steam API key: https://steamcommunity.com/dev/apikey
    - For dev use dotnet user-secrets to store the apikey with command: dotnet user-secrets set "SteamAPIKEY" "yourapikeyhere"
    - Note: Since switching to dev container I haven't verified if this actually still works this way
- Build and open as dev container in VS Code
- All of the following should be available with the dev container running:
    - Frontend at: http://localhost:5173  
        - Hot reload active
    - Wishlist API Swagger UI at: http://localhost:5186/swagger/index.html
        - no hot reload due to issue with build (System.ArgumentException: An item with the same key has already been added)
        - To restart after code changes: docker compose restart api 
    - RabbitMQ management at: http://localhost:15672
    - SteamTracker microservice
        - No hot reload (idem Wishlist API)
    - Adminer at: http://localhost:8085/
        - username postgres
        - password example

## CRUD app template
- React frontend
    - Tanstack Query, Router
    - TypeScript, ESLint, Tailwind CSS, Vite, React Hook Form with Zod for validations, Zustand
    - Vitest (unit), Playwright (E2E)
- Backend (C# services)
    - WishlistApi (ASP.NET Core Web API)
        - REST
        - Entity Framework Core code first
        - Swagger/OpenAPI
        - JWT-based authorization
        - SignalR hub (`/auctionHub`)
    - SteamTracker microservice
        - `SteamTracker.API` — REST API for price data
        - `SteamTracker.Worker` — background worker consuming RabbitMQ messages
        - Fetches prices from Steam (triggered by WishlistApi messages)
        - Entity Framework Core code first
    - Tests: xUnit, Moq, FluentAssertions
- PostgreSQL
- docker compose
    - For development we use a devcontainer
    - For production setup we additionally run:
        - Reverse proxy nginx
        - React frontend served from an internal nginx instance
- Topic: Enhanced Steam Wishlist (consuming steam api https://api.steampowered.com/ISteamApps/GetAppList/v2/)
    - In the app you can make a wishlist
    - If you have an account it will remember your wishlist

## Run with prod setup:
- docker compose -p templateapp_prod -f docker-compose.prod.yml up -d (run command twice if the backend crashed, TODO fix)
- Frontend at: http://localhost

## Adding new EF migration:
- cd api/WishlistApi
- dotnet ef migrations add UserDetails --project Infrastructure --startup-project WishlistApi

## Tests overview

[docs/TESTS.md](docs/TESTS.md)