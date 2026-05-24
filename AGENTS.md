Environment: Windows 10 + Bash.

Hard rules:
- Only read/edit inside of api\WishlistApi
- When using a primary constructor, don't add private fields, just use the parameter directly
- Don't try to delete files, put '// TODO DELETE THIS' at the top of the file

If you have to run unit tests, use command:
dotnet test api/WishlistApi/WishlistApi.sln 2>&1

CRITICAL FOR FILE EDITS: When using the replace_in_file tool, you must match the exact indentation and whitespace of the target file. If a search-and-replace edit fails or says "Retrying...", do not repeat the same search block. Instead, immediately fall back to using the write_to_file tool to rewrite the file completely, or ask the user for clarification.

If you need to run 'git diff', always use with --no-pager