import { Injectable, signal } from '@angular/core';

/**
 * Manages the Microsoft Graph access token used by the Graph API service.
 * The generated GraphService does not handle token management, so this service
 * stores the token in memory and exposes it for the graph-token interceptor.
 */
@Injectable({ providedIn: 'root' })
export class GraphTokenService {
  private readonly _token = signal<string | null>(null);

  readonly token = this._token.asReadonly();

  hasToken(): boolean {
    return !!this._token();
  }

  setToken(token: string): void {
    this._token.set(token);
  }

  clearToken(): void {
    this._token.set(null);
  }

  getToken(): string | null {
    return this._token();
  }
}
