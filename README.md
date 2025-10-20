# TemplateApp

This is a template for a typical business CRUD app. The goal is to have a template using technologies that are modern, stable, popular and technologies I had good success with in my career. Reasons for focusing on those points:
- Modern: To not fall behind with my experience 
- Stable: An app made from this template should still be easy to run and maintain 10 years from now
- Popular: Popular doesn't mean best in my opinion, but it greatly helps with development and diagnosing issues when you are googling/asking an LLM about a popular technology because there is a lot more information about it

Installation steps:
- Install Node.js v22
    - New frontend
        - cd frontend
        - npm run dev
        - http://localhost:5173
    - Old frontend
        - cd wishlist
        - npm run dev
        - http://localhost:3000
- docker-compose up -d (postgres)
    - Adminer: http://localhost:8080/
    - username postgres
    - password example
- cd api/WishlistApi
    - dotnet ef database update --project DataAccess --startup-project WishlistApi
- Run backend API in VS (debug, any cpu, http) not https!
    - http://localhost:5186/swagger/index.html


CRUD app template
- React frontend, there are two frontends right now:
    - /wishlist: Next.js create-next-app with TypeScript, ESLint, Tailwind CSS, App Router, Webpack
    - /frontend: Tanstack Query, Router, React Hook Form, Zod, TypeScript, ESLint, Tailwind CSS, Vite
- .NET Web Api backend. Swagger, Moq. MSTest vs xUnit?
- Split up API into BFF and REST?
- PostgreSQL
- docker compose
    - Reverse proxy nginx to fix CORS https://aistudio.google.com/prompts/18eIrTUYifiLP6D_tK_pOUzVrj9Gpkcly
- Topic? Enhanced Steam Wishlist (integrate with steam api https://api.steampowered.com/ISteamApps/GetAppList/v2/)
    - In the app you can make wishlist
    - If you have an account it will remember your wishlist

Adding new migration:
    - dotnet ef migrations add UserTable --project DataAccess --startup-project WishlistApi

TODO:
- Authentication (OAuth?), make login screen, block api calls when not logged in
    - frontend 
    - OpenID Connect flow or an OAuth standard flow for creating access tokens
    - https://chatgpt.com/c/68f4f466-7fd4-8325-b7c6-e579c6727d8d

Later steps to add:
- Fix mix of casing in names in backend
- Make backend async
- Read more about how to use Microsoft.AspNetCore.Authentication.JwtBearer https://devblogs.microsoft.com/dotnet/jwt-validation-and-authorization-in-asp-net-core/
- run as container for future deployment (make dockerfile, run with https, reverse proxy (nginx, yarp?))
- How to use Zod (runtime validation of type?)
- TODOs in code

Things to try/add later:
- tailwind primary secondary color (once we have some buttons )
- unit tests (Moq. MSTest vs xUnit?)
- concurrency for CRUD operations
- gRCP
- GraphQL
- Redis cache (or redis fork) (maybe not much point when using tanstack query)
- steam openID integration
- Add jenkinsfile for CI/CD
- upgrade from .NET 9 to 10 (november)
- scan container (Anchore ?)
- scan code (SonarQube?)
- nicer loading screen (use with suspense)
- Reconsider current approach of using POCO db classes throughout the whole backend. Use DTOs?
- Async in method names or not?
- auth
    - Use refresh tokens for long sessions.
    - Add roles/claims for authorization.
- temporal