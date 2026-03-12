import { ChangeDetectionStrategy, Component, inject, signal, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { PinnedContentsService } from '../../api/api/pinnedContents.service';
import { PinnedContentDto } from '../../api/model/pinnedContentDto';

@Component({
  selector: 'app-pinned',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, HlmButton, ...HlmCardImports],
  template: `
    <div class="space-y-6">
      <div class="flex items-center justify-between">
        <h1 class="text-2xl font-bold text-foreground">Pinned Items</h1>
        <span class="text-sm text-muted-foreground">{{ items().length }} item{{ items().length === 1 ? '' : 's' }}</span>
      </div>

      @if (loading()) {
        <div class="flex items-center justify-center py-12">
          <div class="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" role="status">
            <span class="sr-only">Loading pinned items...</span>
          </div>
          <span class="ml-3 text-muted-foreground" aria-hidden="true">Loading pinned items...</span>
        </div>
      } @else if (error()) {
        <div class="rounded-lg border border-destructive/50 bg-destructive/10 p-6" role="alert">
          <p class="text-sm text-destructive">{{ error() }}</p>
        </div>
      } @else if (items().length === 0) {
        <div hlmCard class="p-8 text-center">
          <svg class="mx-auto h-12 w-12 text-muted-foreground" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" aria-hidden="true">
            <path stroke-linecap="round" stroke-linejoin="round" d="M17.593 3.322c1.1.128 1.907 1.077 1.907 2.185V21L12 17.25 4.5 21V5.507c0-1.108.806-2.057 1.907-2.185a48.507 48.507 0 0 1 11.186 0Z" />
          </svg>
          <h2 class="mt-4 text-lg font-semibold text-foreground">No pinned items</h2>
          <p class="mt-2 text-sm text-muted-foreground">
            Pin content from your courses to save it for quick access and AI features.
          </p>
        </div>
      } @else {
        <div class="space-y-3">
          @for (item of items(); track item.id) {
            <div hlmCard class="flex items-center gap-4 p-4 transition-colors hover:bg-accent">
              <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded bg-muted text-xs font-medium text-muted-foreground uppercase" aria-hidden="true">
                {{ (item.contentType ?? '').slice(0, 3) }}
              </div>

              <div class="min-w-0 flex-1">
                <p class="font-medium text-foreground">{{ item.contentTitle }}</p>
                @if (item.notes) {
                  <p class="text-xs text-muted-foreground truncate">{{ item.notes }}</p>
                }
                <p class="text-xs text-muted-foreground">
                  Pinned {{ item.pinnedAt | date:'mediumDate' }}
                </p>
              </div>

              <div class="flex items-center gap-2">
                <button
                  hlmBtn
                  variant="ghost"
                  size="icon"
                  (click)="toggleStar(item)"
                  [attr.aria-label]="item.isStarred ? 'Unstar' : 'Star'"
                >
                  <svg class="h-5 w-5" [class]="item.isStarred ? 'fill-yellow-400 text-yellow-400' : 'text-muted-foreground'" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" fill="none" aria-hidden="true">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M11.48 3.499a.562.562 0 0 1 1.04 0l2.125 5.111a.563.563 0 0 0 .475.345l5.518.442c.499.04.701.663.321.988l-4.204 3.602a.563.563 0 0 0-.182.557l1.285 5.385a.562.562 0 0 1-.84.61l-4.725-2.885a.562.562 0 0 0-.586 0L6.982 20.54a.562.562 0 0 1-.84-.61l1.285-5.386a.562.562 0 0 0-.182-.557l-4.204-3.602a.562.562 0 0 1 .321-.988l5.518-.442a.563.563 0 0 0 .475-.345L11.48 3.5Z" />
                  </svg>
                </button>
                <button
                  hlmBtn
                  variant="ghost"
                  size="icon"
                  (click)="unpin(item)"
                  aria-label="Unpin"
                  class="text-muted-foreground hover:text-destructive"
                >
                  <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" aria-hidden="true">
                    <path stroke-linecap="round" stroke-linejoin="round" d="m14.74 9-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 0 1-2.244 2.077H8.084a2.25 2.25 0 0 1-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 0 0-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 0 1 3.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 0 0-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 0 0-7.5 0" />
                  </svg>
                </button>
              </div>
            </div>
          }
        </div>
      }
    </div>
  `,
})
export class PinnedComponent implements OnInit {
  private readonly pinnedService = inject(PinnedContentsService);

  readonly items = signal<PinnedContentDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.loadPinned();
  }

  private loadPinned(): void {
    this.loading.set(true);
    this.pinnedService.apiPinnedGet().subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: (err) => {
        const message = typeof err.error === 'string' ? err.error : err.message ?? 'Failed to load pinned items.';
        this.error.set(message);
        this.loading.set(false);
      },
    });
  }

  toggleStar(item: PinnedContentDto): void {
    this.pinnedService.apiPinnedContentIdStarPost(item.contentId!).subscribe({
      next: () => {
        this.items.update((items) =>
          items.map((i) => (i.id === item.id ? { ...i, isStarred: !i.isStarred } : i))
        );
      },
    });
  }

  unpin(item: PinnedContentDto): void {
    this.pinnedService.apiPinnedContentIdDelete(item.contentId!).subscribe({
      next: () => {
        this.items.update((items) => items.filter((i) => i.id !== item.id));
      },
    });
  }
}
