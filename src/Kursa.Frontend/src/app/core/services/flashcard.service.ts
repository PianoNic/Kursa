import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface FlashcardDeck {
  id: string;
  title: string;
  description: string | null;
  cardCount: number;
  dueCount: number;
  createdAt: string;
}

export interface FlashcardDeckDetail {
  id: string;
  title: string;
  description: string | null;
  cards: Flashcard[];
}

export interface Flashcard {
  id: string;
  front: string;
  back: string;
  type: 'Basic' | 'Cloze' | 'Reversible';
  repetitions: number;
  easeFactor: number;
  intervalDays: number;
  nextReviewAt: string | null;
  lastReviewedAt: string | null;
}

export interface ReviewResult {
  cardId: string;
  newRepetitions: number;
  newEaseFactor: number;
  newIntervalDays: number;
  nextReviewAt: string;
}

@Injectable({ providedIn: 'root' })
export class FlashcardService {
  private readonly http = inject(HttpClient);

  getDecks(): Observable<FlashcardDeck[]> {
    return this.http.get<FlashcardDeck[]>('/api/flashcards/decks');
  }

  getDeckDetail(deckId: string): Observable<FlashcardDeckDetail> {
    return this.http.get<FlashcardDeckDetail>(`/api/flashcards/decks/${deckId}`);
  }

  getDueCards(deckId: string): Observable<Flashcard[]> {
    return this.http.get<Flashcard[]>(`/api/flashcards/decks/${deckId}/due`);
  }

  generateFlashcards(contentId: string, cardCount: number, topic?: string): Observable<FlashcardDeckDetail> {
    return this.http.post<FlashcardDeckDetail>('/api/flashcards/generate', {
      contentId,
      cardCount,
      topic,
    });
  }

  createCard(deckId: string, front: string, back: string, type = 'Basic'): Observable<Flashcard> {
    return this.http.post<Flashcard>(`/api/flashcards/decks/${deckId}/cards`, {
      front,
      back,
      type,
    });
  }

  reviewCard(cardId: string, quality: number): Observable<ReviewResult> {
    return this.http.post<ReviewResult>(`/api/flashcards/cards/${cardId}/review`, { quality });
  }
}
