import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MoodleService } from '../../core/services/moodle.service';

@Component({
  selector: 'app-settings',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  template: `
    <div class="mx-auto max-w-2xl space-y-8 p-6">
      <h1 class="text-2xl font-bold text-foreground">Settings</h1>

      <!-- Moodle Integration -->
      <section aria-labelledby="moodle-heading" class="rounded-lg border border-border bg-card p-6 space-y-4">
        <div>
          <h2 id="moodle-heading" class="text-lg font-semibold text-foreground">Moodle Integration</h2>
          <p class="mt-1 text-sm text-muted-foreground">
            Connect your Moodle account to access courses, assignments, and grades.
          </p>
        </div>

        @if (isConnected()) {
          <div class="flex items-center gap-3 rounded-md bg-green-500/10 border border-green-500/20 px-4 py-3">
            <svg class="h-4 w-4 shrink-0 text-green-500" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" aria-hidden="true">
              <path stroke-linecap="round" stroke-linejoin="round" d="M9 12.75 11.25 15 15 9.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
            </svg>
            <p class="text-sm text-green-600 dark:text-green-400">Moodle account connected</p>
          </div>

          <button
            type="button"
            (click)="unlinkMoodle()"
            [disabled]="isSubmitting()"
            class="rounded-md border border-destructive px-4 py-2 text-sm font-medium text-destructive hover:bg-destructive/10 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {{ isSubmitting() ? 'Disconnecting…' : 'Disconnect Moodle' }}
          </button>
        } @else {
          <form [formGroup]="tokenForm" (ngSubmit)="linkMoodle()" class="space-y-4" novalidate>
            <div>
              <label for="moodle-token" class="block text-sm font-medium text-foreground">
                Moodle API Token
              </label>
              <p class="mt-0.5 text-xs text-muted-foreground">
                Find your token in Moodle → User menu → Preferences → Security keys.
              </p>
              <input
                id="moodle-token"
                type="password"
                formControlName="token"
                autocomplete="current-password"
                placeholder="Enter your Moodle API token"
                class="mt-2 block w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                [class]="tokenForm.controls.token.invalid && tokenForm.controls.token.touched ? 'border-destructive' : ''"
                [attr.aria-describedby]="tokenForm.controls.token.invalid && tokenForm.controls.token.touched ? 'token-error' : null"
                [attr.aria-invalid]="tokenForm.controls.token.invalid && tokenForm.controls.token.touched ? true : null"
              />
              @if (tokenForm.controls.token.invalid && tokenForm.controls.token.touched) {
                <p id="token-error" class="mt-1 text-xs text-destructive" role="alert">
                  Please enter your Moodle API token.
                </p>
              }
            </div>

            @if (errorMessage()) {
              <p class="text-sm text-destructive" role="alert">{{ errorMessage() }}</p>
            }

            <button
              type="submit"
              [disabled]="isSubmitting() || tokenForm.invalid"
              class="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {{ isSubmitting() ? 'Connecting…' : 'Connect Moodle' }}
            </button>
          </form>
        }
      </section>
    </div>
  `,
})
export class SettingsComponent implements OnInit {
  private readonly moodleService = inject(MoodleService);
  private readonly fb = inject(FormBuilder);

  readonly isConnected = signal(false);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly tokenForm = this.fb.group({
    token: ['', [Validators.required, Validators.minLength(1)]],
  });

  ngOnInit(): void {
    this.loadStatus();
  }

  private loadStatus(): void {
    this.moodleService.getConnectionStatus().subscribe({
      next: (status) => this.isConnected.set(status.isConnected),
      error: () => this.isConnected.set(false),
    });
  }

  linkMoodle(): void {
    if (this.tokenForm.invalid) return;

    const token = this.tokenForm.controls.token.value!;
    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.moodleService.linkToken(token).subscribe({
      next: () => {
        this.isConnected.set(true);
        this.isSubmitting.set(false);
        this.tokenForm.reset();
      },
      error: () => {
        this.errorMessage.set('Failed to connect. Check your token and try again.');
        this.isSubmitting.set(false);
      },
    });
  }

  unlinkMoodle(): void {
    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.moodleService.unlinkToken().subscribe({
      next: () => {
        this.isConnected.set(false);
        this.isSubmitting.set(false);
      },
      error: () => {
        this.errorMessage.set('Failed to disconnect. Please try again.');
        this.isSubmitting.set(false);
      },
    });
  }
}
