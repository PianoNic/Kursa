import { Injectable, signal, computed } from '@angular/core';

export interface ViewContext {
  type: 'course' | 'content' | 'module' | 'none';
  id?: string;
  title?: string;
  description?: string;
}

@Injectable({ providedIn: 'root' })
export class AiContextService {
  private readonly _currentContext = signal<ViewContext>({ type: 'none' });
  private readonly _panelOpen = signal(false);

  readonly currentContext = this._currentContext.asReadonly();
  readonly panelOpen = this._panelOpen.asReadonly();
  readonly hasContext = computed(() => this._currentContext().type !== 'none');

  setContext(context: ViewContext): void {
    this._currentContext.set(context);
  }

  clearContext(): void {
    this._currentContext.set({ type: 'none' });
  }

  togglePanel(): void {
    this._panelOpen.update((open) => !open);
  }

  openPanel(): void {
    this._panelOpen.set(true);
  }

  closePanel(): void {
    this._panelOpen.set(false);
  }
}
