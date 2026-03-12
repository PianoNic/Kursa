# CLAUDE.md — Kursa

> **Project Name**: Kursa
> **Owner**: Niclas (PianoNic) — pianonic.ch
> **Type**: AI-powered LMS + Study Companion with Moodle integration

---

## Project Overview

Kursa is a full-fledged LMS that uses Moodle, OneNote, and SharePoint as **lazy-loaded data sources** (fetched on demand, not bulk-synced), combined with AI-powered study tools and a lesson recording pipeline. Think "Moodle but actually good, with AI baked in."

### Architecture Philosophy
- **Own LMS** with Moodle as a backend data source via MoodlewareAPI
- **Lazy loading**: content is only stored/indexed locally when the user interacts with it (opens, pins, stars)
- **AI features** (RAG, summaries, quizzes) only work on content the user has explicitly pulled in
- **"Spotify model"**: stream/browse anything, but only saved items are truly yours
- Lesson recordings from companion apps (Android APK + Windows EXE) are always first-class citizens

### System Components
1. **Web App** — Angular 21 + spartan.ng + Tailwind (this repo's frontend)
2. **Backend API** — .NET Core (this repo's backend)
3. **Moodle Bridge** — MoodlewareAPI (separate repo, FastAPI/Python) at github.com/MoodleNG/MoodlewareAPI
4. **Recording Apps** — Android (Kotlin) + Windows (.NET WPF/WinUI) — separate repos
5. **Audio Processing** — Python microservice (Whisper + pyannote)

---

## Tech Stack

| Layer | Technology | Notes |
|---|---|---|
| **Frontend** | Angular 21 | Standalone components, signals, new control flow |
| **UI Library** | spartan.ng | shadcn/ui alternative for Angular — use brain/helm pattern |
| **Styling** | Tailwind CSS 4 | Dark mode default, utility-first |
| **Backend** | .NET 10 / ASP.NET Core | Minimal APIs or Controllers, depends on endpoint complexity |
| **ORM** | Entity Framework Core | Code-first migrations, PostgreSQL provider |
| **Architecture** | CQRS + Mediator | Commands/Queries separated via source-generated Mediator, pipeline behaviors for validation/logging |
| **Database** | PostgreSQL | Primary relational store |
| **Vector DB** | Qdrant | Semantic search, RAG embeddings |
| **Cache** | Redis | Moodle API response caching, session data |
| **Object Storage** | MinIO | Audio recordings, PDFs, cached content |
| **Background Jobs** | Hangfire | Moodle polling, embedding generation, summary pipelines |
| **Auth** | OIDC (Pocket ID, PKCE) + Moodle credentials | Pocket ID for user auth, Moodle username/password via MoodlewareAPI /auth |
| **LLM** | Microsoft Semantic Kernel | Agentic tool-calling, KursaAgentPlugin, config-driven provider selection |
| **Speech-to-Text** | Whisper | API or self-hosted via Python microservice |
| **Diarization** | pyannote | Speaker separation in Python microservice |
| **Search** | Qdrant (semantic) + PostgreSQL FTS | Qdrant for AI-indexed content, PG for full-text fallback |
| **Containerization** | Docker + docker-compose | Everything runs containerized |

---

## Project Structure

```
Kursa/
├── .claude/                    # Claude Code configuration
│   ├── settings.json
│   └── commands/               # Custom slash commands
├── docs/                       # Project documentation
│   ├── features.md             # Full feature brainstorm
│   ├── architecture.md         # Architecture decisions
│   └── api-contracts.md        # API endpoint contracts
├── src/
│   ├── Kursa.API/              # ASP.NET Core Web API
│   │   ├── Controllers/        # API controllers
│   │   ├── Middleware/         # Auth, error handling, logging
│   │   └── Program.cs
│   ├── Kursa.Application/      # Application layer (CQRS handlers, DTOs, interfaces)
│   │   ├── Common/             # Behaviors (validation, logging), interfaces, models
│   │   └── Features/           # Feature folders (Commands, Queries, Handlers, DTOs)
│   ├── Kursa.Domain/           # Domain layer (entities, value objects, enums)
│   ├── Kursa.Infrastructure/   # Infrastructure layer (EF, external services, repositories)
│   ├── Kursa.Frontend/         # Angular 21 frontend
│   │   ├── src/
│   │   │   ├── app/
│   │   │   │   ├── core/       # Guards, interceptors, services, auth
│   │   │   │   ├── shared/     # Shared components, pipes, directives
│   │   │   │   ├── features/   # Feature modules (dashboard, courses, chat, study-tools, etc.)
│   │   │   │   └── layout/     # Shell, sidebar, topbar, AI panel
│   │   │   ├── lib/ui/         # spartan.ng hlm-* components
│   │   │   ├── environments/
│   │   │   └── styles/
│   │   ├── angular.json
│   │   └── package.json
│   └── Kursa.Tests/            # Backend tests (xUnit)
├── compose.yml                 # Full stack: API, DB, Redis, Qdrant, MinIO
├── CLAUDE.md                   # This file
├── README.md
├── .gitignore
├── .editorconfig
└── Kursa.slnx
```

---

## Coding Standards & Conventions

### General
- **Language**: English for all code, comments, commit messages, and documentation
- **No warnings**: Code must compile without warnings
- **Explicit over implicit**: Prefer clarity over cleverness
- **Error handling**: Use Result pattern or exceptions with proper middleware — never swallow exceptions silently

### C# / .NET Backend
- **Target**: .NET 10, C# 14
- **Nullable reference types**: Enabled project-wide (`<Nullable>enable</Nullable>`)
- **File-scoped namespaces**: Always (`namespace X;`)
- **Primary constructors**: Use for DI in services/handlers
- **Record types**: Use for DTOs, Commands, Queries, Events
- **Naming**: PascalCase for public members, `_camelCase` for private fields, `I` prefix for interfaces
- **Async**: All I/O operations must be async. Suffix with `Async`. Accept `CancellationToken` everywhere.
- **CQRS**: Commands return `Result<T>` or void, Queries return data. Never mix read/write.
- **Feature folders**: Group by feature, not by type. Each feature folder contains its Command, Query, Handler, Validator, DTO.
- **Dependency injection**: Constructor injection only. Register via extension methods per feature/layer.
- **EF Core**: No lazy loading. Use `.Include()` explicitly. Migrations in Infrastructure project.
- **No magic strings**: Use constants or enums.

### Angular / Frontend
- **Package manager**: ALWAYS use `bun` — NEVER use `npm` or `yarn`. Use `bun install`, `bun run build`, `bun add`, `bunx`, etc.
- **Angular version**: 21+ with standalone components (no NgModules)
- **Signals**: Prefer Angular signals over RxJS where possible for state
- **New control flow**: Use `@if`, `@for`, `@switch` — never `*ngIf`, `*ngFor`
- **spartan.ng**: Follow brain/helm pattern — `brn-*` directives for behavior, `hlm-*` components for styling
- **Tailwind**: Utility-first, no custom CSS unless absolutely necessary. Dark mode via `class` strategy.
- **File naming**: `feature-name.component.ts`, `feature-name.service.ts`, kebab-case
- **Barrel exports**: Use `index.ts` in feature folders
- **HTTP**: Use Angular's `HttpClient` with typed responses. Interceptors for auth tokens and error handling.
- **State management**: Angular signals + services for local state. Consider NgRx signals store only if complexity demands it.
- **Lazy loading**: All feature routes lazy-loaded
- **i18n**: Prepare for it but don't implement yet — English only for now

### Commit Messages
- Format: `type(scope): description`
- Types: `feat`, `fix`, `refactor`, `docs`, `style`, `test`, `chore`, `ci`, `build`
- Examples: `feat(chat): add RAG context filtering`, `fix(moodle): handle 401 token refresh`
- Keep commits atomic and focused

### Docker
- Single Dockerfile (`src/Kursa.API/Dockerfile`) — multi-stage build serving both API and Angular SPA
- `compose.yml` for full local dev stack
- .NET serves the Angular SPA via `SpaServices.Extensions` middleware (no nginx)
- `.env.example` for all environment variables
- Health checks on all services

### API Client Generation
- OpenAPI client generated via `@openapitools/openapi-generator-cli` with `typescript-angular` generator
- Run `bun run apigen` in the frontend directory to regenerate from Swagger
- Generated client lives in `src/Kursa.Frontend/src/app/api/`
- Configuration provider wired in `app.config.ts` with `apiBaseUrl` from environment

---

## Key Design Decisions

### Moodle Integration (Lazy Loading)
- Browse courses/content → fetch from MoodlewareAPI on demand, display in our UI, don't store
- Open a course/module → fetch and render, cache locally for speed
- Pin/star content → now it gets stored in PostgreSQL + indexed in Qdrant for AI features
- AI features → only work on pinned/indexed content
- Lightweight polling for change detection (metadata only)

### LLM / AI (Microsoft Semantic Kernel)
- Microsoft Semantic Kernel for all LLM interactions (replaced custom ILlmProvider)
- `KursaAgentPlugin` provides tool-calling capabilities (vector search, content retrieval)
- Agentic tool-calling loop for RAG chat (not hardcoded pipeline)
- Configuration-driven provider selection via `appsettings.json` (OpenAI, Ollama, etc.)

### Content Pipeline
1. Content arrives (from Moodle, OneNote, SharePoint, or recording upload)
2. Text extraction (PDF parsing, HTML stripping, OCR if needed)
3. Chunking (semantic chunking with overlap)
4. Embedding generation (via configured LLM provider)
5. Storage in Qdrant with metadata (source, course, module, type, date)
6. Summary generation (optional, for pinned content)

### Audio Recording Pipeline
1. Recording uploaded from companion app → stored in MinIO
2. Hangfire job triggers: Whisper transcription → pyannote diarization
3. Transcript stored in PostgreSQL, linked to course/lesson
4. Content pipeline runs on transcript (embedding, summarization, concept extraction)
5. Processed results available in web app

---

## Development Workflow

### Running Locally
```bash
# Start infrastructure
docker compose up -d postgres redis qdrant minio

# Run backend
cd src/Kursa.API
dotnet run

# Run frontend
cd src/Kursa.Frontend
bun install
bun run start
```

### Running Full Stack (Docker)
```bash
docker compose up -d
```

### Testing
```bash
# Backend tests
dotnet test

# Frontend tests
cd src/Kursa.Frontend
bun run test
```

### Database Migrations
```bash
dotnet ef migrations add <MigrationName> --project src/Kursa.Infrastructure --startup-project src/Kursa.API
dotnet ef database update --startup-project src/Kursa.API
```

---

## Important Notes for the Agent

### DO
- Always run `dotnet build` after making C# changes to verify compilation
- Always run `ng build` or `ng serve` after Angular changes to verify
- Write tests for new features — at minimum, unit tests for handlers and services
- Use the existing patterns when adding new features (look at existing feature folders)
- Keep the feature brainstorm (`docs/features.md`) as the source of truth for what to build
- Docker-compose must always work — test with `docker compose build` after Dockerfile changes
- Use `dotnet format` and `prettier` to format code

### Self-Debugging (MANDATORY)
**You MUST debug your own work autonomously. Never leave broken code or ask the user to debug for you.**

- After every code change, **build and verify** — if it fails, read the error, fix it, and retry
- If a build/test fails, **analyze the error output yourself**, trace the root cause, and fix it before moving on
- Run the application and **verify it actually works** — don't just assume compilation = success
- If you hit an unexpected error, investigate logs, stack traces, and related code — don't give up or ask the user
- Loop: code → build → test → fix → repeat until everything passes

### Playwright / Puppeteer for Frontend (MANDATORY)
**When doing frontend/web development, you MUST use Playwright (MCP Puppeteer/browser tools) to visually verify your work.**

- After building UI components, **open the browser and check the result yourself**
- Navigate to the page, take screenshots, verify layout, interactions, and responsiveness
- If something looks wrong, fix it and re-check — don't rely on build success alone
- Use browser tools to test: clicking, form filling, navigation, dark mode, responsive sizes
- This applies to all frontend work: components, pages, layouts, styling, accessibility

### Context7 for Documentation (MANDATORY)
**When you encounter documentation questions or need up-to-date library/framework docs, you MUST use Context7 (MCP).**

- Before implementing anything with a library you're unsure about, **fetch the latest docs via Context7 first**
- Use Context7 for: Angular, spartan.ng, Tailwind, EF Core, Mediator, Semantic Kernel, Qdrant client, or any dependency
- Don't rely on training data for API signatures, configuration patterns, or breaking changes — **always check docs**
- If a build fails due to an API mismatch, check Context7 for the correct usage before guessing fixes

### Serena MCP for Code Intelligence (MANDATORY)
**You MUST use Serena's symbolic tools for navigating and editing the codebase. Serena provides language-server-powered code intelligence for both C# and TypeScript.**

- Use `get_symbols_overview` to understand a file before editing — don't just read the raw file
- Use `find_symbol` to locate classes, methods, interfaces by name across the codebase
- Use `find_referencing_symbols` to find all usages before renaming or modifying a symbol
- Use `replace_symbol_body` for precise edits to methods/classes — prefer this over raw text replacement
- Use `insert_after_symbol` / `insert_before_symbol` to add new code at the right location
- Read Serena's project memories (`read_memory`) at the start of each session for context
- After activating the project, always `check_onboarding_performed` to ensure memories are loaded
- Serena understands both C# (.NET backend) and TypeScript (Angular frontend)

### Discovered Issues → GitHub Issue First (MANDATORY)
**If you discover a bug, tech debt, missing feature, or anything that needs fixing — ALWAYS create a GitHub issue BEFORE touching code.**

- Found a bug while working on something else? `gh issue create --label "bug"` first, then decide whether to fix now or later
- Noticed missing validation, broken config, outdated dependency? Issue first.
- Never silently fix things — every fix must be traceable to an issue
- If it's unrelated to your current task, create the issue and continue your current work — don't context-switch
- If it blocks your current task, create the issue, then branch off and fix it following the full workflow (branch → PR → review → merge)

### DON'T
- Don't bulk-sync Moodle data — always lazy-load on demand
- Don't hardcode LLM provider — always use the abstraction
- Don't create Angular modules — use standalone components
- Don't use `any` type in TypeScript — always type properly
- Don't commit secrets, API keys, or connection strings — use `.env` and user-secrets
- Don't skip error handling — every external call (Moodle, LLM, Qdrant) can fail
- Don't use `var` in C# for non-obvious types — prefer explicit types for readability at boundaries
- Don't leave broken code — if it doesn't build or pass tests, fix it before moving on
- Don't guess library APIs — use Context7 to verify

### Useful Commands
```bash
# Generate Angular component
cd src/Kursa.Frontend && ng g c features/dashboard/components/course-card

# Generate Angular service
cd src/Kursa.Frontend && ng g s core/services/moodle-proxy

# Add NuGet package
dotnet add src/Kursa.API package <PackageName>

# Add EF migration
dotnet ef migrations add <Name> --project src/Kursa.Infrastructure --startup-project src/Kursa.API

# Run specific test
dotnet test --filter "FullyQualifiedName~TestClassName"

# Check Docker compose
docker compose config
```

---

## GitHub (MANDATORY — always use `gh` CLI)

**You MUST always use the GitHub CLI (`gh`) for ALL GitHub interactions. Never use the GitHub web UI or raw API calls when `gh` can do it.**

### Repository
- **Remote**: `PianoNic/Kursa` on GitHub
- **Default branch**: `main`

### ⚠️ CRITICAL: Git Workflow (NEVER violate this)

**NEVER commit or push directly to `main`. ALWAYS follow this workflow:**

1. **Create an issue first** — every piece of work starts with a GitHub issue
   ```bash
   gh issue create --title "Add user authentication" --body "Description..." --label "feature"
   ```

2. **Create a branch from the issue** — branch naming is strict:
   ```
   feature/<issue-number>_FeatureName     # New features
   bug/<issue-number>_BugName             # Bug fixes
   refactor/<issue-number>_RefactorName   # Refactoring
   enhancement/<issue-number>_EnhanceName # Enhancements
   ```
   Examples:
   ```bash
   git checkout -b feature/42_UserAuthentication
   git checkout -b bug/57_FixTokenRefresh
   git checkout -b refactor/63_CleanupMoodleProxy
   ```

3. **Do all work on the branch** — commit as needed, push to remote
   ```bash
   git push -u origin feature/42_UserAuthentication
   ```

4. **Create a PR back to `main`** — with proper labels matching the branch type
   ```bash
   gh pr create --title "feat(auth): add user authentication" --body "Closes #42" --label "feature"
   ```

5. **Self-review the PR** — check the diff, verify quality, and approve
   ```bash
   # Review your own diff
   gh pr diff <number>

   # Add a review comment summarizing what was done and why it's correct
   gh pr review <number> --approve --body "Reviewed: <summary of what was verified>"
   ```

6. **Merge the PR** — squash merge to keep history clean
   ```bash
   gh pr merge <number> --squash --delete-branch
   ```

**This is non-negotiable. Every single change — no matter how small — goes through issue → branch → PR → self-review → merge.**

### PR Self-Review Checklist (MANDATORY before approving)
Before approving your own PR, verify ALL of the following:
- [ ] Code builds without errors or warnings (`dotnet build` / `bun run build`)
- [ ] Tests pass (`dotnet test`)
- [ ] No secrets, API keys, or credentials in the diff
- [ ] PR has the correct label(s) matching the branch type
- [ ] PR body references the issue with `Closes #<number>`
- [ ] Changes are focused — no unrelated modifications
- [ ] Frontend changes were visually verified with Playwright/browser tools
- [ ] Library usage was verified against docs via Context7 (if applicable)

### Labels (configured on repo)
| Label | Color | Purpose |
|---|---|---|
| `bug` | #d73a4a | Something isn't working |
| `feature` | #0052cc | New capability or function |
| `enhancement` | #a2eeef | New feature or request |
| `refactor` | #fbca04 | Code structure/performance improvements |
| `duplicate` | #cfd3d7 | Already exists |

### CI/CD Pipelines
- **Build & Test** (`build.yml`): Runs on push/PR to `main`. Builds .NET backend + Angular frontend.
- **Release Drafter** (`release-drafter.yml`): Auto-drafts release notes from merged PRs based on labels.
- **Docker Publish** (`docker-publish-release.yaml`): Builds and pushes multi-arch Docker images on release.
- **Version Bump** (`version-bump.yml`): Manual workflow to bump version in all `.csproj` files.
- **Restrict Dev Issues** (`restrict-dev-issues.yml`): Auto-closes `[DEV]` issues from non-maintainers.

### Common `gh` Commands
```bash
# Issues
gh issue create --title "Title" --body "Description" --label "feature"
gh issue list
gh issue view <number>
gh issue close <number>

# Pull Requests
gh pr create --title "Title" --body "Description" --label "feature"
gh pr list
gh pr view <number>
gh pr merge <number>
gh pr checks <number>

# Releases
gh release list
gh release create v1.0.0 --title "v1.0.0" --notes "Release notes"
gh release view <tag>

# Labels
gh label list
gh label create <name> --description "desc" --color <hex>

# Repo info
gh repo view
gh api repos/PianoNic/Kursa/...

# Workflow runs
gh run list
gh run view <id>
gh workflow run version-bump.yml -f bump_type=patch
```

### Workflow Rules
- **NEVER push to `main`** — all changes go through PRs
- Every task starts with `gh issue create` — get the issue number first
- Branch names MUST follow the pattern: `<type>/<issue-number>_<Name>`
- Apply the correct label to PRs so the release drafter categorizes them
- Reference the issue in the PR body with `Closes #<number>`
- After merging, the release drafter auto-updates the draft release
- To publish a release: `gh release create` — this triggers Docker image builds

---

## External Dependencies & APIs

| Service | Purpose | Auth |
|---|---|---|
| Pocket ID (OIDC) | User authentication | OpenID Connect (PKCE flow) |
| MoodlewareAPI | Moodle data bridge | Username/password via /auth endpoint |
| Microsoft Graph | OneNote, SharePoint, Calendar | OAuth2 (delegated) |
| Qdrant | Vector search | API key or no auth (local) |
| MinIO | Object storage | Access/secret key |
| Redis | Caching | Password (optional) |
| PostgreSQL | Relational data | Connection string |
| LLM Provider (via Semantic Kernel) | AI features | API key (provider-specific) |
| Whisper | Speech-to-text | Internal microservice |
| pyannote | Speaker diarization | Internal microservice |

---

## Phase Roadmap

### Phase 1 — Foundation ✅
- [x] Project scaffolding (solution, projects, Docker, CI)
- [x] PostgreSQL + EF Core setup with initial entities (User, Course, Module, Content)
- [x] OIDC authentication (Pocket ID, PKCE flow, JWT validation, login/logout)
- [x] User management (profiles, roles, settings)
- [x] Moodle token linking per user (username/password via MoodlewareAPI /auth)
- [x] User onboarding flow (first login wizard)
- [x] Angular shell with spartan.ng (sidebar, topbar, routing, dark mode)
- [x] Moodle proxy: authenticate, list courses, fetch course content on demand
- [x] Basic content viewer (render PDFs, text, HTML inline)
- [x] Pin/star system: save content to local DB

### Phase 2 — AI Layer ✅
- [x] Qdrant integration + content pipeline (embed pinned content)
- [x] RAG chat: agentic tool-calling loop via Semantic Kernel with citations
- [x] AI side panel: context-aware, knows what you're viewing, markdown rendering
- [x] Auto-summarization on pin

### Phase 3 — Study Tools ✅
- [x] Quiz generator (AI generates questions from materials)
- [x] Flashcard engine with spaced repetition (SM-2)
- [x] Study sessions (Pomodoro timer + combined tools)
- [x] Progress tracking + analytics dashboard

### Phase 4 — Recording Pipeline ✅
- [x] Audio upload endpoint + MinIO storage
- [x] Whisper transcription microservice
- [x] Transcript viewer with topic timeline
- [x] Recording → content pipeline integration (auto-index + summarize)

### Phase 5 — Full LMS ✅
- [x] Assignment tracker with deadlines + calendar view
- [x] Grade viewer with trends
- [x] Forum/discussion proxy
- [x] Timetable integration
- [x] OneNote + SharePoint integration via Graph API
- [x] Agentic behavior (proactive study suggestions)

### Post-Phase — Polish & Iteration (current)
- [x] Migrate to MoodlewareAPI bridge (Kiota typed client)
- [x] OIDC login with Pocket ID (PKCE)
- [x] Migrate AI stack to Microsoft Semantic Kernel
- [x] Agentic tool-calling RAG (replaced hardcoded pipeline)
- [x] Full frontend migration to spartan.ng components
- [x] Migrate from MediatR to Mediator source generator (licensing)
- [ ] Index full module content — HTML, page body, file text extraction (#109)
- [ ] Self-enrol in courses via MoodlewareAPI (#81)
- [ ] Topbar user profile picture with settings dropdown (#86)
- [ ] Fix EF Core LINQ GroupBy translation in analytics (#94)

---

## References

- **Feature Brainstorm**: `docs/features.md`
- **spartan.ng docs**: https://www.spartan.ng/
- **MoodlewareAPI**: https://github.com/MoodleNG/MoodlewareAPI
- **Qdrant docs**: https://qdrant.tech/documentation/
- **Mediator**: https://github.com/martinothamar/Mediator
- **Semantic Kernel**: https://github.com/microsoft/semantic-kernel
- **Hangfire**: https://www.hangfire.io/

---

## Frontend Coding Guidelines

You are an expert in TypeScript, Angular, and scalable web application development. You write functional, maintainable, performant, and accessible code following Angular and TypeScript best practices.

### TypeScript Best Practices

- Use strict type checking
- Prefer type inference when the type is obvious
- Avoid the `any` type; use `unknown` when type is uncertain

### Angular Best Practices

- Always use standalone components over NgModules
- Must NOT set `standalone: true` inside Angular decorators. It's the default in Angular v20+.
- Use signals for state management
- Implement lazy loading for feature routes
- Do NOT use the `@HostBinding` and `@HostListener` decorators. Put host bindings inside the `host` object of the `@Component` or `@Directive` decorator instead
- Use `NgOptimizedImage` for all static images.
  - `NgOptimizedImage` does not work for inline base64 images.

### Accessibility Requirements

- It MUST pass all AXE checks.
- It MUST follow all WCAG AA minimums, including focus management, color contrast, and ARIA attributes.

### Components

- Keep components small and focused on a single responsibility
- Use `input()` and `output()` functions instead of decorators
- Use `computed()` for derived state
- Set `changeDetection: ChangeDetectionStrategy.OnPush` in `@Component` decorator
- Prefer inline templates for small components
- Prefer Reactive forms instead of Template-driven ones
- Do NOT use `ngClass`, use `class` bindings instead
- Do NOT use `ngStyle`, use `style` bindings instead
- When using external templates/styles, use paths relative to the component TS file.

### State Management

- Use signals for local component state
- Use `computed()` for derived state
- Keep state transformations pure and predictable
- Do NOT use `mutate` on signals, use `update` or `set` instead

### Templates

- Keep templates simple and avoid complex logic
- Use native control flow (`@if`, `@for`, `@switch`) instead of `*ngIf`, `*ngFor`, `*ngSwitch`
- Use the async pipe to handle observables
- Do not assume globals like (`new Date()`) are available.
- Do not write arrow functions in templates (they are not supported).

### Services

- Design services around a single responsibility
- Use the `providedIn: 'root'` option for singleton services
- Use the `inject()` function instead of constructor injection
