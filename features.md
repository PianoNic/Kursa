# Study App — Feature Brainstorm (v2)

## Architecture Philosophy

**Own LMS with Moodle (+ OneNote/SharePoint) as lazy-loaded data sources.**

- App is a standalone LMS — it doesn't mirror or sync everything
- Moodle, OneNote, SharePoint are remote backends, fetched on demand via APIs
- Content is only stored/indexed locally when the user interacts with it (opens, pins, stars, studies)
- AI features (RAG, summaries, quizzes) only work on content you've explicitly pulled in
- Think "Spotify model" — stream/browse anything, but only saved items are truly yours
- MoodlewareAPI (FastAPI bridge) simplifies interaction with Moodle's Web Services API

---

## 1. Data Sources & Integrations

### Moodle (via MoodlewareAPI)
- Authenticate with Moodle token
- Browse courses, modules, sections — fetched on demand, not bulk synced
- Lazy-load content: PDFs, slides, links, wiki pages, embedded resources
- Fetch assignments with deadlines, submission status, rubrics
- Fetch grades on demand
- Fetch announcements / news feed
- Fetch forum posts and discussions
- Fetch Moodle quizzes and quiz results
- Cache layer: recently viewed content stored locally for speed + offline access
- Pin/star system: explicitly save content → triggers indexing in vector DB for AI features
- Change detection: lightweight polling for new/updated content in enrolled courses (metadata only, not full download)

### OneNote (Microsoft Graph API)
- OAuth2 with school Microsoft 365 account (delegated permissions)
- Browse shared teacher notebooks — sections, pages — on demand
- Content extraction: text, images, tables, embedded files
- Ink/handwriting OCR for tablet-written notes
- Pull embedded PDFs, Word docs, images from OneNote pages
- Same pin/cache model: only store what you interact with
- Fallback: local folder watching if Graph API is blocked by school IT

### SharePoint (Microsoft Graph API)
- Browse shared class document libraries on demand
- Pull files when needed
- Same auth as OneNote

### Audio Recordings (from companion apps)
- Receive uploads from Android APK + Windows EXE
- Course tagging per recording (manual or auto-matched via timetable)
- Stored locally in app storage (MinIO / filesystem)
- Transcription pipeline (speech-to-text)
- Speaker diarization (separate teacher vs students)
- Smart splitting (detect lesson boundaries if recorded continuously)
- Always indexed — recordings are first-class content, not lazy-loaded

### Other Microsoft 365 (optional, low priority)
- Teams — announcements, shared files
- Outlook calendar — timetable sync for auto-tagging recordings
- To Do / Planner — homework task sync

---

## 2. Full LMS Features (Moodle Replacement)

### Course Dashboard
- All enrolled courses, fetched from Moodle on login / refresh
- Course cards with: name, teacher, last activity, upcoming deadline, progress indicator
- Pinned / favorite courses at top
- Quick access to recent materials
- "What's new" badge: lightweight change detection shows new content since last visit

### Course View
- Full course structure: sections, modules, activities — fetched on demand from Moodle
- Render content inline: PDF viewer, markdown, HTML, images, slides, embedded video
- Show content source indicator (Moodle / OneNote / SharePoint / Recording / Local)
- Module completion tracking (synced from Moodle or tracked locally)
- Breadcrumb navigation: Course → Section → Module

### Content Browser / File Manager
- All materials you've interacted with, organized by course
- Search: filename, full-text (for indexed content), semantic (for AI-indexed content)
- Filter by source, type, date, course
- Bulk actions: pin, download, add to study set
- Inline viewers for all common formats

### Assignments
- List all assignments across courses with deadlines
- Detailed view: description, rubric, attached files, submission status
- Calendar view (daily / weekly / monthly)
- Countdown timers for upcoming deadlines
- Priority sorting (due date, weight, difficulty)
- Submit assignments through the app (proxy to Moodle's submission API)
- Draft system: work on submissions locally before submitting

### Grades
- Grade overview per course, fetched from Moodle
- Grade detail: individual items, weights, feedback
- Trends over time (chart)
- GPA / average calculations
- Predictions: "you need X% on the final to get Y overall"

### Forums / Discussions
- Browse Moodle forums per course
- Read threads, view replies
- Post replies / new threads through the app
- AI-powered: summarize long threads, suggest answers
- Notification on new posts in subscribed forums

### Moodle Quizzes
- View available quizzes, attempt history, scores
- Take quizzes through the app (proxy to Moodle quiz engine)
- Review past attempts with correct answers
- Feed quiz results into study analytics

### Announcements / Notifications
- Aggregated from Moodle + OneNote changes + assignment deadlines
- AI-generated TL;DRs for long announcements
- Smart notification priority (not everything needs a ping)
- Notification center in-app

### Timetable / Schedule
- Synced from Outlook calendar, or manually configured
- Daily / weekly view
- Auto-tags recordings to correct course/lesson
- Shows upcoming classes, deadlines, study sessions
- "What's next" widget on dashboard

### Messaging (optional)
- If Moodle messaging API is accessible: read/send messages through the app
- Or skip entirely and let people use Teams/WhatsApp

### Progress Tracking
- Per-course: which modules completed, percentage through material
- Visual progress bars
- Activity history: what you viewed, when, for how long
- Streaks: consecutive days of activity

### Offline Mode
- All pinned/cached content available offline
- Study tools (flashcards, quizzes) work offline
- Sync changes when back online
- Recordings stored locally until uploaded

---

## 3. AI / Agent Features

### RAG Chat (Core)
- Chat interface grounded in YOUR indexed content (pinned materials + recordings)
- Inline source citations with clickable links to jump to source document/page/timestamp
- Filter context by: course, module, topic, source type, date range
- Thread-based conversations per course or per topic
- Multi-turn: follow-up questions, drill deeper
- "Ask about this" — right-click any content to start a chat with that document as context

### Contextual AI Side Panel
- Persistent side panel alongside any content view
- Context-aware: automatically knows what you're currently viewing
- Ask questions about the specific document/page/slide
- "Explain like I'm 5" / complexity toggle
- "Translate this" — explain in simpler terms or another language
- Code execution inline (for programming courses)

### Auto-Summarization
- Triggered when content is pinned/indexed
- Module-level summaries (combines all materials in a module)
- Course-level overview (high-level summary of entire course so far)
- Lesson recording summaries (per recording)
- "What changed" summaries when content is updated
- Browsable as a "Study Notes" view — structured, not just chat history

### Smart Content Processing
- Auto-tagging: AI categorizes content by topic/concept/keyword
- Auto-generated glossary: terms + definitions per course, growing over time
- Cross-source merging: Moodle slide + OneNote annotation + lesson transcript → unified summary
- Contradiction detection: teacher said X, slides say Y → flag it
- Concept extraction: identify key ideas, formulas, dates, definitions automatically
- Relationship mapping: "this topic relates to X from week 2"

### From Audio Recordings
- Structured lesson summaries
- Key concepts extraction (terms, definitions, formulas mentioned)
- Teacher emphasis detection: "this will be on the exam" / "this is important"
- Topic timeline: visual scrubber showing what was discussed when
- Action items: homework mentioned, dates dropped, things to look up
- Cross-lesson links: "today's topic builds on lesson from 2 weeks ago"
- Q&A extraction: questions asked in class + teacher's answers

### Agentic Behavior (Proactive AI)
- "Exam in 5 days, you haven't reviewed Topic Y" push notifications
- "Based on quiz results, revisit Chapter 3" suggestions
- "Assignment due in 2 days, you haven't looked at the material yet" alerts
- "Here's relevant material from lectures that could help with this assignment"
- Daily study recommendations based on schedule + gaps
- "You were absent — here's what was covered" (from classmates' recordings or Moodle content)
- Auto-suggest flashcards from newly indexed content
- Weekend review reminder: "here's what you covered this week"

---

## 4. Active Study Tools

### Quiz Generator
- Select course, topic, or specific materials → AI generates questions
- Question types: multiple choice, fill-in-the-blank, true/false, open-ended, matching
- Source-aware: questions come from your actual materials with references
- Difficulty scaling: adapts based on your performance
- Score tracking + history
- Timed mode (simulate exam pressure)
- "Quick quiz" — 5-minute random quiz from recent material

### Flashcard Engine
- Auto-generated from indexed course materials + lesson recordings
- Manual creation too (add your own)
- Spaced repetition: SM-2 algorithm or Leitner box system
- Review scheduling: tells you when to review which cards
- Card types: text, image, cloze deletion, reversible (term ↔ definition)
- Decks organized by course / topic
- Import/export (Anki format compatibility?)

### Study Sessions
- Timed study mode with Pomodoro timer built in
- Combines: flashcard review + quiz + material review
- Focuses on weak areas automatically
- Session summary: what you covered, what you got right/wrong
- Session history / study log

### Exam Prep Mode
- Select an upcoming exam (from assignment tracker or manual)
- AI aggregates everything relevant: slides, transcripts, summaries, OneNote pages
- Generates a study plan with timeline (days until exam → daily targets)
- Creates targeted practice questions based on likely exam content
- Gap analysis: what you've mastered vs what needs work
- "Cram mode" vs "deep study" mode

### Daily / Weekly Digests
- End of day: "Here's what you learned today" — summaries, new flashcards, deadlines
- End of week: "This week's recap" — 15-minute retention quiz, progress update
- Configurable: push notification, email, or just in-app

---

## 5. Knowledge & Progress Intelligence

### Unified Knowledge Base
- All pinned/indexed content in one vector DB (Qdrant)
- Sources: Moodle files, OneNote pages, SharePoint docs, recordings, manual notes
- Semantic search across everything — not just filename matching
- Source-aware results: shows where each result came from + direct link
- Full-text search as fallback for non-embedded content

### Knowledge Graph / Concept Map
- Visual graph of topics per course
- Shows how topics connect to each other and across courses
- Nodes colored by mastery level (based on quiz/flashcard performance)
- Skill tree or heatmap visualization options
- Click a node → see all related materials, summaries, quizzes

### Weak Spot Detection
- Cross-references: quiz results + flashcard retention + time spent
- Per-topic mastery score
- Identifies gaps: "You understand A and C but skipped B which connects them"
- Feeds into agentic recommendations

### Study Analytics Dashboard
- Time spent studying per course / topic / day
- Quiz performance trends over time
- Flashcard retention rates + upcoming reviews
- Study streaks + consistency tracking
- Material coverage: how much of each course you've actually engaged with
- Comparison: time invested vs grade outcome

---

## 6. Social / Collaborative (future, multi-user)

- Shared lesson recordings: one person records, everyone benefits
- Collaborative notes: layer annotations on top of AI summaries
- Study groups: shared flashcard decks, group quizzes
- Quiz leaderboards
- Course-level discussion (separate from Moodle forums)
- "Share my study plan" with classmates
- Peer explanations: answer each other's questions, AI validates

---

## 7. UX / Quality of Life

### Layout
- Sidebar: course navigation (collapsible, like Moodle's drawer but good)
- Main area: content viewer / dashboard / study tools
- Right panel: AI assistant side panel (toggleable, context-aware)
- Top bar: global search, notifications, user menu, study streak indicator

### Design
- Dark mode (default, obviously)
- Light mode option
- spartan.ng component library for consistent, clean UI
- Tailwind for styling
- Responsive: works on desktop, tablet, mobile browser
- Minimal, modern — anti-Moodle aesthetic

### Power User Features
- Keyboard shortcuts for everything (vim-style optional?)
- Command palette (Ctrl+K) — search anything, navigate anywhere, trigger actions
- Customizable dashboard widgets
- Quick actions: pin, star, start quiz, open chat — minimal clicks

### Gamification
- Study streaks (consecutive days)
- XP system: earn XP for quizzes, flashcard reviews, completing modules
- Level per course or overall
- Achievements / badges (optional, not cringe)
- Weekly goals: set targets, track completion

### Notifications
- Smart priority: not everything is a ping
- Configurable per category (deadlines, new content, AI suggestions, social)
- Quiet hours / study mode (suppress non-urgent)
- Desktop notifications + in-app notification center

---

## 8. Companion Apps (Recording)

### Android APK
- Foreground service with persistent notification for reliable background recording
- One-tap recording (dead simple UX)
- Tag lesson to course (manual or auto from timetable)
- Background recording with screen off
- Chunked recording: save every X minutes, crash-safe
- Local storage first, upload on wifi or manual trigger
- Multi-lesson day: record → pause for break → record next
- Battery optimization exemption prompt

### Windows EXE
- System tray app, always running in background
- Startup app / runs on login
- Hotkey to start/stop recording (global shortcut)
- Same features: chunked recording, local storage, course tagging
- Upload to backend when ready
- Minimal resource usage

### Shared Features (both platforms)
- Audio quality settings (bitrate, format)
- Recording history with status (recorded / uploading / uploaded / processed)
- Push notification when processing is complete
- Simple — no feature creep, these apps just record and upload

---

## 9. Tech Stack

| Component | Technology |
|---|---|
| Frontend | Angular 21 + spartan.ng + Tailwind |
| Backend API | .NET Core |
| Moodle Bridge | MoodlewareAPI (FastAPI, Python) |
| Vector DB | Qdrant |
| Database | PostgreSQL |
| Object Storage | MinIO or filesystem |
| LLM | Configurable: OpenAI / Anthropic / local Ollama |
| Speech-to-Text | Whisper (API or self-hosted) |
| Speaker Diarization | pyannote (Python microservice) |
| Background Jobs | Hangfire |
| Cache | Redis (for Moodle API response caching) |
| Auth | Microsoft OAuth2 (Graph API) + Moodle token |
| Android App | Kotlin |
| Windows App | .NET (WPF/WinUI) |
| Search | Qdrant (semantic) + PostgreSQL full-text (fallback) |

---

## 10. Open Questions

- App name?
- MoodlewareAPI: use as-is, fork, or rewrite the bridge in .NET?
- LLM provider: cloud only, local only, or configurable?
- Multi-user from the start or single-user first?
- How much of Moodle's quiz engine to replicate vs proxy?
- Mobile web responsive enough or dedicated mobile app later?
- Recording apps: shared codebase (Flutter/MAUI) or native per platform?
