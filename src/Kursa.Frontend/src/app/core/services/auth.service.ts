import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
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
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly oauthService = inject(OAuthService);

  private readonly _profile = signal<UserProfile | null>(null);

  readonly profile = this._profile.asReadonly();
  readonly isAuthenticated = computed(() => this.oauthService.hasValidAccessToken());

  async initialize(): Promise<void> {
    const config: AuthConfig = {
      issuer: environment.oidc.issuer,
      clientId: environment.oidc.clientId,
      redirectUri: environment.oidc.redirectUri,
      postLogoutRedirectUri: environment.oidc.postLogoutRedirectUri,
      scope: environment.oidc.scope,
      responseType: environment.oidc.responseType,
      showDebugInformation: environment.oidc.showDebugInformation,
      strictDiscoveryDocumentValidation: environment.oidc.strictDiscoveryDocumentValidation,
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
      .get<UserProfile>('/api/auth/me')
      .pipe(tap((user) => this._profile.set(user)));
  }

  completeOnboarding(): Observable<void> {
    return this.http.post<void>('/api/users/me/onboarding/complete', {});
  }
}
