import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface StudySession {
  id: string;
  title: string;
  status: 'Active' | 'Completed' | 'Abandoned';
  workMinutes: number;
  breakMinutes: number;
  completedPomodoros: number;
  totalDurationSeconds: number;
  cardsReviewed: number;
  quizQuestionsAnswered: number;
  quizCorrectAnswers: number;
  summary: string | null;
  createdAt: string;
  completedAt: string | null;
}

@Injectable({ providedIn: 'root' })
export class StudySessionService {
  private readonly http = inject(HttpClient);

  getSessions(): Observable<StudySession[]> {
    return this.http.get<StudySession[]>('/api/study-sessions');
  }

  startSession(title: string, workMinutes = 25, breakMinutes = 5): Observable<StudySession> {
    return this.http.post<StudySession>('/api/study-sessions/start', {
      title,
      workMinutes,
      breakMinutes,
    });
  }

  completeSession(
    sessionId: string,
    completedPomodoros: number,
    totalDurationSeconds: number,
    cardsReviewed: number,
    quizQuestionsAnswered: number,
    quizCorrectAnswers: number,
  ): Observable<StudySession> {
    return this.http.post<StudySession>(`/api/study-sessions/${sessionId}/complete`, {
      completedPomodoros,
      totalDurationSeconds,
      cardsReviewed,
      quizQuestionsAnswered,
      quizCorrectAnswers,
    });
  }
}
