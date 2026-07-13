Agent: ignore this

Make an overview of the build warnings in wishlist and steamtracker

root@924186ffb5ae:/workspace# cd /workspace/api/WishlistApi && dotnet build --no-incremental 2>&1
Restore complete (1.2s)
  Domain net10.0 succeeded (0.7s) → Domain/bin/Debug/net10.0/Domain.dll
  Application net10.0 succeeded (0.7s) → Application/bin/Debug/net10.0/Application.dll
  Infrastructure net10.0 succeeded with 1 warning(s) (0.8s) → Infrastructure/bin/Debug/net10.0/Infrastructure.dll
    /workspace/api/WishlistApi/Infrastructure/Persistence/Auctions/Auction.cs(28,39): warning CS8618: Non-nullable property 'AppListing' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
  WishlistApi net10.0 succeeded with 4 warning(s) (2.2s) → WishlistApi/bin/Debug/net10.0/WishlistApi.dll
    /workspace/api/WishlistApi/WishlistApi/Controllers/WishlistController.cs(79,55): warning CS8602: Dereference of a possibly null reference.
    /workspace/api/WishlistApi/WishlistApi/Controllers/WishlistController.cs(80,71): warning CS8602: Dereference of a possibly null reference.
    /workspace/api/WishlistApi/WishlistApi/Controllers/WishlistController.cs(81,71): warning CS8602: Dereference of a possibly null reference.
    /workspace/api/WishlistApi/WishlistApi/Controllers/WishlistController.cs(82,48): warning CS8602: Dereference of a possibly null reference.

Things I'm going to try/add/learn more about later :
- warnings in wishlist and steamtracker
- warnings during api container startup
- make diagram of architecture
- get prices by api instead of shared db https://claude.ai/chat/0b8c3ba5-e549-458c-a07c-32872b326100
- gRPC for api-steamtracker communication
- finish alerts
- Auction: add end date to entity, remove static mutable Domain.Auction.Duration
- Auction OCC: remove client side part since we also have the admin form which is more suitable for RowVersion round-trip, auction use case fits better with only server side OCC
- DDD add domain event example https://gemini.google.com/app/f3480cdb192b1625
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
- logging in controllers
- host
    - where? Hetzner? Azure?
    - Configure https in nginx + app.UseForwardedHeaders(), app.UseHsts()  https://gemini.google.com/app/a3815289ab113d8c
    - prod JWT/steam api key set with env var instead of commited to git
    - configure urls, still using localhost in prod config