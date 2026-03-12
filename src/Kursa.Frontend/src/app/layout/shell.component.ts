import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, RouterOutlet } from '@angular/router';
import { SidebarComponent } from './sidebar.component';
import { TopbarComponent } from './topbar.component';
import { AiPanelComponent } from './ai-panel.component';
import { AiContextService } from '../core/services/ai-context.service';
import { AuthService } from '../core/services/auth.service';

@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, SidebarComponent, TopbarComponent, AiPanelComponent],
  template: `
    <div class="min-h-screen">
      <app-sidebar [isOpen]="sidebarOpen()" />

      <div
        class="transition-[margin] duration-300"
        [class.ml-64]="sidebarOpen()"
        [class.mr-96]="aiContext.panelOpen()"
      >
        <app-topbar (toggleSidebar)="toggleSidebar()" (toggleAiPanel)="aiContext.togglePanel()" />

        <main class="p-6">
          <router-outlet />
        </main>
      </div>

      <app-ai-panel />
    </div>
  `,
})
export class ShellComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  readonly aiContext = inject(AiContextService);
  sidebarOpen = signal(true);

  ngOnInit(): void {
    // Load user profile; if 404 the user hasn't registered → send to onboarding
    if (!this.authService.profile()) {
      this.authService.getCurrentUser().subscribe({
        error: (err: HttpErrorResponse) => {
          if (err.status === 404) {
            this.router.navigate(['/onboarding']);
          }
        },
      });
    }
  }

  toggleSidebar() {
    this.sidebarOpen.update((v) => !v);
  }
}
