Environment: Windows 10 + PowerShell.

Hard rules:
- Only read/edit inside of api\WishlistApi
- When using a primary constructor, don't add private fields, just use the parameter directly
- Don't try to delete files, put '// TODO DELETE THIS' at the top of the file

If you have to run unit tests, use command:
dotnet test api/WishlistApi/WishlistApi.sln 2>&1