Environment: Windows 10 + Bash.

Hard rules:
- Only read/edit inside of api\WishlistApi
- When using a primary constructor, don't add private fields, just use the parameter directly
- When you want to delete files, instead of deleting, put '// TODO DELETE THIS' at the top of the file

If you have to run unit tests, use command:
dotnet test api/WishlistApi/WishlistApi.sln 2>&1

If you need to run 'git diff', always use with --no-pager

Ask the user if you are not sure how to proceed.