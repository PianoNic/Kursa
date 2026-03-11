import { ChangeDetectionStrategy, Component, signal, computed, inject, OnInit } from '@angular/core';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { GradeService, CourseGradeSummary } from '../../core/services/grade.service';

@Component({
  selector: 'app-grades',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [...HlmCardImports],
  template: `
    <div class="mx-auto max-w-7xl space-y-6 p-6">
      <div>
        <h1 class="text-2xl font-bold text-foreground">Grades</h1>
        <p class="text-sm text-muted-foreground">View your grades and track progress across courses</p>
      </div>

      @if (loading()) {
        <div class="flex items-center justify-center py-12">
          <div class="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" role="status">
            <span class="sr-only">Loading grades...</span>
          </div>
        </div>
      } @else if (error()) {
        <div class="rounded-lg border border-destructive/50 bg-destructive/10 p-4 text-sm text-destructive" role="alert">
          {{ error() }}
        </div>
      } @else if (courses().length === 0) {
        <div hlmCard class="p-8 text-center text-muted-foreground">
          No grades available yet.
        </div>
      } @else {
        <!-- Overview Cards -->
        <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <div hlmCard class="p-4 gap-1">
            <p class="text-xs font-medium text-muted-foreground">Courses</p>
            <p class="mt-1 text-2xl font-bold text-foreground">{{ courses().length }}</p>
          </div>
          <div hlmCard class="p-4 gap-1">
            <p class="text-xs font-medium text-muted-foreground">Graded Items</p>
            <p class="mt-1 text-2xl font-bold text-foreground">{{ totalGraded() }}</p>
          </div>
          <div hlmCard class="p-4 gap-1">
            <p class="text-xs font-medium text-muted-foreground">Average</p>
            <p class="mt-1 text-2xl font-bold text-foreground">{{ averageGrade() }}</p>
          </div>
          <div hlmCard class="p-4 gap-1">
            <p class="text-xs font-medium text-muted-foreground">Highest</p>
            <p class="mt-1 text-2xl font-bold text-primary">{{ highestGrade() }}</p>
          </div>
        </div>

        <!-- Course Grade Cards -->
        <div class="space-y-4">
          @for (course of courses(); track course.courseId) {
            <div hlmCard class="p-0 gap-0 overflow-hidden">
              <!-- Course Header -->
              <button
                type="button"
                class="flex w-full items-center justify-between p-4 text-left transition-colors hover:bg-accent/50"
                (click)="toggleCourse(course.courseId)"
                [attr.aria-expanded]="expandedCourses().has(course.courseId)"
              >
                <div class="min-w-0 flex-1">
                  <h2 class="truncate font-semibold text-foreground">{{ course.courseName }}</h2>
                  <p class="text-sm text-muted-foreground">
                    {{ course.gradedItemCount }} of {{ course.totalItemCount }} items graded
                  </p>
                </div>
                <div class="ml-4 flex items-center gap-4">
                  @if (course.percentage) {
                    <div class="text-right">
                      <p class="text-lg font-bold" [class]="getGradeColor(course.courseTotalRaw, course.courseTotalMax)">
                        {{ course.percentage }}
                      </p>
                      @if (course.courseTotal) {
                        <p class="text-xs text-muted-foreground">{{ course.courseTotal }}</p>
                      }
                    </div>
                  }
                  <svg
                    xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none"
                    stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"
                    class="h-5 w-5 text-muted-foreground transition-transform"
                    [class.rotate-180]="expandedCourses().has(course.courseId)"
                    aria-hidden="true"
                  >
                    <path d="m6 9 6 6 6-6" />
                  </svg>
                </div>
              </button>

              <!-- Grade Items (expandable) -->
              @if (expandedCourses().has(course.courseId)) {
                <div class="border-t border-border">
                  @if (course.items.length === 0) {
                    <p class="p-4 text-sm text-muted-foreground">No grade items in this course.</p>
                  } @else {
                    <div class="overflow-x-auto">
                      <table class="w-full text-sm" role="table">
                        <thead>
                          <tr class="border-b border-border text-left">
                            <th class="p-3 font-medium text-muted-foreground">Item</th>
                            <th class="p-3 font-medium text-muted-foreground">Type</th>
                            <th class="p-3 text-right font-medium text-muted-foreground">Grade</th>
                            <th class="p-3 text-right font-medium text-muted-foreground">Percentage</th>
                            <th class="p-3 text-right font-medium text-muted-foreground">Weight</th>
                          </tr>
                        </thead>
                        <tbody>
                          @for (item of course.items; track item.id) {
                            <tr class="border-b border-border/50 last:border-b-0">
                              <td class="p-3">
                                <div class="flex items-center gap-2">
                                  <span class="inline-flex h-6 w-6 items-center justify-center rounded text-xs" [class]="getModuleBadgeClass(item.itemModule)">
                                    {{ getModuleIcon(item.itemModule) }}
                                  </span>
                                  <span class="text-foreground">{{ item.itemName ?? 'Unnamed item' }}</span>
                                </div>
                              </td>
                              <td class="p-3 text-muted-foreground capitalize">{{ item.itemModule ?? item.itemType }}</td>
                              <td class="p-3 text-right">
                                @if (item.gradeFormatted) {
                                  <span [class]="getGradeColor(item.grade, item.gradeMax)">
                                    {{ item.gradeFormatted }}
                                  </span>
                                } @else {
                                  <span class="text-muted-foreground">—</span>
                                }
                              </td>
                              <td class="p-3 text-right">
                                @if (item.percentage) {
                                  <span [class]="getGradeColor(item.grade, item.gradeMax)">{{ item.percentage }}</span>
                                } @else {
                                  <span class="text-muted-foreground">—</span>
                                }
                              </td>
                              <td class="p-3 text-right text-muted-foreground">
                                {{ item.weight ?? '—' }}
                              </td>
                            </tr>
                          }
                        </tbody>
                      </table>
                    </div>
                  }

                  <!-- Grade Trend Bar -->
                  @if (getGradedItems(course).length > 1) {
                    <div class="border-t border-border p-4">
                      <h3 class="mb-3 text-xs font-medium text-muted-foreground">Grade Trend</h3>
                      <div class="flex items-end gap-1" style="height: 80px" role="img" [attr.aria-label]="'Grade trend for ' + course.courseName">
                        @for (item of getGradedItems(course); track item.id) {
                          <div
                            class="flex-1 rounded-t transition-colors"
                            [class]="getBarColor(item.grade, item.gradeMax)"
                            [style.height.%]="getBarHeight(item.grade, item.gradeMax)"
                            [title]="(item.itemName ?? 'Item') + ': ' + (item.percentage ?? 'N/A')"
                          ></div>
                        }
                      </div>
                    </div>
                  }
                </div>
              }
            </div>
          }
        </div>
      }
    </div>
  `,
})
export class GradesComponent implements OnInit {
  private readonly gradeService = inject(GradeService);

  readonly courses = signal<CourseGradeSummary[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly expandedCourses = signal<Set<number>>(new Set());

  readonly totalGraded = computed(() =>
    this.courses().reduce((sum, c) => sum + c.gradedItemCount, 0)
  );

  readonly averageGrade = computed(() => {
    const all = this.courses().filter(c => c.courseTotalRaw != null);
    if (all.length === 0) return '—';
    const avg = all.reduce((sum, c) => sum + ((c.courseTotalRaw ?? 0) / c.courseTotalMax) * 100, 0) / all.length;
    return `${avg.toFixed(1)}%`;
  });

  readonly highestGrade = computed(() => {
    const all = this.courses().filter(c => c.courseTotalRaw != null);
    if (all.length === 0) return '—';
    const max = Math.max(...all.map(c => ((c.courseTotalRaw ?? 0) / c.courseTotalMax) * 100));
    return `${max.toFixed(1)}%`;
  });

  ngOnInit(): void {
    this.loadGrades();
  }

  toggleCourse(courseId: number): void {
    this.expandedCourses.update(set => {
      const next = new Set(set);
      if (next.has(courseId)) {
        next.delete(courseId);
      } else {
        next.add(courseId);
      }
      return next;
    });
  }

  getGradeColor(grade: number | null | undefined, max: number): string {
    if (grade == null) return 'text-muted-foreground';
    const pct = (grade / max) * 100;
    if (pct >= 80) return 'text-green-500';
    if (pct >= 60) return 'text-yellow-500';
    if (pct >= 40) return 'text-orange-500';
    return 'text-destructive';
  }

  getModuleBadgeClass(module: string | null): string {
    switch (module) {
      case 'assign': return 'bg-blue-500/10 text-blue-500';
      case 'quiz': return 'bg-purple-500/10 text-purple-500';
      case 'forum': return 'bg-green-500/10 text-green-500';
      default: return 'bg-muted text-muted-foreground';
    }
  }

  getModuleIcon(module: string | null): string {
    switch (module) {
      case 'assign': return 'A';
      case 'quiz': return 'Q';
      case 'forum': return 'F';
      default: return 'G';
    }
  }

  getGradedItems(course: CourseGradeSummary): CourseGradeSummary['items'] {
    return course.items.filter(i => i.grade != null);
  }

  getBarHeight(grade: number | null | undefined, max: number): number {
    if (grade == null || max === 0) return 0;
    return Math.max(5, (grade / max) * 100);
  }

  getBarColor(grade: number | null | undefined, max: number): string {
    if (grade == null) return 'bg-muted';
    const pct = (grade / max) * 100;
    if (pct >= 80) return 'bg-green-500';
    if (pct >= 60) return 'bg-yellow-500';
    if (pct >= 40) return 'bg-orange-500';
    return 'bg-destructive';
  }

  private loadGrades(): void {
    this.loading.set(true);
    this.error.set(null);

    this.gradeService.getGrades().subscribe({
      next: (courses) => {
        this.courses.set(courses);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load grades. Make sure your Moodle account is linked.');
        this.loading.set(false);
      },
    });
  }
}
