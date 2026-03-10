import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface GradeView {
  id: number;
  courseId: number;
  courseName: string;
  itemName: string | null;
  itemType: string;
  itemModule: string | null;
  grade: number | null;
  gradeFormatted: string | null;
  gradeMin: number;
  gradeMax: number;
  percentage: string | null;
  feedback: string | null;
  weight: string | null;
}

export interface CourseGradeSummary {
  courseId: number;
  courseName: string;
  courseTotal: string | null;
  courseTotalRaw: number | null;
  courseTotalMax: number;
  percentage: string | null;
  gradedItemCount: number;
  totalItemCount: number;
  items: GradeView[];
}

@Injectable({ providedIn: 'root' })
export class GradeService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/moodle';

  getGrades(courseId?: number): Observable<CourseGradeSummary[]> {
    let params = new HttpParams();
    if (courseId !== undefined) {
      params = params.set('courseId', courseId.toString());
    }
    return this.http.get<CourseGradeSummary[]>(`${this.baseUrl}/grades`, { params });
  }
}
