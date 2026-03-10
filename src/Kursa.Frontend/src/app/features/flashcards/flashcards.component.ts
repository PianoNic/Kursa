import { ChangeDetectionStrategy, Component, inject, signal, computed, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  FlashcardDeck,
  Flashcard,
  FlashcardService,
} from '../../core/services/flashcard.service';
import { PinnedContent, PinnedContentService } from '../../core/services/pinned-content.service';

type View = 'decks' | 'generate' | 'review' | 'add-card';

@Component({
  selector: 'app-flashcards',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, FormsModule],
  template: `
    <div class="space-y-6">
      @switch (view()) {
        @case ('decks') {
          <div class="flex items-center justify-between">
            <h1 class="text-2xl font-bold text-foreground">Flashcards</h1>
            <button
              (click)="showGenerate()"
              class="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
            >
              Generate Deck
            </button>
          </div>

          @if (loading()) {
            <div class="flex items-center justify-center py-12">
              <div class="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent"></div>
              <span class="ml-3 text-muted-foreground">Loading decks...</span>
            </div>
          } @else if (decks().length === 0) {
            <div class="rounded-lg border border-border bg-card p-8 text-center">
              <svg class="mx-auto h-12 w-12 text-muted-foreground" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" d="M6.429 9.75 2.25 12l4.179 2.25m0-4.5 5.571 3 5.571-3m-11.142 0L2.25 7.5 12 2.25l9.75 5.25-4.179 2.25m0 0L21.75 12l-4.179 2.25m0 0 4.179 2.25L12 21.75 2.25 16.5l4.179-2.25m11.142 0-5.571 3-5.571-3" />
              </svg>
              <h2 class="mt-4 text-lg font-semibold text-foreground">No flashcard decks</h2>
              <p class="mt-2 text-sm text-muted-foreground">
                Generate flashcards from your pinned course materials for spaced repetition study.
              </p>
              <button
                (click)="showGenerate()"
                class="mt-4 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
              >
                Generate Your First Deck
              </button>
            </div>
          } @else {
            <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
              @for (deck of decks(); track deck.id) {
                <div class="rounded-lg border border-border bg-card p-5 transition-colors hover:border-primary/50">
                  <h3 class="font-semibold text-foreground">{{ deck.title }}</h3>
                  @if (deck.description) {
                    <p class="mt-1 text-xs text-muted-foreground">{{ deck.description }}</p>
                  }
                  <div class="mt-3 flex items-center gap-4 text-xs text-muted-foreground">
                    <span>{{ deck.cardCount }} cards</span>
                    @if (deck.dueCount > 0) {
                      <span class="font-medium text-primary">{{ deck.dueCount }} due</span>
                    } @else {
                      <span class="text-green-500">All caught up</span>
                    }
                  </div>
                  <div class="mt-4 flex gap-2">
                    <button
                      (click)="startReview(deck.id)"
                      [disabled]="deck.dueCount === 0"
                      class="flex-1 rounded-md bg-primary px-3 py-1.5 text-xs font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
                    >
                      Review ({{ deck.dueCount }})
                    </button>
                    <button
                      (click)="showAddCard(deck.id)"
                      class="rounded-md border border-border px-3 py-1.5 text-xs font-medium text-muted-foreground hover:bg-accent"
                    >
                      Add Card
                    </button>
                  </div>
                  <p class="mt-2 text-xs text-muted-foreground">{{ deck.createdAt | date:'mediumDate' }}</p>
                </div>
              }
            </div>
          }
        }

        @case ('generate') {
          <div class="flex items-center gap-3">
            <button
              (click)="view.set('decks')"
              class="rounded-md p-2 text-muted-foreground hover:bg-accent"
              aria-label="Back to decks"
            >
              <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5 3 12m0 0 7.5-7.5M3 12h18" />
              </svg>
            </button>
            <h1 class="text-2xl font-bold text-foreground">Generate Flashcards</h1>
          </div>

          @if (loadingPinned()) {
            <div class="flex items-center justify-center py-12">
              <div class="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent"></div>
            </div>
          } @else if (indexedItems().length === 0) {
            <div class="rounded-lg border border-border bg-card p-8 text-center">
              <h2 class="text-lg font-semibold text-foreground">No indexed content</h2>
              <p class="mt-2 text-sm text-muted-foreground">Pin and index course materials first.</p>
            </div>
          } @else {
            <div class="mx-auto max-w-lg space-y-6 rounded-lg border border-border bg-card p-6">
              <div>
                <label for="fc-content" class="block text-sm font-medium text-foreground">Select Material</label>
                <select
                  id="fc-content"
                  [(ngModel)]="selectedContentId"
                  class="mt-1 w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                >
                  <option value="">Choose pinned content...</option>
                  @for (item of indexedItems(); track item.contentId) {
                    <option [value]="item.contentId">{{ item.contentTitle }}</option>
                  }
                </select>
              </div>
              <div>
                <label for="fc-count" class="block text-sm font-medium text-foreground">Number of Cards</label>
                <input
                  id="fc-count"
                  type="number"
                  [(ngModel)]="cardCount"
                  min="1"
                  max="100"
                  class="mt-1 w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                />
              </div>
              <div>
                <label for="fc-topic" class="block text-sm font-medium text-foreground">Topic (optional)</label>
                <input
                  id="fc-topic"
                  type="text"
                  [(ngModel)]="topicInput"
                  placeholder="Focus on a specific topic..."
                  class="mt-1 w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                />
              </div>

              @if (error()) {
                <div class="rounded-md border border-destructive/50 bg-destructive/10 p-3">
                  <p class="text-sm text-destructive">{{ error() }}</p>
                </div>
              }

              <button
                (click)="generateDeck()"
                [disabled]="generating() || !selectedContentId"
                class="w-full rounded-md bg-primary px-4 py-2.5 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
              >
                @if (generating()) {
                  <span class="flex items-center justify-center gap-2">
                    <span class="h-4 w-4 animate-spin rounded-full border-2 border-primary-foreground border-t-transparent"></span>
                    Generating...
                  </span>
                } @else {
                  Generate Flashcards
                }
              </button>
            </div>
          }
        }

        @case ('review') {
          @if (reviewCards().length > 0) {
            <div class="mx-auto max-w-xl space-y-6">
              <div class="flex items-center justify-between">
                <div class="flex items-center gap-3">
                  <button
                    (click)="backToDecks()"
                    class="rounded-md p-2 text-muted-foreground hover:bg-accent"
                    aria-label="Back to decks"
                  >
                    <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                      <path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5 3 12m0 0 7.5-7.5M3 12h18" />
                    </svg>
                  </button>
                  <h1 class="text-xl font-bold text-foreground">Review</h1>
                </div>
                <span class="text-sm text-muted-foreground">
                  {{ currentCardIndex() + 1 }}/{{ reviewCards().length }}
                </span>
              </div>

              <!-- Progress -->
              <div class="h-1.5 w-full overflow-hidden rounded-full bg-muted">
                <div
                  class="h-full rounded-full bg-primary transition-all"
                  [style.width.%]="((currentCardIndex() + 1) / reviewCards().length) * 100"
                ></div>
              </div>

              @if (currentCard(); as card) {
                <!-- Flashcard -->
                <button
                  (click)="flipped.set(!flipped())"
                  class="w-full cursor-pointer rounded-lg border border-border bg-card p-8 text-center transition-all hover:border-primary/50 focus:outline-none focus:ring-2 focus:ring-ring"
                  [attr.aria-label]="flipped() ? 'Showing answer, click to show question' : 'Showing question, click to reveal answer'"
                >
                  @if (!flipped()) {
                    <p class="text-xs font-medium uppercase tracking-wider text-muted-foreground">Question</p>
                    <p class="mt-4 text-lg font-medium text-foreground">{{ card.front }}</p>
                    <p class="mt-6 text-xs text-muted-foreground">Click to reveal answer</p>
                  } @else {
                    <p class="text-xs font-medium uppercase tracking-wider text-muted-foreground">Answer</p>
                    <p class="mt-4 text-lg font-medium text-foreground">{{ card.back }}</p>
                  }
                </button>

                <!-- Rating buttons (only when flipped) -->
                @if (flipped()) {
                  <div class="space-y-2">
                    <p class="text-center text-sm text-muted-foreground">How well did you know this?</p>
                    <div class="grid grid-cols-4 gap-2">
                      <button
                        (click)="rateCard(1)"
                        class="rounded-md border border-destructive/50 bg-destructive/10 py-2 text-xs font-medium text-destructive hover:bg-destructive/20"
                      >
                        Again
                      </button>
                      <button
                        (click)="rateCard(3)"
                        class="rounded-md border border-orange-500/50 bg-orange-500/10 py-2 text-xs font-medium text-orange-500 hover:bg-orange-500/20"
                      >
                        Hard
                      </button>
                      <button
                        (click)="rateCard(4)"
                        class="rounded-md border border-primary/50 bg-primary/10 py-2 text-xs font-medium text-primary hover:bg-primary/20"
                      >
                        Good
                      </button>
                      <button
                        (click)="rateCard(5)"
                        class="rounded-md border border-green-500/50 bg-green-500/10 py-2 text-xs font-medium text-green-500 hover:bg-green-500/20"
                      >
                        Easy
                      </button>
                    </div>
                  </div>
                }
              }
            </div>
          } @else {
            <div class="mx-auto max-w-xl text-center">
              <div class="rounded-lg border border-border bg-card p-8">
                <svg class="mx-auto h-12 w-12 text-green-500" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M9 12.75 11.25 15 15 9.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
                </svg>
                <h2 class="mt-4 text-lg font-semibold text-foreground">Review Complete!</h2>
                <p class="mt-2 text-sm text-muted-foreground">
                  You reviewed {{ reviewedCount() }} card{{ reviewedCount() === 1 ? '' : 's' }}. Great work!
                </p>
                <button
                  (click)="backToDecks()"
                  class="mt-4 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
                >
                  Back to Decks
                </button>
              </div>
            </div>
          }
        }

        @case ('add-card') {
          <div class="flex items-center gap-3">
            <button
              (click)="backToDecks()"
              class="rounded-md p-2 text-muted-foreground hover:bg-accent"
              aria-label="Back to decks"
            >
              <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5 3 12m0 0 7.5-7.5M3 12h18" />
              </svg>
            </button>
            <h1 class="text-2xl font-bold text-foreground">Add Flashcard</h1>
          </div>

          <div class="mx-auto max-w-lg space-y-6 rounded-lg border border-border bg-card p-6">
            <div>
              <label for="card-front" class="block text-sm font-medium text-foreground">Front (Question)</label>
              <textarea
                id="card-front"
                [(ngModel)]="newCardFront"
                rows="3"
                placeholder="Enter the question or prompt..."
                class="mt-1 w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
              ></textarea>
            </div>
            <div>
              <label for="card-back" class="block text-sm font-medium text-foreground">Back (Answer)</label>
              <textarea
                id="card-back"
                [(ngModel)]="newCardBack"
                rows="3"
                placeholder="Enter the answer..."
                class="mt-1 w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
              ></textarea>
            </div>

            @if (error()) {
              <div class="rounded-md border border-destructive/50 bg-destructive/10 p-3">
                <p class="text-sm text-destructive">{{ error() }}</p>
              </div>
            }

            @if (cardAdded()) {
              <div class="rounded-md border border-green-500/50 bg-green-500/10 p-3">
                <p class="text-sm text-green-500">Card added successfully!</p>
              </div>
            }

            <button
              (click)="addCard()"
              [disabled]="!newCardFront.trim() || !newCardBack.trim()"
              class="w-full rounded-md bg-primary px-4 py-2.5 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
            >
              Add Card
            </button>
          </div>
        }
      }
    </div>
  `,
})
export class FlashcardsComponent implements OnInit {
  private readonly flashcardService = inject(FlashcardService);
  private readonly pinnedService = inject(PinnedContentService);

  readonly view = signal<View>('decks');
  readonly loading = signal(true);
  readonly generating = signal(false);
  readonly error = signal<string | null>(null);

  readonly decks = signal<FlashcardDeck[]>([]);
  readonly pinnedItems = signal<PinnedContent[]>([]);
  readonly loadingPinned = signal(false);

  // Generate form
  selectedContentId = '';
  cardCount = 20;
  topicInput = '';

  // Review state
  readonly reviewCards = signal<Flashcard[]>([]);
  readonly currentCardIndex = signal(0);
  readonly flipped = signal(false);
  readonly reviewedCount = signal(0);
  private currentDeckId = '';

  // Add card state
  private addCardDeckId = '';
  newCardFront = '';
  newCardBack = '';
  readonly cardAdded = signal(false);

  readonly indexedItems = computed(() => this.pinnedItems().filter((p) => p.isIndexed));

  readonly currentCard = computed(() => {
    const cards = this.reviewCards();
    const index = this.currentCardIndex();
    return cards[index] ?? null;
  });

  ngOnInit(): void {
    this.loadDecks();
  }

  loadDecks(): void {
    this.loading.set(true);
    this.flashcardService.getDecks().subscribe({
      next: (decks) => {
        this.decks.set(decks);
        this.loading.set(false);
      },
      error: (err) => {
        const message = typeof err.error === 'string' ? err.error : err.message ?? 'Failed to load decks.';
        this.error.set(message);
        this.loading.set(false);
      },
    });
  }

  showGenerate(): void {
    this.view.set('generate');
    this.error.set(null);
    if (this.pinnedItems().length === 0) {
      this.loadingPinned.set(true);
      this.pinnedService.getPinnedContents().subscribe({
        next: (items) => {
          this.pinnedItems.set(items);
          this.loadingPinned.set(false);
        },
        error: () => this.loadingPinned.set(false),
      });
    }
  }

  generateDeck(): void {
    if (!this.selectedContentId) return;
    this.generating.set(true);
    this.error.set(null);

    const topic = this.topicInput.trim() || undefined;

    this.flashcardService.generateFlashcards(this.selectedContentId, this.cardCount, topic).subscribe({
      next: () => {
        this.generating.set(false);
        this.view.set('decks');
        this.loadDecks();
      },
      error: (err) => {
        const message = typeof err.error === 'string' ? err.error : err.message ?? 'Failed to generate flashcards.';
        this.error.set(message);
        this.generating.set(false);
      },
    });
  }

  startReview(deckId: string): void {
    this.currentDeckId = deckId;
    this.loading.set(true);
    this.flashcardService.getDueCards(deckId).subscribe({
      next: (cards) => {
        this.reviewCards.set(cards);
        this.currentCardIndex.set(0);
        this.flipped.set(false);
        this.reviewedCount.set(0);
        this.view.set('review');
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  rateCard(quality: number): void {
    const card = this.currentCard();
    if (!card) return;

    this.flashcardService.reviewCard(card.id, quality).subscribe({
      next: () => {
        this.reviewedCount.update((c) => c + 1);
        const nextIndex = this.currentCardIndex() + 1;
        if (nextIndex < this.reviewCards().length) {
          this.currentCardIndex.set(nextIndex);
          this.flipped.set(false);
        } else {
          // Review complete — clear cards to show completion screen
          this.reviewCards.set([]);
        }
      },
    });
  }

  showAddCard(deckId: string): void {
    this.addCardDeckId = deckId;
    this.newCardFront = '';
    this.newCardBack = '';
    this.cardAdded.set(false);
    this.error.set(null);
    this.view.set('add-card');
  }

  addCard(): void {
    if (!this.newCardFront.trim() || !this.newCardBack.trim()) return;

    this.flashcardService.createCard(this.addCardDeckId, this.newCardFront.trim(), this.newCardBack.trim()).subscribe({
      next: () => {
        this.cardAdded.set(true);
        this.newCardFront = '';
        this.newCardBack = '';
      },
      error: (err) => {
        const message = typeof err.error === 'string' ? err.error : err.message ?? 'Failed to add card.';
        this.error.set(message);
      },
    });
  }

  backToDecks(): void {
    this.view.set('decks');
    this.loadDecks();
  }
}
