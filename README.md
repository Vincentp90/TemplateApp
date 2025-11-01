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
- dotnet ef migrations add InitialMigration --project DataAccess --startup-project WishlistApi


Things I'm going to try/add later:
- Add a lessons learned page
    - Review XSS vulnerabilities
        - https://pragmaticwebsecurity.com/articles/oauthoidc/localstorage-xss.html
        - https://pragmaticwebsecurity.com/img/cheatsheets/reactxss.png
- OAuth https://chatgpt.com/c/69039540-1818-832e-88ef-20605eba31c7
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
    - Add roles/claims for authorization (add admin role, need an admin screen first)
- temporal
- backend build warning CS8618
- UI test
- TODOs in code
- WCAG
- concurrency for CRUD operations: add a functionality that could have concurrent edits on same data, add optimistic concurrency (Timestamp attribute EF) and implement merge with tanstack query on receiving a 409 from the backend https://chatgpt.com/c/6903b2f9-d6f4-8330-8e15-9b7524a6f121
- stress test API
- dark mode
- Split up API into BFF and REST?
- more unit tests
- How to handle a very large application? Should i use more design patterns to ensure maintainability and extensibility?
- host
    - where? Hetzner? Azure?
    - Configure https in nginx + app.UseForwardedHeaders(), app.UseHsts()  https://gemini.google.com/app/a3815289ab113d8c
    - prod JWT set with env var instead of commited to git
    - configure urls, still using localhost in prod config