import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, firstValueFrom } from 'rxjs';
import { tap } from 'rxjs/operators';
import { OAuthService, AuthConfig } from 'angular-oauth2-oidc';
import { environment } from '../../../environments/environment';

export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
  avatarUrl: string | null;
  role: string;
  onboardingCompleted: boolean;
  moodleConnected: boolean;
  createdAt?: string;
}

interface AppInfo {
  version: string;
  environment: string;
  isHealthy: boolean;
  oidc: {
    issuer: string;
    clientId: string;
    redirectUri: string;
    postLogoutRedirectUri: string;
    scope: string;
  };
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly oauthService = inject(OAuthService);

  private readonly _profile = signal<UserProfile | null>(null);

  readonly profile = this._profile.asReadonly();
  readonly isAuthenticated = computed(() => this.oauthService.hasValidAccessToken());

  async initialize(): Promise<void> {
    const appInfo = await firstValueFrom(this.http.get<AppInfo>(`${environment.apiBaseUrl}/api/app`));

    const config: AuthConfig = {
      issuer: appInfo.oidc.issuer,
      clientId: appInfo.oidc.clientId,
      redirectUri: appInfo.oidc.redirectUri,
      postLogoutRedirectUri: appInfo.oidc.postLogoutRedirectUri,
      scope: appInfo.oidc.scope,
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

  getCurrentUser(): Observable<UserProfile> {
    return this.http
      .get<UserProfile>(`${environment.apiBaseUrl}/api/auth/me`)
      .pipe(tap((user) => this._profile.set(user)));
  }

  /** Creates user in DB + marks onboarding complete. */
  register(): Observable<UserProfile> {
    return this.http
      .post<UserProfile>(`${environment.apiBaseUrl}/api/auth/register`, {})
      .pipe(tap((user) => this._profile.set(user)));
  }

  completeOnboarding(): Observable<void> {
    return this.http.post<void>(`${environment.apiBaseUrl}/api/users/me/onboarding/complete`, {});
  }
}
