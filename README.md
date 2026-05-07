# TemplateApp

This is a template for a typical business CRUD app. The goal is to have a template using technologies that are modern, stable, popular and technologies I had good success with in my career(C#) or that seem worth learning(React). Reasons for focusing on those points:
- Modern: To not fall behind with my experience and learn new technologies
- Stable: An app made from this template should still be easy to run and maintain 10 years from now
- Popular: Popular doesn't mean best in my opinion, but it greatly helps with development and diagnosing issues when you are googling/asking an LLM about a popular technology because there is a lot more information about it, people who encountered the same issues

Installation steps:
- Install Node.js v22
    - cd frontend
    - npm run dev
    - http://localhost:5173
- docker-compose up -d (postgres)
    - Adminer: http://localhost:8080/
    - username postgres
    - password example
- cd api/WishlistApi
    - Get a steam API key: https://steamcommunity.com/dev/apikey
    - For dev use dotnet user-secrets to store the apikey with command: dotnet user-secrets set "SteamAPIKEY" "yourapikeyhere"
    - dotnet ef database update --project DataAccess --startup-project WishlistApi
- Run backend API in VS (debug, any cpu, http) not https!
    - http://localhost:5186/swagger/index.html


CRUD app template
- React frontend
    - Tanstack Query, Router
    - TypeScript, ESLint, Tailwind CSS, Vite, React Hook Form with Zod for validations, Zustand
    - Vitest (to run tests: npm run test)
- ASP.NET Web Api backend
    - REST 
    - Entity Framework code first
    - Swagger
    - Tests: xUnit, Moq, FluentAssertions
- JWT-based authorization
- PostgreSQL
- docker compose
    - For development we only use docker to run postgres and adminer
    - For production setup everything is running as containers, with additionally:
        - Reverse proxy nginx
        - React frontend served from an internal nginx instance
- Topic: Enhanced Steam Wishlist (consuming steam api https://api.steampowered.com/ISteamApps/GetAppList/v2/)
    - In the app you can make wishlist
    - If you have an account it will remember your wishlist

Run for prod:
- docker compose -p templateapp_prod -f docker-compose.prod.yml up -d (run command twice if the backend crashed, TODO fix)
- http://localhost

Adding new EF migration:
- cd api/WishlistApi
- dotnet ef migrations add UserDetails --project DataAccess --startup-project WishlistApi
