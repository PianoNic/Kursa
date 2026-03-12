import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { OAuthService } from 'angular-oauth2-oidc';
import { catchError, throwError } from 'rxjs';

let isLoggingOut = false;

function isApiRequest(url: string): boolean {
  return url.startsWith('/api') || url.includes('/api/');
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const oauthService = inject(OAuthService);
  const router = inject(Router);
  const token = oauthService.getAccessToken();

  const request = token && isApiRequest(req.url)
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(request).pipe(
    catchError((error) => {
      if (error.status === 401 && isApiRequest(req.url) && !isLoggingOut) {
        isLoggingOut = true;
        // Clear local tokens without triggering OIDC RP-Initiated Logout
        sessionStorage.clear();
        localStorage.removeItem('access_token');
        localStorage.removeItem('id_token');
        localStorage.removeItem('refresh_token');
        localStorage.removeItem('nonce');
        localStorage.removeItem('PKCE_verifier');
        router.navigate(['/login'], { queryParams: { expired: 'true' } }).then(() => {
          isLoggingOut = false;
        });
      }
      return throwError(() => error);
    }),
  );
};
