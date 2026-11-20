import { Component, effect, inject, input, model, output, signal, untracked } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';

import { nonNegativeNumber } from '../../../../core/forms/validators';
import { ApiError, resolveApiError } from '../../../../core/http/api-error';
import { WbsItemApuResponse, WbsNode } from '../../../../core/models/wbs.model';
import { WbsService } from '../../../../core/services/wbs.service';

/**
 * Edición de un ítem desde el EDT (`REQ-EDT-31`): cantidad de obra obligatoria, código opcional y bloque
 * APU opcional con `apuRowVersion`. Los insumos se editan en la página del APU.
 */
@Component({
  selector: 'app-item-form-dialog',
  imports: [
    ReactiveFormsModule,
    DialogModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    MessageModule
  ],
  templateUrl: './item-form-dialog.html',
  styleUrl: './item-form-dialog.scss'
})
export class ItemFormDialog {
  private readonly wbsService = inject(WbsService);
  private readonly fb = inject(FormBuilder);

  readonly visible = model(false);
  readonly projectId = input.required<string>();
  readonly node = input<WbsNode | null>(null);
  readonly canEditApu = input(false);
  readonly saved = output<WbsItemApuResponse>();
  readonly failed = output<ApiError>();

  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    code: ['', [Validators.maxLength(10)]],
    quantityWork: [1, [nonNegativeNumber]],
    description: [''],
    unit: ['', [Validators.maxLength(10)]]
  });

  constructor() {
    effect(() => {
      if (!this.visible()) {
        return;
      }

      untracked(() => {
        const node = this.node();
        this.form.reset({
          code: node?.code ?? '',
          quantityWork: node?.quantityWork ?? 1,
          description: node?.apu?.description ?? '',
          unit: node?.apu?.unit ?? ''
        });
        this.errorMessage.set(null);
      });
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const node = this.node();
    if (!node) {
      return;
    }

    const raw = this.form.getRawValue();
    const code = raw.code.trim();
    const sendApu = this.canEditApu() && node.apu !== null;

    this.saving.set(true);
    this.errorMessage.set(null);

    this.wbsService
      .updateItem(this.projectId(), node.id, {
        ...(code && code !== node.code ? { code } : {}),
        quantityWork: Number(raw.quantityWork),
        rowVersion: node.rowVersion,
        ...(sendApu
          ? {
              apu: {
                description: raw.description.trim() || undefined,
                unit: raw.unit.trim() || undefined,
                apuRowVersion: node.apu!.rowVersion
              }
            }
          : {})
      })
      .subscribe({
        next: (response) => {
          this.saving.set(false);
          this.visible.set(false);
          this.saved.emit(response);
        },
        error: (error: unknown) => {
          this.saving.set(false);
          const apiError = resolveApiError(error);
          this.errorMessage.set(apiError.message);
          this.failed.emit(apiError);
        }
      });
  }
}
