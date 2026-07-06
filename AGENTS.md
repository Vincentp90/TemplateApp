## Environment
Linux + Bash 
You are running in dev container api specified in docker-compose.devcontainer.yml
Current year: 2026

## Important:
- Ask the user if you are not sure how to proceed.
- If you need to run 'git diff', always use with --no-pager
- Any time you finish a self-contained coding task, run tests
- Apply TDD principles during development:
    - Add interfaces first and unit tests that fail (red phase)
    - Then do the implementation and then run tests, they should pass now (green phase)

## Project Overviews
- **Frontend (React/TypeScript)**: [frontend/OVERVIEW.md](frontend/OVERVIEW.md)
- **Backend (.NET 10 APIs)**: [api/OVERVIEW.md](api/OVERVIEW.md)

## Running Tests
Always run `dotnet test` with a tail to limit output:

    dotnet test 2>&1 | tail -n 50

If the tail shows a truncated stack trace, cut-off error, or you need full context to diagnose a failure, re-run without the tail:

    dotnet test 2>&1 | tee test-output.log

Then inspect `test-output.log` in full (or grep it) rather than dumping it all into context at once.