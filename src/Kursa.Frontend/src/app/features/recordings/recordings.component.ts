import { ChangeDetectionStrategy, Component, inject, signal, computed, OnInit, ElementRef, viewChild } from '@angular/core';
import { DatePipe } from '@angular/common';
import {
  Recording,
  RecordingDetail,
  RecordingService,
  RecordingStatus,
} from '../../core/services/recording.service';

type View = 'list' | 'upload' | 'detail';

@Component({
  selector: 'app-recordings',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe],
  template: `
    <div class="space-y-6">
      @switch (view()) {
        @case ('list') {
          <div class="flex items-center justify-between">
            <h1 class="text-2xl font-bold text-foreground">Recordings</h1>
            <button
              (click)="view.set('upload')"
              class="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
            >
              Upload Recording
            </button>
          </div>

          @if (loading()) {
            <div class="flex items-center justify-center py-12">
              <div class="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent"></div>
            </div>
          } @else if (recordings().length === 0) {
            <div class="rounded-lg border border-border bg-card p-8 text-center">
              <svg class="mx-auto h-12 w-12 text-muted-foreground" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" d="M12 18.75a6 6 0 0 0 6-6v-1.5m-6 7.5a6 6 0 0 1-6-6v-1.5m6 7.5v3.75m-3.75 0h7.5M12 15.75a3 3 0 0 1-3-3V4.5a3 3 0 1 1 6 0v8.25a3 3 0 0 1-3 3Z" />
              </svg>
              <h2 class="mt-4 text-lg font-semibold text-foreground">No recordings yet</h2>
              <p class="mt-2 text-sm text-muted-foreground">Upload lesson recordings from your companion apps.</p>
              <button
                (click)="view.set('upload')"
                class="mt-4 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
              >
                Upload your first recording
              </button>
            </div>
          } @else {
            <div class="space-y-3">
              @for (rec of recordings(); track rec.id) {
                <button
                  (click)="openDetail(rec.id)"
                  class="flex w-full items-center gap-4 rounded-lg border border-border bg-card p-4 text-left transition-colors hover:bg-accent"
                >
                  <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-primary/10">
                    <svg class="h-5 w-5 text-primary" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                      <path stroke-linecap="round" stroke-linejoin="round" d="M12 18.75a6 6 0 0 0 6-6v-1.5m-6 7.5a6 6 0 0 1-6-6v-1.5m6 7.5v3.75m-3.75 0h7.5M12 15.75a3 3 0 0 1-3-3V4.5a3 3 0 1 1 6 0v8.25a3 3 0 0 1-3 3Z" />
                    </svg>
                  </div>
                  <div class="min-w-0 flex-1">
                    <p class="truncate font-medium text-foreground">{{ rec.title }}</p>
                    <div class="flex items-center gap-3 text-xs text-muted-foreground">
                      <span>{{ rec.createdAt | date:'medium' }}</span>
                      <span>{{ formatFileSize(rec.fileSizeBytes) }}</span>
                      @if (rec.courseTitle) {
                        <span>{{ rec.courseTitle }}</span>
                      }
                    </div>
                  </div>
                  <span
                    class="shrink-0 rounded-full px-2.5 py-0.5 text-xs font-medium"
                    [class]="getStatusClasses(rec.status)"
                  >
                    {{ rec.status }}
                  </span>
                </button>
              }
            </div>
          }
        }

        @case ('upload') {
          <div class="flex items-center gap-3">
            <button
              (click)="view.set('list')"
              class="rounded-md p-1 text-muted-foreground hover:bg-accent hover:text-foreground"
              aria-label="Back to recordings"
            >
              <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M15.75 19.5 8.25 12l7.5-7.5" /></svg>
            </button>
            <h1 class="text-2xl font-bold text-foreground">Upload Recording</h1>
          </div>

          <div class="rounded-lg border border-border bg-card p-6">
            <div class="space-y-4">
              <div>
                <label for="title" class="block text-sm font-medium text-foreground">Title</label>
                <input
                  #titleInput
                  id="title"
                  type="text"
                  placeholder="e.g. Math Lecture Week 5"
                  class="mt-1 w-full rounded-md border border-border bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                />
              </div>

              <div>
                <label for="description" class="block text-sm font-medium text-foreground">Description (optional)</label>
                <textarea
                  #descInput
                  id="description"
                  rows="2"
                  placeholder="Notes about this recording..."
                  class="mt-1 w-full rounded-md border border-border bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                ></textarea>
              </div>

              <div>
                <label for="file" class="block text-sm font-medium text-foreground">Audio File</label>
                <div
                  class="mt-1 flex items-center justify-center rounded-lg border-2 border-dashed border-border p-8 transition-colors"
                  [class.border-primary]="selectedFile()"
                  [class.bg-primary/5]="selectedFile()"
                >
                  @if (selectedFile(); as f) {
                    <div class="text-center">
                      <svg class="mx-auto h-8 w-8 text-primary" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M9 12.75 11.25 15 15 9.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
                      </svg>
                      <p class="mt-2 text-sm font-medium text-foreground">{{ f.name }}</p>
                      <p class="text-xs text-muted-foreground">{{ formatFileSize(f.size) }}</p>
                      <button
                        (click)="clearFile()"
                        class="mt-2 text-xs text-primary hover:underline"
                      >
                        Change file
                      </button>
                    </div>
                  } @else {
                    <div class="text-center">
                      <svg class="mx-auto h-8 w-8 text-muted-foreground" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M3 16.5v2.25A2.25 2.25 0 0 0 5.25 21h13.5A2.25 2.25 0 0 0 21 18.75V16.5m-13.5-9L12 3m0 0 4.5 4.5M12 3v13.5" />
                      </svg>
                      <p class="mt-2 text-sm text-muted-foreground">
                        <button (click)="fileInput.click()" class="font-medium text-primary hover:underline">Click to upload</button>
                        or drag and drop
                      </p>
                      <p class="mt-1 text-xs text-muted-foreground">MP3, WAV, OGG, FLAC, AAC, M4A, WebM (max 500 MB)</p>
                    </div>
                  }
                </div>
                <input
                  #fileInput
                  id="file"
                  type="file"
                  accept="audio/*"
                  (change)="onFileSelected($event)"
                  class="hidden"
                />
              </div>

              @if (uploadError()) {
                <div class="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
                  {{ uploadError() }}
                </div>
              }

              <button
                (click)="upload(titleInput.value, descInput.value)"
                [disabled]="uploading() || !selectedFile() || !titleInput.value.trim()"
                class="w-full rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90 disabled:cursor-not-allowed disabled:opacity-50"
              >
                @if (uploading()) {
                  <span class="flex items-center justify-center gap-2">
                    <div class="h-4 w-4 animate-spin rounded-full border-2 border-primary-foreground border-t-transparent"></div>
                    Uploading...
                  </span>
                } @else {
                  Upload Recording
                }
              </button>
            </div>
          </div>
        }

        @case ('detail') {
          @if (detailLoading()) {
            <div class="flex items-center justify-center py-12">
              <div class="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent"></div>
            </div>
          } @else if (detail(); as d) {
            <div class="flex items-center gap-3">
              <button
                (click)="view.set('list')"
                class="rounded-md p-1 text-muted-foreground hover:bg-accent hover:text-foreground"
                aria-label="Back to recordings"
              >
                <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M15.75 19.5 8.25 12l7.5-7.5" /></svg>
              </button>
              <h1 class="text-2xl font-bold text-foreground">{{ d.title }}</h1>
              <span
                class="rounded-full px-2.5 py-0.5 text-xs font-medium"
                [class]="getStatusClasses(d.status)"
              >
                {{ d.status }}
              </span>
            </div>

            <div class="grid gap-6 lg:grid-cols-3">
              <!-- Info card -->
              <div class="rounded-lg border border-border bg-card p-5 lg:col-span-2">
                @if (d.description) {
                  <p class="text-sm text-muted-foreground">{{ d.description }}</p>
                }
                <div class="mt-4 grid grid-cols-2 gap-4 text-sm">
                  <div>
                    <p class="text-muted-foreground">File</p>
                    <p class="font-medium text-foreground">{{ d.fileName }}</p>
                  </div>
                  <div>
                    <p class="text-muted-foreground">Size</p>
                    <p class="font-medium text-foreground">{{ formatFileSize(d.fileSizeBytes) }}</p>
                  </div>
                  <div>
                    <p class="text-muted-foreground">Uploaded</p>
                    <p class="font-medium text-foreground">{{ d.createdAt | date:'medium' }}</p>
                  </div>
                  @if (d.courseTitle) {
                    <div>
                      <p class="text-muted-foreground">Course</p>
                      <p class="font-medium text-foreground">{{ d.courseTitle }}</p>
                    </div>
                  }
                  @if (d.durationSeconds) {
                    <div>
                      <p class="text-muted-foreground">Duration</p>
                      <p class="font-medium text-foreground">{{ formatDuration(d.durationSeconds) }}</p>
                    </div>
                  }
                </div>
              </div>

              <!-- Actions card -->
              <div class="space-y-3">
                <div class="rounded-lg border border-border bg-card p-5">
                  <h2 class="text-sm font-semibold text-foreground">Actions</h2>
                  <div class="mt-3 space-y-2">
                    <button
                      (click)="downloadRecording(d.id)"
                      class="w-full rounded-md border border-border px-3 py-2 text-sm text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
                    >
                      Download Audio
                    </button>
                    <button
                      (click)="confirmDelete(d.id)"
                      class="w-full rounded-md border border-destructive/30 px-3 py-2 text-sm text-destructive transition-colors hover:bg-destructive/10"
                    >
                      Delete Recording
                    </button>
                  </div>
                </div>
              </div>
            </div>

            <!-- Transcript -->
            @if (d.transcriptText) {
              <div class="rounded-lg border border-border bg-card p-5">
                <div class="flex items-center justify-between">
                  <h2 class="text-sm font-semibold text-foreground">Transcript</h2>
                  @if (d.transcribedAt) {
                    <span class="text-xs text-muted-foreground">Transcribed {{ d.transcribedAt | date:'medium' }}</span>
                  }
                </div>
                <div class="mt-3 max-h-96 overflow-y-auto whitespace-pre-wrap text-sm text-foreground">
                  {{ d.transcriptText }}
                </div>
              </div>
            } @else if (d.status === 'Transcribing') {
              <div class="rounded-lg border border-border bg-card p-5 text-center">
                <div class="mx-auto h-6 w-6 animate-spin rounded-full border-4 border-primary border-t-transparent"></div>
                <p class="mt-2 text-sm text-muted-foreground">Transcription in progress...</p>
              </div>
            } @else if (d.status === 'Failed' && d.errorMessage) {
              <div class="rounded-md bg-destructive/10 p-4 text-sm text-destructive">
                <p class="font-medium">Processing failed</p>
                <p class="mt-1">{{ d.errorMessage }}</p>
              </div>
            }
          }
        }
      }
    </div>
  `,
})
export class RecordingsComponent implements OnInit {
  private readonly recordingService = inject(RecordingService);

  readonly view = signal<View>('list');
  readonly loading = signal(true);
  readonly recordings = signal<Recording[]>([]);
  readonly selectedFile = signal<File | null>(null);
  readonly uploading = signal(false);
  readonly uploadError = signal<string | null>(null);
  readonly detail = signal<RecordingDetail | null>(null);
  readonly detailLoading = signal(false);

  ngOnInit(): void {
    this.loadRecordings();
  }

  loadRecordings(): void {
    this.loading.set(true);
    this.recordingService.getRecordings().subscribe({
      next: (data) => {
        this.recordings.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) {
      this.selectedFile.set(file);
      this.uploadError.set(null);
    }
  }

  clearFile(): void {
    this.selectedFile.set(null);
  }

  upload(title: string, description: string): void {
    const file = this.selectedFile();
    if (!file || !title.trim()) return;

    this.uploading.set(true);
    this.uploadError.set(null);

    this.recordingService.upload(file, title.trim(), description.trim() || undefined).subscribe({
      next: () => {
        this.uploading.set(false);
        this.selectedFile.set(null);
        this.view.set('list');
        this.loadRecordings();
      },
      error: (err) => {
        this.uploading.set(false);
        this.uploadError.set(err.error || 'Upload failed. Please try again.');
      },
    });
  }

  openDetail(id: string): void {
    this.detailLoading.set(true);
    this.detail.set(null);
    this.view.set('detail');

    this.recordingService.getRecording(id).subscribe({
      next: (data) => {
        this.detail.set(data);
        this.detailLoading.set(false);
      },
      error: () => {
        this.detailLoading.set(false);
        this.view.set('list');
      },
    });
  }

  downloadRecording(id: string): void {
    this.recordingService.getDownloadUrl(id).subscribe({
      next: (res) => {
        window.open(res.url, '_blank');
      },
    });
  }

  confirmDelete(id: string): void {
    this.recordingService.delete(id).subscribe({
      next: () => {
        this.view.set('list');
        this.loadRecordings();
      },
    });
  }

  formatFileSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    if (bytes < 1024 * 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
    return `${(bytes / (1024 * 1024 * 1024)).toFixed(1)} GB`;
  }

  formatDuration(seconds: number): string {
    const h = Math.floor(seconds / 3600);
    const m = Math.floor((seconds % 3600) / 60);
    const s = seconds % 60;
    if (h > 0) return `${h}h ${m}m ${s}s`;
    if (m > 0) return `${m}m ${s}s`;
    return `${s}s`;
  }

  getStatusClasses(status: RecordingStatus): string {
    switch (status) {
      case 'Uploaded':
        return 'bg-blue-500/10 text-blue-500';
      case 'Transcribing':
      case 'Indexing':
        return 'bg-orange-500/10 text-orange-500';
      case 'Transcribed':
        return 'bg-cyan-500/10 text-cyan-500';
      case 'Completed':
        return 'bg-green-500/10 text-green-500';
      case 'Failed':
        return 'bg-destructive/10 text-destructive';
    }
  }
}
