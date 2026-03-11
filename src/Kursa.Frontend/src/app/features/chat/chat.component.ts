import { ChangeDetectionStrategy, Component, inject, signal, ElementRef, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { marked } from 'marked';
import { ChatService, ChatThread, ChatMessage, Citation, ChatResponse } from '../../core/services/chat.service';

@Component({
  selector: 'app-chat',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, DatePipe],
  template: `
    <div class="flex h-full">
      <!-- Thread sidebar -->
      <aside class="w-64 shrink-0 border-r border-border bg-card p-4" role="complementary" aria-label="Chat threads">
        <button
          (click)="newThread()"
          class="mb-4 w-full rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
        >
          New Chat
        </button>

        <nav aria-label="Chat thread list">
          <ul class="space-y-1" role="list">
            @for (thread of threads(); track thread.id) {
              <li>
                <button
                  (click)="selectThread(thread)"
                  class="w-full rounded-md px-3 py-2 text-left text-sm transition-colors"
                  [class]="thread.id === activeThreadId() ? 'bg-accent text-accent-foreground' : 'text-muted-foreground hover:bg-accent/50'"
                >
                  <span class="line-clamp-1">{{ thread.title }}</span>
                </button>
              </li>
            }
          </ul>
        </nav>
      </aside>

      <!-- Chat area -->
      <div class="flex flex-1 flex-col">
        <!-- Messages -->
        <div #messagesContainer class="flex-1 overflow-y-auto p-6 space-y-4" role="log" aria-label="Chat messages">
          @if (messages().length === 0 && !loading()) {
            <div class="flex h-full items-center justify-center">
              <div class="text-center space-y-3">
                <svg class="mx-auto h-12 w-12 text-muted-foreground/50" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M7.5 8.25h9m-9 3H12m-9.75 1.51c0 1.6 1.123 2.994 2.707 3.227 1.129.166 2.27.293 3.423.379.35.026.67.21.865.501L12 21l2.755-4.133a1.14 1.14 0 0 1 .865-.501 48.172 48.172 0 0 0 3.423-.379c1.584-.233 2.707-1.626 2.707-3.228V6.741c0-1.602-1.123-2.995-2.707-3.228A48.394 48.394 0 0 0 12 3c-2.392 0-4.744.175-7.043.513C3.373 3.746 2.25 5.14 2.25 6.741v6.018Z" />
                </svg>
                <h3 class="text-lg font-medium text-foreground">Ask Kursa anything</h3>
                <p class="text-sm text-muted-foreground">Questions are answered using your pinned course materials.</p>
              </div>
            </div>
          }

          @for (msg of messages(); track msg.id) {
            <div [class]="msg.role === 'user' ? 'flex justify-end' : 'flex justify-start'">
              <div
                class="max-w-2xl rounded-lg px-4 py-3"
                [class]="msg.role === 'user' ? 'bg-primary text-primary-foreground' : 'bg-muted text-foreground'"
              >
                @if (msg.role === 'assistant') {
                  <div class="markdown-body text-sm text-foreground" [innerHTML]="renderMarkdown(msg.content)"></div>
                } @else {
                  <p class="whitespace-pre-wrap text-sm">{{ msg.content }}</p>
                }
                <time class="mt-1 block text-xs opacity-60">{{ msg.createdAt | date:'short' }}</time>
              </div>
            </div>
          }

          @if (activeSources().length > 0) {
            <div class="rounded-lg border border-border bg-card p-4">
              <h4 class="mb-2 text-sm font-medium text-foreground">Sources</h4>
              <ul class="space-y-2" role="list">
                @for (source of activeSources(); track source.contentId; let i = $index) {
                  <li class="text-xs text-muted-foreground">
                    <span class="font-medium text-foreground">[{{ i + 1 }}]</span>
                    {{ source.contentTitle }}
                    <span class="italic"> — score: {{ source.score.toFixed(2) }}</span>
                  </li>
                }
              </ul>
            </div>
          }

          @if (loading()) {
            <div class="flex justify-start">
              <div class="rounded-lg bg-muted px-4 py-3">
                <div class="flex space-x-1">
                  <span class="h-2 w-2 animate-bounce rounded-full bg-muted-foreground/50"></span>
                  <span class="h-2 w-2 animate-bounce rounded-full bg-muted-foreground/50" style="animation-delay: 0.15s"></span>
                  <span class="h-2 w-2 animate-bounce rounded-full bg-muted-foreground/50" style="animation-delay: 0.3s"></span>
                </div>
              </div>
            </div>
          }
        </div>

        <!-- Input -->
        <div class="border-t border-border p-4">
          <form (submit)="send($event)" class="flex gap-3">
            <label for="chat-input" class="sr-only">Type your message</label>
            <input
              id="chat-input"
              type="text"
              [(ngModel)]="inputMessage"
              name="message"
              placeholder="Ask about your course materials..."
              [disabled]="loading()"
              class="flex-1 rounded-md border border-input bg-background px-4 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary disabled:opacity-50"
            />
            <button
              type="submit"
              [disabled]="loading() || !inputMessage.trim()"
              class="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
            >
              Send
            </button>
          </form>
          @if (error()) {
            <p class="mt-2 text-sm text-destructive" role="alert">{{ error() }}</p>
          }
        </div>
      </div>
    </div>
  `,
})
export class ChatComponent {
  private readonly chatService = inject(ChatService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly messagesContainer = viewChild<ElementRef<HTMLDivElement>>('messagesContainer');

  readonly threads = signal<ChatThread[]>([]);
  readonly messages = signal<ChatMessage[]>([]);
  readonly activeThreadId = signal<string | null>(null);
  readonly activeSources = signal<Citation[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  inputMessage = '';

  constructor() {
    this.loadThreads();
  }

  loadThreads(): void {
    this.chatService.getThreads().subscribe({
      next: (threads) => this.threads.set(threads),
      error: () => {},
    });
  }

  selectThread(thread: ChatThread): void {
    this.activeThreadId.set(thread.id);
    this.activeSources.set([]);
    this.chatService.getMessages(thread.id).subscribe({
      next: (messages) => {
        this.messages.set(messages);
        this.scrollToBottom();
      },
      error: () => this.error.set('Failed to load messages.'),
    });
  }

  newThread(): void {
    this.activeThreadId.set(null);
    this.messages.set([]);
    this.activeSources.set([]);
    this.error.set(null);
  }

  send(event: Event): void {
    event.preventDefault();
    const message = this.inputMessage.trim();
    if (!message || this.loading()) return;

    this.loading.set(true);
    this.error.set(null);
    this.inputMessage = '';

    // Optimistically show user message
    const tempUserMsg: ChatMessage = {
      id: crypto.randomUUID(),
      role: 'user',
      content: message,
      citations: null,
      tokensUsed: 0,
      createdAt: new Date().toISOString(),
    };
    this.messages.update((msgs) => [...msgs, tempUserMsg]);
    this.scrollToBottom();

    const threadId = this.activeThreadId() ?? undefined;

    this.chatService.sendMessage(message, threadId).subscribe({
      next: (response: ChatResponse) => {
        this.loading.set(false);

        // Set thread ID if this was a new thread
        if (!this.activeThreadId()) {
          this.loadThreads();
        }

        this.messages.update((msgs) => [...msgs, response.message]);
        this.activeSources.set(response.sources);

        // Extract thread ID from response if available
        if (response.message.id) {
          this.scrollToBottom();
        }
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Failed to send message. Please try again.');
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
