import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { HlmButton } from '@spartan-ng/helm/button';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [HlmButton],
  template: `
    <div class="flex min-h-screen items-center justify-center bg-background">
      <div class="text-center">
        <div class="mb-6 flex items-center justify-center gap-3">
          <img src="favicon.ico" alt="Kursa" class="h-10 w-10" />
          <span class="text-3xl font-bold text-foreground">Kursa</span>
        </div>

        @if (isExpired) {
          <p class="mb-4 text-sm text-destructive">Your session has expired. Please sign in again.</p>
        } @else {
          <p class="mb-4 text-muted-foreground">Sign in to access your courses and study tools.</p>
        }

        <button hlmBtn (click)="authService.login()">Sign in with Pocket ID</button>
      </div>
    </div>
  `,
})
export class LoginComponent {
  readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  readonly isExpired = this.route.snapshot.queryParamMap.get('expired') === 'true';
}
