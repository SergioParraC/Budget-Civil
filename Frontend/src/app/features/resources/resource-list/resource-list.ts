import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { ConfirmationService, MenuItem, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { MenuModule } from 'primeng/menu';
import { MessageModule } from 'primeng/message';
import { SelectModule } from 'primeng/select';
import { TableLazyLoadEvent, TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';

import { AuthService } from '../../../core/auth/auth.service';
import { Permission } from '../../../core/auth/permissions';
import {
  ApiError,
  alertTitleFor,
  errorNeedsRefresh,
  resolveApiError
} from '../../../core/http/api-error';
import {
  MATRIX_RESOURCE_TYPE_CODE,
  Resource,
  ResourceQuotationFilter,
  ResourceSortField,
  ResourceTypeOption,
  SortDirection
} from '../../../core/models/resource.model';
import { CatalogService } from '../../../core/services/catalog.service';
import { ProjectContextService } from '../../../core/services/project-context.service';
import { ResourceService } from '../../../core/services/resource.service';
import { ProjectUnavailable } from '../../proyectos/project-unavailable/project-unavailable';
import { ConflictDialog } from '../../wbs/edt/conflict-dialog/conflict-dialog';
import { QuotationDialog } from '../quotation-dialog/quotation-dialog';
import { ResourceCopyDialog } from '../resource-copy-dialog/resource-copy-dialog';
import { ResourceDetailDialog } from '../resource-detail-dialog/resource-detail-dialog';
import { ResourceFormDialog } from '../resource-form-dialog/resource-form-dialog';

interface Option {
  id: string;
  name: string;
}

/**
 * Listado de insumos del proyecto (`REQ-INSUMOS-23`): filtros, orden y paginación en servidor; todas las
 * demás operaciones se resuelven en modales.
 */
@Component({
  selector: 'app-resource-list',
  imports: [
    FormsModule,
    TableModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    SelectModule,
    MenuModule,
    MessageModule,
    TagModule,
    TooltipModule,
    DecimalPipe,
    DatePipe,
    ProjectUnavailable,
    ResourceFormDialog,
    ResourceDetailDialog,
    ResourceCopyDialog,
    QuotationDialog,
    ConflictDialog
  ],
  templateUrl: './resource-list.html',
  styleUrl: './resource-list.scss'
})
export class ResourceList {
  private readonly route = inject(ActivatedRoute);
  private readonly authService = inject(AuthService);
  private readonly resourceService = inject(ResourceService);
  private readonly catalogService = inject(CatalogService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  readonly projectContext = inject(ProjectContextService);

  readonly projectId = this.route.snapshot.paramMap.get('id');

  readonly canWrite = computed(() => this.authService.can(Permission.resourcesWrite));
  readonly canReadApus = computed(() => this.authService.can(Permission.apusRead));
  readonly canWriteApus = computed(() => this.authService.can(Permission.apusWrite));

  readonly rows = signal<Resource[]>([]);
  readonly totalRecords = signal(0);
  readonly loading = signal(false);
  readonly loadError = signal(false);
  readonly selection = signal<Resource[]>([]);
  readonly types = signal<ResourceTypeOption[]>([]);
  readonly municipalities = signal<Option[]>([]);

  readonly search = signal('');
  readonly typeId = signal('');
  readonly quotationStatus = signal('');
  readonly brand = signal('');
  readonly municipalityId = signal('');
  readonly minPrice = signal<number | null>(null);
  readonly maxPrice = signal<number | null>(null);

  readonly pageSize = signal<10 | 50 | 100>(10);
  readonly sortBy = signal<ResourceSortField>('description');
  readonly sortDirection = signal<SortDirection>('asc');

  readonly formVisible = signal(false);
  readonly formResource = signal<Resource | null>(null);
  readonly detailVisible = signal(false);
  readonly detailResource = signal<Resource | null>(null);
  readonly copyVisible = signal(false);
  readonly quotationVisible = signal(false);

  readonly alertVisible = signal(false);
  readonly alert = signal({
    title: '',
    message: '',
    code: null as string | null,
    traceId: null as string | null,
    refresh: false
  });

  readonly quotationFilter = computed<ResourceQuotationFilter>(() => ({
    description: this.search().trim() || undefined,
    resourceTypeId: this.typeId() || undefined,
    quotationStatus: this.quotationStatus() || undefined,
    brand: this.brand().trim() || undefined,
    municipalityId: this.municipalityId() || undefined,
    minPrice: this.minPrice() ?? undefined,
    maxPrice: this.maxPrice() ?? undefined
  }));

  readonly selectedIds = computed(() => this.selection().map((resource) => resource.resourceId));

  readonly project = computed(() => this.projectContext.project());

  readonly statusOptions = [
    { label: 'Falta información', value: 'FALTA_INFORMACION' },
    { label: 'Por cotizar', value: 'POR_COTIZAR' },
    { label: 'Cotizado', value: 'COTIZADO' }
  ];

  constructor() {
    this.projectContext.load(this.projectId);

    effect(() => {
      const project = this.projectContext.project();
      if (!project) {
        return;
      }

      untracked(() => {
        this.loadOptions(project.departmentId);
        this.fetch(1);
      });
    });
  }

  private loadOptions(departmentId: string | null | undefined): void {
    this.resourceService.types(this.projectId ?? '').subscribe({
      next: (types) => this.types.set(types),
      error: () => this.types.set([])
    });

    if (!departmentId) {
      this.municipalities.set([]);
      return;
    }

    this.catalogService.municipalities(departmentId).subscribe({
      next: (response) =>
        this.municipalities.set(
          response.items.map((municipality) => ({ id: municipality.id, name: municipality.name }))
        ),
      error: () => this.municipalities.set([])
    });
  }

  private fetch(page: number): void {
    const projectId = this.projectId;
    if (!projectId) {
      return;
    }

    const min = this.minPrice();
    const max = this.maxPrice();

    if (min !== null && max !== null && min > max) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Rango de precio inválido',
        detail: 'El precio mínimo no puede ser mayor que el máximo.'
      });
      return;
    }

    this.loading.set(true);
    this.loadError.set(false);

    this.resourceService
      .list(projectId, {
        search: this.search(),
        resourceTypeId: this.typeId() || undefined,
        quotationStatus: this.quotationStatus() || undefined,
        brand: this.brand(),
        municipalityId: this.municipalityId() || undefined,
        minUnitPrice: min ?? undefined,
        maxUnitPrice: max ?? undefined,
        page,
        pageSize: this.pageSize(),
        sortBy: this.sortBy(),
        sortDirection: this.sortDirection()
      })
      .subscribe({
        next: (response) => {
          this.rows.set(response.items);
          this.totalRecords.set(response.totalItems);
          this.loading.set(false);
        },
        error: (error: unknown) => {
          this.rows.set([]);
          this.totalRecords.set(0);
          this.loading.set(false);
          this.loadError.set(true);
          this.handleFailure(resolveApiError(error));
        }
      });
  }

  onLazyLoad(event: TableLazyLoadEvent): void {
    const size = (event.rows ?? 10) as 10 | 50 | 100;
    this.pageSize.set(size);

    if (event.sortField) {
      this.sortBy.set(event.sortField as ResourceSortField);
      this.sortDirection.set(event.sortOrder === -1 ? 'desc' : 'asc');
    }

    this.fetch(Math.floor((event.first ?? 0) / size) + 1);
  }

  applyFilters(): void {
    this.fetch(1);
  }

  clearFilters(): void {
    this.search.set('');
    this.typeId.set('');
    this.quotationStatus.set('');
    this.brand.set('');
    this.municipalityId.set('');
    this.minPrice.set(null);
    this.maxPrice.set(null);
    this.fetch(1);
  }

  openCreate(): void {
    this.formResource.set(null);
    this.formVisible.set(true);
  }

  openEdit(resource: Resource): void {
    this.detailVisible.set(false);
    this.formResource.set(resource);
    this.formVisible.set(true);
  }

  openDetail(resource: Resource): void {
    this.detailResource.set(resource);
    this.detailVisible.set(true);
  }

  onSaved(resource: Resource): void {
    this.messageService.add({
      severity: 'success',
      summary: this.formResource() ? 'Insumo actualizado' : 'Insumo creado',
      detail: resource.description
    });

    this.formResource.set(null);
    this.fetch(1);

    if (resource.resourceTypeCode === MATRIX_RESOURCE_TYPE_CODE) {
      this.openDetail(resource);
    }
  }

  openCopy(): void {
    this.copyVisible.set(true);
  }

  onCopied(resource: Resource): void {
    this.messageService.add({
      severity: 'success',
      summary: 'Insumo copiado',
      detail: resource.description
    });
    this.fetch(1);
  }

  openQuotation(): void {
    this.quotationVisible.set(true);
  }

  onQuotationCompleted(): void {
    this.selection.set([]);
    this.fetch(1);
  }

  onDetailChanged(): void {
    this.refreshDetail();
    this.fetch(1);
  }

  private refreshDetail(): void {
    const current = this.detailResource();
    const projectId = this.projectId;

    if (!current || !projectId) {
      return;
    }

    this.resourceService.getById(projectId, current.resourceId).subscribe({
      next: (resource) => this.detailResource.set(resource),
      error: () => this.detailVisible.set(false)
    });
  }

  rowMenuItems(resource: Resource): MenuItem[] {
    const items: MenuItem[] = [
      {
        label: 'Ver detalle',
        icon: 'pi pi-eye',
        command: () => this.openDetail(resource)
      }
    ];

    if (this.canWrite()) {
      items.push(
        { label: 'Editar', icon: 'pi pi-pencil', command: () => this.openEdit(resource) },
        { label: 'Duplicar/Copiar', icon: 'pi pi-copy', command: () => this.openCopy() },
        { separator: true },
        {
          label: 'Eliminar',
          icon: 'pi pi-trash',
          command: () => this.confirmDelete(resource)
        }
      );
    }

    return items;
  }

  confirmDelete(resource: Resource): void {
    const matrix = resource.resourceTypeCode === MATRIX_RESOURCE_TYPE_CODE
      ? ' Se eliminarán también sus componentes.'
      : '';

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
    const projectId = this.projectId;
    if (!projectId) {
      return;
    }

    this.resourceService.delete(projectId, resource.resourceId).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Insumo eliminado' });
        this.detailVisible.set(false);
        this.fetch(1);
      },
      error: (error: unknown) => this.handleFailure(resolveApiError(error))
    });
  }

  private handleFailure(apiError: ApiError): void {
    this.alert.set({
      title: alertTitleFor(apiError.code),
      message: apiError.message,
      code: apiError.code,
      traceId: apiError.traceId,
      refresh: errorNeedsRefresh(apiError.code)
    });
    this.alertVisible.set(true);
  }

  onAlertRefresh(): void {
    this.fetch(1);
    this.refreshDetail();
  }

  isMatrix(resource: Resource): boolean {
    return resource.resourceTypeCode === MATRIX_RESOURCE_TYPE_CODE;
  }

  onDetailFailed(apiError: ApiError): void {
    this.handleFailure(apiError);
  }
}
