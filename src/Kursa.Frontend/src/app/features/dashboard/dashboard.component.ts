import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-dashboard',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="space-y-6">
      <h1 class="text-2xl font-bold text-foreground">Dashboard</h1>

      <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <div class="rounded-lg border border-border bg-card p-6">
          <div class="text-sm font-medium text-muted-foreground">Courses</div>
          <div class="mt-2 text-2xl font-bold text-foreground">0</div>
        </div>
        <div class="rounded-lg border border-border bg-card p-6">
          <div class="text-sm font-medium text-muted-foreground">Pinned Items</div>
          <div class="mt-2 text-2xl font-bold text-foreground">0</div>
        </div>
        <div class="rounded-lg border border-border bg-card p-6">
          <div class="text-sm font-medium text-muted-foreground">Study Sessions</div>
          <div class="mt-2 text-2xl font-bold text-foreground">0</div>
        </div>
        <div class="rounded-lg border border-border bg-card p-6">
          <div class="text-sm font-medium text-muted-foreground">Recordings</div>
          <div class="mt-2 text-2xl font-bold text-foreground">0</div>
        </div>
      </div>

      <div class="rounded-lg border border-border bg-card p-6">
        <h2 class="text-lg font-semibold text-foreground">Welcome to Kursa</h2>
        <p class="mt-2 text-muted-foreground">
          Connect your Moodle account in Settings to start browsing courses.
        </p>
      </div>
    </div>
  `,
})
export class DashboardComponent {}
