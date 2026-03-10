Scaffold the entire StudyApp project from scratch. This is the initial project setup command.

Follow these steps in order:

## 1. .NET Solution & Projects
```bash
dotnet new sln -n StudyApp
dotnet new webapi -n StudyApp.Api -o src/StudyApp.Api --use-controllers false
dotnet new classlib -n StudyApp.Domain -o src/StudyApp.Domain
dotnet new classlib -n StudyApp.Application -o src/StudyApp.Application
dotnet new classlib -n StudyApp.Infrastructure -o src/StudyApp.Infrastructure
dotnet new xunit -n StudyApp.Api.Tests -o tests/StudyApp.Api.Tests
dotnet new xunit -n StudyApp.Application.Tests -o tests/StudyApp.Application.Tests
```

Add all projects to the solution and set up project references:
- Api → Application → Domain
- Api → Infrastructure → Application → Domain
- Tests → respective projects

## 2. NuGet Packages
**StudyApp.Api**: MediatR, FluentValidation.AspNetCore, Hangfire, Serilog, Swashbuckle
**StudyApp.Application**: MediatR, FluentValidation
**StudyApp.Infrastructure**: Npgsql.EntityFrameworkCore.PostgreSQL, Microsoft.EntityFrameworkCore.Tools, Qdrant.Client, StackExchange.Redis, Minio
**StudyApp.Domain**: (minimal, maybe just abstractions)

## 3. Angular Frontend
```bash
cd src
ng new StudyApp.Web --style css --routing --standalone --skip-tests false
cd StudyApp.Web
npm install -D tailwindcss @tailwindcss/postcss postcss
# Install spartan.ng CLI and core packages
npx nx g @spartan-ng/cli:init
```

Set up:
- Tailwind config with dark mode `class` strategy
- spartan.ng base components (button, input, card, dialog, sidebar, etc.)
- App shell with sidebar + topbar + main content + right panel layout
- Dark mode as default
- Routing structure for features (dashboard, courses, chat, study-tools, settings)

## 4. Docker Setup
Create:
- `docker/Dockerfile.api` (multi-stage .NET build)
- `docker/Dockerfile.web` (multi-stage Angular build with nginx)
- `docker-compose.yml` with services: api, web, postgres, redis, qdrant, minio
- `.env.example` with all required environment variables

## 5. Configuration Files
- `.editorconfig` (C# and TypeScript conventions)
- `.gitignore` (comprehensive for .NET + Angular + Docker)
- `README.md` (basic project overview)

## 6. Verify
```bash
dotnet build
cd src/StudyApp.Web && ng build
docker compose config
```

Everything must compile and Docker config must be valid before finishing.
