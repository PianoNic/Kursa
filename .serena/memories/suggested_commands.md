# Suggested Commands

## Build & Run
```bash
# Backend
dotnet build
dotnet run --project src/Kursa.API

# Frontend (ALWAYS use bun, NEVER npm)
cd src/Kursa.Frontend
bun install
bun run start
bun run build

# Full stack (Docker)
docker compose up -d
docker compose build
```

## Testing
```bash
dotnet test
dotnet test --filter "FullyQualifiedName~TestClassName"
cd src/Kursa.Frontend && bun run test
```

## Formatting & Linting
```bash
dotnet format
cd src/Kursa.Frontend && bun run lint
```

## Database Migrations
```bash
dotnet ef migrations add <Name> --project src/Kursa.Infrastructure --startup-project src/Kursa.API
dotnet ef database update --startup-project src/Kursa.API
```

## GitHub (MUST use gh CLI)
```bash
gh issue create --title "Title" --body "Description" --label "feature"
gh issue list
git checkout -b feature/<issue-number>_Name
git push -u origin feature/<issue-number>_Name
gh pr create --title "Title" --body "Closes #<number>" --label "feature"
gh pr diff <number>
gh pr review <number> --approve --body "Reviewed: summary"
gh pr merge <number> --squash --delete-branch
```

## System Utils (Windows)
```bash
# Git Bash / Unix-style commands work
ls, cd, grep, find, cat, mkdir, rm
git status, git log, git diff
```
