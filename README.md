# StudyApp

> **AI-powered LMS + Study Companion** — Moodle but actually good, with AI baked in.

StudyApp is a full-fledged learning management system that uses Moodle, OneNote, and SharePoint as lazy-loaded data sources, combined with AI-powered study tools (RAG chat, quiz generation, flashcards, spaced repetition) and a lesson recording pipeline.

## Features

- **LMS**: Course dashboard, content browser, assignments, grades, timetable — all fetched on demand from Moodle
- **AI Chat**: Ask questions about your course materials with source citations (RAG)
- **Study Tools**: AI-generated quizzes, flashcards with spaced repetition, exam prep mode
- **Lesson Recording**: Companion apps (Android/Windows) record lessons, backend transcribes and summarizes
- **Smart Search**: Semantic search across all your materials (Moodle, OneNote, SharePoint, recordings)
- **Knowledge Intelligence**: Concept maps, weak spot detection, study analytics

## Tech Stack

**Frontend**: Angular 21 + spartan.ng + Tailwind CSS
**Backend**: .NET 10 + EF Core + MediatR + PostgreSQL
**AI**: Qdrant (vector search) + configurable LLM (OpenAI/Anthropic/Ollama)
**Infrastructure**: Docker, Redis, MinIO, Hangfire

## Quick Start

```bash
# Clone
git clone https://github.com/PianoNic/StudyApp.git
cd StudyApp

# Copy env file
cp .env.example .env
# Edit .env with your configuration

# Start everything
docker compose up -d

# Or run individually:
# Backend
cd src/StudyApp.Api && dotnet run

# Frontend
cd src/StudyApp.Web && npm install && ng serve
```

## Documentation

- **[Feature Brainstorm](docs/features.md)** — Full feature list
- **[Architecture Decisions](docs/architecture.md)** — ADRs and design rationale
- **[CLAUDE.md](CLAUDE.md)** — Agent instructions for Claude Code

## License

MIT

---

Made with ❤️ by [PianoNic](https://github.com/PianoNic)
