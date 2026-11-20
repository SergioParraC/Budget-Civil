import { Component, effect, inject, input, model, output, signal, untracked } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';

import { resolveApiError } from '../../../../core/http/api-error';
import { Unit } from '../../../../core/models/unit.model';
import { UnitService } from '../../../../core/services/unit.service';

/** Submodal “Nueva unidad” del formulario de APU e insumo (`REQ-UNIDADES-06`). */
@Component({
  selector: 'app-unit-create-dialog',
  imports: [ReactiveFormsModule, DialogModule, ButtonModule, InputTextModule, MessageModule],
  templateUrl: './unit-create-dialog.html',
  styleUrl: './unit-create-dialog.scss'
})
export class UnitCreateDialog {
  private readonly unitService = inject(UnitService);
  private readonly fb = inject(FormBuilder);

  readonly visible = model(false);
  readonly created = output<Unit>();

  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    shortUnit: ['', [Validators.required, Validators.maxLength(10)]],
    unit: ['', [Validators.required, Validators.maxLength(100)]]
  });

  constructor() {
    effect(() => {
      if (!this.visible()) {
        return;
      }

      untracked(() => {
        this.form.reset({ shortUnit: '', unit: '' });
        this.errorMessage.set(null);
      });
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.errorMessage.set(null);

    this.unitService.create(this.form.getRawValue()).subscribe({
      next: (unit) => {
        this.saving.set(false);
        this.visible.set(false);
        this.created.emit(unit);
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.errorMessage.set(resolveApiError(error).message);
      }
    });
  }
}
