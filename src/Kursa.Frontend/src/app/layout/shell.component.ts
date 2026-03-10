import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from './sidebar.component';
import { TopbarComponent } from './topbar.component';
import { AiPanelComponent } from './ai-panel.component';
import { AiContextService } from '../core/services/ai-context.service';

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
export class ShellComponent {
  readonly aiContext = inject(AiContextService);
  sidebarOpen = signal(true);

  toggleSidebar() {
    this.sidebarOpen.update((v) => !v);
  }
}
