import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex min-h-screen items-center justify-center bg-background">
      <div class="text-center">
        <div class="mb-6 flex items-center justify-center gap-3">
          <img src="favicon.ico" alt="Kursa" class="h-10 w-10" />
          <span class="text-3xl font-bold text-foreground">Kursa</span>
        </div>
        <p class="mb-8 text-muted-foreground">Signing you in…</p>
        <div class="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent mx-auto"></div>
      </div>
    </div>
  `,
})
export class LoginComponent implements OnInit {
  private readonly authService = inject(AuthService);

  ngOnInit(): void {
    this.authService.login();
  }
}
