Scaffold the entire Kursa project from scratch. This is the initial project setup command.

Follow these steps in order:

## 1. .NET Solution & Projects
```bash
dotnet new sln -n Kursa
dotnet new webapi -n Kursa.API -o src/Kursa.API --use-controllers false
dotnet new classlib -n Kursa.Domain -o src/Kursa.Domain
dotnet new classlib -n Kursa.Application -o src/Kursa.Application
dotnet new classlib -n Kursa.Infrastructure -o src/Kursa.Infrastructure
dotnet new xunit -n Kursa.API.Tests -o tests/Kursa.API.Tests
dotnet new xunit -n Kursa.Application.Tests -o tests/Kursa.Application.Tests
```

Add all projects to the solution and set up project references:
- Api → Application → Domain
- Api → Infrastructure → Application → Domain
- Tests → respective projects

## 2. NuGet Packages
**Kursa.API**: MediatR, FluentValidation.AspNetCore, Hangfire, Serilog, Swashbuckle
**Kursa.Application**: MediatR, FluentValidation
**Kursa.Infrastructure**: Npgsql.EntityFrameworkCore.PostgreSQL, Microsoft.EntityFrameworkCore.Tools, Qdrant.Client, StackExchange.Redis, Minio
**Kursa.Domain**: (minimal, maybe just abstractions)

## 3. Angular Frontend
```bash
cd src
ng new Kursa.Frontend --style css --routing --standalone --skip-tests false
cd Kursa.Frontend
bun install -D tailwindcss @tailwindcss/postcss postcss

# Install spartan.ng CLI
bun add -D @spartan-ng/cli

# Initialize spartan
ng g @spartan-ng/cli:init

# Set up theme (Tailwind integration)
ng g @spartan-ng/cli:ui-theme

# Add core components (select: button, input, card, dialog, sidebar, sheet, separator, label, avatar, dropdown-menu, tooltip)
ng g @spartan-ng/cli:ui
```

Set up:
- Tailwind config with dark mode `class` strategy
- spartan.ng uses brain/helm pattern: brain (brn-*) = behavior, helm (hlm-*) = styled components
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
cd src/Kursa.Frontend && bun run build
docker compose config
```

Everything must compile and Docker config must be valid before finishing.
