import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

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

  getCurrentUser(): Observable<UserProfile> {
    return this.http.get<UserProfile>('/api/auth/me');
  }

  completeOnboarding(): Observable<void> {
    return this.http.post<void>('/api/users/me/onboarding/complete', {});
  }
}
