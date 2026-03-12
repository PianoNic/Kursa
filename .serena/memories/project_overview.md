# Kursa — Project Overview

**Purpose**: AI-powered LMS + Study Companion with Moodle integration. Uses Moodle, OneNote, and SharePoint as lazy-loaded data sources, combined with AI study tools (RAG chat, quiz generation, flashcards, spaced repetition) and a lesson recording pipeline.

## Tech Stack
- **Frontend**: Angular 21 + spartan.ng (full hlm-* component library) + Tailwind CSS 4
- **Backend**: .NET 10 / ASP.NET Core (C# 14)
- **Architecture**: CQRS + MediatR, feature folders, Clean Architecture
- **Database**: PostgreSQL (EF Core, code-first)
- **Vector DB**: Qdrant (semantic search, RAG embeddings)
- **Cache**: Redis
- **Object Storage**: MinIO
- **Background Jobs**: Hangfire
- **Auth**: OIDC via Pocket ID (PKCE flow) + Moodle username/password via MoodlewareAPI /auth. Dev bypass mode available.
- **LLM**: Microsoft Semantic Kernel (replaced custom ILlmProvider). KursaAgentPlugin for agentic tool-calling.
- **RAG**: Agentic tool-calling loop via Semantic Kernel (not hardcoded pipeline)
- **Moodle**: All calls via MoodlewareAPI (FastAPI bridge). Kiota typed client. Redis caching.
- **Package Manager (frontend)**: bun (NEVER npm/yarn)
- **Containerization**: Docker + compose.yml. Single container serves API + Angular SPA via SpaServices.Extensions (no nginx)
- **API Client**: Generated via `@openapitools/openapi-generator-cli` (`typescript-angular`). Run `bun run apigen`. 68 fully typed models. All frontend components use generated services exclusively (no hand-written services except auth.service.ts for OIDC and ai-context.service.ts for state).
- **Swagger**: All controller endpoints have `[ProducesResponseType]` attributes for typed Swagger output. Response DTOs in `src/Kursa.API/Dtos/` and `src/Kursa.Application/Features/*/`.
- **Graph Token**: `GraphTokenService` manages Microsoft Graph access token; `graphTokenInterceptor` injects `X-Graph-Token` header.
- **Swagger**: Swashbuckle in development mode at /swagger
- **AppController**: GET /api/app returns version, environment, health, OIDC config

## Project Structure
```
Kursa/
├── src/
│   ├── Kursa.API/           # ASP.NET Core Web API + Dockerfile
│   ├── Kursa.Application/   # CQRS handlers, DTOs, interfaces
│   ├── Kursa.Domain/        # Entities, value objects, enums
│   ├── Kursa.Infrastructure/ # EF Core, external service clients
│   └── Kursa.Frontend/      # Angular 21 frontend
│       └── src/app/api/     # Generated OpenAPI client
│       └── src/lib/ui/      # spartan.ng hlm-* components
├── compose.yml
├── Kursa.slnx               # Solution file (NOT .sln)
├── CLAUDE.md
└── .github/workflows/       # CI/CD pipelines
```

## Current State
**ALL 5 PHASES COMPLETE.** 120+ issues closed, 50+ PRs merged. Currently in polish/iteration mode.

### Open Issues
- **#81** feat(courses): self-enrol via MoodlewareAPI
- **#94** bug(analytics): EF Core LINQ GroupBy translation issue
- **#109** feat(rag): index full module content (HTML, page body, file text)

## GitHub
- Repo: PianoNic/Kursa
- MUST use `gh` CLI for all GitHub interactions
- NEVER push to main — always issue → branch → PR → self-review → merge
- Branch naming: `feature/<issue>_Name`, `bug/<issue>_Name`, `refactor/<issue>_Name`
