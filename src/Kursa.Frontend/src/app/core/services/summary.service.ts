import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ContentSummaryDto {
  id: string;
  contentId: string;
  contentTitle: string;
  summary: string;
  tokensUsed: number;
  generatedAt: string;
}

@Injectable({ providedIn: 'root' })
export class SummaryService {
  private readonly http = inject(HttpClient);

  getSummary(contentId: string): Observable<ContentSummaryDto> {
    return this.http.get<ContentSummaryDto>(`/api/summaries/${contentId}`);
  }

  generateSummary(contentId: string): Observable<{ summary: string }> {
    return this.http.post<{ summary: string }>(`/api/summaries/${contentId}`, {});
  }
}
