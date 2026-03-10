import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Recording {
  id: string;
  title: string;
  description: string | null;
  courseId: string | null;
  courseTitle: string | null;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  durationSeconds: number | null;
  status: RecordingStatus;
  hasTranscript: boolean;
  createdAt: string;
}

export interface RecordingDetail {
  id: string;
  title: string;
  description: string | null;
  courseId: string | null;
  courseTitle: string | null;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  durationSeconds: number | null;
  status: RecordingStatus;
  transcriptText: string | null;
  transcribedAt: string | null;
  errorMessage: string | null;
  createdAt: string;
}

export type RecordingStatus =
  | 'Uploaded'
  | 'Transcribing'
  | 'Transcribed'
  | 'Indexing'
  | 'Completed'
  | 'Failed';

@Injectable({ providedIn: 'root' })
export class RecordingService {
  private readonly http = inject(HttpClient);

  getRecordings(): Observable<Recording[]> {
    return this.http.get<Recording[]>('/api/recordings');
  }

  getRecording(id: string): Observable<RecordingDetail> {
    return this.http.get<RecordingDetail>(`/api/recordings/${id}`);
  }

  getDownloadUrl(id: string): Observable<{ url: string }> {
    return this.http.get<{ url: string }>(`/api/recordings/${id}/download-url`);
  }

  upload(
    file: File,
    title: string,
    description?: string,
    courseId?: string,
  ): Observable<Recording> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('title', title);
    if (description) formData.append('description', description);
    if (courseId) formData.append('courseId', courseId);
    return this.http.post<Recording>('/api/recordings/upload', formData);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`/api/recordings/${id}`);
  }
}
