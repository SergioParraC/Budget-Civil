import { Component, input, model, output, signal } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';

/**
 * Alerta única para rechazos del EDT y de los insumos: título, mensaje en español que explica la causa,
 * botón “Entendido” y, cuando la causa lo exige, “Actualizar” para volver a consultar el servidor.
 */
@Component({
  selector: 'app-conflict-dialog',
  imports: [DialogModule, ButtonModule],
  templateUrl: './conflict-dialog.html',
  styleUrl: './conflict-dialog.scss'
})
export class ConflictDialog {
  readonly visible = model(false);
  readonly title = input('No se pudo completar la operación');
  readonly message = input('');
  readonly canRefresh = input(false);
  readonly technicalCode = input<string | null>(null);
  readonly traceId = input<string | null>(null);
  readonly refreshRequested = output<void>();

  readonly showDetails = signal(false);

  close(): void {
    this.showDetails.set(false);
    this.visible.set(false);
  }

  refresh(): void {
    this.visible.set(false);
    this.refreshRequested.emit();
  }
}
