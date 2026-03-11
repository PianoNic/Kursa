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
            Connect your Moodle account using your login credentials.
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
          <form [formGroup]="loginForm" (ngSubmit)="linkMoodle()" class="space-y-4" novalidate>
            <div>
              <label for="moodle-username" class="block text-sm font-medium text-foreground">
                Moodle Username
              </label>
              <input
                id="moodle-username"
                type="text"
                formControlName="username"
                autocomplete="username"
                placeholder="Enter your Moodle username"
                class="mt-2 block w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                [class]="loginForm.controls.username.invalid && loginForm.controls.username.touched ? 'border-destructive' : ''"
                [attr.aria-invalid]="loginForm.controls.username.invalid && loginForm.controls.username.touched ? true : null"
                [attr.aria-describedby]="loginForm.controls.username.invalid && loginForm.controls.username.touched ? 'username-error' : null"
              />
              @if (loginForm.controls.username.invalid && loginForm.controls.username.touched) {
                <p id="username-error" class="mt-1 text-xs text-destructive" role="alert">
                  Please enter your Moodle username.
                </p>
              }
            </div>

            <div>
              <label for="moodle-password" class="block text-sm font-medium text-foreground">
                Moodle Password
              </label>
              <input
                id="moodle-password"
                type="password"
                formControlName="password"
                autocomplete="current-password"
                placeholder="Enter your Moodle password"
                class="mt-2 block w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                [class]="loginForm.controls.password.invalid && loginForm.controls.password.touched ? 'border-destructive' : ''"
                [attr.aria-invalid]="loginForm.controls.password.invalid && loginForm.controls.password.touched ? true : null"
                [attr.aria-describedby]="loginForm.controls.password.invalid && loginForm.controls.password.touched ? 'password-error' : null"
              />
              @if (loginForm.controls.password.invalid && loginForm.controls.password.touched) {
                <p id="password-error" class="mt-1 text-xs text-destructive" role="alert">
                  Please enter your Moodle password.
                </p>
              }
            </div>

            @if (errorMessage()) {
              <p class="text-sm text-destructive" role="alert">{{ errorMessage() }}</p>
            }

            <button
              type="submit"
              [disabled]="isSubmitting() || loginForm.invalid"
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

  readonly loginForm = this.fb.group({
    username: ['', [Validators.required]],
    password: ['', [Validators.required]],
  });

  ngOnInit(): void {
    this.moodleService.getConnectionStatus().subscribe({
      next: (status) => this.isConnected.set(status.isConnected),
      error: () => this.isConnected.set(false),
    });
  }

  linkMoodle(): void {
    if (this.loginForm.invalid) return;

    const { username, password } = this.loginForm.value;
    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.moodleService.linkMoodle(username!, password!).subscribe({
      next: () => {
        this.isConnected.set(true);
        this.isSubmitting.set(false);
        this.loginForm.reset();
      },
      error: () => {
        this.errorMessage.set('Invalid credentials or Moodle unavailable. Please try again.');
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
