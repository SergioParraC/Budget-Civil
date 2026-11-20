import { Component, computed, effect, inject, input, model, output, signal, untracked } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { SelectModule } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';
import { TooltipModule } from 'primeng/tooltip';

import { AuthService } from '../../../core/auth/auth.service';
import { Permission } from '../../../core/auth/permissions';
import { ApiError, resolveApiError } from '../../../core/http/api-error';
import {
  Resource,
  ResourceSuggestion,
  ResourceTypeOption,
  SupplierOption,
  SupplierSummary
} from '../../../core/models/resource.model';
import { Unit } from '../../../core/models/unit.model';
import { CatalogService } from '../../../core/services/catalog.service';
import { ResourceService } from '../../../core/services/resource.service';
import { SupplierService } from '../../../core/services/supplier.service';
import { UnitService } from '../../../core/services/unit.service';
import { UnitCreateDialog } from '../../wbs/composition-editor/unit-create-dialog/unit-create-dialog';
import { SupplierCreateDialog } from '../supplier-create-dialog/supplier-create-dialog';

interface Option {
  id: string;
  name: string;
}

/**
 * Alta y edición de un insumo en modal (`REQ-INSUMOS-24`): identificación, costo y abastecimiento, y
 * detalles, con sugerencias de duplicados y unidad del catálogo como sugerencia.
 */
@Component({
  selector: 'app-resource-form-dialog',
  imports: [
    ReactiveFormsModule,
    DialogModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    SelectModule,
    TextareaModule,
    MessageModule,
    TooltipModule,
    UnitCreateDialog,
    SupplierCreateDialog
  ],
  templateUrl: './resource-form-dialog.html',
  styleUrl: './resource-form-dialog.scss'
})
export class ResourceFormDialog {
  private readonly resourceService = inject(ResourceService);
  private readonly catalogService = inject(CatalogService);
  private readonly supplierService = inject(SupplierService);
  private readonly unitService = inject(UnitService);
  private readonly authService = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  readonly visible = model(false);
  readonly projectId = input.required<string>();
  readonly resource = input<Resource | null>(null);
  readonly companyId = input<string | null>(null);
  readonly defaultCountryId = input<string | null>(null);
  readonly defaultDepartmentId = input<string | null>(null);
  readonly defaultMunicipalityId = input<string | null>(null);
  readonly saved = output<Resource>();
  readonly failed = output<ApiError>();

  readonly canWrite = this.authService.can(Permission.resourcesWrite);
  readonly canUseSuppliers = this.authService.can(Permission.suppliersRead);
  readonly canCreateSupplier = this.authService.can(Permission.suppliersWrite);
  readonly canCreateUnit = this.authService.can(Permission.catalogsWrite);

  readonly types = signal<ResourceTypeOption[]>([]);
  readonly units = signal<Unit[]>([]);
  readonly departments = signal<Option[]>([]);
  readonly municipalities = signal<Option[]>([]);
  readonly suppliers = signal<SupplierOption[]>([]);
  readonly suggestions = signal<ResourceSuggestion[]>([]);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly unitDialogVisible = signal(false);
  readonly supplierDialogVisible = signal(false);

  readonly isEdit = computed(() => this.resource() !== null);
  readonly unitOptions = computed(() => this.units().map((unit) => unit.shortUnit));

  readonly form = this.fb.nonNullable.group({
    resourceTypeId: ['', [Validators.required]],
    description: ['', [Validators.required, Validators.maxLength(255)]],
    unit: ['', [Validators.maxLength(20)]],
    departmentId: [''],
    municipalityId: ['', [Validators.required]],
    unitPrice: this.fb.control<number | null>(null, [Validators.min(0)]),
    brand: ['', [Validators.maxLength(100)]],
    distributor: ['', [Validators.maxLength(150)]],
    supplierId: [''],
    contactNumber: ['', [Validators.maxLength(50)]],
    userKey: ['', [Validators.maxLength(50)]],
    details: ['']
  });

  constructor() {
    effect(() => {
      if (!this.visible()) {
        return;
      }

      untracked(() => this.open());
    });
  }

  private open(): void {
    const resource = this.resource();

    this.suggestions.set([]);
    this.errorMessage.set(null);

    this.form.reset({
      resourceTypeId: resource?.resourceTypeId ?? '',
      description: resource?.description ?? '',
      unit: resource?.unit ?? '',
      departmentId: this.defaultDepartmentId() ?? '',
      municipalityId: resource?.municipalityId ?? this.defaultMunicipalityId() ?? '',
      unitPrice: resource?.unitPrice ?? null,
      brand: resource?.brand ?? '',
      distributor: resource?.distributor ?? '',
      supplierId: resource?.supplierId ?? '',
      contactNumber: resource?.contactNumber ?? '',
      userKey: resource?.userKey ?? '',
      details: resource?.details ?? ''
    });

    this.loadOptions();

    this.departments.set([]);
    this.municipalities.set([]);

    const countryId = this.defaultCountryId();
    if (countryId) {
      this.loadDepartments(countryId, !this.isEdit());
    }
  }

  private loadOptions(): void {
    this.unitService.list().subscribe({
      next: (units) => this.units.set(units),
      error: () => this.units.set([])
    });

    this.resourceService.types(this.projectId()).subscribe({
      next: (types) => {
        this.types.set(types);
        this.applyDefaultType(types);
      },
      error: () => this.types.set([])
    });

    if (this.canUseSuppliers) {
      this.supplierService.options({ companyId: this.companyId() ?? undefined }).subscribe({
        next: (response) => this.suppliers.set(response.items),
        error: () => this.suppliers.set([])
      });
    }
  }

  private applyDefaultType(types: ResourceTypeOption[]): void {
    const control = this.form.controls.resourceTypeId;
    if (this.isEdit() || control.value) {
      return;
    }

    const material = types.find((type) => type.code === 'MATERIAL');
    if (material) {
      control.setValue(material.id);
    }
  }

  private loadDepartments(countryId: string, keepMunicipality: boolean): void {
    this.catalogService.departments(countryId).subscribe({
      next: (response) => {
        const departments = response.items.map((department) => ({
          id: department.id,
          name: department.name
        }));

        this.departments.set(departments);

        const departmentId = keepMunicipality
          ? this.defaultDepartmentId()
          : this.form.controls.departmentId.value;

        if (departmentId) {
          if (keepMunicipality) {
            this.form.controls.departmentId.setValue(departmentId);
          }
          this.loadMunicipalities(departmentId, keepMunicipality);
        }
      },
      error: () => this.departments.set([])
    });
  }

  onDepartmentChange(departmentId: string): void {
    this.form.controls.municipalityId.setValue('');
    this.municipalities.set([]);

    if (departmentId) {
      this.loadMunicipalities(departmentId, false);
    }
  }

  private loadMunicipalities(departmentId: string, keepMunicipality: boolean): void {
    this.catalogService.municipalities(departmentId).subscribe({
      next: (response) => {
        const municipalities = response.items.map((municipality) => ({
          id: municipality.id,
          name: municipality.name
        }));

        if (keepMunicipality) {
          const currentId = this.resource()?.municipalityId ?? this.defaultMunicipalityId();
          if (currentId && !municipalities.some((municipality) => municipality.id === currentId)) {
            municipalities.push({ id: currentId, name: 'Municipio actual' });
          }
        }

        this.municipalities.set(municipalities);
      },
      error: () => this.municipalities.set([])
    });
  }

  onUnitCreated(unit: Unit): void {
    this.units.update((units) =>
      units.some((candidate) => candidate.id === unit.id) ? units : [...units, unit]
    );
    this.form.controls.unit.setValue(unit.shortUnit);
  }

  onSupplierCreated(supplier: SupplierSummary): void {
    this.suppliers.update((suppliers) =>
      suppliers.some((candidate) => candidate.supplierId === supplier.supplierId)
        ? suppliers
        : [...suppliers, { supplierId: supplier.supplierId, businessName: supplier.businessName }]
    );
    this.form.controls.supplierId.setValue(supplier.supplierId);

    const contact = this.form.controls.contactNumber;
    if (!contact.value && supplier.contactPhone) {
      contact.setValue(supplier.contactPhone);
    }
  }

  checkSuggestions(): void {
    if (!this.canWrite) {
      return;
    }

    const description = this.form.controls.description.value.trim();
    const userKey = this.form.controls.userKey.value.trim();

    if (description.length < 3 && userKey.length < 3) {
      this.suggestions.set([]);
      return;
    }

    this.resourceService
      .suggestions(this.projectId(), description || undefined, userKey || undefined)
      .subscribe({
        next: (response) => this.suggestions.set(response.items),
        error: () => this.suggestions.set([])
      });
  }

  submit(): void {
    if (!this.canWrite) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const request = {
      resourceTypeId: raw.resourceTypeId,
      municipalityId: raw.municipalityId,
      description: raw.description.trim(),
      unit: raw.unit.trim() || null,
      unitPrice: raw.unitPrice,
      brand: raw.brand.trim() || null,
      distributor: raw.distributor.trim() || null,
      userKey: raw.userKey.trim() || null,
      details: raw.details.trim() || null,
      contactNumber: raw.contactNumber.trim() || null,
      supplierId: raw.supplierId || null
    };

    this.saving.set(true);
    this.errorMessage.set(null);

    const resource = this.resource();
    const request$ = resource
      ? this.resourceService.update(this.projectId(), resource.resourceId, {
          ...request,
          rowVersion: resource.rowVersion
        })
      : this.resourceService.create(this.projectId(), request);

    request$.subscribe({
      next: (stored) => {
        this.saving.set(false);
        this.visible.set(false);
        this.saved.emit(stored);
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
