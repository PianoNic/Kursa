import { ChangeDetectionStrategy, Component, signal, computed, inject, OnInit } from '@angular/core';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { TimetableService, CalendarEventView } from '../../core/services/timetable.service';

@Component({
  selector: 'app-timetable',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [HlmButton, ...HlmCardImports],
  template: `
    <div class="mx-auto max-w-7xl space-y-6 p-6">
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-foreground">Timetable</h1>
          <p class="text-sm text-muted-foreground">Your weekly schedule</p>
        </div>
        <div class="flex items-center gap-2">
          <button hlmBtn variant="ghost" size="sm" type="button" (click)="goToToday()">
            Today
          </button>
          <button hlmBtn variant="ghost" size="icon" type="button" (click)="previousWeek()" aria-label="Previous week">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="h-5 w-5" aria-hidden="true"><path d="m15 18-6-6 6-6" /></svg>
          </button>
          <span class="min-w-48 text-center text-sm font-medium text-foreground">{{ weekLabel() }}</span>
          <button hlmBtn variant="ghost" size="icon" type="button" (click)="nextWeek()" aria-label="Next week">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="h-5 w-5" aria-hidden="true"><path d="m9 18 6-6-6-6" /></svg>
          </button>
        </div>
      </div>

      @if (loading()) {
        <div class="flex items-center justify-center py-12">
          <div class="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" role="status">
            <span class="sr-only">Loading timetable...</span>
          </div>
        </div>
      } @else if (error()) {
        <div class="rounded-lg border border-destructive/50 bg-destructive/10 p-4 text-sm text-destructive" role="alert">
          {{ error() }}
        </div>
      } @else {
        <!-- Weekly Grid -->
        <div hlmCard class="p-0 gap-0 overflow-x-auto">
          <div class="grid min-w-[800px] grid-cols-8">
            <!-- Header row -->
            <div class="border-b border-r border-border p-2"></div>
            @for (day of weekDays(); track $index) {
              <div
                class="border-b border-r border-border p-2 text-center last:border-r-0"
                [class.bg-primary\/5]="day.isToday"
              >
                <div class="text-xs font-medium text-muted-foreground">{{ day.dayName }}</div>
                <div
                  class="mt-0.5 inline-flex h-7 w-7 items-center justify-center rounded-full text-sm"
                  [class]="day.isToday ? 'bg-primary text-primary-foreground font-bold' : 'text-foreground'"
                >
                  {{ day.date.getDate() }}
                </div>
              </div>
            }

            <!-- Time slots -->
            @for (hour of hours; track hour) {
              <div class="border-b border-r border-border p-1 text-right text-xs text-muted-foreground">
                {{ formatHour(hour) }}
              </div>
              @for (day of weekDays(); track $index) {
                <div
                  class="relative min-h-12 border-b border-r border-border last:border-r-0"
                  [class.bg-primary\/5]="day.isToday"
                >
                  @for (event of getEventsForSlot(day.date, hour); track event.id) {
                    <div
                      class="absolute inset-x-0.5 rounded px-1 py-0.5 text-xs"
                      [class]="getEventColor(event.eventType)"
                      [style.top.px]="getEventTopOffset(event)"
                      [style.height.px]="getEventHeight(event)"
                      [title]="event.title + ' (' + formatTime(event.startTime) + ' - ' + formatTime(event.endTime) + ')'"
                    >
                      <div class="truncate font-medium">{{ event.title }}</div>
                      <div class="truncate opacity-75">{{ formatTime(event.startTime) }}</div>
                    </div>
                  }
                </div>
              }
            }
          </div>
        </div>

        <!-- Event List (below grid for quick reference) -->
        @if (events().length > 0) {
          <div>
            <h2 class="mb-3 text-sm font-medium text-muted-foreground">This week's events ({{ events().length }})</h2>
            <div class="space-y-2">
              @for (event of events(); track event.id) {
                <div hlmCard class="flex items-center gap-3 p-3">
                  <div class="h-2 w-2 shrink-0 rounded-full" [class]="getEventDotColor(event.eventType)" aria-hidden="true"></div>
                  <div class="min-w-0 flex-1">
                    <p class="truncate text-sm font-medium text-foreground">{{ event.title }}</p>
                    <p class="text-xs text-muted-foreground">
                      {{ formatEventDate(event.startTime) }} — {{ formatTime(event.startTime) }} to {{ formatTime(event.endTime) }}
                    </p>
                  </div>
                  <span class="shrink-0 rounded-full bg-muted px-2 py-0.5 text-xs text-muted-foreground capitalize">
                    {{ event.eventType }}
                  </span>
                </div>
              }
            </div>
          </div>
        } @else {
          <div hlmCard class="p-6 text-center text-sm text-muted-foreground">
            No events this week.
          </div>
        }
      }
    </div>
  `,
})
export class TimetableComponent implements OnInit {
  private readonly timetableService = inject(TimetableService);

  readonly events = signal<CalendarEventView[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly weekStart = signal(TimetableComponent.getMonday(new Date()));

  readonly hours = Array.from({ length: 14 }, (_, i) => i + 7); // 07:00 - 20:00

  readonly weekLabel = computed(() => {
    const start = this.weekStart();
    const end = new Date(start);
    end.setDate(end.getDate() + 6);
    const opts: Intl.DateTimeFormatOptions = { month: 'short', day: 'numeric' };
    return `${start.toLocaleDateString('default', opts)} — ${end.toLocaleDateString('default', { ...opts, year: 'numeric' })}`;
  });

  readonly weekDays = computed(() => {
    const start = this.weekStart();
    const today = new Date();
    const dayNames = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

    return Array.from({ length: 7 }, (_, i) => {
      const date = new Date(start);
      date.setDate(date.getDate() + i);
      return {
        dayName: dayNames[i],
        date,
        isToday:
          date.getDate() === today.getDate() &&
          date.getMonth() === today.getMonth() &&
          date.getFullYear() === today.getFullYear(),
      };
    });
  });

  ngOnInit(): void {
    this.loadEvents();
  }

  previousWeek(): void {
    const current = this.weekStart();
    const prev = new Date(current);
    prev.setDate(prev.getDate() - 7);
    this.weekStart.set(prev);
    this.loadEvents();
  }

  nextWeek(): void {
    const current = this.weekStart();
    const next = new Date(current);
    next.setDate(next.getDate() + 7);
    this.weekStart.set(next);
    this.loadEvents();
  }

  goToToday(): void {
    this.weekStart.set(TimetableComponent.getMonday(new Date()));
    this.loadEvents();
  }

  getEventsForSlot(date: Date, hour: number): CalendarEventView[] {
    return this.events().filter(e => {
      const start = new Date(e.startTime);
      return (
        start.getDate() === date.getDate() &&
        start.getMonth() === date.getMonth() &&
        start.getFullYear() === date.getFullYear() &&
        start.getHours() === hour
      );
    });
  }

  getEventTopOffset(event: CalendarEventView): number {
    const start = new Date(event.startTime);
    return (start.getMinutes() / 60) * 48; // 48px per hour slot
  }

  getEventHeight(event: CalendarEventView): number {
    return Math.max(20, (event.durationMinutes / 60) * 48);
  }

  getEventColor(type: string): string {
    switch (type) {
      case 'course': return 'bg-blue-500/20 text-blue-400 border-l-2 border-blue-500';
      case 'due': return 'bg-red-500/20 text-red-400 border-l-2 border-red-500';
      case 'user': return 'bg-green-500/20 text-green-400 border-l-2 border-green-500';
      case 'site': return 'bg-purple-500/20 text-purple-400 border-l-2 border-purple-500';
      default: return 'bg-primary/20 text-primary border-l-2 border-primary';
    }
  }

  getEventDotColor(type: string): string {
    switch (type) {
      case 'course': return 'bg-blue-500';
      case 'due': return 'bg-red-500';
      case 'user': return 'bg-green-500';
      case 'site': return 'bg-purple-500';
      default: return 'bg-primary';
    }
  }

  formatHour(hour: number): string {
    return `${hour.toString().padStart(2, '0')}:00`;
  }

  formatTime(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleTimeString('default', { hour: '2-digit', minute: '2-digit' });
  }

  formatEventDate(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleDateString('default', { weekday: 'short', month: 'short', day: 'numeric' });
  }

  private loadEvents(): void {
    this.loading.set(true);
    this.error.set(null);

    this.timetableService.getCalendarEvents(this.weekStart()).subscribe({
      next: (events) => {
        this.events.set(events);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load timetable. Make sure your Moodle account is linked.');
        this.loading.set(false);
      },
    });
  }

  private static getMonday(date: Date): Date {
    const d = new Date(date);
    const day = d.getDay();
    const diff = d.getDate() - day + (day === 0 ? -6 : 1);
    d.setDate(diff);
    d.setHours(0, 0, 0, 0);
    return d;
  }
}
