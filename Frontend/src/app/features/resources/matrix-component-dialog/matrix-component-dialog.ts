import { Component, effect, inject, input, model, output, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { MessageModule } from 'primeng/message';

import { ApiError, resolveApiError } from '../../../core/http/api-error';
import { MatrixComponentItem, MatrixDetail } from '../../../core/models/apu.model';
import { Resource } from '../../../core/models/resource.model';
import { ApuService } from '../../../core/services/apu.service';
import { ResourcePickerDialog } from '../../wbs/composition-editor/resource-picker-dialog/resource-picker-dialog';

/** Submodal para agregar o editar un componente de Matriz (`REQ-INSUMOS-28`). */
@Component({
  selector: 'app-matrix-component-dialog',
  imports: [
    FormsModule,
    DialogModule,
    ButtonModule,
    InputNumberModule,
    MessageModule,
    ResourcePickerDialog
  ],
  templateUrl: './matrix-component-dialog.html',
  styleUrl: './matrix-component-dialog.scss'
})
export class MatrixComponentDialog {
  private readonly apuService = inject(ApuService);

  readonly visible = model(false);
  readonly projectId = input.required<string>();
  readonly matrixResourceId = input.required<string>();
  readonly matrixRowVersion = input.required<string>();
  readonly component = input<MatrixComponentItem | null>(null);
  readonly saved = output<MatrixDetail>();
  readonly failed = output<ApiError>();

  readonly pickerVisible = signal(false);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly selected = signal<{ resourceId: string; description: string; unit: string | null } | null>(
    null
  );
  readonly consumption = signal(1);
  readonly wastePercentage = signal(0);

  constructor() {
    effect(() => {
      if (!this.visible()) {
        return;
      }

      untracked(() => {
        const component = this.component();
        this.errorMessage.set(null);
        this.selected.set(
          component
            ? {
                resourceId: component.resourceId,
                description: component.description,
                unit: component.unit
              }
            : null
        );
        this.consumption.set(component?.quantity ?? 1);
        this.wastePercentage.set(component?.wastePercentage ?? 0);
      });
    });
  }

  onPicked(resource: Resource): void {
    this.selected.set({
      resourceId: resource.resourceId,
      description: resource.description,
      unit: resource.unit
    });
  }

  submit(): void {
    const selected = this.selected();
    if (!selected) {
      this.errorMessage.set('Selecciona el insumo del componente.');
      return;
    }

    if (!(this.consumption() > 0) || this.wastePercentage() < 0 || this.wastePercentage() > 100) {
      this.errorMessage.set(
        'El consumo debe ser mayor que cero y el desperdicio debe estar entre 0 y 100.'
      );
      return;
    }

    const component = this.component();
    this.saving.set(true);
    this.errorMessage.set(null);

    const request$ = component
      ? this.apuService.updateMatrixComponent(
          this.projectId(),
          this.matrixResourceId(),
          component.componentId,
          {
            consumption: this.consumption(),
            wastePercentage: this.wastePercentage(),
            rowVersion: this.matrixRowVersion()
          }
        )
      : this.apuService.addMatrixComponent(this.projectId(), this.matrixResourceId(), {
          componentResourceId: selected.resourceId,
          consumption: this.consumption(),
          wastePercentage: this.wastePercentage(),
          rowVersion: this.matrixRowVersion()
        });

    request$.subscribe({
      next: (detail) => {
        this.saving.set(false);
        this.visible.set(false);
        this.saved.emit(detail);
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
