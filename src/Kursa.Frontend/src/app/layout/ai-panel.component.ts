import { ChangeDetectionStrategy, Component, inject, signal, ElementRef, viewChild, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { marked } from 'marked';
import { NgIconComponent, provideIcons } from '@ng-icons/core';
import { lucideArrowUp } from '@ng-icons/lucide';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmInputGroupImports } from '@spartan-ng/helm/input-group';
import { AiContextService, ViewContext } from '../core/services/ai-context.service';
import { ChatService, ChatMessage, Citation, ChatResponse } from '../core/services/chat.service';

@Component({
  selector: 'app-ai-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, HlmButton, ...HlmInputGroupImports, NgIconComponent],
  providers: [provideIcons({ lucideArrowUp })],
  template: `
    <aside
      class="fixed inset-y-0 right-0 z-30 flex w-96 flex-col border-l border-border bg-card transition-transform duration-300"
      [class.translate-x-full]="!contextService.panelOpen()"
      [class.translate-x-0]="contextService.panelOpen()"
      role="complementary"
      aria-label="AI Assistant"
    >
      <!-- Header -->
      <div class="flex h-14 items-center justify-between border-b border-border px-4">
        <div class="flex items-center gap-2">
          <svg class="h-5 w-5 text-primary" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" aria-hidden="true">
            <path stroke-linecap="round" stroke-linejoin="round" d="M9.813 15.904 9 18.75l-.813-2.846a4.5 4.5 0 0 0-3.09-3.09L2.25 12l2.846-.813a4.5 4.5 0 0 0 3.09-3.09L9 5.25l.813 2.846a4.5 4.5 0 0 0 3.09 3.09L15.75 12l-2.846.813a4.5 4.5 0 0 0-3.09 3.09ZM18.259 8.715 18 9.75l-.259-1.035a3.375 3.375 0 0 0-2.455-2.456L14.25 6l1.036-.259a3.375 3.375 0 0 0 2.455-2.456L18 2.25l.259 1.035a3.375 3.375 0 0 0 2.455 2.456L21.75 6l-1.036.259a3.375 3.375 0 0 0-2.455 2.456ZM16.894 20.567 16.5 21.75l-.394-1.183a2.25 2.25 0 0 0-1.423-1.423L13.5 18.75l1.183-.394a2.25 2.25 0 0 0 1.423-1.423l.394-1.183.394 1.183a2.25 2.25 0 0 0 1.423 1.423l1.183.394-1.183.394a2.25 2.25 0 0 0-1.423 1.423Z" />
          </svg>
          <span class="text-sm font-semibold text-foreground">Kursa AI</span>
        </div>
        <button
          hlmBtn
          variant="ghost"
          size="icon"
          (click)="contextService.closePanel()"
          aria-label="Close AI panel"
        >
          <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" d="M6 18 18 6M6 6l12 12" />
          </svg>
        </button>
      </div>

      <!-- Context banner -->
      @if (contextService.hasContext()) {
        <div class="border-b border-border bg-primary/5 px-4 py-2.5 flex items-start gap-2.5">
          <div class="mt-0.5 flex h-6 w-6 shrink-0 items-center justify-center rounded-md bg-primary/15 text-primary" aria-hidden="true">
            <svg class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" [attr.d]="contextIconPath()" />
            </svg>
          </div>
          <div class="min-w-0 flex-1">
            <p class="text-xs font-medium text-primary">{{ contextLabel() }}</p>
            <p class="text-sm font-semibold text-foreground leading-tight line-clamp-2">{{ contextService.currentContext().title }}</p>
            @if (contextService.currentContext().description) {
              <p class="text-xs text-muted-foreground mt-0.5">{{ contextService.currentContext().description }}</p>
            }
          </div>
        </div>
      }

      <!-- Messages -->
      <div #messagesContainer class="flex-1 overflow-y-auto p-4 space-y-3" role="log" aria-label="AI conversation">
        @if (messages().length === 0) {
          <div class="flex h-full items-center justify-center">
            <div class="text-center space-y-2 px-4">
              <p class="text-sm text-muted-foreground">
                @if (contextService.hasContext()) {
                  Ask me about <strong>{{ contextService.currentContext().title }}</strong>
                } @else {
                  Ask me anything about your course materials.
                }
              </p>
            </div>
          </div>
        }

        @for (msg of messages(); track msg.id) {
          <div [class]="msg.role === 'user' ? 'flex justify-end' : 'flex justify-start'">
            <div
              class="max-w-[85%] rounded-lg px-3 py-2 text-sm"
              [class]="msg.role === 'user' ? 'bg-primary text-primary-foreground' : 'bg-muted text-foreground'"
            >
              @if (msg.role === 'assistant') {
                <div class="markdown-body prose prose-sm prose-invert max-w-none" [innerHTML]="renderMarkdown(msg.content)"></div>
              } @else {
                <p class="whitespace-pre-wrap">{{ msg.content }}</p>
              }
            </div>
          </div>
        }

        @if (sources().length > 0) {
          <div class="rounded-md border border-border bg-card p-3">
            <p class="mb-1 text-xs font-medium text-muted-foreground">Sources</p>
            @for (source of sources(); track source.contentId; let i = $index) {
              <p class="text-xs text-muted-foreground">
                <span class="font-medium text-foreground">[{{ i + 1 }}]</span> {{ source.contentTitle }}
              </p>
            }
          </div>
        }

        @if (loading()) {
          <div class="flex justify-start">
            <div class="rounded-lg bg-muted px-3 py-2">
              <div class="flex space-x-1">
                <span class="h-1.5 w-1.5 animate-bounce rounded-full bg-muted-foreground/50"></span>
                <span class="h-1.5 w-1.5 animate-bounce rounded-full bg-muted-foreground/50" style="animation-delay: 0.15s"></span>
                <span class="h-1.5 w-1.5 animate-bounce rounded-full bg-muted-foreground/50" style="animation-delay: 0.3s"></span>
              </div>
            </div>
          </div>
        }
      </div>

      <!-- Input -->
      <div class="border-t border-border p-3">
        <form (submit)="send($event)">
          <label for="ai-panel-input" class="sr-only">Ask Kursa AI</label>
          <div hlmInputGroup>
            <textarea
              hlmInputGroupTextarea
              id="ai-panel-input"
              [(ngModel)]="inputMessage"
              name="message"
              [placeholder]="inputPlaceholder()"
              [disabled]="loading()"
              rows="1"
              (keydown.enter)="onEnterKey($event)"
              class="resize-none text-sm"
            ></textarea>
            <div hlmInputGroupAddon align="block-end">
              <button
                hlmInputGroupButton
                type="submit"
                variant="default"
                size="icon-sm"
                class="ml-auto rounded-full"
                [disabled]="loading() || !inputMessage.trim()"
                aria-label="Send message"
              >
                <ng-icon name="lucideArrowUp" aria-hidden="true" />
              </button>
            </div>
          </div>
        </form>
      </div>
    </aside>
  `,
})
export class AiPanelComponent {
  readonly contextService = inject(AiContextService);
  private readonly chatService = inject(ChatService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly messagesContainer = viewChild<ElementRef<HTMLDivElement>>('messagesContainer');

  readonly messages = signal<ChatMessage[]>([]);
  readonly sources = signal<Citation[]>([]);
  readonly loading = signal(false);

  readonly contextLabel = computed(() => {
    const type = this.contextService.currentContext().type;
    const labels: Partial<Record<ViewContext['type'], string>> = {
      course: 'Viewing course',
      module: 'Viewing module',
      content: 'Viewing content',
      recording: 'Viewing recording',
      flashcards: 'Flashcards',
      quizzes: 'Quizzes',
    };
    return labels[type] ?? 'Context';
  });

  readonly contextIconPath = computed(() => {
    const type = this.contextService.currentContext().type;
    switch (type) {
      case 'course':
        return 'M12 6.042A8.967 8.967 0 0 0 6 3.75c-1.052 0-2.062.18-3 .512v14.25A8.987 8.987 0 0 1 6 18c2.305 0 4.408.867 6 2.292m0-14.25a8.966 8.966 0 0 1 6-2.292c1.052 0 2.062.18 3 .512v14.25A8.987 8.987 0 0 0 18 18a8.967 8.967 0 0 0-6 2.292m0-14.25v14.25';
      case 'recording':
        return 'M12 18.75a6 6 0 0 0 6-6v-1.5m-6 7.5a6 6 0 0 1-6-6v-1.5m6 7.5v3.75m-3.75 0h7.5M12 15.75a3 3 0 0 1-3-3V4.5a3 3 0 1 1 6 0v8.25a3 3 0 0 1-3 3Z';
      case 'flashcards':
        return 'M6.429 9.75 2.25 12l4.179 2.25m0-4.5 5.571 3 5.571-3m-11.142 0L2.25 7.5 12 2.25l9.75 5.25-4.179 2.25m0 0L21.75 12l-4.179 2.25m0 0 4.179 2.25L12 21.75 2.25 16.5l4.179-2.25m11.142 0-5.571 3-5.571-3';
      case 'quizzes':
        return 'M9.879 7.519c1.171-1.025 3.071-1.025 4.242 0 1.172 1.025 1.172 2.687 0 3.712-.203.179-.43.326-.67.442-.745.361-1.45.999-1.45 1.827v.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Zm-9 5.25h.008v.008H12v-.008Z';
      default:
        return 'M19.5 14.25v-2.625a3.375 3.375 0 0 0-3.375-3.375h-1.5A1.125 1.125 0 0 1 13.5 7.125v-1.5a3.375 3.375 0 0 0-3.375-3.375H8.25m2.25 0H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 0 0-9-9Z';
    }
  });

  readonly inputPlaceholder = computed(() => {
    const ctx = this.contextService.currentContext();
    if (ctx.type === 'course' && ctx.title) {
      return `Ask about ${ctx.title}…`;
    }
    return 'Ask about your course materials…';
  });

  inputMessage = '';
  private threadId: string | undefined;

  onEnterKey(event: Event): void {
    const ke = event as KeyboardEvent;
    if (!ke.shiftKey) {
      event.preventDefault();
      this.send(event);
    }
  }

  send(event: Event): void {
    event.preventDefault();
    const message = this.inputMessage.trim();
    if (!message || this.loading()) return;

    this.loading.set(true);
    this.inputMessage = '';

    // Add context prefix if available
    const context = this.contextService.currentContext();
    let fullMessage = message;
    if (context.type !== 'none' && context.title) {
      fullMessage = `[Context: ${context.type} — ${context.title}] ${message}`;
    }

    // Optimistic user message
    const tempMsg: ChatMessage = {
      id: crypto.randomUUID(),
      role: 'user',
      content: message,
      citations: null,
      tokensUsed: 0,
      createdAt: new Date().toISOString(),
    };
    this.messages.update((msgs) => [...msgs, tempMsg]);
    this.scrollToBottom();

    this.chatService.sendMessage(fullMessage, this.threadId).subscribe({
      next: (response: ChatResponse) => {
        this.loading.set(false);
        this.messages.update((msgs) => [...msgs, response.message]);
        this.sources.set(response.sources);
        this.scrollToBottom();
      },
      error: () => {
        this.loading.set(false);
        const errorMsg: ChatMessage = {
          id: crypto.randomUUID(),
          role: 'assistant',
          content: 'Sorry, something went wrong. Please try again.',
          citations: null,
          tokensUsed: 0,
          createdAt: new Date().toISOString(),
        };
        this.messages.update((msgs) => [...msgs, errorMsg]);
      },
    });
  }

  renderMarkdown(content: string): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(marked.parse(content) as string);
  }

  private scrollToBottom(): void {
    setTimeout(() => {
      const container = this.messagesContainer()?.nativeElement;
      if (container) {
        container.scrollTop = container.scrollHeight;
      }
    });
  }
}
