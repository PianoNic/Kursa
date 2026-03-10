import { ChangeDetectionStrategy, Component, output } from '@angular/core';

@Component({
  selector: 'app-topbar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="sticky top-0 z-20 flex h-14 items-center border-b border-border bg-card/80 px-4 backdrop-blur-sm">
      <button
        (click)="toggleSidebar.emit()"
        class="mr-4 rounded-md p-2 text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
        aria-label="Toggle sidebar"
      >
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="h-5 w-5" aria-hidden="true"><line x1="4" x2="20" y1="12" y2="12" /><line x1="4" x2="20" y1="6" y2="6" /><line x1="4" x2="20" y1="18" y2="18" /></svg>
      </button>

      <div class="flex flex-1 items-center gap-4">
        <div class="relative max-w-md flex-1">
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" aria-hidden="true"><circle cx="11" cy="11" r="8" /><path d="m21 21-4.3-4.3" /></svg>
          <input
            type="search"
            placeholder="Search courses, content..."
            class="w-full rounded-md border border-input bg-background py-2 pl-10 pr-4 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
            aria-label="Search"
          />
        </div>
      </div>

      <div class="flex items-center gap-2">
        <button
          class="rounded-md p-2 text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
          aria-label="Notifications"
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="h-5 w-5" aria-hidden="true"><path d="M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9" /><path d="M10.3 21a1.94 1.94 0 0 0 3.4 0" /></svg>
        </button>

        <button
          class="flex h-8 w-8 items-center justify-center rounded-full bg-primary text-sm font-medium text-primary-foreground"
          aria-label="User menu"
        >
          U
        </button>
      </div>
    </header>
  `,
})
export class TopbarComponent {
  toggleSidebar = output<void>();
}
