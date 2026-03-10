# Kursa — Project Overview

**Purpose**: AI-powered LMS + Study Companion with Moodle integration. Uses Moodle, OneNote, and SharePoint as lazy-loaded data sources, combined with AI study tools (RAG chat, quiz generation, flashcards, spaced repetition) and a lesson recording pipeline.

## Tech Stack
- **Frontend**: Angular 21 + spartan.ng + Tailwind CSS 4 (standalone components, signals)
- **Backend**: .NET 10 / ASP.NET Core (C# 14)
- **Architecture**: CQRS + MediatR, feature folders
- **Database**: PostgreSQL 18 (EF Core, code-first)
- **Vector DB**: Qdrant (semantic search, RAG)
- **Cache**: Redis
- **Object Storage**: MinIO
- **Background Jobs**: Hangfire
- **Auth**: OIDC (external provider) + Moodle tokens per user
- **LLM**: Configurable (OpenAI, Anthropic, Ollama) behind ILlmProvider interface
- **Package Manager (frontend)**: bun (NEVER npm/yarn)
- **UI Library**: spartan.ng (brain/helm pattern, install via `bun add -D @spartan-ng/cli`)
- **Containerization**: Docker + docker-compose

## Project Structure
```
Kursa/
├── src/
│   ├── Kursa.API/           # ASP.NET Core Web API
│   ├── Kursa.Application/   # CQRS handlers, DTOs, interfaces
│   ├── Kursa.Domain/        # Entities, value objects, enums
│   ├── Kursa.Infrastructure/ # EF Core, external service clients
│   └── Kursa.Frontend/      # Angular 21 frontend
├── docker-compose.yml
├── CLAUDE.md
└── .github/workflows/       # CI/CD pipelines
```

## Current State
Project is in early scaffolding phase. Only base structure exists — no actual features implemented yet. 34 GitHub issues created covering Phases 1-5.

## GitHub
- Repo: PianoNic/Kursa
- MUST use `gh` CLI for all GitHub interactions
- NEVER push to main — always issue → branch → PR → self-review → merge
- Branch naming: `feature/<issue>_Name`, `bug/<issue>_Name`, `refactor/<issue>_Name`
