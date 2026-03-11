import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { OAuthService } from 'angular-oauth2-oidc';

@Component({
  selector: 'app-callback',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex min-h-screen items-center justify-center bg-background">
      <div class="text-center">
        <div class="mb-4 flex items-center justify-center gap-3">
          <img src="favicon.ico" alt="Kursa" class="h-10 w-10" />
          <span class="text-3xl font-bold text-foreground">Kursa</span>
        </div>
        <p class="mb-8 text-muted-foreground">Completing sign-in…</p>
        <div class="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent mx-auto"></div>
      </div>
    </div>
  `,
})
export class CallbackComponent implements OnInit {
  private readonly oauthService = inject(OAuthService);
  private readonly router = inject(Router);

  async ngOnInit(): Promise<void> {
    await this.oauthService.loadDiscoveryDocumentAndTryLogin();

    if (this.oauthService.hasValidAccessToken()) {
      await this.router.navigate(['/dashboard']);
    } else {
      await this.router.navigate(['/login']);
    }
  }
}
