import { ChangeDetectionStrategy, Component, inject, signal, computed, OnInit } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  Quiz,
  QuizDetail,
  QuizQuestion,
  QuizAttemptDetail,
  QuizAnswerResult,
  QuizService,
} from '../../core/services/quiz.service';
import { PinnedContent, PinnedContentService } from '../../core/services/pinned-content.service';

type View = 'list' | 'generate' | 'taking' | 'results';

@Component({
  selector: 'app-quizzes',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, DecimalPipe, FormsModule],
  template: `
    <div class="space-y-6">
      @switch (view()) {
        @case ('list') {
          <div class="flex items-center justify-between">
            <h1 class="text-2xl font-bold text-foreground">Quizzes</h1>
            <button
              (click)="showGenerate()"
              class="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
            >
              Generate Quiz
            </button>
          </div>

          @if (loading()) {
            <div class="flex items-center justify-center py-12">
              <div class="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent"></div>
              <span class="ml-3 text-muted-foreground">Loading quizzes...</span>
            </div>
          } @else if (quizzes().length === 0) {
            <div class="rounded-lg border border-border bg-card p-8 text-center">
              <svg class="mx-auto h-12 w-12 text-muted-foreground" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" d="M9.879 7.519c1.171-1.025 3.071-1.025 4.242 0 1.172 1.025 1.172 2.687 0 3.712-.203.179-.43.326-.67.442-.745.361-1.45.999-1.45 1.827v.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Zm-9 5.25h.008v.008H12v-.008Z" />
              </svg>
              <h2 class="mt-4 text-lg font-semibold text-foreground">No quizzes yet</h2>
              <p class="mt-2 text-sm text-muted-foreground">
                Generate a quiz from your pinned course materials to test your knowledge.
              </p>
              <button
                (click)="showGenerate()"
                class="mt-4 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
              >
                Generate Your First Quiz
              </button>
            </div>
          } @else {
            <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
              @for (quiz of quizzes(); track quiz.id) {
                <div class="rounded-lg border border-border bg-card p-5 transition-colors hover:border-primary/50">
                  <h3 class="font-semibold text-foreground">{{ quiz.title }}</h3>
                  @if (quiz.topic) {
                    <p class="mt-1 text-xs text-muted-foreground">{{ quiz.topic }}</p>
                  }
                  <div class="mt-3 flex items-center gap-4 text-xs text-muted-foreground">
                    <span>{{ quiz.questionCount }} questions</span>
                    <span>{{ quiz.attemptCount }} attempt{{ quiz.attemptCount === 1 ? '' : 's' }}</span>
                  </div>
                  @if (quiz.bestScore !== null) {
                    <div class="mt-2">
                      <div class="flex items-center justify-between text-xs text-muted-foreground">
                        <span>Best score</span>
                        <span class="font-medium" [class]="quiz.bestScore >= quiz.questionCount * 0.7 ? 'text-green-500' : 'text-orange-500'">
                          {{ quiz.bestScore }}/{{ quiz.questionCount }}
                        </span>
                      </div>
                      <div class="mt-1 h-1.5 w-full overflow-hidden rounded-full bg-muted">
                        <div
                          class="h-full rounded-full transition-all"
                          [class]="quiz.bestScore >= quiz.questionCount * 0.7 ? 'bg-green-500' : 'bg-orange-500'"
                          [style.width.%]="(quiz.bestScore / quiz.questionCount) * 100"
                        ></div>
                      </div>
                    </div>
                  }
                  <div class="mt-4 flex gap-2">
                    <button
                      (click)="startQuiz(quiz.id)"
                      class="flex-1 rounded-md bg-primary px-3 py-1.5 text-xs font-medium text-primary-foreground hover:bg-primary/90"
                    >
                      Take Quiz
                    </button>
                    <button
                      (click)="viewResults(quiz.id)"
                      class="rounded-md border border-border px-3 py-1.5 text-xs font-medium text-muted-foreground hover:bg-accent"
                      [disabled]="quiz.attemptCount === 0"
                      [class.opacity-50]="quiz.attemptCount === 0"
                    >
                      Results
                    </button>
                  </div>
                  <p class="mt-2 text-xs text-muted-foreground">{{ quiz.createdAt | date:'mediumDate' }}</p>
                </div>
              }
            </div>
          }
        }

        @case ('generate') {
          <div class="flex items-center gap-3">
            <button
              (click)="view.set('list')"
              class="rounded-md p-2 text-muted-foreground hover:bg-accent"
              aria-label="Back to quizzes"
            >
              <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5 3 12m0 0 7.5-7.5M3 12h18" />
              </svg>
            </button>
            <h1 class="text-2xl font-bold text-foreground">Generate Quiz</h1>
          </div>

          @if (loadingPinned()) {
            <div class="flex items-center justify-center py-12">
              <div class="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent"></div>
              <span class="ml-3 text-muted-foreground">Loading pinned content...</span>
            </div>
          } @else if (pinnedItems().length === 0) {
            <div class="rounded-lg border border-border bg-card p-8 text-center">
              <h2 class="mt-4 text-lg font-semibold text-foreground">No pinned content</h2>
              <p class="mt-2 text-sm text-muted-foreground">
                Pin and index some course materials first to generate quizzes.
              </p>
            </div>
          } @else {
            <div class="mx-auto max-w-lg space-y-6 rounded-lg border border-border bg-card p-6">
              <div>
                <label for="content-select" class="block text-sm font-medium text-foreground">Select Material</label>
                <select
                  id="content-select"
                  [(ngModel)]="selectedContentId"
                  class="mt-1 w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                >
                  <option value="">Choose pinned content...</option>
                  @for (item of indexedItems(); track item.contentId) {
                    <option [value]="item.contentId">{{ item.contentTitle }}</option>
                  }
                </select>
              </div>
              <div>
                <label for="question-count" class="block text-sm font-medium text-foreground">Number of Questions</label>
                <input
                  id="question-count"
                  type="number"
                  [(ngModel)]="questionCount"
                  min="1"
                  max="50"
                  class="mt-1 w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                />
              </div>
              <div>
                <label for="topic" class="block text-sm font-medium text-foreground">Topic (optional)</label>
                <input
                  id="topic"
                  type="text"
                  [(ngModel)]="topicInput"
                  placeholder="Focus on a specific topic..."
                  class="mt-1 w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                />
              </div>
              <div>
                <label for="duration" class="block text-sm font-medium text-foreground">Time Limit (minutes)</label>
                <input
                  id="duration"
                  type="number"
                  [(ngModel)]="durationMinutes"
                  min="1"
                  max="120"
                  class="mt-1 w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                />
              </div>

              @if (error()) {
                <div class="rounded-md border border-destructive/50 bg-destructive/10 p-3">
                  <p class="text-sm text-destructive">{{ error() }}</p>
                </div>
              }

              <button
                (click)="generateQuiz()"
                [disabled]="generating() || !selectedContentId"
                class="w-full rounded-md bg-primary px-4 py-2.5 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
              >
                @if (generating()) {
                  <span class="flex items-center justify-center gap-2">
                    <span class="h-4 w-4 animate-spin rounded-full border-2 border-primary-foreground border-t-transparent"></span>
                    Generating quiz...
                  </span>
                } @else {
                  Generate Quiz
                }
              </button>
            </div>
          }
        }

        @case ('taking') {
          @if (currentQuiz()) {
            <div class="mx-auto max-w-2xl space-y-6">
              <div class="flex items-center justify-between">
                <h1 class="text-xl font-bold text-foreground">{{ currentQuiz()!.title }}</h1>
                <div class="flex items-center gap-3">
                  <span class="text-sm text-muted-foreground">
                    {{ currentQuestionIndex() + 1 }}/{{ currentQuiz()!.questions.length }}
                  </span>
                  <span class="rounded-md bg-muted px-3 py-1 text-sm font-medium text-foreground">
                    {{ formattedTimer() }}
                  </span>
                </div>
              </div>

              <!-- Progress bar -->
              <div class="h-1.5 w-full overflow-hidden rounded-full bg-muted">
                <div
                  class="h-full rounded-full bg-primary transition-all"
                  [style.width.%]="((currentQuestionIndex() + 1) / currentQuiz()!.questions.length) * 100"
                ></div>
              </div>

              @if (currentQuestion(); as q) {
                <div class="rounded-lg border border-border bg-card p-6">
                  <p class="text-lg font-medium text-foreground">{{ q.questionText }}</p>

                  <div class="mt-6 space-y-3">
                    @switch (q.type) {
                      @case ('MultipleChoice') {
                        @if (q.options) {
                          @for (option of q.options; track option; let i = $index) {
                            <button
                              (click)="selectAnswer(option)"
                              class="flex w-full items-center gap-3 rounded-md border p-3 text-left text-sm transition-colors"
                              [class]="answers()[q.id] === option
                                ? 'border-primary bg-primary/10 text-foreground'
                                : 'border-border text-muted-foreground hover:border-primary/50 hover:bg-accent'"
                            >
                              <span
                                class="flex h-6 w-6 items-center justify-center rounded-full border text-xs font-medium"
                                [class]="answers()[q.id] === option
                                  ? 'border-primary bg-primary text-primary-foreground'
                                  : 'border-border'"
                              >
                                {{ ['A','B','C','D'][i] }}
                              </span>
                              {{ option }}
                            </button>
                          }
                        }
                      }
                      @case ('TrueFalse') {
                        <div class="flex gap-3">
                          @for (option of ['True', 'False']; track option) {
                            <button
                              (click)="selectAnswer(option)"
                              class="flex-1 rounded-md border p-3 text-center text-sm font-medium transition-colors"
                              [class]="answers()[q.id] === option
                                ? 'border-primary bg-primary/10 text-foreground'
                                : 'border-border text-muted-foreground hover:border-primary/50 hover:bg-accent'"
                            >
                              {{ option }}
                            </button>
                          }
                        </div>
                      }
                      @case ('FillInTheBlank') {
                        <input
                          type="text"
                          [value]="answers()[q.id] || ''"
                          (input)="onInputAnswer($event)"
                          placeholder="Type your answer..."
                          class="w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                        />
                      }
                      @default {
                        <textarea
                          [value]="answers()[q.id] || ''"
                          (input)="onInputAnswer($event)"
                          placeholder="Write your answer..."
                          rows="4"
                          class="w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                        ></textarea>
                      }
                    }
                  </div>
                </div>

                <div class="flex justify-between">
                  <button
                    (click)="prevQuestion()"
                    [disabled]="currentQuestionIndex() === 0"
                    class="rounded-md border border-border px-4 py-2 text-sm font-medium text-muted-foreground hover:bg-accent disabled:opacity-50"
                  >
                    Previous
                  </button>
                  @if (currentQuestionIndex() < currentQuiz()!.questions.length - 1) {
                    <button
                      (click)="nextQuestion()"
                      class="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
                    >
                      Next
                    </button>
                  } @else {
                    <button
                      (click)="submitQuiz()"
                      [disabled]="submitting()"
                      class="rounded-md bg-green-600 px-6 py-2 text-sm font-medium text-white hover:bg-green-700 disabled:opacity-50"
                    >
                      @if (submitting()) {
                        Submitting...
                      } @else {
                        Submit Quiz
                      }
                    </button>
                  }
                </div>
              }
            </div>
          }
        }

        @case ('results') {
          @if (attemptResult()) {
            <div class="mx-auto max-w-2xl space-y-6">
              <div class="flex items-center gap-3">
                <button
                  (click)="backToList()"
                  class="rounded-md p-2 text-muted-foreground hover:bg-accent"
                  aria-label="Back to quizzes"
                >
                  <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5 3 12m0 0 7.5-7.5M3 12h18" />
                  </svg>
                </button>
                <h1 class="text-2xl font-bold text-foreground">Quiz Results</h1>
              </div>

              <!-- Score card -->
              <div class="rounded-lg border border-border bg-card p-6 text-center">
                <div
                  class="mx-auto flex h-24 w-24 items-center justify-center rounded-full"
                  [class]="scorePercentage() >= 70 ? 'bg-green-500/10' : scorePercentage() >= 40 ? 'bg-orange-500/10' : 'bg-destructive/10'"
                >
                  <span
                    class="text-3xl font-bold"
                    [class]="scorePercentage() >= 70 ? 'text-green-500' : scorePercentage() >= 40 ? 'text-orange-500' : 'text-destructive'"
                  >
                    {{ scorePercentage() | number:'1.0-0' }}%
                  </span>
                </div>
                <p class="mt-3 text-lg font-semibold text-foreground">
                  {{ attemptResult()!.score }}/{{ attemptResult()!.totalQuestions }} correct
                </p>
                <p class="mt-1 text-sm text-muted-foreground">
                  @if (scorePercentage() >= 90) {
                    Excellent work!
                  } @else if (scorePercentage() >= 70) {
                    Good job!
                  } @else if (scorePercentage() >= 40) {
                    Keep studying!
                  } @else {
                    Review the material and try again.
                  }
                </p>
              </div>

              <!-- Answer review -->
              <div class="space-y-4">
                <h2 class="text-lg font-semibold text-foreground">Review</h2>
                @for (answer of attemptResult()!.answers; track answer.questionId; let i = $index) {
                  <div
                    class="rounded-lg border p-4"
                    [class]="answer.isCorrect ? 'border-green-500/30 bg-green-500/5' : 'border-destructive/30 bg-destructive/5'"
                  >
                    <div class="flex items-start gap-3">
                      <span
                        class="mt-0.5 flex h-6 w-6 items-center justify-center rounded-full text-xs font-medium text-white"
                        [class]="answer.isCorrect ? 'bg-green-500' : 'bg-destructive'"
                      >
                        @if (answer.isCorrect) {
                          <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="m4.5 12.75 6 6 9-13.5" /></svg>
                        } @else {
                          <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M6 18 18 6M6 6l12 12" /></svg>
                        }
                      </span>
                      <div class="flex-1">
                        <p class="font-medium text-foreground">{{ answer.questionText }}</p>
                        <p class="mt-1 text-sm">
                          <span class="text-muted-foreground">Your answer: </span>
                          <span [class]="answer.isCorrect ? 'text-green-500' : 'text-destructive'">{{ answer.userAnswer || '(no answer)' }}</span>
                        </p>
                        @if (!answer.isCorrect) {
                          <p class="mt-1 text-sm">
                            <span class="text-muted-foreground">Correct answer: </span>
                            <span class="text-green-500">{{ answer.correctAnswer }}</span>
                          </p>
                        }
                        @if (answer.explanation) {
                          <p class="mt-2 text-sm text-muted-foreground">{{ answer.explanation }}</p>
                        }
                      </div>
                    </div>
                  </div>
                }
              </div>

              <div class="flex gap-3">
                <button
                  (click)="retakeQuiz()"
                  class="flex-1 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
                >
                  Retake Quiz
                </button>
                <button
                  (click)="backToList()"
                  class="flex-1 rounded-md border border-border px-4 py-2 text-sm font-medium text-muted-foreground hover:bg-accent"
                >
                  Back to Quizzes
                </button>
              </div>
            </div>
          }
        }
      }
    </div>
  `,
})
export class QuizzesComponent implements OnInit {
  private readonly quizService = inject(QuizService);
  private readonly pinnedService = inject(PinnedContentService);

  readonly view = signal<View>('list');
  readonly loading = signal(true);
  readonly generating = signal(false);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);

  readonly quizzes = signal<Quiz[]>([]);
  readonly pinnedItems = signal<PinnedContent[]>([]);
  readonly loadingPinned = signal(false);

  // Generate form
  selectedContentId = '';
  questionCount = 10;
  topicInput = '';
  durationMinutes = 10;

  // Quiz taking
  readonly currentQuiz = signal<QuizDetail | null>(null);
  readonly currentQuestionIndex = signal(0);
  readonly answers = signal<Record<string, string>>({});
  readonly timerSeconds = signal(0);
  private timerInterval: ReturnType<typeof setInterval> | null = null;

  // Results
  readonly attemptResult = signal<QuizAttemptDetail | null>(null);

  readonly indexedItems = computed(() => this.pinnedItems().filter((p) => p.isIndexed));

  readonly currentQuestion = computed(() => {
    const quiz = this.currentQuiz();
    if (!quiz) return null;
    return quiz.questions[this.currentQuestionIndex()] ?? null;
  });

  readonly formattedTimer = computed(() => {
    const total = this.timerSeconds();
    const minutes = Math.floor(total / 60);
    const seconds = total % 60;
    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
  });

  readonly scorePercentage = computed(() => {
    const result = this.attemptResult();
    if (!result || result.totalQuestions === 0) return 0;
    return (result.score / result.totalQuestions) * 100;
  });

  ngOnInit(): void {
    this.loadQuizzes();
  }

  loadQuizzes(): void {
    this.loading.set(true);
    this.quizService.getQuizzes().subscribe({
      next: (quizzes) => {
        this.quizzes.set(quizzes);
        this.loading.set(false);
      },
      error: (err) => {
        const message = typeof err.error === 'string' ? err.error : err.message ?? 'Failed to load quizzes.';
        this.error.set(message);
        this.loading.set(false);
      },
    });
  }

  showGenerate(): void {
    this.view.set('generate');
    this.error.set(null);
    if (this.pinnedItems().length === 0) {
      this.loadingPinned.set(true);
      this.pinnedService.getPinnedContents().subscribe({
        next: (items) => {
          this.pinnedItems.set(items);
          this.loadingPinned.set(false);
        },
        error: () => {
          this.loadingPinned.set(false);
        },
      });
    }
  }

  generateQuiz(): void {
    if (!this.selectedContentId) return;
    this.generating.set(true);
    this.error.set(null);

    const topic = this.topicInput.trim() || undefined;
    const durationSeconds = this.durationMinutes * 60;

    this.quizService.generateQuiz(this.selectedContentId, this.questionCount, topic, durationSeconds).subscribe({
      next: (quiz) => {
        this.generating.set(false);
        this.currentQuiz.set(quiz);
        this.currentQuestionIndex.set(0);
        this.answers.set({});
        this.startTimer(quiz.durationSeconds);
        this.view.set('taking');
      },
      error: (err) => {
        const message = typeof err.error === 'string' ? err.error : err.message ?? 'Failed to generate quiz.';
        this.error.set(message);
        this.generating.set(false);
      },
    });
  }

  startQuiz(quizId: string): void {
    this.loading.set(true);
    this.quizService.getQuizDetail(quizId).subscribe({
      next: (quiz) => {
        this.currentQuiz.set(quiz);
        this.currentQuestionIndex.set(0);
        this.answers.set({});
        this.startTimer(quiz.durationSeconds);
        this.view.set('taking');
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      },
    });
  }

  selectAnswer(answer: string): void {
    const q = this.currentQuestion();
    if (!q) return;
    this.answers.update((prev) => ({ ...prev, [q.id]: answer }));
  }

  onInputAnswer(event: Event): void {
    const target = event.target as HTMLInputElement | HTMLTextAreaElement;
    this.selectAnswer(target.value);
  }

  nextQuestion(): void {
    const quiz = this.currentQuiz();
    if (!quiz) return;
    if (this.currentQuestionIndex() < quiz.questions.length - 1) {
      this.currentQuestionIndex.update((i) => i + 1);
    }
  }

  prevQuestion(): void {
    if (this.currentQuestionIndex() > 0) {
      this.currentQuestionIndex.update((i) => i - 1);
    }
  }

  submitQuiz(): void {
    const quiz = this.currentQuiz();
    if (!quiz) return;

    this.submitting.set(true);
    this.stopTimer();

    const elapsed = quiz.durationSeconds - this.timerSeconds();
    const answerList = quiz.questions.map((q) => ({
      questionId: q.id,
      answer: this.answers()[q.id] ?? '',
    }));

    this.quizService.submitAttempt(quiz.id, answerList, elapsed).subscribe({
      next: (result) => {
        this.attemptResult.set(result);
        this.submitting.set(false);
        this.view.set('results');
        this.loadQuizzes(); // Refresh list in background
      },
      error: (err) => {
        const message = typeof err.error === 'string' ? err.error : err.message ?? 'Failed to submit quiz.';
        this.error.set(message);
        this.submitting.set(false);
      },
    });
  }

  viewResults(quizId: string): void {
    this.quizService.getQuizResults(quizId).subscribe({
      next: (attempts) => {
        if (attempts.length > 0) {
          this.quizService.getAttemptDetail(attempts[0].id).subscribe({
            next: (detail) => {
              this.attemptResult.set(detail);
              this.view.set('results');
            },
          });
        }
      },
    });
  }

  retakeQuiz(): void {
    const result = this.attemptResult();
    if (result) {
      this.startQuiz(result.quizId);
    }
  }

  backToList(): void {
    this.stopTimer();
    this.view.set('list');
    this.loadQuizzes();
  }

  private startTimer(totalSeconds: number): void {
    this.stopTimer();
    this.timerSeconds.set(totalSeconds);
    this.timerInterval = setInterval(() => {
      this.timerSeconds.update((s) => {
        if (s <= 0) {
          this.submitQuiz();
          return 0;
        }
        return s - 1;
      });
    }, 1000);
  }

  private stopTimer(): void {
    if (this.timerInterval) {
      clearInterval(this.timerInterval);
      this.timerInterval = null;
    }
  }
}
