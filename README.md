# TemplateApp

This is a template for a typical business CRUD app. The goal is to have a template using technologies that are modern, stable, popular and technologies I had good success with in my career(C#) or that seem worth learning(React). Reasons for focusing on those points:
- Modern: To not fall behind with my experience and learn new technologies
- Stable: An app made from this template should still be easy to run and maintain 10 years from now
- Popular: Popular doesn't mean best in my opinion, but it greatly helps with development and diagnosing issues when you are googling/asking an LLM about a popular technology because there is a lot more information about it

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
    - For dev use dotnet user-secrets to store the apikey
    - dotnet ef database update --project DataAccess --startup-project WishlistApi
- Run backend API in VS (debug, any cpu, http) not https!
    - http://localhost:5186/swagger/index.html


CRUD app template
- React frontend
    - Tanstack Query, Router
    - React Hook Form with Zod for validations
    - TypeScript, ESLint, Tailwind CSS, Vite
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
- docker compose -p templateapp_prod -f docker-compose.prod.yml up -d
- docker compose -p templateapp_prod -f docker-compose.prod.yml up -d (if backend crashed TODO fix)
- http://localhost

Adding new EF migration:
- cd api/WishlistApi
- dotnet ef migrations add UserDetails --project DataAccess --startup-project WishlistApi


Things I'm going to try/add later (my todo list):
- Add admin role and admin section where admins can change any user profile's details
    - Add a form with optimistic concurrency to have a better example for merging an update
    - Add auditing: keep track who changed profile details
- TODOs in code
- dark mode
- Add BFF?
    - Explains why BFF is more secure than bearer+refresh tokens: https://www.pingidentity.com/en/resources/blog/post/refresh-token-rotation-spa.html
    - If we don't add BFF:
        - Use refresh tokens for long sessions.
- more unit tests
- How to handle a very large application? Should i use more design patterns to ensure maintainability and extensibility?
    - Use DTOs
- pgvector
- Add jenkinsfile for CI/CD
    - scan container (Anchore ?)
    - scan code (SonarQube,Semgrep?)
- OAuth https://chatgpt.com/c/69039540-1818-832e-88ef-20605eba31c7
- steam openID integration
- Add general functionality:
    - Delete profile (GDPR right to forget)
- host
    - where? Hetzner? Azure?
    - Configure https in nginx + app.UseForwardedHeaders(), app.UseHsts()  https://gemini.google.com/app/a3815289ab113d8c
    - prod JWT/steam api key set with env var instead of commited to git
    - configure urls, still using localhost in prod config