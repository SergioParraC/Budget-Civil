import { DecimalPipe } from '@angular/common';
import { Component, effect, inject, input, model, output, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { ToggleSwitchModule } from 'primeng/toggleswitch';

import { resolveApiError } from '../../../../core/http/api-error';
import {
  MATRIX_RESOURCE_TYPE_CODE,
  Resource,
  ResourceTypeOption
} from '../../../../core/models/resource.model';
import { ResourceService } from '../../../../core/services/resource.service';

/**
 * Selector de insumos del proyecto con búsqueda, tipo, unidad y precio visibles y filtro opcional por la
 * unidad del APU (`REQ-APUS-24`). `onlySimple` excluye las Matrices para los componentes de Matriz.
 */
@Component({
  selector: 'app-resource-picker-dialog',
  imports: [
    FormsModule,
    DialogModule,
    ButtonModule,
    InputTextModule,
    SelectModule,
    ToggleSwitchModule,
    TableModule,
    MessageModule,
    DecimalPipe
  ],
  templateUrl: './resource-picker-dialog.html',
  styleUrl: './resource-picker-dialog.scss'
})
export class ResourcePickerDialog {
  private readonly resourceService = inject(ResourceService);

  readonly visible = model(false);
  readonly projectId = input.required<string>();
  readonly onlySimple = input(false);
  readonly initialUnit = input<string | null>(null);
  readonly picked = output<Resource>();

  readonly term = signal('');
  readonly types = signal<ResourceTypeOption[]>([]);
  readonly typeId = signal('');
  readonly onlySameUnit = signal(false);
  readonly items = signal<Resource[]>([]);
  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  constructor() {
    effect(() => {
      if (!this.visible()) {
        return;
      }

      untracked(() => {
        this.term.set('');
        this.typeId.set('');
        this.onlySameUnit.set(false);
        this.loadTypes();
        this.search();
      });
    });
  }

  private loadTypes(): void {
    if (this.types().length > 0) {
      return;
    }

    this.resourceService.types(this.projectId()).subscribe({
      next: (types) => this.types.set(types),
      error: () => this.types.set([])
    });
  }

  search(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    const unit = this.initialUnit();

    if (this.onlySameUnit() && unit) {
      this.resourceService.byUnit(this.projectId(), unit).subscribe({
        next: (resources) => {
          this.items.set(this.filterEligible(resources));
          this.loading.set(false);
        },
        error: (error: unknown) => {
          this.items.set([]);
          this.loading.set(false);
          this.errorMessage.set(resolveApiError(error).message);
        }
      });
      return;
    }

    this.resourceService
      .list(this.projectId(), {
        search: this.term(),
        resourceTypeId: this.typeId() || undefined,
        page: 1,
        pageSize: 100
      })
      .subscribe({
        next: (response) => {
          this.items.set(this.filterEligible(response.items));
          this.loading.set(false);
        },
        error: (error: unknown) => {
          this.items.set([]);
          this.loading.set(false);
          this.errorMessage.set(resolveApiError(error).message);
        }
      });
  }

  choose(resource: Resource): void {
    this.picked.emit(resource);
    this.visible.set(false);
  }

  private filterEligible(resources: Resource[]): Resource[] {
    return resources.filter(
      (resource) => !this.onlySimple() || resource.resourceTypeCode !== MATRIX_RESOURCE_TYPE_CODE
    );
  }
}
