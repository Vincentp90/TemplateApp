Environment: Windows 10 + PowerShell.

Hard rules:
- Only read/edit inside of api\WishlistApi
- When using a primary constructor, don't add private fields, just use the parameter directly

If you have to run unit tests, use command:
dotnet test api/WishlistApi/WishlistApi.sln 2>&1