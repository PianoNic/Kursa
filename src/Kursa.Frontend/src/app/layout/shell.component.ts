import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from './sidebar.component';
import { TopbarComponent } from './topbar.component';

@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, SidebarComponent, TopbarComponent],
  template: `
    <div class="min-h-screen">
      <app-sidebar [isOpen]="sidebarOpen()" />

      <div class="transition-[margin] duration-300" [class.ml-64]="sidebarOpen()">
        <app-topbar (toggleSidebar)="toggleSidebar()" />

        <main class="p-6">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
})
export class ShellComponent {
  sidebarOpen = signal(true);

  toggleSidebar() {
    this.sidebarOpen.update((v) => !v);
  }
}
