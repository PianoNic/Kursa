# CLAUDE.md — StudyApp

> **Project Codename**: StudyApp (name TBD — replace everywhere once finalized)
> **Owner**: Niclas (PianoNic) — pianonic.ch
> **Type**: AI-powered LMS + Study Companion with Moodle integration

---

## Project Overview

StudyApp is a full-fledged LMS that uses Moodle, OneNote, and SharePoint as **lazy-loaded data sources** (fetched on demand, not bulk-synced), combined with AI-powered study tools and a lesson recording pipeline. Think "Moodle but actually good, with AI baked in."

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
| **Architecture** | CQRS + MediatR | Commands/Queries separated, pipeline behaviors for validation/logging |
| **Database** | PostgreSQL | Primary relational store |
| **Vector DB** | Qdrant | Semantic search, RAG embeddings |
| **Cache** | Redis | Moodle API response caching, session data |
| **Object Storage** | MinIO | Audio recordings, PDFs, cached content |
| **Background Jobs** | Hangfire | Moodle polling, embedding generation, summary pipelines |
| **Auth** | Microsoft OAuth2 + Moodle tokens | Graph API for OneNote/SharePoint, Moodle token for LMS data |
| **LLM** | Configurable | Support OpenAI, Anthropic, and local Ollama — abstracted behind interface |
| **Speech-to-Text** | Whisper | API or self-hosted via Python microservice |
| **Diarization** | pyannote | Speaker separation in Python microservice |
| **Search** | Qdrant (semantic) + PostgreSQL FTS | Qdrant for AI-indexed content, PG for full-text fallback |
| **Containerization** | Docker + docker-compose | Everything runs containerized |

---

## Project Structure

```
StudyApp/
├── .claude/                    # Claude Code configuration
│   ├── settings.json
│   └── commands/               # Custom slash commands
├── docs/                       # Project documentation
│   ├── features.md             # Full feature brainstorm
│   ├── architecture.md         # Architecture decisions
│   └── api-contracts.md        # API endpoint contracts
├── src/
│   ├── StudyApp.Api/           # ASP.NET Core Web API
│   │   ├── Controllers/        # API controllers (or Endpoints/ for minimal APIs)
│   │   ├── Features/           # CQRS feature folders (Commands, Queries, Handlers)
│   │   ├── Infrastructure/     # EF Core, Qdrant client, Redis, MinIO, LLM clients
│   │   ├── Domain/             # Domain entities, value objects
│   │   ├── Middleware/         # Auth, error handling, logging
│   │   ├── Services/           # Application services (Moodle proxy, content pipeline, etc.)
│   │   └── Program.cs
│   ├── StudyApp.Domain/        # Domain layer (entities, interfaces, enums)
│   ├── StudyApp.Application/   # Application layer (CQRS handlers, DTOs, interfaces)
│   ├── StudyApp.Infrastructure/# Infrastructure layer (EF, external services, repositories)
│   └── StudyApp.Web/           # Angular frontend
│       ├── src/
│       │   ├── app/
│       │   │   ├── core/       # Guards, interceptors, services, auth
│       │   │   ├── shared/     # Shared components, pipes, directives
│       │   │   ├── features/   # Feature modules (dashboard, courses, chat, study-tools, etc.)
│       │   │   └── layout/     # Shell, sidebar, topbar, AI panel
│       │   ├── environments/
│       │   └── styles/
│       ├── angular.json
│       ├── tailwind.config.js
│       └── package.json
├── tests/
│   ├── StudyApp.Api.Tests/
│   ├── StudyApp.Application.Tests/
│   └── StudyApp.Web.Tests/     # Angular tests (Karma/Jest)
├── docker/
│   ├── Dockerfile.api
│   ├── Dockerfile.web
│   └── docker-compose.yml      # Full stack: API, DB, Redis, Qdrant, MinIO
├── CLAUDE.md                   # This file
├── README.md
├── .gitignore
├── .editorconfig
└── StudyApp.sln
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
- Every service gets a Dockerfile
- `docker-compose.yml` for full local dev stack
- Use multi-stage builds for .NET and Angular
- `.env.example` for all environment variables
- Health checks on all services

---

## Key Design Decisions

### Moodle Integration (Lazy Loading)
- Browse courses/content → fetch from MoodlewareAPI on demand, display in our UI, don't store
- Open a course/module → fetch and render, cache locally for speed
- Pin/star content → now it gets stored in PostgreSQL + indexed in Qdrant for AI features
- AI features → only work on pinned/indexed content
- Lightweight polling for change detection (metadata only)

### LLM Abstraction
- `ILlmProvider` interface with implementations for OpenAI, Anthropic, Ollama
- Configuration-driven: switch providers via `appsettings.json`
- All LLM calls go through a service that handles retries, rate limiting, token counting
- System prompts stored as templates, not hardcoded

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
cd src/StudyApp.Api
dotnet run

# Run frontend
cd src/StudyApp.Web
npm install
ng serve
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
cd src/StudyApp.Web
ng test

# E2E (if configured)
ng e2e
```

### Database Migrations
```bash
cd src/StudyApp.Infrastructure
dotnet ef migrations add <MigrationName> -s ../StudyApp.Api
dotnet ef database update -s ../StudyApp.Api
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

### DON'T
- Don't bulk-sync Moodle data — always lazy-load on demand
- Don't hardcode LLM provider — always use the abstraction
- Don't create Angular modules — use standalone components
- Don't use `any` type in TypeScript — always type properly
- Don't commit secrets, API keys, or connection strings — use `.env` and user-secrets
- Don't skip error handling — every external call (Moodle, LLM, Qdrant) can fail
- Don't use `var` in C# for non-obvious types — prefer explicit types for readability at boundaries

### Useful Commands
```bash
# Generate Angular component
ng g c features/dashboard/components/course-card --standalone

# Generate Angular service
ng g s core/services/moodle-proxy

# Add NuGet package
dotnet add src/StudyApp.Api package <PackageName>

# Add EF migration
dotnet ef migrations add <Name> --project src/StudyApp.Infrastructure --startup-project src/StudyApp.Api

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

5. **Merge via PR only** — never fast-forward or push to main directly

**This is non-negotiable. Every single change — no matter how small — goes through issue → branch → PR → merge.**

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
| MoodlewareAPI | Moodle data bridge | Moodle token |
| Microsoft Graph | OneNote, SharePoint, Calendar | OAuth2 (delegated) |
| Qdrant | Vector search | API key or no auth (local) |
| MinIO | Object storage | Access/secret key |
| Redis | Caching | Password (optional) |
| PostgreSQL | Relational data | Connection string |
| LLM Provider | AI features | API key (provider-specific) |
| Whisper | Speech-to-text | Internal microservice |
| pyannote | Speaker diarization | Internal microservice |

---

## Phase Roadmap

### Phase 1 — Foundation
- [ ] Project scaffolding (solution, projects, Docker, CI)
- [ ] PostgreSQL + EF Core setup with initial entities (User, Course, Module, Content)
- [ ] Angular shell with spartan.ng (sidebar, topbar, routing, dark mode)
- [ ] Moodle proxy: authenticate, list courses, fetch course content on demand
- [ ] Basic content viewer (render PDFs, text, HTML inline)
- [ ] Pin/star system: save content to local DB

### Phase 2 — AI Layer
- [ ] Qdrant integration + content pipeline (embed pinned content)
- [ ] RAG chat: ask questions about indexed content with citations
- [ ] AI side panel: context-aware, knows what you're viewing
- [ ] Auto-summarization on pin

### Phase 3 — Study Tools
- [ ] Quiz generator (AI generates questions from materials)
- [ ] Flashcard engine with spaced repetition (SM-2)
- [ ] Study sessions (combine quizzes + flashcards + review)
- [ ] Progress tracking + analytics dashboard

### Phase 4 — Recording Pipeline
- [ ] Audio upload endpoint + MinIO storage
- [ ] Whisper transcription microservice
- [ ] pyannote diarization microservice
- [ ] Transcript viewer with topic timeline
- [ ] Recording → content pipeline integration

### Phase 5 — Full LMS
- [ ] Assignment tracker with deadlines + calendar view
- [ ] Grade viewer with trends
- [ ] Forum/discussion proxy
- [ ] Timetable integration
- [ ] OneNote + SharePoint integration via Graph API
- [ ] Agentic behavior (proactive study suggestions)

---

## References

- **Feature Brainstorm**: `docs/features.md`
- **spartan.ng docs**: https://www.spartan.ng/
- **MoodlewareAPI**: https://github.com/MoodleNG/MoodlewareAPI
- **Qdrant docs**: https://qdrant.tech/documentation/
- **MediatR**: https://github.com/jbogard/MediatR
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
