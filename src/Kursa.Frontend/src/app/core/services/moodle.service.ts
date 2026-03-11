import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface MoodleCourse {
  id: number;
  shortName: string;
  fullName: string;
  summary: string;
  startDate: number;
  endDate: number;
  visible: number;
  courseImage: string | null;
  progress: number | null;
  completed: boolean | null;
  category: number;
}

export interface MoodleCourseSection {
  id: number;
  name: string;
  summary: string;
  section: number;
  visible: number;
  modules: MoodleModule[];
}

export interface MoodleModule {
  id: number;
  name: string;
  modName: string;
  modPlural: string | null;
  description: string | null;
  url: string | null;
  visible: number;
  contents: MoodleContent[] | null;
}

export interface MoodleContent {
  type: string;
  fileName: string;
  filePath: string | null;
  fileSize: number;
  fileUrl: string | null;
  timeCreated: number;
  timeModified: number;
  mimeType: string | null;
}

export interface MoodleConnectionStatus {
  isConnected: boolean;
  moodleUrl: string | null;
}

@Injectable({ providedIn: 'root' })
export class MoodleService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/moodle';

  getConnectionStatus(): Observable<MoodleConnectionStatus> {
    return this.http.get<MoodleConnectionStatus>(`${this.baseUrl}/status`);
  }

  linkMoodle(username: string, password: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/link`, { username, password });
  }

  unlinkToken(): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/link`);
  }

  getEnrolledCourses(): Observable<MoodleCourse[]> {
    return this.http.get<MoodleCourse[]>(`${this.baseUrl}/courses`);
  }

  getCourseContent(courseId: number): Observable<MoodleCourseSection[]> {
    return this.http.get<MoodleCourseSection[]>(`${this.baseUrl}/courses/${courseId}/content`);
  }
}
