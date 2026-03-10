import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CalendarEventView {
  id: number;
  title: string;
  description: string | null;
  courseId: number;
  startTime: string;
  endTime: string;
  durationMinutes: number;
  eventType: string;
  moduleName: string | null;
}

@Injectable({ providedIn: 'root' })
export class TimetableService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/moodle';

  getCalendarEvents(weekStart: Date): Observable<CalendarEventView[]> {
    const params = new HttpParams().set('weekStart', weekStart.toISOString());
    return this.http.get<CalendarEventView[]>(`${this.baseUrl}/calendar`, { params });
  }
}
