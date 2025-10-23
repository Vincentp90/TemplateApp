# TemplateApp

This is a template for a typical business CRUD app. The goal is to have a template using technologies that are modern, stable, popular and technologies I had good success with in my career. Reasons for focusing on those points:
- Modern: To not fall behind with my experience 
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
    - http://localhost:80

Adding new migration:
    - dotnet ef migrations add InitialMigration --project DataAccess --startup-project WishlistApi

TODO:
    - 502 bad gateway
    - https

Later steps to add:
- run as container for future deployment (make dockerfile, run with https, reverse proxy (nginx, yarp?))
- How to use Zod (runtime validation of type?)
- Delete buttons are very ugly
- Add Logout and do localStorage.removeItem('token'); queryClient.clear();
- TODOs in code
- review dockerfiles, probably a lot of unnecessary stuff in there
- prod JWT set with env var instead of commited to git

Things to try/add later:
- read more: OpenID Connect flow or an OAuth standard flow for creating access tokens https://devblogs.microsoft.com/dotnet/jwt-validation-and-authorization-in-asp-net-core/
- tailwind primary secondary color (once we have some buttons )
- unit tests (Moq. MSTest vs xUnit?)
- concurrency for CRUD operations
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
- backend build warning CS8618
- Review XSS vulnerabilities
    - https://pragmaticwebsecurity.com/articles/oauthoidc/localstorage-xss.html
    - https://pragmaticwebsecurity.com/img/cheatsheets/reactxss.png
- WCAG
- stress test API