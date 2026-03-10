import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface StudySuggestion {
  type: string;
  title: string;
  description: string;
  actionUrl: string | null;
  priority: string;
}

@Injectable({ providedIn: 'root' })
export class SuggestionService {
  private readonly http = inject(HttpClient);

  getSuggestions(): Observable<StudySuggestion[]> {
    return this.http.get<StudySuggestion[]>('/api/suggestions');
  }
}
