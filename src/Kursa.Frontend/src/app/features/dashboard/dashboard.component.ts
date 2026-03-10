import { ChangeDetectionStrategy, Component, inject, signal, computed, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import {
  Analytics,
  AnalyticsService,
  StudyActivity,
} from '../../core/services/analytics.service';

@Component({
  selector: 'app-dashboard',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, RouterLink],
  template: `
    <div class="space-y-6">
      <div class="flex items-center justify-between">
        <h1 class="text-2xl font-bold text-foreground">Dashboard</h1>
        @if (data(); as d) {
          @if (d.currentStreak > 0) {
            <div class="flex items-center gap-2 rounded-md bg-orange-500/10 px-3 py-1.5">
              <svg class="h-5 w-5 text-orange-500" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M15.362 5.214A8.252 8.252 0 0 1 12 21 8.25 8.25 0 0 1 6.038 7.047 8.287 8.287 0 0 0 9 9.601a8.983 8.983 0 0 1 3.361-6.867 8.21 8.21 0 0 0 3 2.48Z" /><path stroke-linecap="round" stroke-linejoin="round" d="M12 18a3.75 3.75 0 0 0 .495-7.468 5.99 5.99 0 0 0-1.925 3.547 5.975 5.975 0 0 1-2.133-1.001A3.75 3.75 0 0 0 12 18Z" /></svg>
              <span class="text-sm font-semibold text-orange-500">{{ d.currentStreak }} day streak</span>
            </div>
          }
        }
      </div>

      @if (loading()) {
        <div class="flex items-center justify-center py-12">
          <div class="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent"></div>
        </div>
      } @else if (data(); as d) {
        <!-- Stat cards -->
        <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          <div class="rounded-lg border border-border bg-card p-5">
            <div class="flex items-center gap-2">
              <svg class="h-4 w-4 text-primary" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" /></svg>
              <span class="text-sm font-medium text-muted-foreground">Study Time</span>
            </div>
            <p class="mt-2 text-2xl font-bold text-foreground">{{ formatDuration(d.overview.totalStudyTimeSeconds) }}</p>
            <p class="mt-1 text-xs text-muted-foreground">{{ d.overview.totalPomodoros }} pomodoros</p>
          </div>
          <div class="rounded-lg border border-border bg-card p-5">
            <div class="flex items-center gap-2">
              <svg class="h-4 w-4 text-primary" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M9.879 7.519c1.171-1.025 3.071-1.025 4.242 0 1.172 1.025 1.172 2.687 0 3.712-.203.179-.43.326-.67.442-.745.361-1.45.999-1.45 1.827v.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Zm-9 5.25h.008v.008H12v-.008Z" /></svg>
              <span class="text-sm font-medium text-muted-foreground">Quizzes</span>
            </div>
            <p class="mt-2 text-2xl font-bold text-foreground">{{ d.overview.totalQuizzesTaken }}</p>
            <p class="mt-1 text-xs text-muted-foreground">attempts taken</p>
          </div>
          <div class="rounded-lg border border-border bg-card p-5">
            <div class="flex items-center gap-2">
              <svg class="h-4 w-4 text-primary" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M6.429 9.75 2.25 12l4.179 2.25m0-4.5 5.571 3 5.571-3m-11.142 0L2.25 7.5 12 2.25l9.75 5.25-4.179 2.25m0 0L21.75 12l-4.179 2.25m0 0 4.179 2.25L12 21.75 2.25 16.5l4.179-2.25m11.142 0-5.571 3-5.571-3" /></svg>
              <span class="text-sm font-medium text-muted-foreground">Flashcards</span>
            </div>
            <p class="mt-2 text-2xl font-bold text-foreground">{{ d.overview.totalCardsReviewed }}</p>
            <p class="mt-1 text-xs text-muted-foreground">cards reviewed</p>
          </div>
          <div class="rounded-lg border border-border bg-card p-5">
            <div class="flex items-center gap-2">
              <svg class="h-4 w-4 text-primary" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M17.593 3.322c1.1.128 1.907 1.077 1.907 2.185V21L12 17.25 4.5 21V5.507c0-1.108.806-2.057 1.907-2.185a48.507 48.507 0 0 1 11.186 0Z" /></svg>
              <span class="text-sm font-medium text-muted-foreground">Pinned</span>
            </div>
            <p class="mt-2 text-2xl font-bold text-foreground">{{ d.overview.totalPinnedContents }}</p>
            <p class="mt-1 text-xs text-muted-foreground">materials saved</p>
          </div>
        </div>

        <div class="grid gap-6 lg:grid-cols-2">
          <!-- Weekly Activity Chart -->
          <div class="rounded-lg border border-border bg-card p-5">
            <h2 class="text-sm font-semibold text-foreground">Weekly Activity</h2>
            <div class="mt-4 flex items-end gap-2" style="height: 120px">
              @for (day of d.weeklyActivity; track day.date) {
                <div class="flex flex-1 flex-col items-center gap-1">
                  <div
                    class="w-full rounded-sm bg-primary transition-all"
                    [style.height.px]="getBarHeight(day, d.weeklyActivity)"
                    [style.min-height.px]="day.studyTimeSeconds > 0 ? 4 : 0"
                  ></div>
                  <span class="text-xs text-muted-foreground">{{ getDayLabel(day.date) }}</span>
                </div>
              }
            </div>
          </div>

          <!-- Flashcard Overview -->
          <div class="rounded-lg border border-border bg-card p-5">
            <div class="flex items-center justify-between">
              <h2 class="text-sm font-semibold text-foreground">Flashcard Status</h2>
              @if (d.flashcardStats.dueToday > 0) {
                <a
                  routerLink="/flashcards"
                  class="text-xs font-medium text-primary hover:underline"
                >
                  Review {{ d.flashcardStats.dueToday }} due
                </a>
              }
            </div>
            @if (d.flashcardStats.totalCards === 0) {
              <p class="mt-4 text-sm text-muted-foreground">No flashcards yet. Generate some from your pinned content.</p>
            } @else {
              <div class="mt-4 space-y-3">
                <div>
                  <div class="flex justify-between text-xs text-muted-foreground">
                    <span>Mastered</span>
                    <span>{{ d.flashcardStats.masteredCards }}/{{ d.flashcardStats.totalCards }}</span>
                  </div>
                  <div class="mt-1 h-2 w-full overflow-hidden rounded-full bg-muted">
                    <div
                      class="h-full rounded-full bg-green-500 transition-all"
                      [style.width.%]="(d.flashcardStats.masteredCards / d.flashcardStats.totalCards) * 100"
                    ></div>
                  </div>
                </div>
                <div>
                  <div class="flex justify-between text-xs text-muted-foreground">
                    <span>Learning</span>
                    <span>{{ d.flashcardStats.learningCards }}</span>
                  </div>
                  <div class="mt-1 h-2 w-full overflow-hidden rounded-full bg-muted">
                    <div
                      class="h-full rounded-full bg-primary transition-all"
                      [style.width.%]="(d.flashcardStats.learningCards / d.flashcardStats.totalCards) * 100"
                    ></div>
                  </div>
                </div>
                <div class="flex items-center justify-between text-xs">
                  <span class="text-muted-foreground">Due today</span>
                  <span
                    class="font-medium"
                    [class]="d.flashcardStats.dueToday > 0 ? 'text-orange-500' : 'text-green-500'"
                  >
                    {{ d.flashcardStats.dueToday }}
                  </span>
                </div>
              </div>
            }
          </div>

          <!-- Recent Quizzes -->
          <div class="rounded-lg border border-border bg-card p-5">
            <div class="flex items-center justify-between">
              <h2 class="text-sm font-semibold text-foreground">Recent Quiz Results</h2>
              <a routerLink="/quizzes" class="text-xs font-medium text-primary hover:underline">View all</a>
            </div>
            @if (d.recentQuizPerformance.length === 0) {
              <p class="mt-4 text-sm text-muted-foreground">No quizzes taken yet.</p>
            } @else {
              <div class="mt-3 space-y-2">
                @for (quiz of d.recentQuizPerformance.slice(0, 5); track quiz.quizId) {
                  <div class="flex items-center justify-between rounded-md p-2 hover:bg-accent">
                    <div class="min-w-0 flex-1">
                      <p class="truncate text-sm text-foreground">{{ quiz.quizTitle }}</p>
                      <p class="text-xs text-muted-foreground">{{ quiz.completedAt | date:'shortDate' }}</p>
                    </div>
                    <span
                      class="ml-3 text-sm font-semibold"
                      [class]="(quiz.score / quiz.totalQuestions) >= 0.7 ? 'text-green-500' : 'text-orange-500'"
                    >
                      {{ quiz.score }}/{{ quiz.totalQuestions }}
                    </span>
                  </div>
                }
              </div>
            }
          </div>

          <!-- Streaks & Quick Actions -->
          <div class="space-y-4">
            <div class="rounded-lg border border-border bg-card p-5">
              <h2 class="text-sm font-semibold text-foreground">Streaks</h2>
              <div class="mt-3 grid grid-cols-2 gap-4">
                <div class="text-center">
                  <p class="text-3xl font-bold text-orange-500">{{ d.currentStreak }}</p>
                  <p class="text-xs text-muted-foreground">Current</p>
                </div>
                <div class="text-center">
                  <p class="text-3xl font-bold text-foreground">{{ d.longestStreak }}</p>
                  <p class="text-xs text-muted-foreground">Longest</p>
                </div>
              </div>
            </div>

            <div class="rounded-lg border border-border bg-card p-5">
              <h2 class="text-sm font-semibold text-foreground">Quick Actions</h2>
              <div class="mt-3 grid grid-cols-2 gap-2">
                <a
                  routerLink="/study"
                  class="rounded-md border border-border p-3 text-center text-xs font-medium text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
                >
                  Start Study Session
                </a>
                <a
                  routerLink="/quizzes"
                  class="rounded-md border border-border p-3 text-center text-xs font-medium text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
                >
                  Take a Quiz
                </a>
                <a
                  routerLink="/flashcards"
                  class="rounded-md border border-border p-3 text-center text-xs font-medium text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
                >
                  Review Flashcards
                </a>
                <a
                  routerLink="/courses"
                  class="rounded-md border border-border p-3 text-center text-xs font-medium text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
                >
                  Browse Courses
                </a>
              </div>
            </div>
          </div>
        </div>
      } @else {
        <div class="rounded-lg border border-border bg-card p-6">
          <h2 class="text-lg font-semibold text-foreground">Welcome to Kursa</h2>
          <p class="mt-2 text-muted-foreground">
            Connect your Moodle account in Settings to start browsing courses.
          </p>
        </div>
      }
    </div>
  `,
})
export class DashboardComponent implements OnInit {
  private readonly analyticsService = inject(AnalyticsService);

  readonly loading = signal(true);
  readonly data = signal<Analytics | null>(null);

  ngOnInit(): void {
    this.analyticsService.getAnalytics().subscribe({
      next: (data) => {
        this.data.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  formatDuration(seconds: number): string {
    const h = Math.floor(seconds / 3600);
    const m = Math.floor((seconds % 3600) / 60);
    if (h > 0) return `${h}h ${m}m`;
    if (m > 0) return `${m}m`;
    return '0m';
  }

  getBarHeight(day: StudyActivity, allDays: StudyActivity[]): number {
    const maxTime = Math.max(...allDays.map((d) => d.studyTimeSeconds), 1);
    return (day.studyTimeSeconds / maxTime) * 100;
  }

  getDayLabel(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleDateString('en', { weekday: 'short' });
  }
}
