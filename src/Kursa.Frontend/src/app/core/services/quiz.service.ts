import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Quiz {
  id: string;
  title: string;
  topic: string | null;
  questionCount: number;
  durationSeconds: number;
  attemptCount: number;
  bestScore: number | null;
  createdAt: string;
}

export interface QuizDetail {
  id: string;
  title: string;
  topic: string | null;
  durationSeconds: number;
  questions: QuizQuestion[];
}

export interface QuizQuestion {
  id: string;
  questionText: string;
  type: 'MultipleChoice' | 'TrueFalse' | 'FillInTheBlank' | 'OpenEnded';
  options: string[] | null;
  orderIndex: number;
}

export interface QuizAttemptDetail {
  id: string;
  quizId: string;
  score: number;
  totalQuestions: number;
  durationSeconds: number;
  completedAt: string;
  answers: QuizAnswerResult[];
}

export interface QuizAnswerResult {
  questionId: string;
  questionText: string;
  type: 'MultipleChoice' | 'TrueFalse' | 'FillInTheBlank' | 'OpenEnded';
  userAnswer: string;
  correctAnswer: string;
  explanation: string | null;
  isCorrect: boolean;
}

export interface QuizAttemptSummary {
  id: string;
  quizId: string;
  score: number;
  totalQuestions: number;
  durationSeconds: number;
  completedAt: string;
}

@Injectable({ providedIn: 'root' })
export class QuizService {
  private readonly http = inject(HttpClient);

  getQuizzes(): Observable<Quiz[]> {
    return this.http.get<Quiz[]>('/api/quizzes');
  }

  getQuizDetail(quizId: string): Observable<QuizDetail> {
    return this.http.get<QuizDetail>(`/api/quizzes/${quizId}`);
  }

  generateQuiz(contentId: string, questionCount: number, topic?: string, durationSeconds = 600): Observable<QuizDetail> {
    return this.http.post<QuizDetail>('/api/quizzes/generate', {
      contentId,
      questionCount,
      topic,
      durationSeconds,
    });
  }

  submitAttempt(quizId: string, answers: { questionId: string; answer: string }[], durationSeconds: number): Observable<QuizAttemptDetail> {
    return this.http.post<QuizAttemptDetail>(`/api/quizzes/${quizId}/submit`, {
      answers,
      durationSeconds,
    });
  }

  getQuizResults(quizId: string): Observable<QuizAttemptSummary[]> {
    return this.http.get<QuizAttemptSummary[]>(`/api/quizzes/${quizId}/results`);
  }

  getAttemptDetail(attemptId: string): Observable<QuizAttemptDetail> {
    return this.http.get<QuizAttemptDetail>(`/api/quizzes/attempts/${attemptId}`);
  }
}
