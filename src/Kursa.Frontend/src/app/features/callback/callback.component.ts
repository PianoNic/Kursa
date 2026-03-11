import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-callback',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex min-h-screen items-center justify-center bg-background">
      <div class="text-center px-4">
        <div class="mb-4 flex items-center justify-center gap-3">
          <span class="text-3xl font-bold text-foreground">Kursa</span>
        </div>

        @if (error()) {
          <div class="mt-4 max-w-sm rounded-lg border border-destructive/50 bg-destructive/10 p-4">
            <p class="text-sm font-semibold text-destructive">Sign-in failed</p>
            <p class="mt-1 text-xs text-muted-foreground">{{ error() }}</p>
            <button
              (click)="retry()"
              class="mt-3 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
            >
              Try again
            </button>
          </div>
        } @else {
          <p class="mb-8 text-muted-foreground">Completing sign-in…</p>
          <div class="mx-auto h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent"></div>
        }
      </div>
    </div>
  `,
})
export class CallbackComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly error = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    const success = await this.authService.handleCallback();
    if (success) {
      await this.router.navigate(['/dashboard']);
    } else {
      this.error.set(
        'Sign-in failed. Make sure the Pocket ID client has redirect URL ' +
          window.location.origin +
          '/callback registered, and that Public client + PKCE are enabled.',
      );
    }
  }

  retry(): void {
    this.authService.login();
  }
}
