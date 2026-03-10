# Architecture Decisions

## ADR-001: Lazy-Loading Moodle Data
**Decision**: Moodle content is fetched on demand via MoodlewareAPI, never bulk-synced.
**Rationale**: School Moodle instances contain too much data. Users only interact with a fraction.
**Consequence**: Content must be explicitly pinned to be available for AI features.

## ADR-002: CQRS with MediatR
**Decision**: Use CQRS pattern with MediatR for all backend operations.
**Rationale**: Clean separation of read/write. Easy to add cross-cutting concerns (validation, logging, caching) via pipeline behaviors. Niclas already uses this pattern in Schuly Backend.
**Consequence**: Every operation is a Command or Query with a dedicated Handler.

## ADR-003: spartan.ng over Angular Material
**Decision**: Use spartan.ng as the UI component library.
**Rationale**: shadcn/ui approach (own your components), Tailwind-native, better DX than Angular Material, more modern aesthetic.
**Consequence**: Must follow brain/helm pattern. Components are copied into project, not imported from a module.

## ADR-004: LLM Provider Abstraction
**Decision**: All LLM interactions go through an `ILlmProvider` interface.
**Rationale**: Support multiple providers (OpenAI, Anthropic, Ollama) without code changes. Niclas has access to both cloud APIs and local GPU infrastructure.
**Consequence**: Each provider implementation handles its own API format, token counting, and error handling.

## ADR-005: Content Pipeline for AI Indexing
**Decision**: All content passes through a standardized pipeline: extract → chunk → embed → store in Qdrant.
**Rationale**: Unified approach regardless of source (Moodle PDF, OneNote page, lesson transcript).
**Consequence**: Need a content extraction layer that handles multiple formats (PDF, HTML, plain text, images with OCR).

## ADR-006: Companion Apps as Separate Repos
**Decision**: Android (Kotlin) and Windows (WPF) recording apps are separate repositories.
**Rationale**: Different tech stacks, different release cycles, simpler CI/CD per platform.
**Consequence**: Need a well-defined upload API contract between companion apps and the backend.

## ADR-007: Docker-First Development
**Decision**: Every service runs in Docker with docker-compose for local dev.
**Rationale**: Consistent environments, easy onboarding, matches Niclas's existing workflow across all projects.
**Consequence**: `docker-compose.yml` must always be kept up-to-date and working.

## ADR-008: PostgreSQL for Structured + Full-Text Search
**Decision**: Use PostgreSQL for both relational data and full-text search fallback.
**Rationale**: Avoids adding another search engine. PG's `tsvector` is good enough for non-semantic search. Qdrant handles semantic search.
**Consequence**: Entities that need full-text search get `tsvector` columns and GIN indexes.

## ADR-009: Hangfire for Background Jobs
**Decision**: Use Hangfire for all background processing (sync polling, embedding, transcription triggers).
**Rationale**: .NET native, dashboard UI, persistent job storage in PostgreSQL, simple API.
**Consequence**: Long-running audio processing is triggered by Hangfire but executed by external Python microservices.

## ADR-010: Angular Signals over RxJS
**Decision**: Prefer Angular signals for component state; reserve RxJS for streams and HTTP.
**Rationale**: Simpler mental model, better performance, Angular's direction for the future.
**Consequence**: Existing team knowledge of RxJS still useful for HTTP and WebSocket streams.
