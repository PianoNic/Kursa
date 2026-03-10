import { ChangeDetectionStrategy, Component, inject, signal, ElementRef, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AiContextService } from '../core/services/ai-context.service';
import { ChatService, ChatMessage, Citation, ChatResponse } from '../core/services/chat.service';

@Component({
  selector: 'app-ai-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
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
          (click)="contextService.closePanel()"
          class="rounded-md p-1 text-muted-foreground hover:bg-accent hover:text-foreground"
          aria-label="Close AI panel"
        >
          <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" d="M6 18 18 6M6 6l12 12" />
          </svg>
        </button>
      </div>

      <!-- Context banner -->
      @if (contextService.hasContext()) {
        <div class="border-b border-border bg-accent/30 px-4 py-2">
          <p class="text-xs text-muted-foreground">Context</p>
          <p class="text-sm font-medium text-foreground line-clamp-1">{{ contextService.currentContext().title }}</p>
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
              class="max-w-[85%] rounded-lg px-3 py-2"
              [class]="msg.role === 'user' ? 'bg-primary text-primary-foreground' : 'bg-muted text-foreground'"
            >
              <p class="whitespace-pre-wrap text-sm">{{ msg.content }}</p>
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
        <form (submit)="send($event)" class="flex gap-2">
          <label for="ai-panel-input" class="sr-only">Ask Kursa AI</label>
          <input
            id="ai-panel-input"
            type="text"
            [(ngModel)]="inputMessage"
            name="message"
            placeholder="Ask about this..."
            [disabled]="loading()"
            class="flex-1 rounded-md border border-input bg-background px-3 py-1.5 text-sm text-foreground placeholder:text-muted-foreground focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary disabled:opacity-50"
          />
          <button
            type="submit"
            [disabled]="loading() || !inputMessage.trim()"
            class="rounded-md bg-primary px-3 py-1.5 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
            aria-label="Send message"
          >
            <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" d="M6 12 3.269 3.125A59.769 59.769 0 0 1 21.485 12 59.768 59.768 0 0 1 3.27 20.875L5.999 12Zm0 0h7.5" />
            </svg>
          </button>
        </form>
      </div>
    </aside>
  `,
})
export class AiPanelComponent {
  readonly contextService = inject(AiContextService);
  private readonly chatService = inject(ChatService);
  private readonly messagesContainer = viewChild<ElementRef<HTMLDivElement>>('messagesContainer');

  readonly messages = signal<ChatMessage[]>([]);
  readonly sources = signal<Citation[]>([]);
  readonly loading = signal(false);

  inputMessage = '';
  private threadId: string | undefined;

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

  private scrollToBottom(): void {
    setTimeout(() => {
      const container = this.messagesContainer()?.nativeElement;
      if (container) {
        container.scrollTop = container.scrollHeight;
      }
    });
  }
}
