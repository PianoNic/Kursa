import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmLabel } from '@spartan-ng/helm/label';
import { MoodleService } from '../../api/api/moodle.service';
import { LinkMoodleTokenCommand } from '../../api/model/linkMoodleTokenCommand';

@Component({
  selector: 'app-settings',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, HlmButton, ...HlmCardImports, HlmInput, HlmLabel],
  template: `
    <div class="mx-auto max-w-2xl space-y-8 p-6">
      <h1 class="text-2xl font-bold text-foreground">Settings</h1>

      <!-- Moodle Integration -->
      <section aria-labelledby="moodle-heading">
        <div hlmCard class="p-6 space-y-4">
          <div>
            <h2 id="moodle-heading" class="text-lg font-semibold text-foreground">Moodle Integration</h2>
            <p class="mt-1 text-sm text-muted-foreground">
              Connect your Moodle account using your login credentials.
            </p>
          </div>

          @if (isConnected()) {
            <div class="flex items-center gap-3 rounded-md bg-green-500/10 border border-green-500/20 px-4 py-3" role="status">
              <svg class="h-4 w-4 shrink-0 text-green-500" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" aria-hidden="true">
                <path stroke-linecap="round" stroke-linejoin="round" d="M9 12.75 11.25 15 15 9.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
              </svg>
              <p class="text-sm text-green-600 dark:text-green-400">Moodle account connected</p>
            </div>

            <button
              hlmBtn
              variant="outline"
              type="button"
              (click)="unlinkMoodle()"
              [disabled]="isSubmitting()"
              class="border-destructive text-destructive hover:bg-destructive/10"
            >
              {{ isSubmitting() ? 'Disconnecting…' : 'Disconnect Moodle' }}
            </button>
          } @else {
            <form [formGroup]="loginForm" (ngSubmit)="linkMoodle()" class="space-y-4" novalidate>
              <div class="space-y-1.5">
                <label hlmLabel for="moodle-username">
                  Moodle Username
                </label>
                <input
                  hlmInput
                  id="moodle-username"
                  type="text"
                  formControlName="username"
                  autocomplete="username"
                  placeholder="Enter your Moodle username"
                  class="w-full"
                  [class]="loginForm.controls.username.invalid && loginForm.controls.username.touched ? 'border-destructive' : ''"
                  [attr.aria-invalid]="loginForm.controls.username.invalid && loginForm.controls.username.touched ? true : null"
                  [attr.aria-describedby]="loginForm.controls.username.invalid && loginForm.controls.username.touched ? 'username-error' : null"
                />
                @if (loginForm.controls.username.invalid && loginForm.controls.username.touched) {
                  <p id="username-error" class="text-xs text-destructive" role="alert">
                    Please enter your Moodle username.
                  </p>
                }
              </div>

              <div class="space-y-1.5">
                <label hlmLabel for="moodle-password">
                  Moodle Password
                </label>
                <input
                  hlmInput
                  id="moodle-password"
                  type="password"
                  formControlName="password"
                  autocomplete="current-password"
                  placeholder="Enter your Moodle password"
                  class="w-full"
                  [class]="loginForm.controls.password.invalid && loginForm.controls.password.touched ? 'border-destructive' : ''"
                  [attr.aria-invalid]="loginForm.controls.password.invalid && loginForm.controls.password.touched ? true : null"
                  [attr.aria-describedby]="loginForm.controls.password.invalid && loginForm.controls.password.touched ? 'password-error' : null"
                />
                @if (loginForm.controls.password.invalid && loginForm.controls.password.touched) {
                  <p id="password-error" class="text-xs text-destructive" role="alert">
                    Please enter your Moodle password.
                  </p>
                }
              </div>

              @if (errorMessage()) {
                <p class="text-sm text-destructive" role="alert">{{ errorMessage() }}</p>
              }

              <button
                hlmBtn
                type="submit"
                [disabled]="isSubmitting() || loginForm.invalid"
              >
                {{ isSubmitting() ? 'Connecting…' : 'Connect Moodle' }}
              </button>
            </form>
          }
        </div>
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
    this.moodleService.apiMoodleStatusGet().subscribe({
      next: (status) => this.isConnected.set(status.isConnected ?? false),
      error: () => this.isConnected.set(false),
    });
  }

  linkMoodle(): void {
    if (this.loginForm.invalid) return;

    const { username, password } = this.loginForm.value;
    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const linkCmd: LinkMoodleTokenCommand = { username: username!, password: password! };
    this.moodleService.apiMoodleLinkPost(linkCmd).subscribe({
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

    this.moodleService.apiMoodleLinkDelete().subscribe({
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
