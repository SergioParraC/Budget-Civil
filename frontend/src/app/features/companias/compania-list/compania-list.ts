import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmationService, MessageService } from 'primeng/api';

import { CompanyService } from '../../../core/services/company.service';
import { CatalogService } from '../../../core/services/catalog.service';
import { UserService } from '../../../core/services/user.service';
import { Company, CompanyType, RepresentativeMode } from '../../../core/models/company.model';
import { resolveApiError } from '../../../core/http/api-error';
import { Country, Department, DocumentType, Municipality } from '../../../core/models/catalog.model';
import { UserListItem } from '../../../core/models/user.model';
import { CompanyUsersDialog } from '../company-users-dialog/company-users-dialog';

@Component({
  selector: 'app-compania-list',
  imports: [TableModule, TagModule, ButtonModule, DialogModule, ReactiveFormsModule, InputTextModule, SelectModule, TooltipModule, CompanyUsersDialog],
  templateUrl: './compania-list.html',
  styleUrl: './compania-list.scss'
})
export class CompaniaList {
  private readonly companyService = inject(CompanyService);
  private readonly catalogService = inject(CatalogService);
  private readonly userService = inject(UserService);
  private readonly fb = inject(FormBuilder);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  readonly companies = signal<Company[]>([]);
  readonly totalRecords = signal(0);
  readonly loading = signal(false);
  readonly saving = signal(false);
  private lastLazyEvent: TableLazyLoadEvent = { first: 0, rows: 10 };

  readonly dialogVisible = signal(false);
  readonly editingCompanyId = signal<string | null>(null);

  readonly countries = signal<Country[]>([]);
  readonly departments = signal<Department[]>([]);
  readonly municipalities = signal<Municipality[]>([]);
  readonly documentTypes = signal<DocumentType[]>([]);

  readonly representativeCandidates = signal<UserListItem[]>([]);
  readonly representativeSearching = signal(false);
  readonly representativeSearchAttempted = signal(false);
  readonly selectedRepresentative = signal<UserListItem | null>(null);

  readonly companyTypes: { label: string; value: CompanyType }[] = [
    { label: 'Privada', value: 'PRIVADA' },
    { label: 'Pública', value: 'PUBLICA' }
  ];

  readonly representativeModes: { label: string; value: RepresentativeMode }[] = [
    { label: 'Referencia a usuario', value: 'USER_REFERENCE' },
    { label: 'Datos manuales', value: 'MANUAL_DATA' }
  ];

  readonly directorsDialogVisible = signal(false);
  readonly directorsCompany = signal<Company | null>(null);

  readonly form = this.fb.nonNullable.group({
    businessName: ['', Validators.required],
    taxId: ['', Validators.required],
    companyType: ['PRIVADA' as CompanyType, Validators.required],
    territorialLevel: [''],
    countryId: ['', Validators.required],
    departmentId: ['', Validators.required],
    municipalityId: ['', Validators.required],
    representativeMode: ['MANUAL_DATA' as RepresentativeMode, Validators.required],
    representativeUserId: [''],
    representativeFullName: [''],
    representativeDocumentTypeId: [''],
    representativeDocumentNumber: ['']
  });

  /** Búsqueda del representante legal por documento (tipo y número obligatorios). */
  readonly representativeSearchForm = this.fb.nonNullable.group({
    documentType: ['', Validators.required],
    documentNumber: ['', Validators.required],
    username: [''],
    email: ['']
  });

  constructor() {
    this.loadCatalogs();

    this.form.controls.representativeMode.valueChanges.subscribe((mode) => this.syncRepresentativeMode(mode));
  }

  private loadCatalogs() {
    this.catalogService.countries().subscribe({
      next: (res) => this.countries.set(res.items),
      error: (error: unknown) => this.showCatalogError('los países', error)
    });
    this.catalogService.documentTypes().subscribe({
      next: (res) => this.documentTypes.set(res.items),
      error: (error: unknown) => this.showCatalogError('los tipos de documento', error)
    });
  }

  private showCatalogError(resource: string, error: unknown) {
    this.messageService.add({
      severity: 'error',
      summary: `No se pudieron cargar ${resource}`,
      detail: resolveApiError(error).message
    });
  }

  load(event: TableLazyLoadEvent) {
    this.lastLazyEvent = event;
    this.loading.set(true);
    const pageSize = (event.rows ?? 10) as 10 | 50 | 100;
    const pageNumber = Math.floor((event.first ?? 0) / pageSize) + 1;

    this.companyService.list({ pageNumber, pageSize }).subscribe({
      next: (response) => {
        this.companies.set(response.items);
        this.totalRecords.set(response.totalItems);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  private reload() {
    this.load(this.lastLazyEvent);
  }

  onCountryChange(countryId: string) {
    this.form.patchValue({ departmentId: '', municipalityId: '' });
    this.municipalities.set([]);
    if (!countryId) {
      this.departments.set([]);
      return;
    }
    this.catalogService.departments(countryId).subscribe({
      next: (res) => this.departments.set(res.items),
      error: (error: unknown) => this.showCatalogError('los departamentos', error)
    });
  }

  onDepartmentChange(departmentId: string) {
    this.form.patchValue({ municipalityId: '' });
    if (!departmentId) {
      this.municipalities.set([]);
      return;
    }
    this.catalogService.municipalities(departmentId).subscribe({
      next: (res) => this.municipalities.set(res.items),
      error: (error: unknown) => this.showCatalogError('los municipios', error)
    });
  }

  onRepresentativeModeChange(mode: RepresentativeMode) {
    this.syncRepresentativeMode(mode);
    if (mode === 'MANUAL_DATA') {
      this.clearRepresentative();
    }
  }

  searchRepresentative() {
    if (this.representativeSearchForm.invalid) {
      this.representativeSearchForm.markAllAsTouched();
      return;
    }

    const { documentType, documentNumber, username, email } = this.representativeSearchForm.getRawValue();
    this.representativeSearching.set(true);
    this.representativeSearchAttempted.set(true);

    this.userService.searchRepresentativeCandidates({ documentType, documentNumber, username, email }).subscribe({
      next: (users) => {
        this.representativeCandidates.set(users);
        this.representativeSearching.set(false);
      },
      error: (error: unknown) => {
        this.representativeSearching.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'No se pudo buscar el usuario',
          detail: resolveApiError(error).message
        });
      }
    });
  }

  selectRepresentative(user: UserListItem) {
    this.selectedRepresentative.set(user);
    this.representativeCandidates.set([]);
    this.representativeSearchAttempted.set(false);
    this.form.patchValue({ representativeUserId: user.userId });
    this.form.controls.representativeUserId.markAsTouched();
  }

  clearRepresentative() {
    this.selectedRepresentative.set(null);
    this.representativeCandidates.set([]);
    this.representativeSearchAttempted.set(false);
    this.form.patchValue({ representativeUserId: '' });
  }

  private resetRepresentativeSearch() {
    this.representativeSearchForm.reset();
    this.selectedRepresentative.set(null);
    this.representativeCandidates.set([]);
    this.representativeSearchAttempted.set(false);
  }

  private syncRepresentativeMode(mode: RepresentativeMode) {
    const control = this.form.controls.representativeUserId;
    control.setValidators(mode === 'USER_REFERENCE' ? [Validators.required] : []);
    control.updateValueAndValidity({ emitEvent: false });
  }

  openCreate() {
    this.loadCatalogs();
    this.editingCompanyId.set(null);
    this.form.reset({ companyType: 'PRIVADA', representativeMode: 'MANUAL_DATA' });
    this.resetRepresentativeSearch();
    this.departments.set([]);
    this.municipalities.set([]);
    this.dialogVisible.set(true);
  }

  openEdit(company: Company) {
    this.loadCatalogs();
    this.editingCompanyId.set(company.companyId);
    this.form.setValue({
      businessName: company.businessName,
      taxId: company.taxId,
      companyType: company.companyType ?? 'PRIVADA',
      territorialLevel: company.territorialLevel ?? '',
      countryId: company.countryId ?? '',
      departmentId: company.departmentId ?? '',
      municipalityId: company.municipalityId ?? '',
      representativeMode: company.representativeMode ?? 'MANUAL_DATA',
      representativeUserId: company.representativeUserId ?? '',
      representativeFullName: company.representativeFullName ?? '',
      representativeDocumentTypeId: company.representativeDocumentTypeId ?? '',
      representativeDocumentNumber: company.representativeDocumentNumber ?? ''
    });

    this.resetRepresentativeSearch();
    this.syncRepresentativeMode(company.representativeMode ?? 'MANUAL_DATA');

    if (company.representativeMode === 'USER_REFERENCE' && company.representativeUserId) {
      this.userService.getById(company.representativeUserId).subscribe({
        next: (user) => this.selectedRepresentative.set(user),
        error: () => this.selectedRepresentative.set(null)
      });
    }

    if (company.countryId) {
      this.catalogService.departments(company.countryId).subscribe((res) => this.departments.set(res.items));
    }

    if (company.departmentId) {
      this.catalogService.municipalities(company.departmentId).subscribe((res) => this.municipalities.set(res.items));
    }

    this.dialogVisible.set(true);
  }

  submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const payload = {
      businessName: value.businessName,
      taxId: value.taxId,
      companyType: value.companyType,
      territorialLevel: value.companyType === 'PUBLICA' ? value.territorialLevel : undefined,
      countryId: value.countryId,
      departmentId: value.departmentId,
      municipalityId: value.municipalityId,
      representative: {
        mode: value.representativeMode,
        userId: value.representativeMode === 'USER_REFERENCE' ? value.representativeUserId : undefined,
        fullName: value.representativeMode === 'MANUAL_DATA' ? value.representativeFullName : undefined,
        documentTypeId: value.representativeMode === 'MANUAL_DATA' ? value.representativeDocumentTypeId : undefined,
        documentNumber: value.representativeMode === 'MANUAL_DATA' ? value.representativeDocumentNumber : undefined
      }
    };

    this.saving.set(true);
    const editingId = this.editingCompanyId();
    const onSettled = {
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.messageService.add({ severity: 'success', summary: editingId ? 'Compañía actualizada' : 'Compañía creada' });
        this.reload();
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'No se pudo guardar la compañía',
          detail: resolveApiError(error).message
        });
      }
    };

    if (editingId) {
      this.companyService.update(editingId, payload).subscribe(onSettled);
    } else {
      this.companyService.create(payload).subscribe(onSettled);
    }
  }

  toggleActive(company: Company) {
    this.confirmationService.confirm({
      header: company.isActive ? 'Desactivar compañía' : 'Activar compañía',
      message: `¿Confirmas ${company.isActive ? 'desactivar' : 'activar'} "${company.businessName}"?`,
      accept: () => {
        const request = company.isActive
          ? this.companyService.disable(company.companyId, 'Desactivada desde administración')
          : this.companyService.enable(company.companyId);

        request.subscribe({
          next: () => {
            this.messageService.add({ severity: 'success', summary: 'Estado actualizado' });
            this.reload();
          },
          error: (error: unknown) =>
            this.messageService.add({
              severity: 'error',
              summary: 'No se pudo cambiar el estado',
              detail: resolveApiError(error).message
            })
        });
      }
    });
  }

  openDirectors(company: Company) {
    this.directorsCompany.set(company);
    this.directorsDialogVisible.set(true);
  }
}
