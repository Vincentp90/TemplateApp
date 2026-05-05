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
- docker compose -p templateapp_prod -f docker-compose.prod.yml up -d
- docker compose -p templateapp_prod -f docker-compose.prod.yml up -d (if backend crashed TODO fix)
- http://localhost

Adding new EF migration:
- cd api/WishlistApi
- dotnet ef migrations add UserDetails --project DataAccess --startup-project WishlistApi


Things I'm going to try/add later (my todo list):
- Refactor ApiTests
- UserContext instead of repeated code in asp.net actions https://gemini.google.com/app/6aec8fb27b1a8e96
- Apply DDD https://chatgpt.com/c/69f73138-654c-83eb-9d6d-71c580be4b5e, https://chatgpt.com/c/69f758ba-6b6c-83eb-aa9f-2c476025d909
    - first add more ApiTests
- CQRS learn more and apply?
- Auctions unit tests
- Add general functionality:
    - Delete profile (GDPR right to forget)
- Add auditing: keep track who changed profile details https://chatgpt.com/c/699b338d-c0b0-8326-b440-035f78f30823
    -Overview screen
- Permissions instead of just roles https://chatgpt.com/c/696517e7-c228-8327-961a-a6aebeff24e1
- pgvector
- Add jenkinsfile for CI/CD
    - scan container (Anchore ?)
    - scan code (SonarQube,Semgrep?)
- OAuth https://chatgpt.com/c/69039540-1818-832e-88ef-20605eba31c7
    - Add BFF?
    - Explains why BFF is more secure than bearer+refresh tokens: https://www.pingidentity.com/en/resources/blog/post/refresh-token-rotation-spa.html
    - If we don't add BFF:
        - Use refresh tokens for long sessions (and reducing the attack window when a token is stolen)
    - steam openID integration
- TODOs in code
- Alternative frontends
    - Vue
    - Blazor
    - Angular
- ops dashboard grafana?
- elasticsearch instead of postgres fuzzy search
- host
    - where? Hetzner? Azure?
    - Configure https in nginx + app.UseForwardedHeaders(), app.UseHsts()  https://gemini.google.com/app/a3815289ab113d8c
    - prod JWT/steam api key set with env var instead of commited to git
    - configure urls, still using localhost in prod config