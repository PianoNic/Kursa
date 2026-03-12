import { Injectable, inject, signal, computed } from '@angular/core';
import { Observable, firstValueFrom } from 'rxjs';
import { tap } from 'rxjs/operators';
import { OAuthService, AuthConfig } from 'angular-oauth2-oidc';
import { AppService } from '../../api/api/app.service';
import { AuthService as ApiAuthService } from '../../api/api/auth.service';
import { UsersService } from '../../api/api/users.service';
import { UserDto } from '../../api/model/userDto';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly appService = inject(AppService);
  private readonly apiAuth = inject(ApiAuthService);
  private readonly usersService = inject(UsersService);
  private readonly oauthService = inject(OAuthService);

  private readonly _profile = signal<UserDto | null>(null);

  readonly profile = this._profile.asReadonly();
  readonly isAuthenticated = computed(() => this.oauthService.hasValidAccessToken());

  async initialize(): Promise<void> {
    const appInfo = await firstValueFrom(this.appService.apiAppGet());

    const config: AuthConfig = {
      issuer: appInfo.oidc?.issuer ?? '',
      clientId: appInfo.oidc?.clientId ?? '',
      redirectUri: appInfo.oidc?.redirectUri ?? '/callback',
      postLogoutRedirectUri: appInfo.oidc?.postLogoutRedirectUri ?? '/',
      scope: appInfo.oidc?.scope ?? 'openid profile email',
      responseType: 'code',
      showDebugInformation: appInfo.environment !== 'Production',
      strictDiscoveryDocumentValidation: false,
      useSilentRefresh: false,
      clearHashAfterLogin: true,
    };

    this.oauthService.configure(config);
    this.oauthService.setupAutomaticSilentRefresh();

    // Only load the discovery document — do NOT try to exchange auth codes.
    // The CallbackComponent handles the actual code exchange via handleCallback().
    // If we called loadDiscoveryDocumentAndTryLogin() here and the token exchange
    // failed (e.g. Pocket ID misconfigured), the app would crash before rendering.
    try {
      await this.oauthService.loadDiscoveryDocument();
    } catch {
      // Non-fatal: app still renders the login page even without discovery doc.
    }
  }

  async handleCallback(): Promise<boolean> {
    try {
      await this.oauthService.tryLogin();
      return this.oauthService.hasValidAccessToken();
    } catch {
      return false;
    }
  }

  login(): void {
    this.oauthService.initCodeFlow();
  }

  logout(): void {
    this.oauthService.logOut();
  }

  getAccessToken(): string | null {
    return this.oauthService.getAccessToken() || null;
  }

  getClaims(): Record<string, unknown> {
    return (this.oauthService.getIdentityClaims() as Record<string, unknown>) ?? {};
  }

  getCurrentUser(): Observable<UserDto> {
    return this.apiAuth
      .apiAuthMeGet()
      .pipe(tap((user) => this._profile.set(user)));
  }

  /** Creates user in DB + marks onboarding complete. */
  register(): Observable<UserDto> {
    return this.apiAuth
      .apiAuthRegisterPost()
      .pipe(tap((user) => this._profile.set(user)));
  }

  completeOnboarding(): Observable<void> {
    return this.usersService.apiUsersMeOnboardingCompletePost();
  }
}
