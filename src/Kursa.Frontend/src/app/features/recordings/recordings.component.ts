import { ChangeDetectionStrategy, Component, inject, signal, computed, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmLabel } from '@spartan-ng/helm/label';
import { HlmTextarea } from '@spartan-ng/helm/textarea';
import { RecordingsService } from '../../api/api/recordings.service';
import { RecordingDto } from '../../api/model/recordingDto';
import { RecordingDetailDto } from '../../api/model/recordingDetailDto';
import { RecordingStatus } from '../../api/model/recordingStatus';
import { TranscriptSegmentDto } from '../../api/model/transcriptSegmentDto';

type View = 'list' | 'upload' | 'detail';

@Component({
  selector: 'app-recordings',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, HlmButton, ...HlmCardImports, HlmInput, HlmLabel, HlmTextarea],
  template: `
    <div class="space-y-6">
      @switch (view()) {
        @case ('list') {
          <div class="flex items-center justify-between">
            <h1 class="text-2xl font-bold text-foreground">Recordings</h1>
            <button hlmBtn (click)="view.set('upload')">
              Upload Recording
            </button>
          </div>

          @if (loading()) {
            <div class="flex items-center justify-center py-12">
              <div class="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" role="status">
                <span class="sr-only">Loading recordings...</span>
              </div>
            </div>
          } @else if (recordings().length === 0) {
            <div hlmCard class="p-8 text-center">
              <svg class="mx-auto h-12 w-12 text-muted-foreground" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" aria-hidden="true">
                <path stroke-linecap="round" stroke-linejoin="round" d="M12 18.75a6 6 0 0 0 6-6v-1.5m-6 7.5a6 6 0 0 1-6-6v-1.5m6 7.5v3.75m-3.75 0h7.5M12 15.75a3 3 0 0 1-3-3V4.5a3 3 0 1 1 6 0v8.25a3 3 0 0 1-3 3Z" />
              </svg>
              <h2 class="mt-4 text-lg font-semibold text-foreground">No recordings yet</h2>
              <p class="mt-2 text-sm text-muted-foreground">Upload lesson recordings from your companion apps.</p>
              <button hlmBtn class="mt-4" (click)="view.set('upload')">
                Upload your first recording
              </button>
            </div>
          } @else {
            <div class="space-y-3">
              @for (rec of recordings(); track rec.id) {
                <button
                  hlmBtn
                  variant="outline"
                  (click)="openDetail(rec.id!)"
                  class="h-auto w-full items-center justify-start gap-4 p-4 text-left"
                >
                  <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-primary/10" aria-hidden="true">
                    <svg class="h-5 w-5 text-primary" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" aria-hidden="true">
                      <path stroke-linecap="round" stroke-linejoin="round" d="M12 18.75a6 6 0 0 0 6-6v-1.5m-6 7.5a6 6 0 0 1-6-6v-1.5m6 7.5v3.75m-3.75 0h7.5M12 15.75a3 3 0 0 1-3-3V4.5a3 3 0 1 1 6 0v8.25a3 3 0 0 1-3 3Z" />
                    </svg>
                  </div>
                  <div class="min-w-0 flex-1">
                    <p class="truncate font-medium text-foreground">{{ rec.title }}</p>
                    <div class="flex items-center gap-3 text-xs text-muted-foreground">
                      <span>{{ rec.createdAt | date:'medium' }}</span>
                      <span>{{ formatFileSize(rec.fileSizeBytes ?? 0) }}</span>
                      @if (rec.courseTitle) {
                        <span>{{ rec.courseTitle }}</span>
                      }
                    </div>
                  </div>
                  <span
                    class="shrink-0 rounded-full px-2.5 py-0.5 text-xs font-medium"
                    [class]="getStatusClasses(rec.status)"
                  >
                    {{ getStatusLabel(rec.status) }}
                  </span>
                </button>
              }
            </div>
          }
        }

        @case ('upload') {
          <div class="flex items-center gap-3">
            <button
              hlmBtn
              variant="ghost"
              size="icon"
              (click)="view.set('list')"
              aria-label="Back to recordings"
            >
              <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" d="M15.75 19.5 8.25 12l7.5-7.5" /></svg>
            </button>
            <h1 class="text-2xl font-bold text-foreground">Upload Recording</h1>
          </div>

          <div hlmCard class="p-6">
            <div class="space-y-4">
              <div class="space-y-1.5">
                <label hlmLabel for="title">Title</label>
                <input
                  hlmInput
                  #titleInput
                  id="title"
                  type="text"
                  placeholder="e.g. Math Lecture Week 5"
                  class="w-full"
                />
              </div>

              <div class="space-y-1.5">
                <label hlmLabel for="description">Description (optional)</label>
                <textarea
                  hlmTextarea
                  #descInput
                  id="description"
                  rows="2"
                  placeholder="Notes about this recording..."
                  class="w-full"
                ></textarea>
              </div>

              <div class="space-y-1.5">
                <label hlmLabel for="file">Audio File</label>
                <div
                  class="flex items-center justify-center rounded-lg border-2 border-dashed border-border p-8 transition-colors"
                  [class.border-primary]="selectedFile()"
                  [class.bg-primary\/5]="selectedFile()"
                >
                  @if (selectedFile(); as f) {
                    <div class="text-center">
                      <svg class="mx-auto h-8 w-8 text-primary" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" aria-hidden="true">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M9 12.75 11.25 15 15 9.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
                      </svg>
                      <p class="mt-2 text-sm font-medium text-foreground">{{ f.name }}</p>
                      <p class="text-xs text-muted-foreground">{{ formatFileSize(f.size) }}</p>
                      <button
                        hlmBtn
                        variant="link"
                        size="sm"
                        (click)="clearFile()"
                        class="mt-2"
                      >
                        Change file
                      </button>
                    </div>
                  } @else {
                    <div class="text-center">
                      <svg class="mx-auto h-8 w-8 text-muted-foreground" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" aria-hidden="true">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M3 16.5v2.25A2.25 2.25 0 0 0 5.25 21h13.5A2.25 2.25 0 0 0 21 18.75V16.5m-13.5-9L12 3m0 0 4.5 4.5M12 3v13.5" />
                      </svg>
                      <p class="mt-2 text-sm text-muted-foreground">
                        <button hlmBtn variant="link" size="sm" (click)="fileInput.click()">Click to upload</button>
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
                <div class="rounded-md bg-destructive/10 p-3 text-sm text-destructive" role="alert">
                  {{ uploadError() }}
                </div>
              }

              <button
                hlmBtn
                class="w-full"
                (click)="upload(titleInput.value, descInput.value)"
                [disabled]="uploading() || !selectedFile() || !titleInput.value.trim()"
              >
                @if (uploading()) {
                  <span class="flex items-center justify-center gap-2">
                    <div class="h-4 w-4 animate-spin rounded-full border-2 border-primary-foreground border-t-transparent" aria-hidden="true"></div>
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
              <div class="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" role="status">
                <span class="sr-only">Loading recording details...</span>
              </div>
            </div>
          } @else if (detail(); as d) {
            <div class="flex items-center gap-3">
              <button
                hlmBtn
                variant="ghost"
                size="icon"
                (click)="view.set('list')"
                aria-label="Back to recordings"
              >
                <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" d="M15.75 19.5 8.25 12l7.5-7.5" /></svg>
              </button>
              <h1 class="text-2xl font-bold text-foreground">{{ d.title }}</h1>
              <span
                class="rounded-full px-2.5 py-0.5 text-xs font-medium"
                [class]="getStatusClasses(d.status)"
              >
                {{ getStatusLabel(d.status) }}
              </span>
            </div>

            <div class="grid gap-6 lg:grid-cols-3">
              <!-- Info card -->
              <div hlmCard class="p-5 lg:col-span-2">
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
                    <p class="font-medium text-foreground">{{ formatFileSize(d.fileSizeBytes ?? 0) }}</p>
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
                <div hlmCard class="p-5">
                  <h2 class="text-sm font-semibold text-foreground">Actions</h2>
                  <div class="mt-3 space-y-2">
                    @if (d.status === 0 || d.status === 5) {
                      <button hlmBtn class="w-full" (click)="retryTranscription(d.id!)">
                        {{ d.status === 5 ? 'Retry Transcription' : 'Start Transcription' }}
                      </button>
                    }
                    <button hlmBtn variant="outline" class="w-full" (click)="downloadRecording(d.id!)">
                      Download Audio
                    </button>
                    <button
                      hlmBtn
                      variant="outline"
                      class="w-full border-destructive/30 text-destructive hover:bg-destructive/10"
                      (click)="confirmDelete(d.id!)"
                    >
                      Delete Recording
                    </button>
                  </div>
                </div>
              </div>
            </div>

            <!-- Transcript Viewer -->
            @if (d.transcriptText) {
              <div hlmCard class="p-5">
                <div class="flex items-center justify-between">
                  <h2 class="text-sm font-semibold text-foreground">Transcript</h2>
                  <div class="flex items-center gap-2">
                    @if (d.transcribedAt) {
                      <span class="text-xs text-muted-foreground">{{ d.transcribedAt | date:'medium' }}</span>
                    }
                    <button
                      hlmBtn
                      variant="outline"
                      size="sm"
                      (click)="exportTranscript(d)"
                    >
                      Export
                    </button>
                  </div>
                </div>

                <!-- Search -->
                <div class="mt-3">
                  <input
                    hlmInput
                    #searchInput
                    type="text"
                    placeholder="Search transcript..."
                    (input)="transcriptSearch.set(searchInput.value)"
                    class="w-full"
                  />
                  @if (transcriptSearch()) {
                    <p class="mt-1 text-xs text-muted-foreground">
                      {{ filteredSegmentCount() }} matching segments
                    </p>
                  }
                </div>

                <!-- Segments or fallback to full text -->
                <div class="mt-3 max-h-[32rem] overflow-y-auto">
                  @if ((d.segments ?? []).length > 0) {
                    <div class="space-y-1">
                      @for (seg of getFilteredSegments(d.segments ?? []); track seg.id) {
                        <div class="flex gap-3 rounded-md px-2 py-1.5 transition-colors hover:bg-accent">
                          <button
                            hlmBtn
                            variant="link"
                            size="sm"
                            class="shrink-0 font-mono text-xs"
                            (click)="copyTimestamp(seg.startSeconds ?? 0)"
                            [attr.aria-label]="'Copy timestamp ' + formatTimestamp(seg.startSeconds ?? 0)"
                          >
                            {{ formatTimestamp(seg.startSeconds ?? 0) }}
                          </button>
                          <p class="text-sm text-foreground">{{ seg.text }}</p>
                        </div>
                      }
                    </div>
                  } @else {
                    <div class="whitespace-pre-wrap text-sm text-foreground">
                      {{ d.transcriptText }}
                    </div>
                  }
                </div>
              </div>
            } @else if (d.status === 1) {
              <div hlmCard class="p-5 text-center">
                <div class="mx-auto h-6 w-6 animate-spin rounded-full border-4 border-primary border-t-transparent" role="status">
                  <span class="sr-only">Transcription in progress...</span>
                </div>
                <p class="mt-2 text-sm text-muted-foreground" aria-hidden="true">Transcription in progress...</p>
              </div>
            } @else if (d.status === 5 && d.errorMessage) {
              <div class="rounded-md bg-destructive/10 p-4 text-sm text-destructive" role="alert">
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
  private readonly recordingService = inject(RecordingsService);

  readonly view = signal<View>('list');
  readonly loading = signal(true);
  readonly recordings = signal<RecordingDto[]>([]);
  readonly selectedFile = signal<File | null>(null);
  readonly uploading = signal(false);
  readonly uploadError = signal<string | null>(null);
  readonly detail = signal<RecordingDetailDto | null>(null);
  readonly detailLoading = signal(false);
  readonly transcriptSearch = signal('');

  readonly filteredSegmentCount = computed(() => {
    const d = this.detail();
    const search = this.transcriptSearch().toLowerCase();
    if (!d || !search) return 0;
    return (d.segments ?? []).filter(s => (s.text ?? '').toLowerCase().includes(search)).length;
  });

  ngOnInit(): void {
    this.loadRecordings();
  }

  loadRecordings(): void {
    this.loading.set(true);
    this.recordingService.apiRecordingsGet().subscribe({
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

    this.recordingService.apiRecordingsUploadPost(title.trim(), description.trim() || undefined, undefined, file).subscribe({
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
    this.transcriptSearch.set('');
    this.view.set('detail');

    this.recordingService.apiRecordingsRecordingIdGet(id).subscribe({
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

  retryTranscription(id: string): void {
    this.recordingService.apiRecordingsRecordingIdTranscribePost(id).subscribe({
      next: () => this.openDetail(id),
    });
  }

  downloadRecording(id: string): void {
    this.recordingService.apiRecordingsRecordingIdDownloadUrlGet(id).subscribe({
      next: (res) => {
        window.open(res.url, '_blank');
      },
    });
  }

  confirmDelete(id: string): void {
    this.recordingService.apiRecordingsRecordingIdDelete(id).subscribe({
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

  getFilteredSegments(segments: TranscriptSegmentDto[]): TranscriptSegmentDto[] {
    const search = this.transcriptSearch().toLowerCase();
    if (!search) return segments;
    return segments.filter(s => (s.text ?? '').toLowerCase().includes(search));
  }

  formatTimestamp(seconds: number): string {
    const h = Math.floor(seconds / 3600);
    const m = Math.floor((seconds % 3600) / 60);
    const s = Math.floor(seconds % 60);
    if (h > 0) return `${h}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
    return `${m}:${String(s).padStart(2, '0')}`;
  }

  copyTimestamp(seconds: number): void {
    navigator.clipboard.writeText(this.formatTimestamp(seconds));
  }

  exportTranscript(d: RecordingDetailDto): void {
    let content: string;
    if ((d.segments ?? []).length > 0) {
      content = (d.segments ?? [])
        .map(s => `[${this.formatTimestamp(s.startSeconds ?? 0)}] ${s.text ?? ''}`)
        .join('\n');
    } else {
      content = d.transcriptText || '';
    }

    const blob = new Blob([content], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${d.title} - Transcript.txt`;
    a.click();
    URL.revokeObjectURL(url);
  }

  getStatusLabel(status: RecordingStatus | undefined): string {
    switch (status) {
      case RecordingStatus.NUMBER_0: return 'Uploaded';
      case RecordingStatus.NUMBER_1: return 'Transcribing';
      case RecordingStatus.NUMBER_2: return 'Transcribed';
      case RecordingStatus.NUMBER_3: return 'Indexing';
      case RecordingStatus.NUMBER_4: return 'Completed';
      case RecordingStatus.NUMBER_5: return 'Failed';
      default: return 'Unknown';
    }
  }

  getStatusClasses(status: RecordingStatus | undefined): string {
    switch (status) {
      case RecordingStatus.NUMBER_0:
        return 'bg-blue-500/10 text-blue-500';
      case RecordingStatus.NUMBER_1:
      case RecordingStatus.NUMBER_3:
        return 'bg-orange-500/10 text-orange-500';
      case RecordingStatus.NUMBER_2:
        return 'bg-cyan-500/10 text-cyan-500';
      case RecordingStatus.NUMBER_4:
        return 'bg-green-500/10 text-green-500';
      case RecordingStatus.NUMBER_5:
        return 'bg-destructive/10 text-destructive';
      default:
        return '';
    }
  }
}
