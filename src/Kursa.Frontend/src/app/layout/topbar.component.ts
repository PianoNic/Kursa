import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  output,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmInput } from '@spartan-ng/helm/input';
import { AuthService } from '../core/services/auth.service';

@Component({
  selector: 'app-topbar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, HlmButton, HlmInput],
  template: `
    <header class="sticky top-0 z-20 flex h-14 items-center border-b border-border bg-card/80 px-4 backdrop-blur-sm">
      <button
        hlmBtn
        variant="ghost"
        size="icon"
        (click)="toggleSidebar.emit()"
        aria-label="Toggle sidebar"
      >
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="h-5 w-5" aria-hidden="true"><line x1="4" x2="20" y1="12" y2="12" /><line x1="4" x2="20" y1="6" y2="6" /><line x1="4" x2="20" y1="18" y2="18" /></svg>
      </button>

      <div class="flex flex-1 items-center gap-4 ml-2">
        <div class="relative max-w-md flex-1">
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" aria-hidden="true"><circle cx="11" cy="11" r="8" /><path d="m21 21-4.3-4.3" /></svg>
          <input
            hlmInput
            type="search"
            placeholder="Search courses, content..."
            class="w-full pl-10"
            aria-label="Search"
          />
        </div>
      </div>

      <div class="flex items-center gap-2">
        <button
          hlmBtn
          variant="ghost"
          size="icon"
          (click)="toggleAiPanel.emit()"
          aria-label="Toggle AI assistant"
        >
          <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" d="M9.813 15.904 9 18.75l-.813-2.846a4.5 4.5 0 0 0-3.09-3.09L2.25 12l2.846-.813a4.5 4.5 0 0 0 3.09-3.09L9 5.25l.813 2.846a4.5 4.5 0 0 0 3.09 3.09L15.75 12l-2.846.813a4.5 4.5 0 0 0-3.09 3.09ZM18.259 8.715 18 9.75l-.259-1.035a3.375 3.375 0 0 0-2.455-2.456L14.25 6l1.036-.259a3.375 3.375 0 0 0 2.455-2.456L18 2.25l.259 1.035a3.375 3.375 0 0 0 2.455 2.456L21.75 6l-1.036.259a3.375 3.375 0 0 0-2.455 2.456ZM16.894 20.567 16.5 21.75l-.394-1.183a2.25 2.25 0 0 0-1.423-1.423L13.5 18.75l1.183-.394a2.25 2.25 0 0 0 1.423-1.423l.394-1.183.394 1.183a2.25 2.25 0 0 0 1.423 1.423l1.183.394-1.183.394a2.25 2.25 0 0 0-1.423 1.423Z" /></svg>
        </button>

        <button
          hlmBtn
          variant="ghost"
          size="icon"
          aria-label="Notifications"
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="h-5 w-5" aria-hidden="true"><path d="M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9" /><path d="M10.3 21a1.94 1.94 0 0 0 3.4 0" /></svg>
        </button>

        <!-- User avatar + dropdown -->
        <div class="relative">
          <button
            (click)="menuOpen.set(!menuOpen())"
            class="flex h-8 w-8 items-center justify-center overflow-hidden rounded-full ring-2 ring-transparent transition-all hover:ring-primary focus:outline-none focus:ring-primary"
            [attr.aria-expanded]="menuOpen()"
            aria-label="User menu"
            aria-haspopup="true"
          >
            @if (avatarUrl()) {
              <img [src]="avatarUrl()" [alt]="displayName()" class="h-full w-full object-cover" referrerpolicy="no-referrer" />
            } @else {
              <span class="flex h-full w-full items-center justify-center bg-primary text-sm font-semibold text-primary-foreground">
                {{ initials() }}
              </span>
            }
          </button>

          @if (menuOpen()) {
            <!-- Backdrop -->
            <div
              class="fixed inset-0 z-40"
              (click)="menuOpen.set(false)"
              aria-hidden="true"
            ></div>

            <!-- Dropdown -->
            <div
              class="absolute right-0 z-50 mt-2 w-56 origin-top-right rounded-lg border border-border bg-card shadow-lg"
              role="menu"
              aria-label="User menu"
            >
              <!-- User info header -->
              <div class="border-b border-border px-4 py-3">
                <p class="truncate text-sm font-medium text-foreground">{{ displayName() }}</p>
                <p class="truncate text-xs text-muted-foreground">{{ email() }}</p>
              </div>

              <!-- Menu items -->
              <div class="p-1">
                <a
                  routerLink="/settings"
                  (click)="menuOpen.set(false)"
                  class="flex items-center gap-2 rounded-md px-3 py-2 text-sm text-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
                  role="menuitem"
                >
                  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="h-4 w-4 text-muted-foreground" aria-hidden="true"><path d="M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z" /><circle cx="12" cy="12" r="3" /></svg>
                  Settings
                </a>

                <button
                  (click)="signOut()"
                  class="flex w-full items-center gap-2 rounded-md px-3 py-2 text-sm text-destructive transition-colors hover:bg-destructive/10"
                  role="menuitem"
                >
                  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="h-4 w-4" aria-hidden="true"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" /><polyline points="16 17 21 12 16 7" /><line x1="21" x2="9" y1="12" y2="12" /></svg>
                  Sign out
                </button>
              </div>
            </div>
          }
        </div>
      </div>
    </header>
  `,
})
export class TopbarComponent {
  toggleSidebar = output<void>();
  toggleAiPanel = output<void>();

  private readonly authService = inject(AuthService);

  readonly menuOpen = signal(false);

  private readonly claims = computed(() => this.authService.getClaims());

  readonly avatarUrl = computed(() => (this.claims()['picture'] as string) ?? null);
  readonly displayName = computed(
    () =>
      (this.claims()['name'] as string) ||
      (this.claims()['given_name'] as string) ||
      'User',
  );
  readonly email = computed(() => (this.claims()['email'] as string) ?? '');
  readonly initials = computed(() => {
    const name = this.displayName();
    return name
      .split(' ')
      .map((n) => n[0])
      .slice(0, 2)
      .join('')
      .toUpperCase();
  });

  signOut(): void {
    this.menuOpen.set(false);
    this.authService.logout();
  }
}
