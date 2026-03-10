import { ChangeDetectionStrategy, Component, inject, signal, OnInit } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MoodleCourse, MoodleService } from '../../core/services/moodle.service';

@Component({
  selector: 'app-courses',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DecimalPipe],
  template: `
    <div class="space-y-6">
      <div class="flex items-center justify-between">
        <h1 class="text-2xl font-bold text-foreground">Courses</h1>
      </div>

      @if (loading()) {
        <div class="flex items-center justify-center py-12">
          <div class="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent"></div>
          <span class="ml-3 text-muted-foreground">Loading courses...</span>
        </div>
      } @else if (error()) {
        <div class="rounded-lg border border-destructive/50 bg-destructive/10 p-6">
          <h2 class="text-lg font-semibold text-destructive">Unable to load courses</h2>
          <p class="mt-2 text-sm text-muted-foreground">{{ error() }}</p>
          <button
            (click)="loadCourses()"
            class="mt-4 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
          >
            Retry
          </button>
        </div>
      } @else if (courses().length === 0) {
        <div class="rounded-lg border border-border bg-card p-8 text-center">
          <svg class="mx-auto h-12 w-12 text-muted-foreground" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" d="M12 6.042A8.967 8.967 0 0 0 6 3.75c-1.052 0-2.062.18-3 .512v14.25A8.987 8.987 0 0 1 6 18c2.305 0 4.408.867 6 2.292m0-14.25a8.966 8.966 0 0 1 6-2.292c1.052 0 2.062.18 3 .512v14.25A8.987 8.987 0 0 0 18 18a8.967 8.967 0 0 0-6 2.292m0-14.25v14.25" />
          </svg>
          <h2 class="mt-4 text-lg font-semibold text-foreground">No courses found</h2>
          <p class="mt-2 text-sm text-muted-foreground">
            Make sure your Moodle account is linked in Settings.
          </p>
        </div>
      } @else {
        <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          @for (course of courses(); track course.id) {
            <a
              [routerLink]="['/courses', course.id]"
              class="group rounded-lg border border-border bg-card p-6 transition-colors hover:border-primary/50 hover:bg-accent"
            >
              <h3 class="font-semibold text-foreground group-hover:text-primary">{{ course.fullName }}</h3>
              <p class="mt-1 text-sm text-muted-foreground">{{ course.shortName }}</p>
              @if (course.progress !== null && course.progress !== undefined) {
                <div class="mt-4">
                  <div class="flex items-center justify-between text-xs text-muted-foreground">
                    <span>Progress</span>
                    <span>{{ course.progress | number:'1.0-0' }}%</span>
                  </div>
                  <div class="mt-1 h-2 w-full overflow-hidden rounded-full bg-muted">
                    <div
                      class="h-full rounded-full bg-primary transition-all"
                      [style.width.%]="course.progress"
                    ></div>
                  </div>
                </div>
              }
            </a>
          }
        </div>
      }
    </div>
  `,
})
export class CoursesComponent implements OnInit {
  private readonly moodleService = inject(MoodleService);

  readonly courses = signal<MoodleCourse[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.loadCourses();
  }

  loadCourses(): void {
    this.loading.set(true);
    this.error.set(null);

    this.moodleService.getEnrolledCourses().subscribe({
      next: (courses) => {
        this.courses.set(courses);
        this.loading.set(false);
      },
      error: (err) => {
        const message = typeof err.error === 'string' ? err.error : err.message ?? 'Failed to load courses. Please check your Moodle connection.';
        this.error.set(message);
        this.loading.set(false);
      },
    });
  }
}
