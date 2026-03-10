import { ChangeDetectionStrategy, Component, inject, signal, computed, OnInit, OnDestroy } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StudySession, StudySessionService } from '../../core/services/study-session.service';
import { FlashcardDeck, Flashcard, FlashcardService } from '../../core/services/flashcard.service';
import { Quiz, QuizDetail, QuizService } from '../../core/services/quiz.service';

type View = 'list' | 'setup' | 'active' | 'summary';
type TimerPhase = 'work' | 'break';

@Component({
  selector: 'app-study',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, FormsModule],
  template: `
    <div class="space-y-6">
      @switch (view()) {
        @case ('list') {
          <div class="flex items-center justify-between">
            <h1 class="text-2xl font-bold text-foreground">Study Sessions</h1>
            <button
              (click)="view.set('setup')"
              class="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
            >
              New Session
            </button>
          </div>

          @if (loading()) {
            <div class="flex items-center justify-center py-12">
              <div class="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent"></div>
            </div>
          } @else if (sessions().length === 0) {
            <div class="rounded-lg border border-border bg-card p-8 text-center">
              <svg class="mx-auto h-12 w-12 text-muted-foreground" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
              </svg>
              <h2 class="mt-4 text-lg font-semibold text-foreground">No study sessions yet</h2>
              <p class="mt-2 text-sm text-muted-foreground">
                Start a Pomodoro-based study session combining flashcards and quizzes.
              </p>
              <button
                (click)="view.set('setup')"
                class="mt-4 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
              >
                Start Studying
              </button>
            </div>
          } @else {
            <div class="space-y-3">
              @for (session of sessions(); track session.id) {
                <div class="flex items-center gap-4 rounded-lg border border-border bg-card p-4">
                  <div
                    class="flex h-10 w-10 items-center justify-center rounded-full"
                    [class]="session.status === 'Completed' ? 'bg-green-500/10 text-green-500' : 'bg-muted text-muted-foreground'"
                  >
                    @if (session.status === 'Completed') {
                      <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="m4.5 12.75 6 6 9-13.5" /></svg>
                    } @else {
                      <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" /></svg>
                    }
                  </div>
                  <div class="min-w-0 flex-1">
                    <p class="font-medium text-foreground">{{ session.title }}</p>
                    @if (session.summary) {
                      <p class="text-xs text-muted-foreground">{{ session.summary }}</p>
                    }
                    <p class="text-xs text-muted-foreground">{{ session.createdAt | date:'medium' }}</p>
                  </div>
                  <div class="flex items-center gap-3 text-xs text-muted-foreground">
                    @if (session.completedPomodoros > 0) {
                      <span>{{ session.completedPomodoros }} pomo</span>
                    }
                    @if (session.totalDurationSeconds > 0) {
                      <span>{{ formatDuration(session.totalDurationSeconds) }}</span>
                    }
                  </div>
                </div>
              }
            </div>
          }
        }

        @case ('setup') {
          <div class="flex items-center gap-3">
            <button
              (click)="view.set('list')"
              class="rounded-md p-2 text-muted-foreground hover:bg-accent"
              aria-label="Back"
            >
              <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5 3 12m0 0 7.5-7.5M3 12h18" />
              </svg>
            </button>
            <h1 class="text-2xl font-bold text-foreground">New Study Session</h1>
          </div>

          <div class="mx-auto max-w-lg space-y-6 rounded-lg border border-border bg-card p-6">
            <div>
              <label for="session-title" class="block text-sm font-medium text-foreground">Session Title</label>
              <input
                id="session-title"
                type="text"
                [(ngModel)]="sessionTitle"
                placeholder="e.g., Math Review, Exam Prep..."
                class="mt-1 w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
              />
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label for="work-min" class="block text-sm font-medium text-foreground">Work (min)</label>
                <input
                  id="work-min"
                  type="number"
                  [(ngModel)]="workMinutes"
                  min="1"
                  max="120"
                  class="mt-1 w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                />
              </div>
              <div>
                <label for="break-min" class="block text-sm font-medium text-foreground">Break (min)</label>
                <input
                  id="break-min"
                  type="number"
                  [(ngModel)]="breakMinutes"
                  min="1"
                  max="60"
                  class="mt-1 w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                />
              </div>
            </div>

            <button
              (click)="startSession()"
              [disabled]="!sessionTitle.trim()"
              class="w-full rounded-md bg-primary px-4 py-2.5 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
            >
              Start Session
            </button>
          </div>
        }

        @case ('active') {
          <div class="mx-auto max-w-lg space-y-6">
            <div class="flex items-center justify-between">
              <h1 class="text-xl font-bold text-foreground">{{ activeSession()?.title }}</h1>
              <span class="text-sm text-muted-foreground">
                Pomodoro #{{ pomodoroCount() + 1 }}
              </span>
            </div>

            <!-- Timer -->
            <div class="rounded-lg border border-border bg-card p-8 text-center">
              <p
                class="text-xs font-medium uppercase tracking-wider"
                [class]="timerPhase() === 'work' ? 'text-primary' : 'text-green-500'"
              >
                {{ timerPhase() === 'work' ? 'Focus Time' : 'Break Time' }}
              </p>
              <p
                class="mt-4 font-mono text-6xl font-bold"
                [class]="timerPhase() === 'work' ? 'text-foreground' : 'text-green-500'"
              >
                {{ formattedTimer() }}
              </p>

              <!-- Timer progress ring -->
              <div class="mt-6 h-2 w-full overflow-hidden rounded-full bg-muted">
                <div
                  class="h-full rounded-full transition-all"
                  [class]="timerPhase() === 'work' ? 'bg-primary' : 'bg-green-500'"
                  [style.width.%]="timerProgress()"
                ></div>
              </div>

              <div class="mt-6 flex justify-center gap-3">
                @if (timerRunning()) {
                  <button
                    (click)="pauseTimer()"
                    class="rounded-md border border-border px-6 py-2 text-sm font-medium text-muted-foreground hover:bg-accent"
                  >
                    Pause
                  </button>
                } @else {
                  <button
                    (click)="resumeTimer()"
                    class="rounded-md bg-primary px-6 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
                  >
                    {{ timerSeconds() === totalPhaseSeconds() ? 'Start' : 'Resume' }}
                  </button>
                }
                <button
                  (click)="skipPhase()"
                  class="rounded-md border border-border px-4 py-2 text-sm font-medium text-muted-foreground hover:bg-accent"
                >
                  Skip
                </button>
              </div>
            </div>

            <!-- Session stats -->
            <div class="grid grid-cols-3 gap-3">
              <div class="rounded-lg border border-border bg-card p-3 text-center">
                <p class="text-2xl font-bold text-foreground">{{ pomodoroCount() }}</p>
                <p class="text-xs text-muted-foreground">Pomodoros</p>
              </div>
              <div class="rounded-lg border border-border bg-card p-3 text-center">
                <p class="text-2xl font-bold text-foreground">{{ sessionCardsReviewed() }}</p>
                <p class="text-xs text-muted-foreground">Cards</p>
              </div>
              <div class="rounded-lg border border-border bg-card p-3 text-center">
                <p class="text-2xl font-bold text-foreground">{{ sessionQuizAnswered() }}</p>
                <p class="text-xs text-muted-foreground">Quiz Q's</p>
              </div>
            </div>

            <!-- End session -->
            <button
              (click)="endSession()"
              class="w-full rounded-md border border-destructive/50 bg-destructive/10 px-4 py-2 text-sm font-medium text-destructive hover:bg-destructive/20"
            >
              End Session
            </button>
          </div>
        }

        @case ('summary') {
          @if (completedSession()) {
            <div class="mx-auto max-w-lg space-y-6">
              <h1 class="text-2xl font-bold text-foreground text-center">Session Complete!</h1>

              <div class="rounded-lg border border-border bg-card p-6 text-center">
                <svg class="mx-auto h-12 w-12 text-green-500" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M9 12.75 11.25 15 15 9.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
                </svg>
                <h2 class="mt-4 text-lg font-semibold text-foreground">{{ completedSession()!.title }}</h2>
                @if (completedSession()!.summary) {
                  <p class="mt-2 text-sm text-muted-foreground">{{ completedSession()!.summary }}</p>
                }
              </div>

              <div class="grid grid-cols-2 gap-3">
                <div class="rounded-lg border border-border bg-card p-4 text-center">
                  <p class="text-3xl font-bold text-primary">{{ completedSession()!.completedPomodoros }}</p>
                  <p class="text-xs text-muted-foreground">Pomodoros</p>
                </div>
                <div class="rounded-lg border border-border bg-card p-4 text-center">
                  <p class="text-3xl font-bold text-primary">{{ formatDuration(completedSession()!.totalDurationSeconds) }}</p>
                  <p class="text-xs text-muted-foreground">Total Time</p>
                </div>
                <div class="rounded-lg border border-border bg-card p-4 text-center">
                  <p class="text-3xl font-bold text-primary">{{ completedSession()!.cardsReviewed }}</p>
                  <p class="text-xs text-muted-foreground">Cards Reviewed</p>
                </div>
                <div class="rounded-lg border border-border bg-card p-4 text-center">
                  <p class="text-3xl font-bold text-primary">
                    {{ completedSession()!.quizCorrectAnswers }}/{{ completedSession()!.quizQuestionsAnswered }}
                  </p>
                  <p class="text-xs text-muted-foreground">Quiz Score</p>
                </div>
              </div>

              <button
                (click)="backToList()"
                class="w-full rounded-md bg-primary px-4 py-2.5 text-sm font-medium text-primary-foreground hover:bg-primary/90"
              >
                Done
              </button>
            </div>
          }
        }
      }
    </div>
  `,
})
export class StudyComponent implements OnInit, OnDestroy {
  private readonly sessionService = inject(StudySessionService);

  readonly view = signal<View>('list');
  readonly loading = signal(true);
  readonly sessions = signal<StudySession[]>([]);

  // Setup
  sessionTitle = '';
  workMinutes = 25;
  breakMinutes = 5;

  // Active session
  readonly activeSession = signal<StudySession | null>(null);
  readonly timerPhase = signal<TimerPhase>('work');
  readonly timerSeconds = signal(0);
  readonly timerRunning = signal(false);
  readonly pomodoroCount = signal(0);
  readonly sessionCardsReviewed = signal(0);
  readonly sessionQuizAnswered = signal(0);
  readonly sessionQuizCorrect = signal(0);
  private timerInterval: ReturnType<typeof setInterval> | null = null;
  private sessionStartTime = 0;

  // Summary
  readonly completedSession = signal<StudySession | null>(null);

  readonly totalPhaseSeconds = computed(() => {
    const phase = this.timerPhase();
    const session = this.activeSession();
    if (!session) return 0;
    return phase === 'work' ? session.workMinutes * 60 : session.breakMinutes * 60;
  });

  readonly formattedTimer = computed(() => {
    const total = this.timerSeconds();
    const minutes = Math.floor(total / 60);
    const seconds = total % 60;
    return `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
  });

  readonly timerProgress = computed(() => {
    const total = this.totalPhaseSeconds();
    if (total === 0) return 0;
    return ((total - this.timerSeconds()) / total) * 100;
  });

  ngOnInit(): void {
    this.loadSessions();
  }

  ngOnDestroy(): void {
    this.stopTimer();
  }

  loadSessions(): void {
    this.loading.set(true);
    this.sessionService.getSessions().subscribe({
      next: (sessions) => {
        this.sessions.set(sessions);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  startSession(): void {
    if (!this.sessionTitle.trim()) return;

    this.sessionService.startSession(this.sessionTitle.trim(), this.workMinutes, this.breakMinutes).subscribe({
      next: (session) => {
        this.activeSession.set(session);
        this.pomodoroCount.set(0);
        this.sessionCardsReviewed.set(0);
        this.sessionQuizAnswered.set(0);
        this.sessionQuizCorrect.set(0);
        this.sessionStartTime = Date.now();
        this.timerPhase.set('work');
        this.timerSeconds.set(session.workMinutes * 60);
        this.view.set('active');
      },
    });
  }

  resumeTimer(): void {
    if (this.timerRunning()) return;
    this.timerRunning.set(true);
    this.timerInterval = setInterval(() => {
      this.timerSeconds.update((s) => {
        if (s <= 1) {
          this.onPhaseComplete();
          return 0;
        }
        return s - 1;
      });
    }, 1000);
  }

  pauseTimer(): void {
    this.timerRunning.set(false);
    this.stopTimer();
  }

  skipPhase(): void {
    this.stopTimer();
    this.onPhaseComplete();
  }

  endSession(): void {
    this.stopTimer();
    const session = this.activeSession();
    if (!session) return;

    const elapsed = Math.round((Date.now() - this.sessionStartTime) / 1000);

    this.sessionService.completeSession(
      session.id,
      this.pomodoroCount(),
      elapsed,
      this.sessionCardsReviewed(),
      this.sessionQuizAnswered(),
      this.sessionQuizCorrect(),
    ).subscribe({
      next: (completed) => {
        this.completedSession.set(completed);
        this.view.set('summary');
      },
    });
  }

  backToList(): void {
    this.view.set('list');
    this.loadSessions();
  }

  formatDuration(seconds: number): string {
    const h = Math.floor(seconds / 3600);
    const m = Math.floor((seconds % 3600) / 60);
    if (h > 0) return `${h}h ${m}m`;
    return `${m}m`;
  }

  private onPhaseComplete(): void {
    this.stopTimer();
    this.timerRunning.set(false);

    if (this.timerPhase() === 'work') {
      this.pomodoroCount.update((c) => c + 1);
      this.timerPhase.set('break');
      const session = this.activeSession();
      this.timerSeconds.set(session ? session.breakMinutes * 60 : 300);
    } else {
      this.timerPhase.set('work');
      const session = this.activeSession();
      this.timerSeconds.set(session ? session.workMinutes * 60 : 1500);
    }
  }

  private stopTimer(): void {
    if (this.timerInterval) {
      clearInterval(this.timerInterval);
      this.timerInterval = null;
    }
  }
}
