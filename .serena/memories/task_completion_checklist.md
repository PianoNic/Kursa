# Task Completion Checklist

When a task is completed, ALWAYS verify the following:

## Build
- [ ] `dotnet build` passes without errors or warnings
- [ ] `bun run build` passes (if frontend changes)

## Tests
- [ ] `dotnet test` passes
- [ ] Frontend tests pass (if applicable)

## Self-Debug
- [ ] Read error output yourself, trace root cause, fix before moving on
- [ ] Run the app and verify it works (not just compiles)
- [ ] For frontend: use Playwright/browser tools to visually verify

## Docs
- [ ] If unsure about library API: check Context7 MCP first
- [ ] If discovered a new issue: create GitHub issue before fixing

## Git Workflow
- [ ] Work was done on a properly named branch (feature/bug/refactor/<issue>_Name)
- [ ] PR created with correct label and "Closes #<number>"
- [ ] Self-reviewed the diff
- [ ] Approved with summary
- [ ] Squash-merged and branch deleted

## Code Quality
- [ ] No secrets or credentials in code
- [ ] No `any` types in TypeScript
- [ ] Proper error handling for external calls
- [ ] Follows existing patterns in the codebase
