import { ChangeDetectionStrategy, Component, inject, signal, computed, OnInit } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmLabel } from '@spartan-ng/helm/label';
import { HlmTextarea } from '@spartan-ng/helm/textarea';
import { QuizzesService } from '../../api/api/quizzes.service';
import { QuizDto } from '../../api/model/quizDto';
import { QuizDetailDto } from '../../api/model/quizDetailDto';
import { QuizAttemptDetailDto } from '../../api/model/quizAttemptDetailDto';
import { PinnedContentsService } from '../../api/api/pinnedContents.service';
import { PinnedContentDto } from '../../api/model/pinnedContentDto';

type View = 'list' | 'generate' | 'taking' | 'results';

@Component({
  selector: 'app-quizzes',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, DecimalPipe, FormsModule, HlmButton, ...HlmCardImports, HlmInput, HlmLabel, HlmTextarea],
  template: `
    <div class="space-y-6">
      @switch (view()) {
        @case ('list') {
          <div class="flex items-center justify-between">
            <h1 class="text-2xl font-bold text-foreground">Quizzes</h1>
            <button hlmBtn (click)="showGenerate()">Generate Quiz</button>
          </div>

          @if (loading()) {
            <div class="flex items-center justify-center py-12">
              <div class="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent"></div>
              <span class="ml-3 text-muted-foreground">Loading quizzes...</span>
            </div>
          } @else if (quizzes().length === 0) {
            <div hlmCard class="p-8 text-center">
              <svg class="mx-auto h-12 w-12 text-muted-foreground" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" d="M9.879 7.519c1.171-1.025 3.071-1.025 4.242 0 1.172 1.025 1.172 2.687 0 3.712-.203.179-.43.326-.67.442-.745.361-1.45.999-1.45 1.827v.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Zm-9 5.25h.008v.008H12v-.008Z" />
              </svg>
              <h2 class="mt-4 text-lg font-semibold text-foreground">No quizzes yet</h2>
              <p class="mt-2 text-sm text-muted-foreground">
                Generate a quiz from your pinned course materials to test your knowledge.
              </p>
              <button hlmBtn (click)="showGenerate()" class="mt-4">Generate Your First Quiz</button>
            </div>
          } @else {
            <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
              @for (quiz of quizzes(); track quiz.id) {
                <div hlmCard class="p-5">
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
                        <span class="font-medium" [class]="(quiz.bestScore ?? 0) >= (quiz.questionCount ?? 1) * 0.7 ? 'text-green-500' : 'text-orange-500'">
                          {{ quiz.bestScore }}/{{ quiz.questionCount }}
                        </span>
                      </div>
                      <div class="mt-1 h-1.5 w-full overflow-hidden rounded-full bg-muted">
                        <div
                          class="h-full rounded-full transition-all"
                          [class]="(quiz.bestScore ?? 0) >= (quiz.questionCount ?? 1) * 0.7 ? 'bg-green-500' : 'bg-orange-500'"
                          [style.width.%]="((quiz.bestScore ?? 0) / (quiz.questionCount ?? 1)) * 100"
                        ></div>
                      </div>
                    </div>
                  }
                  <div class="mt-4 flex gap-2">
                    <button hlmBtn (click)="startQuiz(quiz.id!)" class="flex-1 text-xs">Take Quiz</button>
                    <button
                      hlmBtn
                      variant="outline"
                      (click)="viewResults(quiz.id!)"
                      [disabled]="quiz.attemptCount === 0"
                      class="text-xs"
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
            <button hlmBtn variant="ghost" size="icon" (click)="view.set('list')" aria-label="Back to quizzes">
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
            <div hlmCard class="p-8 text-center">
              <h2 class="mt-4 text-lg font-semibold text-foreground">No pinned content</h2>
              <p class="mt-2 text-sm text-muted-foreground">
                Pin and index some course materials first to generate quizzes.
              </p>
            </div>
          } @else {
            <div hlmCard class="mx-auto max-w-lg p-6">
              <div class="space-y-4">
                <div>
                  <label hlmLabel for="content-select">Select Material</label>
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
                  <label hlmLabel for="question-count">Number of Questions</label>
                  <input
                    hlmInput
                    id="question-count"
                    type="number"
                    [(ngModel)]="questionCount"
                    min="1"
                    max="50"
                    class="mt-1 w-full"
                  />
                </div>
                <div>
                  <label hlmLabel for="topic">Topic (optional)</label>
                  <input
                    hlmInput
                    id="topic"
                    type="text"
                    [(ngModel)]="topicInput"
                    placeholder="Focus on a specific topic..."
                    class="mt-1 w-full"
                  />
                </div>
                <div>
                  <label hlmLabel for="duration">Time Limit (minutes)</label>
                  <input
                    hlmInput
                    id="duration"
                    type="number"
                    [(ngModel)]="durationMinutes"
                    min="1"
                    max="120"
                    class="mt-1 w-full"
                  />
                </div>

                @if (error()) {
                  <div class="rounded-md border border-destructive/50 bg-destructive/10 p-3">
                    <p class="text-sm text-destructive">{{ error() }}</p>
                  </div>
                }

                <button
                  hlmBtn
                  (click)="generateQuiz()"
                  [disabled]="generating() || !selectedContentId"
                  class="w-full"
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
                    {{ currentQuestionIndex() + 1 }}/{{ (currentQuiz()!.questions ?? []).length }}
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
                  [style.width.%]="((currentQuestionIndex() + 1) / (currentQuiz()!.questions ?? []).length) * 100"
                ></div>
              </div>

              @if (currentQuestion(); as q) {
                <div hlmCard class="p-6">
                  <p class="text-lg font-medium text-foreground">{{ q.questionText }}</p>

                  <div class="mt-6 space-y-3">
                    @switch (q.type) {
                      @case (0) {
                        @if (q.options) {
                          @for (option of q.options; track option; let i = $index) {
                            <button
                              (click)="selectAnswer(option)"
                              class="flex w-full items-center gap-3 rounded-md border p-3 text-left text-sm transition-colors"
                              [class]="answers()[q.id!] === option
                                ? 'border-primary bg-primary/10 text-foreground'
                                : 'border-border text-muted-foreground hover:border-primary/50 hover:bg-accent'"
                            >
                              <span
                                class="flex h-6 w-6 items-center justify-center rounded-full border text-xs font-medium"
                                [class]="answers()[q.id!] === option
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
                      @case (1) {
                        <div class="flex gap-3">
                          @for (option of ['True', 'False']; track option) {
                            <button
                              (click)="selectAnswer(option)"
                              class="flex-1 rounded-md border p-3 text-center text-sm font-medium transition-colors"
                              [class]="answers()[q.id!] === option
                                ? 'border-primary bg-primary/10 text-foreground'
                                : 'border-border text-muted-foreground hover:border-primary/50 hover:bg-accent'"
                            >
                              {{ option }}
                            </button>
                          }
                        </div>
                      }
                      @case (2) {
                        <input
                          hlmInput
                          type="text"
                          [value]="answers()[q.id!] || ''"
                          (input)="onInputAnswer($event)"
                          placeholder="Type your answer..."
                          class="w-full"
                        />
                      }
                      @default {
                        <textarea
                          hlmTextarea
                          [value]="answers()[q.id!] || ''"
                          (input)="onInputAnswer($event)"
                          placeholder="Write your answer..."
                          rows="4"
                          class="w-full"
                        ></textarea>
                      }
                    }
                  </div>
                </div>

                <div class="flex justify-between">
                  <button
                    hlmBtn
                    variant="outline"
                    (click)="prevQuestion()"
                    [disabled]="currentQuestionIndex() === 0"
                  >
                    Previous
                  </button>
                  @if (currentQuestionIndex() < (currentQuiz()!.questions ?? []).length - 1) {
                    <button hlmBtn (click)="nextQuestion()">Next</button>
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
                <button hlmBtn variant="ghost" size="icon" (click)="backToList()" aria-label="Back to quizzes">
                  <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5 3 12m0 0 7.5-7.5M3 12h18" />
                  </svg>
                </button>
                <h1 class="text-2xl font-bold text-foreground">Quiz Results</h1>
              </div>

              <!-- Score card -->
              <div hlmCard class="p-6 text-center">
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
                <button hlmBtn (click)="retakeQuiz()" class="flex-1">Retake Quiz</button>
                <button hlmBtn variant="outline" (click)="backToList()" class="flex-1">Back to Quizzes</button>
              </div>
            </div>
          }
        }
      }
    </div>
  `,
})
export class QuizzesComponent implements OnInit {
  private readonly quizService = inject(QuizzesService);
  private readonly pinnedService = inject(PinnedContentsService);

  readonly view = signal<View>('list');
  readonly loading = signal(true);
  readonly generating = signal(false);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);

  readonly quizzes = signal<QuizDto[]>([]);
  readonly pinnedItems = signal<PinnedContentDto[]>([]);
  readonly loadingPinned = signal(false);

  // Generate form
  selectedContentId = '';
  questionCount = 10;
  topicInput = '';
  durationMinutes = 10;

  // Quiz taking
  readonly currentQuiz = signal<QuizDetailDto | null>(null);
  readonly currentQuestionIndex = signal(0);
  readonly answers = signal<Record<string, string>>({});
  readonly timerSeconds = signal(0);
  private timerInterval: ReturnType<typeof setInterval> | null = null;

  // Results
  readonly attemptResult = signal<QuizAttemptDetailDto | null>(null);

  readonly indexedItems = computed(() => this.pinnedItems().filter((p) => p.isIndexed));

  readonly currentQuestion = computed(() => {
    const quiz = this.currentQuiz();
    if (!quiz) return null;
    return (quiz.questions ?? [])[this.currentQuestionIndex()] ?? null;
  });

  readonly formattedTimer = computed(() => {
    const total = this.timerSeconds();
    const minutes = Math.floor(total / 60);
    const seconds = total % 60;
    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
  });

  readonly scorePercentage = computed(() => {
    const result = this.attemptResult();
    if (!result || !result.totalQuestions) return 0;
    return ((result.score ?? 0) / result.totalQuestions) * 100;
  });

  ngOnInit(): void {
    this.loadQuizzes();
  }

  loadQuizzes(): void {
    this.loading.set(true);
    this.quizService.apiQuizzesGet().subscribe({
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
      this.pinnedService.apiPinnedGet().subscribe({
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

    this.quizService.apiQuizzesGeneratePost({ contentId: this.selectedContentId, questionCount: this.questionCount, topic, durationSeconds }).subscribe({
      next: (quiz) => {
        this.generating.set(false);
        this.currentQuiz.set(quiz);
        this.currentQuestionIndex.set(0);
        this.answers.set({});
        this.startTimer(quiz.durationSeconds ?? 600);
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
    this.quizService.apiQuizzesQuizIdGet(quizId).subscribe({
      next: (quiz) => {
        this.currentQuiz.set(quiz);
        this.currentQuestionIndex.set(0);
        this.answers.set({});
        this.startTimer(quiz.durationSeconds ?? 600);
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
    this.answers.update((prev) => ({ ...prev, [q.id!]: answer }));
  }

  onInputAnswer(event: Event): void {
    const target = event.target as HTMLInputElement | HTMLTextAreaElement;
    this.selectAnswer(target.value);
  }

  nextQuestion(): void {
    const quiz = this.currentQuiz();
    if (!quiz) return;
    if (this.currentQuestionIndex() < (quiz.questions ?? []).length - 1) {
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

    const elapsed = (quiz.durationSeconds ?? 0) - this.timerSeconds();
    const answerList = (quiz.questions ?? []).map((q) => ({
      questionId: q.id,
      answer: this.answers()[q.id!] ?? '',
    }));

    this.quizService.apiQuizzesQuizIdSubmitPost(quiz.id!, { answers: answerList, durationSeconds: elapsed }).subscribe({
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
    this.quizService.apiQuizzesQuizIdResultsGet(quizId).subscribe({
      next: (attempts) => {
        if (attempts.length > 0) {
          this.quizService.apiQuizzesAttemptsAttemptIdGet(attempts[0].id!).subscribe({
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
      this.startQuiz(result.quizId!);
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
