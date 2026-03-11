import { ChangeDetectionStrategy, Component, signal, inject, OnInit } from '@angular/core';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { ForumService, ForumView, DiscussionView } from '../../core/services/forum.service';
import { MoodleService, MoodleCourse } from '../../core/services/moodle.service';

@Component({
  selector: 'app-forums',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [HlmButton, ...HlmCardImports],
  template: `
    <div class="mx-auto max-w-7xl space-y-6 p-6">
      <div>
        <h1 class="text-2xl font-bold text-foreground">Forums</h1>
        <p class="text-sm text-muted-foreground">Browse and follow course discussions</p>
      </div>

      @if (loading()) {
        <div class="flex items-center justify-center py-12">
          <div class="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" role="status">
            <span class="sr-only">Loading...</span>
          </div>
        </div>
      } @else if (error()) {
        <div class="rounded-lg border border-destructive/50 bg-destructive/10 p-4 text-sm text-destructive" role="alert">
          {{ error() }}
        </div>
      } @else if (activeView() === 'courses') {
        <!-- Course Selection -->
        @if (courses().length === 0) {
          <div hlmCard class="p-8 text-center text-muted-foreground">
            No courses found. Link your Moodle account first.
          </div>
        } @else {
          <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            @for (course of courses(); track course.id) {
              <button
                hlmBtn
                variant="outline"
                type="button"
                class="h-auto flex-col items-start p-4 text-left"
                (click)="selectCourse(course)"
              >
                <h3 class="font-medium text-foreground">{{ course.shortName }}</h3>
                <p class="mt-1 line-clamp-2 text-sm text-muted-foreground">{{ course.fullName }}</p>
              </button>
            }
          </div>
        }
      } @else if (activeView() === 'forums') {
        <!-- Forum List -->
        <div class="flex items-center gap-2">
          <button
            hlmBtn
            variant="ghost"
            size="icon"
            type="button"
            (click)="backToCourses()"
            aria-label="Back to courses"
          >
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="h-5 w-5" aria-hidden="true"><path d="m15 18-6-6 6-6" /></svg>
          </button>
          <h2 class="text-lg font-semibold text-foreground">{{ selectedCourseName() }}</h2>
        </div>

        @if (forums().length === 0) {
          <div hlmCard class="p-8 text-center text-muted-foreground">
            No forums in this course.
          </div>
        } @else {
          <div class="space-y-3">
            @for (forum of forums(); track forum.id) {
              <button
                hlmBtn
                variant="outline"
                type="button"
                class="h-auto w-full flex-row items-start justify-between p-4 text-left"
                (click)="selectForum(forum)"
              >
                <div class="min-w-0 flex-1">
                  <h3 class="font-medium text-foreground">{{ forum.name }}</h3>
                  @if (forum.description) {
                    <p class="mt-1 line-clamp-2 text-sm text-muted-foreground" [innerHTML]="forum.description"></p>
                  }
                </div>
                <div class="shrink-0 text-right">
                  <span class="rounded-full bg-muted px-2 py-0.5 text-xs font-medium text-muted-foreground">
                    {{ forum.discussionCount }} discussions
                  </span>
                </div>
              </button>
            }
          </div>
        }
      } @else {
        <!-- Discussion List -->
        <div class="flex items-center gap-2">
          <button
            hlmBtn
            variant="ghost"
            size="icon"
            type="button"
            (click)="backToForums()"
            aria-label="Back to forums"
          >
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="h-5 w-5" aria-hidden="true"><path d="m15 18-6-6 6-6" /></svg>
          </button>
          <h2 class="text-lg font-semibold text-foreground">{{ selectedForumName() }}</h2>
        </div>

        @if (discussionsLoading()) {
          <div class="flex items-center justify-center py-8">
            <div class="h-6 w-6 animate-spin rounded-full border-4 border-primary border-t-transparent" role="status">
              <span class="sr-only">Loading discussions...</span>
            </div>
          </div>
        } @else if (discussions().length === 0) {
          <div hlmCard class="p-8 text-center text-muted-foreground">
            No discussions yet in this forum.
          </div>
        } @else {
          <div class="space-y-3">
            @for (discussion of discussions(); track discussion.id) {
              <div hlmCard class="p-4">
                <div class="flex items-start gap-3">
                  @if (discussion.authorAvatar) {
                    <img
                      [src]="discussion.authorAvatar"
                      [alt]="discussion.author"
                      class="h-10 w-10 rounded-full"
                    />
                  } @else {
                    <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-primary/10 text-sm font-medium text-primary" aria-hidden="true">
                      {{ getInitials(discussion.author) }}
                    </div>
                  }
                  <div class="min-w-0 flex-1">
                    <div class="flex items-center gap-2">
                      <h3 class="font-medium text-foreground">{{ discussion.title }}</h3>
                      @if (discussion.isPinned) {
                        <span class="rounded-full bg-primary/10 px-2 py-0.5 text-xs font-medium text-primary">Pinned</span>
                      }
                    </div>
                    <div class="mt-1 flex items-center gap-3 text-xs text-muted-foreground">
                      <span>{{ discussion.author }}</span>
                      <span>{{ formatDate(discussion.createdAt) }}</span>
                      <span>{{ discussion.replyCount }} replies</span>
                    </div>
                    @if (discussion.message) {
                      <div class="mt-2 line-clamp-3 text-sm text-muted-foreground" [innerHTML]="discussion.message"></div>
                    }
                  </div>
                </div>
              </div>
            }
          </div>
        }
      }
    </div>
  `,
})
export class ForumsComponent implements OnInit {
  private readonly forumService = inject(ForumService);
  private readonly moodleService = inject(MoodleService);

  readonly courses = signal<MoodleCourse[]>([]);
  readonly forums = signal<ForumView[]>([]);
  readonly discussions = signal<DiscussionView[]>([]);
  readonly loading = signal(true);
  readonly discussionsLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly activeView = signal<'courses' | 'forums' | 'discussions'>('courses');
  readonly selectedCourseName = signal('');
  readonly selectedForumName = signal('');

  private selectedCourseId = 0;

  ngOnInit(): void {
    this.loadCourses();
  }

  selectCourse(course: MoodleCourse): void {
    this.selectedCourseId = course.id;
    this.selectedCourseName.set(course.shortName);
    this.loading.set(true);
    this.activeView.set('forums');

    this.forumService.getForums(course.id).subscribe({
      next: (forums) => {
        this.forums.set(forums);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load forums.');
        this.loading.set(false);
      },
    });
  }

  selectForum(forum: ForumView): void {
    this.selectedForumName.set(forum.name);
    this.discussionsLoading.set(true);
    this.activeView.set('discussions');

    this.forumService.getDiscussions(forum.id).subscribe({
      next: (discussions) => {
        this.discussions.set(discussions);
        this.discussionsLoading.set(false);
      },
      error: () => {
        this.error.set('Failed to load discussions.');
        this.discussionsLoading.set(false);
      },
    });
  }

  backToCourses(): void {
    this.activeView.set('courses');
    this.forums.set([]);
    this.error.set(null);
  }

  backToForums(): void {
    this.activeView.set('forums');
    this.discussions.set([]);
    this.error.set(null);
  }

  getInitials(name: string): string {
    return name
      .split(' ')
      .map(p => p[0])
      .join('')
      .substring(0, 2)
      .toUpperCase();
  }

  formatDate(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleDateString('default', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
  }

  private loadCourses(): void {
    this.loading.set(true);
    this.error.set(null);

    this.moodleService.getEnrolledCourses().subscribe({
      next: (courses) => {
        this.courses.set(courses);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load courses. Make sure your Moodle account is linked.');
        this.loading.set(false);
      },
    });
  }
}
