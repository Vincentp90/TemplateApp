# TemplateApp

This is a template for a typical business CRUD app. The goal is to have a template using technologies that are modern, stable, popular and technologies I had good success with in my career. Reasons for focusing on those points:
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

Run for prod:
    - docker compose -p templateapp_prod -f docker-compose.prod.yml up -d
    - docker compose -p templateapp_prod -f docker-compose.prod.yml up -d (if backend crashed TODO fix)
    - http://localhost

Adding new migration:
    - cd api/WishlistApi
    - dotnet ef migrations add InitialMigration --project DataAccess --startup-project WishlistApi


Later steps to add:
- unit tests (Moq. MSTest vs xUnit?)
- concurrency for CRUD operations

Things to try/add later:
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
- backend build warning CS8618
- TODOs in code
- Review XSS vulnerabilities
    - https://pragmaticwebsecurity.com/articles/oauthoidc/localstorage-xss.html
    - https://pragmaticwebsecurity.com/img/cheatsheets/reactxss.png
- WCAG
- stress test API
- dark mode
- host
    - where? Hetzner? Azure?
    - Configure https in nginx + app.UseForwardedHeaders(), app.UseHsts()  https://gemini.google.com/app/a3815289ab113d8c
    - prod JWT set with env var instead of commited to git
    - configure urls, still using localhost in prod config