# TemplateApp

This is a template for a typical business CRUD app. The goal is to have a template using technologies that are modern, stable, popular and technologies I had good success with in my career. Reasons for focusing on those points:
- Modern: To not fall behind with my experience 
- Stable: An app made from this template should still be easy to run and maintain 10 years from now
- Popular: Popular doesn't mean best in my opinion, but it greatly helps with development and diagnosing issues when you are googling/asking an LLM about a popular technology

Installation steps:
- Install Node.js v22
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
- React frontend.
    - Next.js create-next-app with TypeScript, ESLint, Tailwind CSS, App Router, Webpack
    - Prettier? ts formating tool
- .NET Web Api backend. Swagger, Moq. MSTest vs xUnit?
- Split up API into BFF and REST?
- PostgreSQL
- docker compose
    - Reverse proxy nginx to fix CORS https://aistudio.google.com/prompts/18eIrTUYifiLP6D_tK_pOUzVrj9Gpkcly
- Topic? Enhanced Steam Wishlist (integrate with steam api https://api.steampowered.com/ISteamApps/GetAppList/v2/)
    - In the app you can make wishlist
    - If you have an account it will remember your wishlist

TODO
- Add game to wishlist on search page
- Page to view wishlist

Adding new migration:
    - dotnet ef migrations add MigrationName --project DataAccess --startup-project WishlistApi

Next steps to add:
- tailwind primary secondary color
- Authentication (OAuth?)

Things to try/add later:
- concurrency for CRUD operations
- gRCP
- GraphQL
- Redis cache (or redis fork)
- steam openID integration
- Add jenkinsfile for CI/CD
- Separate frontend made with Vite + React Router (+ tailwind + primereact)
- upgrade from .NET 9 to 10 (november)
- scan container (Anchore ?)
- scan code (SonarQube?)