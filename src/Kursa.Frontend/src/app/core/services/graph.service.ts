import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface OneNoteNotebook {
  id: string;
  displayName: string;
  createdAt: string | null;
  lastModifiedAt: string | null;
}

export interface OneNoteSection {
  id: string;
  displayName: string;
  createdAt: string | null;
  lastModifiedAt: string | null;
}

export interface OneNotePage {
  id: string;
  title: string;
  createdAt: string | null;
  lastModifiedAt: string | null;
  contentUrl: string | null;
}

export interface SharePointSite {
  id: string;
  displayName: string;
  webUrl: string | null;
  description: string | null;
}

export interface SharePointDriveItem {
  id: string;
  name: string;
  webUrl: string | null;
  size: number;
  lastModifiedAt: string | null;
  isFolder: boolean;
  mimeType: string | null;
}

@Injectable({ providedIn: 'root' })
export class GraphService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/graph';

  private graphToken: string | null = null;

  setToken(token: string): void {
    this.graphToken = token;
  }

  clearToken(): void {
    this.graphToken = null;
  }

  hasToken(): boolean {
    return this.graphToken !== null;
  }

  // -- OneNote --

  getNotebooks(): Observable<OneNoteNotebook[]> {
    return this.http.get<OneNoteNotebook[]>(
      `${this.baseUrl}/onenote/notebooks`,
      { headers: this.getHeaders() }
    );
  }

  getSections(notebookId: string): Observable<OneNoteSection[]> {
    return this.http.get<OneNoteSection[]>(
      `${this.baseUrl}/onenote/notebooks/${notebookId}/sections`,
      { headers: this.getHeaders() }
    );
  }

  getPages(sectionId: string): Observable<OneNotePage[]> {
    return this.http.get<OneNotePage[]>(
      `${this.baseUrl}/onenote/sections/${sectionId}/pages`,
      { headers: this.getHeaders() }
    );
  }

  getPageContent(pageId: string): Observable<string> {
    return this.http.get(
      `${this.baseUrl}/onenote/pages/${pageId}/content`,
      { headers: this.getHeaders(), responseType: 'text' }
    );
  }

  // -- SharePoint --

  getSites(search?: string): Observable<SharePointSite[]> {
    let params = new HttpParams();
    if (search) {
      params = params.set('search', search);
    }
    return this.http.get<SharePointSite[]>(
      `${this.baseUrl}/sharepoint/sites`,
      { headers: this.getHeaders(), params }
    );
  }

  getDriveItems(siteId: string, folderId?: string): Observable<SharePointDriveItem[]> {
    let params = new HttpParams();
    if (folderId) {
      params = params.set('folderId', folderId);
    }
    return this.http.get<SharePointDriveItem[]>(
      `${this.baseUrl}/sharepoint/sites/${siteId}/items`,
      { headers: this.getHeaders(), params }
    );
  }

  private getHeaders(): HttpHeaders {
    let headers = new HttpHeaders();
    if (this.graphToken) {
      headers = headers.set('X-Graph-Token', this.graphToken);
    }
    return headers;
  }
}
