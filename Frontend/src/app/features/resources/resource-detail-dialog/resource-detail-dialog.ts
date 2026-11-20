import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, effect, inject, input, model, output, signal, untracked } from '@angular/core';
import { ConfirmationService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { MessageModule } from 'primeng/message';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';

import { ApiError, resolveApiError } from '../../../core/http/api-error';
import { MatrixComponentItem, MatrixDetail } from '../../../core/models/apu.model';
import { MATRIX_RESOURCE_TYPE_CODE, Resource } from '../../../core/models/resource.model';
import { ApuService } from '../../../core/services/apu.service';
import { ResourceService } from '../../../core/services/resource.service';
import { MatrixComponentDialog } from '../matrix-component-dialog/matrix-component-dialog';

/** Detalle del insumo en modal, con la composición de la Matriz (`REQ-INSUMOS-27`, `REQ-INSUMOS-28`). */
@Component({
  selector: 'app-resource-detail-dialog',
  imports: [
    DialogModule,
    ButtonModule,
    TableModule,
    TagModule,
    MessageModule,
    DecimalPipe,
    DatePipe,
    MatrixComponentDialog
  ],
  templateUrl: './resource-detail-dialog.html',
  styleUrl: './resource-detail-dialog.scss'
})
export class ResourceDetailDialog {
  private readonly resourceService = inject(ResourceService);
  private readonly apuService = inject(ApuService);
  private readonly confirmationService = inject(ConfirmationService);

  readonly visible = model(false);
  readonly projectId = input.required<string>();
  readonly resource = input<Resource | null>(null);
  readonly canWrite = input(false);
  readonly canReadApus = input(false);
  readonly canWriteApus = input(false);

  readonly editRequested = output<Resource>();
  readonly copyRequested = output<Resource>();
  readonly changed = output<void>();
  readonly failed = output<ApiError>();

  readonly matrix = signal<MatrixDetail | null>(null);
  readonly matrixLoading = signal(false);
  readonly componentDialogVisible = signal(false);
  readonly componentToEdit = signal<MatrixComponentItem | null>(null);
  readonly deleting = signal(false);

  readonly isMatrix = computed(
    () => this.resource()?.resourceTypeCode === MATRIX_RESOURCE_TYPE_CODE
  );

  readonly matrixComponents = computed(() =>
    (this.matrix()?.resources ?? []).flatMap((group) =>
      group.items.map((item) => ({ ...item, resourceType: group.resourceType }))
    )
  );

  constructor() {
    effect(() => {
      if (!this.visible()) {
        return;
      }

      untracked(() => {
        this.matrix.set(null);
        if (this.isMatrix() && this.canReadApus()) {
          this.loadMatrix();
        }
      });
    });
  }

  loadMatrix(): void {
    const resource = this.resource();
    if (!resource) {
      return;
    }

    this.matrixLoading.set(true);
    this.apuService.getMatrixComponents(this.projectId(), resource.resourceId).subscribe({
      next: (detail) => {
        this.matrix.set(detail);
        this.matrixLoading.set(false);
      },
      error: (error: unknown) => {
        this.matrixLoading.set(false);
        this.failed.emit(resolveApiError(error));
      }
    });
  }

  openAddComponent(): void {
    this.componentToEdit.set(null);
    this.componentDialogVisible.set(true);
  }

  openEditComponent(component: MatrixComponentItem): void {
    this.componentToEdit.set(component);
    this.componentDialogVisible.set(true);
  }

  onComponentSaved(detail: MatrixDetail): void {
    this.matrix.set(detail);
    this.componentDialogVisible.set(false);
    this.changed.emit();
  }

  onComponentFailed(apiError: ApiError): void {
    this.failed.emit(apiError);
  }

  removeComponent(component: MatrixComponentItem): void {
    const resource = this.resource();
    const matrix = this.matrix();

    if (!resource || !matrix || !this.canWriteApus()) {
      return;
    }

    this.confirmationService.confirm({
      header: 'Quitar componente',
      message: `¿Confirmas quitar “${component.description}” de la Matriz?`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Quitar',
      rejectLabel: 'Cancelar',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.apuService
          .removeMatrixComponent(
            this.projectId(),
            resource.resourceId,
            component.componentId,
            matrix.rowVersion
          )
          .subscribe({
            next: () => {
              this.loadMatrix();
              this.changed.emit();
            },
            error: (error: unknown) => this.failed.emit(resolveApiError(error))
          });
      }
    });
  }

  requestEdit(): void {
    const resource = this.resource();
    if (resource) {
      this.editRequested.emit(resource);
    }
  }

  requestCopy(): void {
    const resource = this.resource();
    if (resource) {
      this.copyRequested.emit(resource);
    }
  }

  confirmDelete(): void {
    const resource = this.resource();
    if (!resource || !this.canWrite()) {
      return;
    }

    const matrix = this.isMatrix() ? ' Se eliminarán también sus componentes.' : '';

    this.confirmationService.confirm({
      header: 'Eliminar insumo',
      message: `¿Confirmas eliminar “${resource.description}”?${matrix}`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Eliminar',
      rejectLabel: 'Cancelar',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => this.deleteResource(resource)
    });
  }

  private deleteResource(resource: Resource): void {
    this.deleting.set(true);

    this.resourceService.delete(this.projectId(), resource.resourceId).subscribe({
      next: () => {
        this.deleting.set(false);
        this.visible.set(false);
        this.changed.emit();
      },
      error: (error: unknown) => {
        this.deleting.set(false);
        this.failed.emit(resolveApiError(error));
      }
    });
  }
}
