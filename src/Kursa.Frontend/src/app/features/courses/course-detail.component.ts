import { ChangeDetectionStrategy, Component, inject, input, signal, OnInit } from '@angular/core';
import { MoodleCourseSection, MoodleService } from '../../core/services/moodle.service';
import { DecimalPipe } from '@angular/common';

@Component({
  selector: 'app-course-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe],
  template: `
    <div class="space-y-6">
      <div class="flex items-center gap-3">
        <a href="/courses" class="text-muted-foreground hover:text-foreground">
          <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" d="M15.75 19.5 8.25 12l7.5-7.5" />
          </svg>
        </a>
        <h1 class="text-2xl font-bold text-foreground">Course Content</h1>
      </div>

      @if (loading()) {
        <div class="flex items-center justify-center py-12">
          <div class="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent"></div>
          <span class="ml-3 text-muted-foreground">Loading course content...</span>
        </div>
      } @else if (error()) {
        <div class="rounded-lg border border-destructive/50 bg-destructive/10 p-6">
          <h2 class="text-lg font-semibold text-destructive">Unable to load content</h2>
          <p class="mt-2 text-sm text-muted-foreground">{{ error() }}</p>
        </div>
      } @else {
        @for (section of sections(); track section.id) {
          <div class="rounded-lg border border-border bg-card">
            <div class="border-b border-border p-4">
              <h2 class="font-semibold text-foreground">{{ section.name }}</h2>
              @if (section.summary) {
                <p class="mt-1 text-sm text-muted-foreground" [innerHTML]="section.summary"></p>
              }
            </div>

            @if (section.modules.length > 0) {
              <ul class="divide-y divide-border">
                @for (mod of section.modules; track mod.id) {
                  <li class="flex items-center gap-3 p-4 hover:bg-accent">
                    <div class="flex h-8 w-8 items-center justify-center rounded bg-muted text-xs font-medium text-muted-foreground uppercase">
                      {{ mod.modName.slice(0, 3) }}
                    </div>
                    <div class="min-w-0 flex-1">
                      <p class="font-medium text-foreground">{{ mod.name }}</p>
                      @if (mod.contents && mod.contents.length > 0) {
                        <p class="text-xs text-muted-foreground">
                          {{ mod.contents.length }} file{{ mod.contents.length === 1 ? '' : 's' }}
                          @if (totalFileSize(mod.contents) > 0) {
                            · {{ totalFileSize(mod.contents) | number:'1.0-1' }} KB
                          }
                        </p>
                      }
                    </div>
                    @if (mod.url) {
                      <a
                        [href]="mod.url"
                        target="_blank"
                        rel="noopener noreferrer"
                        class="text-sm text-primary hover:underline"
                      >
                        Open
                      </a>
                    }
                  </li>
                }
              </ul>
            } @else {
              <p class="p-4 text-sm text-muted-foreground">No activities in this section.</p>
            }
          </div>
        }

        @if (sections().length === 0) {
          <div class="rounded-lg border border-border bg-card p-8 text-center">
            <p class="text-muted-foreground">No content found for this course.</p>
          </div>
        }
      }
    </div>
  `,
})
export class CourseDetailComponent implements OnInit {
  private readonly moodleService = inject(MoodleService);

  readonly courseId = input.required<number>();

  readonly sections = signal<MoodleCourseSection[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.moodleService.getCourseContent(this.courseId()).subscribe({
      next: (sections) => {
        this.sections.set(sections);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err.error ?? 'Failed to load course content.');
        this.loading.set(false);
      },
    });
  }

  totalFileSize(contents: { fileSize: number }[]): number {
    return contents.reduce((sum, c) => sum + c.fileSize, 0) / 1024;
  }
}
