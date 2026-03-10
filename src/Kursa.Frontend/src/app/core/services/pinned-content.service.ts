import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PinnedContent {
  id: string;
  contentId: string;
  contentTitle: string;
  contentDescription: string | null;
  contentType: string;
  url: string | null;
  isStarred: boolean;
  isIndexed: boolean;
  notes: string | null;
  pinnedAt: string;
}

@Injectable({ providedIn: 'root' })
export class PinnedContentService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/pinned';

  getPinnedContents(): Observable<PinnedContent[]> {
    return this.http.get<PinnedContent[]>(this.baseUrl);
  }

  pinContent(contentId: string, notes?: string): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.baseUrl}/${contentId}`, { notes });
  }

  unpinContent(contentId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${contentId}`);
  }

  toggleStar(contentId: string): Observable<{ isStarred: boolean }> {
    return this.http.post<{ isStarred: boolean }>(`${this.baseUrl}/${contentId}/star`, {});
  }
}
