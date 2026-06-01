### Environment
Windows 10 + Bash
Current year: 2026

### Important:
- Ask the user if you are not sure how to proceed.
- Only read/edit inside of api\WishlistApi
- Run unit tests with this command:   
    dotnet test api/WishlistApi/WishlistApi.sln 2>&1
- If you need to run 'git diff', always use with --no-pager

### Development guidelines
- When using a primary constructor, don't add private fields, just use the parameter directly
- This is .NET 10 project so use .NET 10 features if suitable