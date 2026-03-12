import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { catchError, map, of } from 'rxjs';

export const onboardingGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const profile = authService.profile();
  if (profile) {
    return profile.onboardingCompleted ? true : router.createUrlTree(['/onboarding']);
  }

  return authService.getCurrentUser().pipe(
    map((user) => (user.onboardingCompleted ? true : router.createUrlTree(['/onboarding']))),
    catchError(() => of(router.createUrlTree(['/onboarding']))),
  );
};
