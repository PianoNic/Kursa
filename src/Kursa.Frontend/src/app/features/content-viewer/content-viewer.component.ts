import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { inject } from '@angular/core';

@Component({
  selector: 'app-content-viewer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="space-y-4">
      @if (breadcrumbs().length > 0) {
        <nav aria-label="Breadcrumb" class="flex items-center gap-2 text-sm text-muted-foreground">
          @for (crumb of breadcrumbs(); track crumb.label; let last = $last) {
            @if (crumb.url) {
              <a [href]="crumb.url" class="hover:text-foreground">{{ crumb.label }}</a>
            } @else {
              <span class="text-foreground font-medium">{{ crumb.label }}</span>
            }
            @if (!last) {
              <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" d="m8.25 4.5 7.5 7.5-7.5 7.5" />
              </svg>
            }
          }
        </nav>
      }

      <div class="rounded-lg border border-border bg-card overflow-hidden">
        @switch (detectedType()) {
          @case ('pdf') {
            <iframe
              [src]="safeUrl()"
              class="h-[80vh] w-full border-0"
              title="PDF viewer"
            ></iframe>
          }
          @case ('image') {
            <div class="flex items-center justify-center bg-black/5 p-4 dark:bg-white/5">
              <img
                [src]="contentUrl()"
                [alt]="fileName()"
                class="max-h-[80vh] max-w-full object-contain"
                loading="lazy"
              />
            </div>
          }
          @case ('html') {
            <div
              class="prose prose-sm dark:prose-invert max-w-none p-6"
              [innerHTML]="htmlContent()"
            ></div>
          }
          @case ('text') {
            <pre class="whitespace-pre-wrap p-6 font-mono text-sm text-foreground">{{ textContent() }}</pre>
          }
          @case ('video') {
            <video
              controls
              class="w-full"
              [src]="contentUrl()"
            >
              <track kind="captions" />
              Your browser does not support the video element.
            </video>
          }
          @case ('audio') {
            <div class="p-6">
              <audio controls class="w-full" [src]="contentUrl()">
                Your browser does not support the audio element.
              </audio>
            </div>
          }
          @case ('link') {
            <div class="p-6 text-center">
              <svg class="mx-auto h-12 w-12 text-muted-foreground" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" d="M13.19 8.688a4.5 4.5 0 0 1 1.242 7.244l-4.5 4.5a4.5 4.5 0 0 1-6.364-6.364l1.757-1.757m9.86-2.439a4.5 4.5 0 0 0-1.242-7.244l-4.5-4.5a4.5 4.5 0 0 0-6.364 6.364L4.757 8.25" />
              </svg>
              <p class="mt-4 text-foreground font-medium">External Link</p>
              <a
                [href]="contentUrl()"
                target="_blank"
                rel="noopener noreferrer"
                class="mt-2 inline-block rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
              >
                Open in new tab
              </a>
            </div>
          }
          @default {
            <div class="p-6 text-center">
              <svg class="mx-auto h-12 w-12 text-muted-foreground" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 0 0-3.375-3.375h-1.5A1.125 1.125 0 0 1 13.5 7.125v-1.5a3.375 3.375 0 0 0-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 0 0-9-9Z" />
              </svg>
              <p class="mt-4 text-foreground font-medium">{{ fileName() }}</p>
              <p class="mt-1 text-sm text-muted-foreground">This file type cannot be previewed inline.</p>
              @if (contentUrl()) {
                <a
                  [href]="contentUrl()"
                  target="_blank"
                  rel="noopener noreferrer"
                  class="mt-4 inline-block rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
                >
                  Download
                </a>
              }
            </div>
          }
        }
      </div>
    </div>
  `,
})
export class ContentViewerComponent {
  private readonly sanitizer = inject(DomSanitizer);

  /** URL of the content to display */
  readonly contentUrl = input<string>('');

  /** MIME type hint (e.g. 'application/pdf', 'text/html') */
  readonly mimeType = input<string>('');

  /** File name for display and type detection fallback */
  readonly fileName = input<string>('');

  /** Raw HTML content for inline rendering */
  readonly htmlContent = input<string>('');

  /** Raw text content for inline rendering */
  readonly textContent = input<string>('');

  /** Breadcrumb navigation items */
  readonly breadcrumbs = input<{ label: string; url?: string }[]>([]);

  readonly detectedType = computed(() => {
    const mime = this.mimeType();
    const name = this.fileName().toLowerCase();
    const url = this.contentUrl().toLowerCase();

    // Check MIME type first
    if (mime) {
      if (mime === 'application/pdf') return 'pdf';
      if (mime.startsWith('image/')) return 'image';
      if (mime.startsWith('video/')) return 'video';
      if (mime.startsWith('audio/')) return 'audio';
      if (mime === 'text/html') return 'html';
      if (mime.startsWith('text/')) return 'text';
    }

    // Fall back to file extension
    if (name.endsWith('.pdf') || url.endsWith('.pdf')) return 'pdf';
    if (/\.(png|jpg|jpeg|gif|webp|svg|bmp)$/.test(name) || /\.(png|jpg|jpeg|gif|webp|svg|bmp)$/.test(url)) return 'image';
    if (/\.(mp4|webm|ogg|mov)$/.test(name)) return 'video';
    if (/\.(mp3|wav|ogg|m4a|flac)$/.test(name)) return 'audio';
    if (/\.(html|htm)$/.test(name)) return 'html';
    if (/\.(txt|md|csv|log|json|xml|yaml|yml)$/.test(name)) return 'text';

    // Check for inline content
    if (this.htmlContent()) return 'html';
    if (this.textContent()) return 'text';

    // If there's a URL but no detectable type, treat as link
    if (this.contentUrl()) return 'link';

    return 'unknown';
  });

  readonly safeUrl = computed((): SafeResourceUrl => {
    return this.sanitizer.bypassSecurityTrustResourceUrl(this.contentUrl());
  });
}
