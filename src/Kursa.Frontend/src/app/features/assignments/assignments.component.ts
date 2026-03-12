import { ChangeDetectionStrategy, Component, signal, computed, inject, OnInit } from '@angular/core';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { MoodleService } from '../../api/api/moodle.service';
import { AssignmentViewDto } from '../../api/model/models';

interface CalendarDay {
  date: Date;
  isCurrentMonth: boolean;
  isToday: boolean;
  assignments: AssignmentViewDto[];
}

@Component({
  selector: 'app-assignments',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [HlmButton, ...HlmCardImports],
  template: `
    <div class="mx-auto max-w-7xl space-y-6 p-6">
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-foreground">Assignments</h1>
          <p class="text-sm text-muted-foreground">Track your deadlines and submissions</p>
        </div>
        <div class="flex items-center gap-2 rounded-lg border border-border bg-card p-1" role="group" aria-label="View selection">
          <button
            hlmBtn
            type="button"
            [variant]="activeView() === 'list' ? 'default' : 'ghost'"
            size="sm"
            (click)="activeView.set('list')"
          >
            List
          </button>
          <button
            hlmBtn
            type="button"
            [variant]="activeView() === 'calendar' ? 'default' : 'ghost'"
            size="sm"
            (click)="activeView.set('calendar')"
          >
            Calendar
          </button>
        </div>
      </div>

      @if (loading()) {
        <div class="flex items-center justify-center py-12">
          <div class="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" role="status">
            <span class="sr-only">Loading assignments...</span>
          </div>
        </div>
      } @else if (error()) {
        <div class="rounded-lg border border-destructive/50 bg-destructive/10 p-4 text-sm text-destructive" role="alert">
          {{ error() }}
        </div>
      } @else if (activeView() === 'list') {
        <!-- Filter tabs -->
        <div class="flex gap-2" role="group" aria-label="Assignment filters">
          <button
            hlmBtn
            type="button"
            [variant]="filter() === 'all' ? 'default' : 'ghost'"
            size="sm"
            class="rounded-full"
            (click)="filter.set('all')"
          >
            All ({{ assignments().length }})
          </button>
          <button
            hlmBtn
            type="button"
            [variant]="filter() === 'upcoming' ? 'default' : 'ghost'"
            size="sm"
            class="rounded-full"
            (click)="filter.set('upcoming')"
          >
            Upcoming ({{ upcomingCount() }})
          </button>
          <button
            hlmBtn
            type="button"
            [variant]="filter() === 'overdue' ? 'destructive' : 'ghost'"
            size="sm"
            class="rounded-full"
            (click)="filter.set('overdue')"
          >
            Overdue ({{ overdueCount() }})
          </button>
          <button
            hlmBtn
            type="button"
            [variant]="filter() === 'no-date' ? 'default' : 'ghost'"
            size="sm"
            class="rounded-full"
            (click)="filter.set('no-date')"
          >
            No deadline ({{ noDateCount() }})
          </button>
        </div>

        @if (filteredAssignments().length === 0) {
          <div hlmCard class="p-8 text-center text-muted-foreground">
            No assignments found.
          </div>
        } @else {
          <div class="space-y-3">
            @for (assignment of filteredAssignments(); track assignment.id) {
              <div hlmCard class="p-4 transition-colors hover:bg-accent/50">
                <div class="flex items-start justify-between gap-4">
                  <div class="min-w-0 flex-1">
                    <div class="flex items-center gap-2">
                      <h3 class="truncate font-medium text-foreground">{{ assignment.name }}</h3>
                      @if (assignment.isOverdue) {
                        <span class="shrink-0 rounded-full bg-destructive/10 px-2 py-0.5 text-xs font-medium text-destructive">
                          Overdue
                        </span>
                      }
                    </div>
                    <p class="mt-1 text-sm text-muted-foreground">{{ assignment.courseShortName }} — {{ assignment.courseName }}</p>
                    @if (assignment.description) {
                      <p class="mt-2 line-clamp-2 text-sm text-muted-foreground" [innerHTML]="assignment.description"></p>
                    }
                  </div>
                  <div class="shrink-0 text-right">
                    @if (assignment.dueDate) {
                      <p class="text-sm font-medium" [class]="assignment.isOverdue ? 'text-destructive' : 'text-foreground'">
                        {{ formatDate(assignment.dueDate) }}
                      </p>
                      <p class="text-xs text-muted-foreground">{{ formatRelative(assignment.dueDate) }}</p>
                    } @else {
                      <p class="text-sm text-muted-foreground">No deadline</p>
                    }
                  </div>
                </div>
              </div>
            }
          </div>
        }
      } @else {
        <!-- Calendar View -->
        <div hlmCard class="p-0 gap-0 overflow-hidden">
          <div class="flex items-center justify-between border-b border-border p-4">
            <button
              hlmBtn
              variant="ghost"
              size="icon"
              type="button"
              (click)="previousMonth()"
              aria-label="Previous month"
            >
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="h-5 w-5" aria-hidden="true"><path d="m15 18-6-6 6-6" /></svg>
            </button>
            <h2 class="text-lg font-semibold text-foreground">{{ monthLabel() }}</h2>
            <button
              hlmBtn
              variant="ghost"
              size="icon"
              type="button"
              (click)="nextMonth()"
              aria-label="Next month"
            >
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="h-5 w-5" aria-hidden="true"><path d="m9 18 6-6-6-6" /></svg>
            </button>
          </div>

          <div class="grid grid-cols-7 border-b border-border" role="row">
            @for (day of weekDays; track day) {
              <div class="p-2 text-center text-xs font-medium text-muted-foreground" role="columnheader">{{ day }}</div>
            }
          </div>

          <div class="grid grid-cols-7" role="grid">
            @for (day of calendarDays(); track $index) {
              <div
                class="min-h-24 border-b border-r border-border p-1.5 last:border-r-0 [&:nth-child(7n)]:border-r-0"
                [class.bg-accent\/30]="day.isToday"
                [class.opacity-40]="!day.isCurrentMonth"
                role="gridcell"
                [attr.aria-label]="day.date.toLocaleDateString()"
              >
                <span
                  class="inline-flex h-6 w-6 items-center justify-center rounded-full text-xs"
                  [class]="day.isToday ? 'bg-primary text-primary-foreground font-bold' : 'text-foreground'"
                  aria-hidden="true"
                >
                  {{ day.date.getDate() }}
                </span>
                <div class="mt-1 space-y-0.5">
                  @for (a of day.assignments; track a.id) {
                    <div
                      class="truncate rounded px-1 py-0.5 text-xs"
                      [class]="a.isOverdue ? 'bg-destructive/10 text-destructive' : 'bg-primary/10 text-primary'"
                      [title]="a.name + ' — ' + a.courseShortName"
                    >
                      {{ a.name }}
                    </div>
                  }
                </div>
              </div>
            }
          </div>
        </div>
      }
    </div>
  `,
})
export class AssignmentsComponent implements OnInit {
  private readonly moodleService = inject(MoodleService);

  readonly assignments = signal<AssignmentViewDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly activeView = signal<'list' | 'calendar'>('list');
  readonly filter = signal<'all' | 'upcoming' | 'overdue' | 'no-date'>('all');
  readonly currentMonth = signal(new Date());

  readonly weekDays = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

  readonly upcomingCount = computed(() =>
    this.assignments().filter(a => a.dueDate && !a.isOverdue).length
  );

  readonly overdueCount = computed(() =>
    this.assignments().filter(a => a.isOverdue).length
  );

  readonly noDateCount = computed(() =>
    this.assignments().filter(a => !a.dueDate).length
  );

  readonly filteredAssignments = computed(() => {
    const all = this.assignments();
    switch (this.filter()) {
      case 'upcoming':
        return all.filter(a => a.dueDate && !a.isOverdue);
      case 'overdue':
        return all.filter(a => a.isOverdue);
      case 'no-date':
        return all.filter(a => !a.dueDate);
      default:
        return all;
    }
  });

  readonly monthLabel = computed(() => {
    const d = this.currentMonth();
    return d.toLocaleString('default', { month: 'long', year: 'numeric' });
  });

  readonly calendarDays = computed<CalendarDay[]>(() => {
    const current = this.currentMonth();
    const year = current.getFullYear();
    const month = current.getMonth();
    const today = new Date();

    const firstDay = new Date(year, month, 1);
    const lastDay = new Date(year, month + 1, 0);

    // Monday = 0 in our grid
    let startOffset = firstDay.getDay() - 1;
    if (startOffset < 0) startOffset = 6;

    const days: CalendarDay[] = [];

    // Previous month padding
    for (let i = startOffset - 1; i >= 0; i--) {
      const date = new Date(year, month, -i);
      days.push({
        date,
        isCurrentMonth: false,
        isToday: false,
        assignments: this.getAssignmentsForDate(date),
      });
    }

    // Current month
    for (let d = 1; d <= lastDay.getDate(); d++) {
      const date = new Date(year, month, d);
      days.push({
        date,
        isCurrentMonth: true,
        isToday:
          date.getDate() === today.getDate() &&
          date.getMonth() === today.getMonth() &&
          date.getFullYear() === today.getFullYear(),
        assignments: this.getAssignmentsForDate(date),
      });
    }

    // Next month padding to fill the grid
    const remaining = 7 - (days.length % 7);
    if (remaining < 7) {
      for (let i = 1; i <= remaining; i++) {
        const date = new Date(year, month + 1, i);
        days.push({
          date,
          isCurrentMonth: false,
          isToday: false,
          assignments: this.getAssignmentsForDate(date),
        });
      }
    }

    return days;
  });

  ngOnInit(): void {
    this.loadAssignments();
  }

  previousMonth(): void {
    const current = this.currentMonth();
    this.currentMonth.set(new Date(current.getFullYear(), current.getMonth() - 1, 1));
  }

  nextMonth(): void {
    const current = this.currentMonth();
    this.currentMonth.set(new Date(current.getFullYear(), current.getMonth() + 1, 1));
  }

  formatDate(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleDateString('default', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
  }

  formatRelative(dateStr: string): string {
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = date.getTime() - now.getTime();
    const diffDays = Math.ceil(diffMs / (1000 * 60 * 60 * 24));

    if (diffDays < 0) {
      const absDays = Math.abs(diffDays);
      return absDays === 1 ? '1 day ago' : `${absDays} days ago`;
    }
    if (diffDays === 0) return 'Due today';
    if (diffDays === 1) return 'Due tomorrow';
    if (diffDays <= 7) return `Due in ${diffDays} days`;
    const weeks = Math.floor(diffDays / 7);
    return weeks === 1 ? 'Due in 1 week' : `Due in ${weeks} weeks`;
  }

  private loadAssignments(): void {
    this.loading.set(true);
    this.error.set(null);

    this.moodleService.apiMoodleAssignmentsGet().subscribe({
      next: (assignments) => {
        this.assignments.set(assignments);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load assignments. Make sure your Moodle account is linked.');
        this.loading.set(false);
      },
    });
  }

  private getAssignmentsForDate(date: Date): AssignmentViewDto[] {
    return this.assignments().filter(a => {
      if (!a.dueDate) return false;
      const due = new Date(a.dueDate);
      return (
        due.getDate() === date.getDate() &&
        due.getMonth() === date.getMonth() &&
        due.getFullYear() === date.getFullYear()
      );
    });
  }
}
