import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { OAuthService } from 'angular-oauth2-oidc';

export const authGuard: CanActivateFn = () => {
  const oauthService = inject(OAuthService);
  const router = inject(Router);

  // Check both that the library considers the token valid AND that a token actually exists
  const token = oauthService.getAccessToken();
  if (token && oauthService.hasValidAccessToken()) {
    return true;
  }

  return router.createUrlTree(['/login']);
};
