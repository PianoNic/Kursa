import { ChangeDetectionStrategy, Component, signal, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  GraphService,
  OneNoteNotebook,
  OneNoteSection,
  OneNotePage,
  SharePointSite,
  SharePointDriveItem,
} from '../../core/services/graph.service';

type ActiveTab = 'onenote' | 'sharepoint';
type OneNoteView = 'notebooks' | 'sections' | 'pages' | 'content';
type SharePointView = 'sites' | 'items';

@Component({
  selector: 'app-microsoft',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  template: `
    <div class="mx-auto max-w-7xl space-y-6 p-6">
      <div>
        <h1 class="text-2xl font-bold text-foreground">Microsoft Integration</h1>
        <p class="text-sm text-muted-foreground">Access OneNote notebooks and SharePoint files</p>
      </div>

      <!-- Token input -->
      @if (!hasToken()) {
        <div class="rounded-lg border border-border bg-card p-6">
          <h2 class="mb-2 font-medium text-foreground">Connect Microsoft Account</h2>
          <p class="mb-4 text-sm text-muted-foreground">
            Enter your Microsoft Graph access token to browse OneNote and SharePoint content.
          </p>
          <div class="flex gap-2">
            <input
              type="password"
              class="flex-1 rounded-md border border-border bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="Microsoft Graph access token"
              [(ngModel)]="tokenInput"
              (keydown.enter)="connectToken()"
            />
            <button
              type="button"
              class="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
              (click)="connectToken()"
            >
              Connect
            </button>
          </div>
        </div>
      } @else {
        <!-- Tab bar -->
        <div class="flex items-center justify-between">
          <div class="flex gap-2 rounded-lg border border-border bg-card p-1">
            <button
              type="button"
              class="rounded-md px-3 py-1.5 text-sm font-medium transition-colors"
              [class]="activeTab() === 'onenote' ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:text-foreground'"
              (click)="switchTab('onenote')"
            >
              OneNote
            </button>
            <button
              type="button"
              class="rounded-md px-3 py-1.5 text-sm font-medium transition-colors"
              [class]="activeTab() === 'sharepoint' ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:text-foreground'"
              (click)="switchTab('sharepoint')"
            >
              SharePoint
            </button>
          </div>
          <button
            type="button"
            class="rounded-md px-3 py-1.5 text-sm text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
            (click)="disconnect()"
          >
            Disconnect
          </button>
        </div>

        @if (loading()) {
          <div class="flex items-center justify-center py-12">
            <div class="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" role="status">
              <span class="sr-only">Loading...</span>
            </div>
          </div>
        } @else if (error()) {
          <div class="rounded-lg border border-destructive/50 bg-destructive/10 p-4 text-sm text-destructive" role="alert">
            {{ error() }}
          </div>
        } @else if (activeTab() === 'onenote') {
          <!-- OneNote -->
          @if (onenoteView() !== 'notebooks') {
            <button
              type="button"
              class="flex items-center gap-1 text-sm text-muted-foreground transition-colors hover:text-foreground"
              (click)="onenoteBack()"
            >
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="h-4 w-4" aria-hidden="true"><path d="m15 18-6-6 6-6" /></svg>
              Back
            </button>
          }

          @if (onenoteView() === 'notebooks') {
            @if (notebooks().length === 0) {
              <div class="rounded-lg border border-border bg-card p-8 text-center text-muted-foreground">No notebooks found.</div>
            } @else {
              <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                @for (nb of notebooks(); track nb.id) {
                  <button
                    type="button"
                    class="rounded-lg border border-border bg-card p-4 text-left transition-colors hover:bg-accent/50"
                    (click)="openNotebook(nb)"
                  >
                    <div class="flex items-center gap-3">
                      <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-purple-500/10 text-purple-500">
                        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="h-5 w-5" aria-hidden="true"><path d="M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z" /><path d="M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z" /></svg>
                      </div>
                      <div class="min-w-0 flex-1">
                        <h3 class="truncate font-medium text-foreground">{{ nb.displayName }}</h3>
                        @if (nb.lastModifiedAt) {
                          <p class="text-xs text-muted-foreground">{{ formatDate(nb.lastModifiedAt) }}</p>
                        }
                      </div>
                    </div>
                  </button>
                }
              </div>
            }
          } @else if (onenoteView() === 'sections') {
            <h2 class="text-lg font-semibold text-foreground">{{ currentNotebookName() }}</h2>
            @if (sections().length === 0) {
              <div class="rounded-lg border border-border bg-card p-8 text-center text-muted-foreground">No sections found.</div>
            } @else {
              <div class="space-y-2">
                @for (sec of sections(); track sec.id) {
                  <button
                    type="button"
                    class="flex w-full items-center gap-3 rounded-lg border border-border bg-card p-3 text-left transition-colors hover:bg-accent/50"
                    (click)="openSection(sec)"
                  >
                    <div class="flex h-8 w-8 items-center justify-center rounded bg-blue-500/10 text-blue-500 text-sm font-medium">S</div>
                    <span class="text-foreground">{{ sec.displayName }}</span>
                  </button>
                }
              </div>
            }
          } @else if (onenoteView() === 'pages') {
            <h2 class="text-lg font-semibold text-foreground">{{ currentSectionName() }}</h2>
            @if (pages().length === 0) {
              <div class="rounded-lg border border-border bg-card p-8 text-center text-muted-foreground">No pages found.</div>
            } @else {
              <div class="space-y-2">
                @for (page of pages(); track page.id) {
                  <button
                    type="button"
                    class="flex w-full items-center gap-3 rounded-lg border border-border bg-card p-3 text-left transition-colors hover:bg-accent/50"
                    (click)="openPage(page)"
                  >
                    <div class="flex h-8 w-8 items-center justify-center rounded bg-green-500/10 text-green-500 text-sm font-medium">P</div>
                    <div class="min-w-0 flex-1">
                      <span class="text-foreground">{{ page.title }}</span>
                      @if (page.lastModifiedAt) {
                        <p class="text-xs text-muted-foreground">{{ formatDate(page.lastModifiedAt) }}</p>
                      }
                    </div>
                  </button>
                }
              </div>
            }
          } @else if (onenoteView() === 'content') {
            <div class="rounded-lg border border-border bg-card p-6">
              <div class="prose prose-invert max-w-none" [innerHTML]="pageContent()"></div>
            </div>
          }
        } @else {
          <!-- SharePoint -->
          @if (sharepointView() === 'sites') {
            @if (sites().length === 0) {
              <div class="rounded-lg border border-border bg-card p-8 text-center text-muted-foreground">No SharePoint sites found.</div>
            } @else {
              <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                @for (site of sites(); track site.id) {
                  <button
                    type="button"
                    class="rounded-lg border border-border bg-card p-4 text-left transition-colors hover:bg-accent/50"
                    (click)="openSite(site)"
                  >
                    <div class="flex items-center gap-3">
                      <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-blue-500/10 text-blue-500">
                        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="h-5 w-5" aria-hidden="true"><path d="M4 20h16a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-7.93a2 2 0 0 1-1.66-.9l-.82-1.2A2 2 0 0 0 7.93 3H4a2 2 0 0 0-2 2v13c0 1.1.9 2 2 2Z" /></svg>
                      </div>
                      <div class="min-w-0 flex-1">
                        <h3 class="truncate font-medium text-foreground">{{ site.displayName }}</h3>
                        @if (site.description) {
                          <p class="truncate text-xs text-muted-foreground">{{ site.description }}</p>
                        }
                      </div>
                    </div>
                  </button>
                }
              </div>
            }
          } @else {
            <button
              type="button"
              class="flex items-center gap-1 text-sm text-muted-foreground transition-colors hover:text-foreground"
              (click)="sharepointBack()"
            >
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="h-4 w-4" aria-hidden="true"><path d="m15 18-6-6 6-6" /></svg>
              Back
            </button>
            <h2 class="text-lg font-semibold text-foreground">{{ currentSiteName() }}</h2>
            @if (driveItems().length === 0) {
              <div class="rounded-lg border border-border bg-card p-8 text-center text-muted-foreground">No files found.</div>
            } @else {
              <div class="space-y-2">
                @for (item of driveItems(); track item.id) {
                  @if (item.isFolder) {
                    <button
                      type="button"
                      class="flex w-full items-center gap-3 rounded-lg border border-border bg-card p-3 text-left transition-colors hover:bg-accent/50"
                      (click)="openFolder(item.id)"
                    >
                      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="h-5 w-5 text-yellow-500" aria-hidden="true"><path d="M4 20h16a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-7.93a2 2 0 0 1-1.66-.9l-.82-1.2A2 2 0 0 0 7.93 3H4a2 2 0 0 0-2 2v13c0 1.1.9 2 2 2Z" /></svg>
                      <span class="text-foreground">{{ item.name }}</span>
                    </button>
                  } @else {
                    <a
                      [href]="item.webUrl"
                      target="_blank"
                      rel="noopener noreferrer"
                      class="flex items-center gap-3 rounded-lg border border-border bg-card p-3 transition-colors hover:bg-accent/50"
                    >
                      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="h-5 w-5 text-muted-foreground" aria-hidden="true"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" /><polyline points="14 2 14 8 20 8" /></svg>
                      <div class="min-w-0 flex-1">
                        <span class="text-foreground">{{ item.name }}</span>
                        <p class="text-xs text-muted-foreground">{{ formatFileSize(item.size) }}</p>
                      </div>
                    </a>
                  }
                }
              </div>
            }
          }
        }
      }
    </div>
  `,
})
export class MicrosoftComponent implements OnInit {
  private readonly graphService = inject(GraphService);

  tokenInput = '';
  readonly activeTab = signal<ActiveTab>('onenote');
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  // OneNote state
  readonly onenoteView = signal<OneNoteView>('notebooks');
  readonly notebooks = signal<OneNoteNotebook[]>([]);
  readonly sections = signal<OneNoteSection[]>([]);
  readonly pages = signal<OneNotePage[]>([]);
  readonly pageContent = signal('');
  readonly currentNotebookName = signal('');
  readonly currentSectionName = signal('');

  // SharePoint state
  readonly sharepointView = signal<SharePointView>('sites');
  readonly sites = signal<SharePointSite[]>([]);
  readonly driveItems = signal<SharePointDriveItem[]>([]);
  readonly currentSiteName = signal('');
  private currentSiteId = '';

  ngOnInit(): void {
    // Check if already connected
  }

  hasToken(): boolean {
    return this.graphService.hasToken();
  }

  connectToken(): void {
    if (!this.tokenInput.trim()) return;
    this.graphService.setToken(this.tokenInput.trim());
    this.tokenInput = '';
    this.loadInitialData();
  }

  disconnect(): void {
    this.graphService.clearToken();
    this.notebooks.set([]);
    this.sites.set([]);
  }

  switchTab(tab: ActiveTab): void {
    this.activeTab.set(tab);
    this.error.set(null);
    if (tab === 'onenote' && this.notebooks().length === 0) {
      this.loadNotebooks();
    } else if (tab === 'sharepoint' && this.sites().length === 0) {
      this.loadSites();
    }
  }

  // -- OneNote navigation --

  openNotebook(nb: OneNoteNotebook): void {
    this.currentNotebookName.set(nb.displayName);
    this.loading.set(true);
    this.graphService.getSections(nb.id).subscribe({
      next: (sections) => {
        this.sections.set(sections);
        this.onenoteView.set('sections');
        this.loading.set(false);
      },
      error: () => this.handleError('Failed to load sections.'),
    });
  }

  openSection(sec: OneNoteSection): void {
    this.currentSectionName.set(sec.displayName);
    this.loading.set(true);
    this.graphService.getPages(sec.id).subscribe({
      next: (pages) => {
        this.pages.set(pages);
        this.onenoteView.set('pages');
        this.loading.set(false);
      },
      error: () => this.handleError('Failed to load pages.'),
    });
  }

  openPage(page: OneNotePage): void {
    this.loading.set(true);
    this.graphService.getPageContent(page.id).subscribe({
      next: (content) => {
        this.pageContent.set(content);
        this.onenoteView.set('content');
        this.loading.set(false);
      },
      error: () => this.handleError('Failed to load page content.'),
    });
  }

  onenoteBack(): void {
    this.error.set(null);
    switch (this.onenoteView()) {
      case 'sections': this.onenoteView.set('notebooks'); break;
      case 'pages': this.onenoteView.set('sections'); break;
      case 'content': this.onenoteView.set('pages'); break;
    }
  }

  // -- SharePoint navigation --

  openSite(site: SharePointSite): void {
    this.currentSiteId = site.id;
    this.currentSiteName.set(site.displayName);
    this.loading.set(true);
    this.graphService.getDriveItems(site.id).subscribe({
      next: (items) => {
        this.driveItems.set(items);
        this.sharepointView.set('items');
        this.loading.set(false);
      },
      error: () => this.handleError('Failed to load files.'),
    });
  }

  openFolder(folderId: string): void {
    this.loading.set(true);
    this.graphService.getDriveItems(this.currentSiteId, folderId).subscribe({
      next: (items) => {
        this.driveItems.set(items);
        this.loading.set(false);
      },
      error: () => this.handleError('Failed to load folder contents.'),
    });
  }

  sharepointBack(): void {
    this.error.set(null);
    this.sharepointView.set('sites');
    this.driveItems.set([]);
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('default', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 B';
    const units = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return `${(bytes / Math.pow(1024, i)).toFixed(1)} ${units[i]}`;
  }

  private loadInitialData(): void {
    this.loadNotebooks();
    this.loadSites();
  }

  private loadNotebooks(): void {
    this.loading.set(true);
    this.error.set(null);
    this.graphService.getNotebooks().subscribe({
      next: (notebooks) => {
        this.notebooks.set(notebooks);
        this.loading.set(false);
      },
      error: () => this.handleError('Failed to load notebooks.'),
    });
  }

  private loadSites(): void {
    this.graphService.getSites().subscribe({
      next: (sites) => this.sites.set(sites),
      error: () => { /* silent for background load */ },
    });
  }

  private handleError(message: string): void {
    this.error.set(message);
    this.loading.set(false);
  }
}
