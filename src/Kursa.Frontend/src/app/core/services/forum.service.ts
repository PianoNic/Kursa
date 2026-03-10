import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ForumView {
  id: number;
  courseId: number;
  name: string;
  description: string | null;
  type: string;
  discussionCount: number;
  lastModified: string | null;
}

export interface DiscussionView {
  id: number;
  title: string;
  message: string | null;
  author: string;
  authorAvatar: string | null;
  createdAt: string;
  modifiedAt: string;
  replyCount: number;
  isPinned: boolean;
}

@Injectable({ providedIn: 'root' })
export class ForumService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/moodle';

  getForums(courseId: number): Observable<ForumView[]> {
    return this.http.get<ForumView[]>(`${this.baseUrl}/courses/${courseId}/forums`);
  }

  getDiscussions(forumId: number): Observable<DiscussionView[]> {
    return this.http.get<DiscussionView[]>(`${this.baseUrl}/forums/${forumId}/discussions`);
  }
}
