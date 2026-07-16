---
name: review-branch
description: Reviews git branches by analyzing diffs, commit history, and code quality. Use when asked to review a branch, compare branches, or assess pull request changes.
---

# Review Branch

## Overview

This skill provides a systematic workflow for reviewing git branches. It helps analyze diffs, understand commit history, and assess code changes against project standards.

## Setup

No additional setup required. Uses standard git tooling available in the environment.

## Usage

### Review a Single Branch

```
1. Identify the current branch (e.g., `feature/my-feature`, `main`)
2. Compare against base branch `main`
3. Run `git diff <base>...<target>` to see the full diff
4. Run `git log <base>..<target>` to review commit history
```

### Review Workflow

1. **Scope Assessment** — Determine the size and nature of changes (files touched, lines changed, new vs modified vs deleted).
2. **Diff Review** — Read through the diff line by line for logic errors, edge cases, and adherence to conventions.
3. **Code Quality** — Check for:
   - Overly defensive programming, examples:
      - Null checking variables that should never be null, unless there is a bug
      - Checking if the user is authorized when we are already in a scope that is only reachable when authorized
   - Consistent naming and style
   - Proper error handling
   - No hardcoded values or secrets
   - Test coverage for new/changed logic
4. **Summary** — Provide a concise review summary with:
   - Changes overview (what and why)
   - Issues found (with line references)
   - Suggestions for improvement
   - Approval recommendation
5. Keep the summary limited to roughly 200 lines. Prune lower priority commentary when 200 lines is not sufficient.

## Reference

For project-specific conventions and standards, see:
- [Frontend Overview](../../frontend/OVERVIEW.md)
- [Backend Overview](../../api/OVERVIEW.md)
