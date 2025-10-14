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

TODO next
- Page to view wishlist, add more details (time added, etc)
- Learn more how to clean up migration cs files (can wrong migration with wishlist appid string be removed)



Adding new migration:
    - dotnet ef migrations add MigrationName --project DataAccess --startup-project WishlistApi

Later steps to add:
- tailwind primary secondary color
- run as container for future deployment (make dockerfile, run with https)
- Authentication (OAuth?)

Things to try/add later:
- concurrency for CRUD operations
- gRCP
- GraphQL
- Redis cache (or redis fork)
- steam openID integration
- Add jenkinsfile for CI/CD
- upgrade from .NET 9 to 10 (november)
- scan container (Anchore ?)
- scan code (SonarQube?)