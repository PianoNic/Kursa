import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Analytics {
  overview: OverviewStats;
  recentQuizPerformance: QuizPerformance[];
  flashcardStats: FlashcardStats;
  weeklyActivity: StudyActivity[];
  currentStreak: number;
  longestStreak: number;
}

export interface OverviewStats {
  totalStudySessions: number;
  totalStudyTimeSeconds: number;
  totalQuizzesTaken: number;
  totalCardsReviewed: number;
  totalPinnedContents: number;
  totalPomodoros: number;
}

export interface QuizPerformance {
  quizId: string;
  quizTitle: string;
  score: number;
  totalQuestions: number;
  completedAt: string;
}

export interface FlashcardStats {
  totalCards: number;
  dueToday: number;
  masteredCards: number;
  learningCards: number;
}

export interface StudyActivity {
  date: string;
  studyTimeSeconds: number;
  cardsReviewed: number;
  quizQuestions: number;
}

@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  private readonly http = inject(HttpClient);

  getAnalytics(): Observable<Analytics> {
    return this.http.get<Analytics>('/api/analytics');
  }
}
