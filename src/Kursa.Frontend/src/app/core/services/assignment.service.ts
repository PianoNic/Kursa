import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface AssignmentView {
  id: number;
  courseId: number;
  courseName: string;
  courseShortName: string;
  name: string;
  description: string | null;
  dueDate: string | null;
  openDate: string | null;
  cutoffDate: string | null;
  isOverdue: boolean;
  isSubmittable: boolean;
}

@Injectable({ providedIn: 'root' })
export class AssignmentService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/moodle';

  getAssignments(courseId?: number): Observable<AssignmentView[]> {
    let params = new HttpParams();
    if (courseId !== undefined) {
      params = params.set('courseId', courseId.toString());
    }
    return this.http.get<AssignmentView[]>(`${this.baseUrl}/assignments`, { params });
  }
}
