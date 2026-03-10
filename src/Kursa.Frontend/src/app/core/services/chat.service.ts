import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ChatThread {
  id: string;
  title: string;
  createdAt: string;
  updatedAt: string;
}

export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  citations: string | null;
  tokensUsed: number;
  createdAt: string;
}

export interface Citation {
  contentId: string;
  contentTitle: string;
  chunkText: string;
  score: number;
  sourceUrl: string | null;
}

export interface ChatResponse {
  message: ChatMessage;
  sources: Citation[];
}

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly http = inject(HttpClient);

  getThreads(): Observable<ChatThread[]> {
    return this.http.get<ChatThread[]>('/api/chat/threads');
  }

  getMessages(threadId: string): Observable<ChatMessage[]> {
    return this.http.get<ChatMessage[]>(`/api/chat/threads/${threadId}/messages`);
  }

  sendMessage(message: string, threadId?: string): Observable<ChatResponse> {
    return this.http.post<ChatResponse>('/api/chat/send', { threadId, message });
  }
}
