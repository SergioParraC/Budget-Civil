import { AbstractControl, ValidationErrors } from '@angular/forms';

/** Cantidad mayor que cero, igual que el servidor (`R-21`, `REQ-EDT-07`). */
export function positiveNumber(control: AbstractControl): ValidationErrors | null {
  const value = Number(control.value);
  return Number.isFinite(value) && value > 0 ? null : { positiveNumber: true };
}

/** Cantidad mayor o igual que cero: el ítem puede quedar con cantidad pendiente por definir. */
export function nonNegativeNumber(control: AbstractControl): ValidationErrors | null {
  const value = Number(control.value);
  return Number.isFinite(value) && value >= 0 ? null : { nonNegativeNumber: true };
}
