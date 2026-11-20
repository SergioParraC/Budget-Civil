import { Component, computed, effect, inject, input, model, output, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConfirmationService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { MessageModule } from 'primeng/message';
import { SelectButtonModule } from 'primeng/selectbutton';

import { ApiError, resolveApiError } from '../../../core/http/api-error';
import {
  ChangeResourceQuotationStatusRequest,
  ChangeResourceQuotationStatusResponse,
  ResourceQuotationFilter,
  ResourceQuotationScope
} from '../../../core/models/resource.model';
import { ResourceService } from '../../../core/services/resource.service';

/** Cotización masiva en modal con los alcances selección, filtro actual y totalidad (`REQ-INSUMOS-31`). */
@Component({
  selector: 'app-quotation-dialog',
  imports: [FormsModule, DialogModule, ButtonModule, SelectButtonModule, MessageModule],
  templateUrl: './quotation-dialog.html',
  styleUrl: './quotation-dialog.scss'
})
export class QuotationDialog {
  private readonly resourceService = inject(ResourceService);
  private readonly confirmationService = inject(ConfirmationService);

  readonly visible = model(false);
  readonly projectId = input.required<string>();
  readonly selection = input<string[]>([]);
  readonly filter = input<ResourceQuotationFilter>({});
  readonly completed = output<ChangeResourceQuotationStatusResponse>();
  readonly failed = output<ApiError>();

  readonly mode = signal<ResourceQuotationScope>('FILTER');
  readonly summary = signal<ChangeResourceQuotationStatusResponse | null>(null);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly modeOptions = [
    { label: 'Selección', value: 'SELECTION' as ResourceQuotationScope },
    { label: 'Filtro actual', value: 'FILTER' as ResourceQuotationScope },
    { label: 'Todos', value: 'ALL' as ResourceQuotationScope }
  ];

  readonly selectionCount = computed(() => this.selection().length);

  readonly confirmDisabled = computed(
    () => this.saving() || (this.mode() === 'SELECTION' && this.selectionCount() === 0)
  );

  constructor() {
    effect(() => {
      if (!this.visible()) {
        return;
      }

      untracked(() => {
        this.mode.set(this.selectionCount() > 0 ? 'SELECTION' : 'FILTER');
        this.summary.set(null);
        this.errorMessage.set(null);
      });
    });
  }

  modeSummary(): string {
    switch (this.mode()) {
      case 'SELECTION':
        return `${this.selectionCount()} insumo(s) seleccionados`;
      case 'ALL':
        return 'Todos los insumos del proyecto';
      default:
        return 'Los insumos que cumplen los filtros actuales del listado';
    }
  }

  requestConfirmation(): void {
    if (this.confirmDisabled()) {
      return;
    }

    this.confirmationService.confirm({
      header: 'Confirmar cotización masiva',
      message: `Se marcarán como COTIZADO ${this.modeSummary()}. Los insumos incompletos se omitirán. ¿Continuar?`,
      icon: 'pi pi-question-circle',
      acceptLabel: 'Cotizar',
      rejectLabel: 'Cancelar',
      accept: () => this.execute()
    });
  }

  private execute(): void {
    this.saving.set(true);
    this.errorMessage.set(null);

    const request: ChangeResourceQuotationStatusRequest =
      this.mode() === 'SELECTION'
        ? { scope: 'SELECTION', resourceIds: this.selection() }
        : this.mode() === 'FILTER'
          ? { scope: 'FILTER', filter: this.filter() }
          : { scope: 'ALL' };

    this.resourceService.changeQuotationStatus(this.projectId(), request).subscribe({
      next: (response) => {
        this.saving.set(false);
        this.summary.set(response);
        this.completed.emit(response);
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
