import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmLabel } from '@spartan-ng/helm/label';
import { AuthService } from '../../core/services/auth.service';
import { MoodleService } from '../../api/api/moodle.service';
import { ValidateMoodleCredentialsCommand } from '../../api/model/validateMoodleCredentialsCommand';
import { LinkMoodleTokenCommand } from '../../api/model/linkMoodleTokenCommand';

@Component({
  selector: 'app-onboarding',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, HlmButton, ...HlmCardImports, HlmInput, HlmLabel],
  template: `
    <div class="flex min-h-screen items-center justify-center bg-background p-4">
      <div class="w-full max-w-lg space-y-8">
        <!-- Progress indicator -->
        <div class="flex items-center justify-center gap-2" role="progressbar" [attr.aria-valuenow]="currentStep() + 1" [attr.aria-valuemin]="1" [attr.aria-valuemax]="steps.length" [attr.aria-label]="'Step ' + (currentStep() + 1) + ' of ' + steps.length">
          @for (s of steps; track s; let i = $index) {
            <div
              class="h-2 w-12 rounded-full transition-colors"
              [class]="i <= currentStep() ? 'bg-primary' : 'bg-muted'"
            ></div>
          }
        </div>

        <div hlmCard class="p-8">
          @switch (currentStep()) {
            @case (0) {
              <!-- Step 1: Welcome -->
              <div class="space-y-4 text-center">
                <svg class="mx-auto h-16 w-16 text-primary" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" aria-hidden="true">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M4.26 10.147a60.438 60.438 0 0 0-.491 6.347A48.62 48.62 0 0 1 12 20.904a48.62 48.62 0 0 1 8.232-4.41 60.46 60.46 0 0 0-.491-6.347m-15.482 0a50.636 50.636 0 0 0-2.658-.813A59.906 59.906 0 0 1 12 3.493a59.903 59.903 0 0 1 10.399 5.84c-.896.248-1.783.52-2.658.814m-15.482 0A50.717 50.717 0 0 1 12 13.489a50.702 50.702 0 0 1 7.74-3.342M6.75 15a.75.75 0 1 0 0-1.5.75.75 0 0 0 0 1.5Zm0 0v-3.675A55.378 55.378 0 0 1 12 8.443m-7.007 11.55A5.981 5.981 0 0 0 6.75 15.75v-1.5" />
                </svg>
                <h2 class="text-2xl font-bold text-foreground">Welcome to Kursa</h2>
                <p class="text-muted-foreground">
                  Let's get you set up. This will only take a minute.
                </p>
              </div>
            }
            @case (1) {
              <!-- Step 2: Link Moodle -->
              <div class="space-y-4">
                <h2 class="text-xl font-bold text-foreground">Connect Moodle</h2>
                <p class="text-sm text-muted-foreground">
                  Enter your Moodle credentials to link your account.
                </p>

                <div class="space-y-3">
                  <div class="space-y-1.5">
                    <label hlmLabel for="moodle-username">Username</label>
                    <input
                      hlmInput
                      id="moodle-username"
                      type="text"
                      [(ngModel)]="moodleUsername"
                      autocomplete="username"
                      placeholder="Your Moodle username"
                      class="w-full"
                    />
                  </div>
                  <div class="space-y-1.5">
                    <label hlmLabel for="moodle-password">Password</label>
                    <input
                      hlmInput
                      id="moodle-password"
                      type="password"
                      [(ngModel)]="moodlePassword"
                      autocomplete="current-password"
                      placeholder="Your Moodle password"
                      class="w-full"
                    />
                  </div>
                </div>

                @if (moodleLinkError()) {
                  <p class="text-sm text-destructive" role="alert">{{ moodleLinkError() }}</p>
                }
                @if (moodleLinkSuccess()) {
                  <p class="text-sm text-green-500" role="status">Moodle account linked successfully!</p>
                }
              </div>
            }
            @case (2) {
              <!-- Step 3: Preferences -->
              <div class="space-y-4">
                <h2 class="text-xl font-bold text-foreground">Preferences</h2>
                <p class="text-sm text-muted-foreground">Customize your experience.</p>

                <div class="space-y-1.5">
                  <label hlmLabel for="theme">Theme</label>
                  <select
                    id="theme"
                    [(ngModel)]="selectedTheme"
                    class="mt-1 block w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                  >
                    <option value="dark">Dark</option>
                    <option value="light">Light</option>
                    <option value="system">System</option>
                  </select>
                </div>
              </div>
            }
            @case (3) {
              <!-- Step 4: Done -->
              <div class="space-y-4 text-center">
                <svg class="mx-auto h-16 w-16 text-green-500" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" aria-hidden="true">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M9 12.75 11.25 15 15 9.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
                </svg>
                <h2 class="text-2xl font-bold text-foreground">You're all set!</h2>
                <p class="text-muted-foreground">
                  Head to the dashboard to start exploring your courses.
                </p>
              </div>
            }
          }

          <!-- Navigation buttons -->
          <div class="mt-8 flex items-center justify-between">
            @if (currentStep() > 0 && currentStep() < steps.length - 1) {
              <button hlmBtn variant="outline" (click)="previous()">
                Back
              </button>
            } @else {
              <div></div>
            }

            <div class="flex items-center gap-3">
              @if (currentStep() < steps.length - 1 && currentStep() !== 1) {
                <button hlmBtn variant="ghost" (click)="skip()">
                  Skip
                </button>
              }

              @if (currentStep() < steps.length - 1) {
                <button hlmBtn (click)="next()" [disabled]="moodleValidating() || (currentStep() === 1 && (!moodleUsername || !moodlePassword))">
                  @if (moodleValidating()) {
                    Validating…
                  } @else {
                    {{ currentStep() === 1 ? 'Connect & Continue' : 'Continue' }}
                  }
                </button>
              } @else {
                <button hlmBtn class="px-6" (click)="finish()">
                  Go to Dashboard
                </button>
              }
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
})
export class OnboardingComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly moodleService = inject(MoodleService);

  readonly steps = ['Welcome', 'Moodle', 'Preferences', 'Done'];
  readonly currentStep = signal(0);
  readonly moodleLinkError = signal<string | null>(null);
  readonly moodleLinkSuccess = signal(false);

  moodleUsername = '';
  moodlePassword = '';
  selectedTheme = 'dark';

  readonly moodleValidating = signal(false);

  ngOnInit(): void {
    // Nothing to do — user record is created only on finish (register)
  }

  next(): void {
    // On Moodle step, validate credentials before proceeding
    if (this.currentStep() === 1 && this.moodleUsername && this.moodlePassword) {
      this.moodleLinkError.set(null);
      this.moodleValidating.set(true);
      const validateCmd: ValidateMoodleCredentialsCommand = { username: this.moodleUsername, password: this.moodlePassword };
      this.moodleService.apiMoodleValidatePost(validateCmd).subscribe({
        next: () => {
          this.moodleValidating.set(false);
          this.moodleLinkSuccess.set(true);
          this.currentStep.update((s) => s + 1);
        },
        error: () => {
          this.moodleValidating.set(false);
          this.moodleLinkError.set('Invalid credentials. Please check and try again.');
        },
      });
      return;
    }

    if (this.currentStep() < this.steps.length - 1) {
      this.currentStep.update((s) => s + 1);
    }
  }

  previous(): void {
    if (this.currentStep() > 0) {
      this.currentStep.update((s) => s - 1);
    }
  }

  skip(): void {
    this.currentStep.update((s) => s + 1);
  }

  finish(): void {
    // Register creates the user in DB + marks onboarding complete
    this.authService.register().subscribe({
      next: () => {
        // Now link Moodle if credentials were provided
        if (this.moodleUsername && this.moodlePassword) {
          const linkCmd: LinkMoodleTokenCommand = { username: this.moodleUsername, password: this.moodlePassword };
          this.moodleService.apiMoodleLinkPost(linkCmd).subscribe({
            next: () => this.router.navigate(['/dashboard']),
            error: () => this.router.navigate(['/dashboard']),
          });
        } else {
          this.router.navigate(['/dashboard']);
        }
      },
      error: () => {
        this.router.navigate(['/dashboard']);
      },
    });
  }
}
