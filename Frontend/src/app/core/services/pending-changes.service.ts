import { Injectable, signal } from '@angular/core';

/** Marca que una página tiene cambios sin guardar para confirmar antes de salir (`REQ-APUS-26`). */
@Injectable({ providedIn: 'root' })
export class PendingChangesService {
  private readonly pending = signal(false);

  hasPendingChanges(): boolean {
    return this.pending();
  }

  setPendingChanges(value: boolean): void {
    this.pending.set(value);
  }

  clear(): void {
    this.pending.set(false);
  }
}
