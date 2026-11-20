import { inject } from '@angular/core';
import { CanDeactivateFn } from '@angular/router';
import { ConfirmationService } from 'primeng/api';

import { PendingChangesService } from '../../../core/services/pending-changes.service';

/** Confirma el descarte de cambios antes de abandonar la página del APU (`SC-APUS-130`). */
export const unsavedChangesGuard: CanDeactivateFn<unknown> = () => {
  const pending = inject(PendingChangesService);
  const confirmation = inject(ConfirmationService);

  if (!pending.hasPendingChanges()) {
    return true;
  }

  return new Promise<boolean>((resolve) => {
    confirmation.confirm({
      header: 'Cambios sin guardar',
      message: 'Tienes cambios sin guardar en el APU. ¿Deseas descartarlos y salir?',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Descartar y salir',
      rejectLabel: 'Seguir editando',
      accept: () => {
        pending.clear();
        resolve(true);
      },
      reject: () => resolve(false)
    });
  });
};
