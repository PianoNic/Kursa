import { ChangeDetectionStrategy, Component, inject, input, signal, OnInit, OnDestroy } from '@angular/core';
import { MoodleService } from '../../api/api/moodle.service';
import { PinnedContentsService } from '../../api/api/pinnedContents.service';
import { MoodleCourseDto } from '../../api/model/moodleCourseDto';
import { MoodleCourseSectionDto } from '../../api/model/moodleCourseSectionDto';
import { MoodleModuleDto } from '../../api/model/moodleModuleDto';
import { MoodleContentDto } from '../../api/model/moodleContentDto';
import { PinMoodleModuleRequest } from '../../api/model/pinMoodleModuleRequest';
import { AiContextService } from '../../core/services/ai-context.service';
import { DecimalPipe } from '@angular/common';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';

@Component({
  selector: 'app-course-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe, HlmButton, ...HlmCardImports],
  template: `
    <div class="space-y-6">
      <div class="flex items-center gap-3">
        <a href="/courses" class="text-muted-foreground hover:text-foreground" aria-label="Back to courses">
          <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" aria-hidden="true">
            <path stroke-linecap="round" stroke-linejoin="round" d="M15.75 19.5 8.25 12l7.5-7.5" />
          </svg>
        </a>
        <h1 class="text-2xl font-bold text-foreground">Course Content</h1>
      </div>

      @if (loading()) {
        <div class="flex items-center justify-center py-12">
          <div class="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" aria-hidden="true"></div>
          <span class="ml-3 text-muted-foreground">Loading course content...</span>
        </div>
      } @else if (error()) {
        <div class="rounded-lg border border-destructive/50 bg-destructive/10 p-6" role="alert">
          <h2 class="text-lg font-semibold text-destructive">Unable to load content</h2>
          <p class="mt-2 text-sm text-muted-foreground">{{ error() }}</p>
        </div>
      } @else {
        @for (section of sections(); track section.id) {
          @if (section.visible !== 0 && section.modules && section.modules.length > 0) {
            <div hlmCard>
              <div hlmCardHeader>
                <h2 hlmCardTitle>{{ section.name }}</h2>
                @if (section.summary) {
                  <p hlmCardDescription [innerHTML]="section.summary"></p>
                }
              </div>

              <div hlmCardContent>
                <ul class="divide-y divide-border -mx-6" role="list">
                  @for (mod of section.modules; track mod.id) {
                    @if (mod.visible !== 0) {
                      <li class="flex items-start gap-3 px-6 py-4 hover:bg-accent/50 transition-colors">
                        <!-- Module type icon -->
                        <div
                          class="mt-0.5 flex h-9 w-9 shrink-0 items-center justify-center rounded-lg"
                          [class]="modIconBg(mod.modName)"
                          aria-hidden="true"
                        >
                          <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" [attr.d]="modIconPath(mod.modName)" />
                          </svg>
                        </div>

                        <!-- Name + meta -->
                        <div class="min-w-0 flex-1">
                          <p class="font-medium text-foreground">{{ mod.name }}</p>
                          <p class="mt-0.5 text-xs text-muted-foreground capitalize">
                            {{ modLabel(mod.modName) }}
                            @if (mod.contents && mod.contents.length > 0) {
                              &middot; {{ mod.contents.length }} file{{ mod.contents.length === 1 ? '' : 's' }}
                              @if (totalFileSize(mod.contents) > 0) {
                                &middot; {{ totalFileSize(mod.contents) | number:'1.0-1' }} KB
                              }
                            }
                          </p>
                          @if (mod.description) {
                            <p class="mt-1 text-xs text-muted-foreground line-clamp-2" [innerHTML]="mod.description"></p>
                          }
                        </div>

                        <!-- Actions -->
                        <div class="flex shrink-0 items-center gap-2">
                          @if (isQuiz(mod.modName) && mod.url) {
                            <a
                              [href]="mod.url"
                              target="_blank"
                              rel="noopener noreferrer"
                              hlmBtn
                              size="sm"
                              class="text-xs"
                            >
                              Start quiz
                            </a>
                          } @else if (isAssignment(mod.modName) && mod.url) {
                            <a
                              [href]="mod.url"
                              target="_blank"
                              rel="noopener noreferrer"
                              class="rounded-md bg-amber-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-amber-700 transition-colors"
                            >
                              View task
                            </a>
                          } @else if (hasFiles(mod) && primaryFileUrl(mod)) {
                            <a
                              [href]="primaryFileUrl(mod)!"
                              target="_blank"
                              rel="noopener noreferrer"
                              hlmBtn
                              variant="outline"
                              size="sm"
                              class="text-xs"
                            >
                              Open
                            </a>
                          } @else if (mod.url) {
                            <a
                              [href]="mod.url"
                              target="_blank"
                              rel="noopener noreferrer"
                              hlmBtn
                              variant="outline"
                              size="sm"
                              class="text-xs"
                            >
                              Open
                            </a>
                          }
                          <!-- Pin for AI button -->
                          <button
                            hlmBtn
                            variant="ghost"
                            size="icon"
                            (click)="pinModule(mod)"
                            [disabled]="pinningModuleId() === mod.id!"
                            [title]="pinnedModuleIds().has(mod.id!) ? 'Pinned for AI' : 'Pin for AI'"
                            [class]="pinnedModuleIds().has(mod.id!) ? 'text-emerald-400' : 'text-muted-foreground'"
                            aria-label="Pin module for AI search"
                          >
                            @if (pinningModuleId() === mod.id!) {
                              <svg class="h-4 w-4 animate-spin" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
                              </svg>
                            } @else if (pinnedModuleIds().has(mod.id!)) {
                              <svg class="h-4 w-4" fill="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                                <path d="M12 2l2.4 7.4H22l-6.2 4.5 2.4 7.4L12 17l-6.2 4.3 2.4-7.4L2 9.4h7.6z"/>
                              </svg>
                            } @else {
                              <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" aria-hidden="true">
                                <path stroke-linecap="round" stroke-linejoin="round" d="M11.48 3.499a.562.562 0 0 1 1.04 0l2.125 5.111a.563.563 0 0 0 .475.345l5.518.442c.499.04.701.663.321.988l-4.204 3.602a.563.563 0 0 0-.182.557l1.285 5.385a.562.562 0 0 1-.84.61l-4.725-3.275a.562.562 0 0 0-.652 0L4.63 20.04a.562.562 0 0 1-.84-.61l1.285-5.386a.562.562 0 0 0-.182-.557L.476 9.996a.563.563 0 0 1 .321-.988l5.518-.442a.563.563 0 0 0 .475-.345L11.48 3.5Z"/>
                              </svg>
                            }
                          </button>
                        </div>
                      </li>
                    }
                  }
                </ul>
              </div>
            </div>
          }
        }

        @if (sections().length === 0) {
          <div hlmCard class="p-8 text-center">
            <p class="text-muted-foreground">No content found for this course.</p>
          </div>
        }
      }
    </div>
  `,
})
export class CourseDetailComponent implements OnInit, OnDestroy {
  private readonly moodleService = inject(MoodleService);
  private readonly pinnedContentsService = inject(PinnedContentsService);
  private readonly aiContext = inject(AiContextService);

  readonly courseId = input.required<number>();
  readonly sections = signal<MoodleCourseSectionDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly course = signal<MoodleCourseDto | null>(null);
  readonly pinnedModuleIds = signal<Set<number>>(new Set());
  readonly pinningModuleId = signal<number | null>(null);

  ngOnInit(): void {
    this.moodleService.apiMoodleCoursesCourseIdContentGet(this.courseId()).subscribe({
      next: (sections) => {
        this.sections.set(sections);
        this.loading.set(false);
      },
      error: (err) => {
        const detail = typeof err.error === 'string'
          ? err.error
          : (err.error?.detail ?? err.error?.title ?? err.message ?? 'Failed to load course content.');
        this.error.set(detail);
        this.loading.set(false);
      },
    });

    this.moodleService.apiMoodleCoursesGet().subscribe({
      next: (courses) => {
        const id = +this.courseId();
        const found = courses.find(c => c.id === id) ?? null;
        this.course.set(found);
        if (found) {
          this.aiContext.setContext({
            type: 'course',
            id: String(found.id),
            title: found.fullName ?? '',
            description: `Moodle course — ${found.shortName ?? ''}`,
          });
        }
      },
    });
  }

  ngOnDestroy(): void {
    this.aiContext.clearContext();
  }

  pinModule(mod: MoodleModuleDto): void {
    const c = this.course();
    if (!c || this.pinningModuleId() === mod.id) return;

    this.pinningModuleId.set(mod.id!);
    const request: PinMoodleModuleRequest = {
      moodleCourseId: c.id,
      courseName: c.fullName,
      courseShortName: c.shortName,
      moodleModuleId: mod.id,
      moduleName: mod.name,
      modType: mod.modName,
      description: mod.description,
      url: mod.url,
      fileUrl: mod.contents?.[0]?.fileUrl ?? null,
    };
    this.pinnedContentsService.apiPinnedMoodlePost(request).subscribe({
      next: () => {
        this.pinnedModuleIds.update(ids => new Set([...ids, mod.id!]));
        this.pinningModuleId.set(null);
      },
      error: () => {
        this.pinningModuleId.set(null);
      },
    });
  }

  totalFileSize(contents: MoodleContentDto[]): number {
    return contents.reduce((sum, c) => sum + (c.fileSize ?? 0), 0) / 1024;
  }

  isQuiz(modName: string | null | undefined): boolean {
    return modName === 'quiz';
  }

  isAssignment(modName: string | null | undefined): boolean {
    return modName === 'assign';
  }

  hasFiles(mod: MoodleModuleDto): boolean {
    return !!mod.contents && mod.contents.length > 0;
  }

  primaryFileUrl(mod: MoodleModuleDto): string | null {
    const raw = mod.contents?.find(c => c.fileUrl)?.fileUrl ?? null;
    return raw ? this.proxyFileUrl(raw) : null;
  }

  /** Routes Moodle pluginfile.php URLs through the backend proxy to inject the auth token. */
  proxyFileUrl(url: string): string {
    if (url.includes('pluginfile.php')) {
      return `/api/moodle/file?url=${encodeURIComponent(url)}`;
    }
    return url;
  }

  modLabel(modName: string | null | undefined): string {
    const labels: Record<string, string> = {
      resource: 'File',
      url: 'Link',
      page: 'Page',
      quiz: 'Quiz',
      assign: 'Assignment',
      forum: 'Forum',
      folder: 'Folder',
      label: 'Label',
      book: 'Book',
      glossary: 'Glossary',
      choice: 'Choice',
      feedback: 'Feedback',
      survey: 'Survey',
      wiki: 'Wiki',
      workshop: 'Workshop',
      scorm: 'SCORM',
      lti: 'External tool',
    };
    return labels[modName ?? ''] ?? modName ?? '';
  }

  modIconBg(modName: string | null | undefined): string {
    const map: Record<string, string> = {
      quiz: 'bg-violet-500/15 text-violet-400',
      assign: 'bg-amber-500/15 text-amber-400',
      resource: 'bg-blue-500/15 text-blue-400',
      url: 'bg-cyan-500/15 text-cyan-400',
      page: 'bg-emerald-500/15 text-emerald-400',
      forum: 'bg-orange-500/15 text-orange-400',
      folder: 'bg-yellow-500/15 text-yellow-400',
      book: 'bg-indigo-500/15 text-indigo-400',
    };
    return map[modName ?? ''] ?? 'bg-muted text-muted-foreground';
  }

  modIconPath(modName: string | null | undefined): string {
    switch (modName) {
      case 'quiz':
        return 'M9.879 7.519c1.171-1.025 3.071-1.025 4.242 0 1.172 1.025 1.172 2.687 0 3.712-.203.179-.43.326-.67.442-.745.361-1.45.999-1.45 1.827v.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Zm-9 5.25h.008v.008H12v-.008Z';
      case 'assign':
        return 'M9 12.75 11.25 15 15 9.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z';
      case 'resource':
        return 'M19.5 14.25v-2.625a3.375 3.375 0 0 0-3.375-3.375h-1.5A1.125 1.125 0 0 1 13.5 7.125v-1.5a3.375 3.375 0 0 0-3.375-3.375H8.25m2.25 0H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 0 0-9-9Z';
      case 'url':
        return 'M13.19 8.688a4.5 4.5 0 0 1 1.242 7.244l-4.5 4.5a4.5 4.5 0 0 1-6.364-6.364l1.757-1.757m13.35-.622 1.757-1.757a4.5 4.5 0 0 0-6.364-6.364l-4.5 4.5a4.5 4.5 0 0 0 1.242 7.244';
      case 'page':
        return 'M12 7.5h1.5m-1.5 3h1.5m-7.5 3h7.5m-7.5 3h7.5m3-9h3.375c.621 0 1.125.504 1.125 1.125V18a2.25 2.25 0 0 1-2.25 2.25M16.5 7.5V18a2.25 2.25 0 0 0 2.25 2.25M16.5 7.5V4.875c0-.621-.504-1.125-1.125-1.125H4.125C3.504 3.75 3 4.254 3 4.875V18a2.25 2.25 0 0 0 2.25 2.25h13.5M6 7.5h3v3H6v-3Z';
      case 'forum':
        return 'M20.25 8.511c.884.284 1.5 1.128 1.5 2.097v4.286c0 1.136-.847 2.1-1.98 2.193-.34.027-.68.052-1.02.072v3.091l-3-3c-1.354 0-2.694-.055-4.02-.163a2.115 2.115 0 0 1-.825-.242m9.345-8.334a2.126 2.126 0 0 0-.476-.095 48.64 48.64 0 0 0-8.048 0c-1.131.094-1.976 1.057-1.976 2.192v4.286c0 .837.46 1.58 1.155 1.951m9.345-8.334V6.637c0-1.621-1.152-3.026-2.76-3.235A48.455 48.455 0 0 0 11.25 3c-2.115 0-4.198.137-6.24.402-1.608.209-2.76 1.614-2.76 3.235v6.226c0 1.621 1.152 3.026 2.76 3.235.577.075 1.157.14 1.74.194V21l4.155-4.155';
      case 'folder':
        return 'M2.25 12.75V12A2.25 2.25 0 0 1 4.5 9.75h15A2.25 2.25 0 0 1 21.75 12v.75m-8.69-6.44-2.12-2.12a1.5 1.5 0 0 0-1.061-.44H4.5A2.25 2.25 0 0 0 2.25 6v12a2.25 2.25 0 0 0 2.25 2.25h15A2.25 2.25 0 0 0 21.75 18V9a2.25 2.25 0 0 0-2.25-2.25h-5.379a1.5 1.5 0 0 1-1.06-.44Z';
      case 'book':
        return 'M12 6.042A8.967 8.967 0 0 0 6 3.75c-1.052 0-2.062.18-3 .512v14.25A8.987 8.987 0 0 1 6 18c2.305 0 4.408.867 6 2.292m0-14.25a8.966 8.966 0 0 1 6-2.292c1.052 0 2.062.18 3 .512v14.25A8.987 8.987 0 0 0 18 18a8.967 8.967 0 0 0-6 2.292m0-14.25v14.25';
      default:
        return 'M4.26 10.147a60.438 60.438 0 0 0-.491 6.347A48.63 48.63 0 0 1 12 20.904a48.63 48.63 0 0 1 8.232-4.41 60.46 60.46 0 0 0-.491-6.347m-15.482 0a50.636 50.636 0 0 0-2.658-.813A59.906 59.906 0 0 1 12 3.493a59.903 59.903 0 0 1 10.399 5.84c-.896.248-1.783.52-2.658.814m-15.482 0A50.717 50.717 0 0 1 12 13.489a50.702 50.702 0 0 1 3.741-3.342M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z';
    }
  }
}
