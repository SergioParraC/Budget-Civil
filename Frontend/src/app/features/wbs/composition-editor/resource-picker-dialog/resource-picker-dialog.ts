import { DecimalPipe } from '@angular/common';
import { Component, effect, inject, input, model, output, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';

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
  /** Se emite cuando el diálogo terminó de cerrarse, con o sin selección. */
  readonly closed = output<void>();

  readonly term = signal('');
  readonly types = signal<ResourceTypeOption[]>([]);
  readonly typeId = signal('');
  readonly onlySameUnit = signal(false);
  readonly items = signal<Resource[]>([]);
  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  /** Página de la tabla; se reinicia en cada búsqueda para no quedar en una página vacía. */
  readonly first = signal(0);

  /** Texto de búsqueda con retardo: la búsqueda se dispara sola mientras se escribe. */
  private readonly termChanges = new Subject<string>();

  constructor() {
    this.termChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe(() => {
        if (this.visible()) {
          this.search();
        }
      });

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

  /** Búsqueda automática por ID o descripción mientras se escribe (sin botón “Buscar”). */
  onTermChange(value: string): void {
    this.term.set(value);
    this.termChanges.next(value.trim());
  }

  onTypeChange(typeId: string | null): void {
    this.typeId.set(typeId ?? '');
    this.search();
  }

  onOnlySameUnitChange(enabled: boolean): void {
    this.onlySameUnit.set(enabled);
    this.search();
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
    this.first.set(0);
    this.loading.set(true);
    this.errorMessage.set(null);

    const unit = this.initialUnit();

    if (this.onlySameUnit() && unit) {
      this.resourceService.byUnit(this.projectId(), unit).subscribe({
        next: (resources) => {
          this.items.set(this.filterByUnitResults(resources));
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

  /** `by-unit` no acepta texto ni tipo: se filtran en el cliente para que la búsqueda siga siendo automática. */
  private filterByUnitResults(resources: Resource[]): Resource[] {
    const term = this.term().trim().toLowerCase();
    const typeId = this.typeId();

    return this.filterEligible(resources).filter((resource) => {
      if (typeId && resource.resourceTypeId !== typeId) {
        return false;
      }

      if (!term) {
        return true;
      }

      return (
        resource.description.toLowerCase().includes(term) ||
        (resource.userKey?.toLowerCase().includes(term) ?? false)
      );
    });
  }
}
