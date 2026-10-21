import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { ConfirmationService, MessageService } from 'primeng/api';

import { CompanyService } from '../../../core/services/company.service';
import { CatalogService } from '../../../core/services/catalog.service';
import { UserService } from '../../../core/services/user.service';
import { Company, CompanyType, RepresentativeMode, RoleCode } from '../../../core/models/company.model';
import { Country, Department, DocumentType, Municipality } from '../../../core/models/catalog.model';
import { UserListItem } from '../../../core/models/user.model';

@Component({
  selector: 'app-compania-list',
  imports: [TableModule, TagModule, ButtonModule, DialogModule, ReactiveFormsModule, InputTextModule, SelectModule],
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

  readonly companyTypes: { label: string; value: CompanyType }[] = [
    { label: 'Privada', value: 'PRIVADA' },
    { label: 'Pública', value: 'PUBLICA' }
  ];

  readonly representativeModes: { label: string; value: RepresentativeMode }[] = [
    { label: 'Referencia a usuario', value: 'USER_REFERENCE' },
    { label: 'Datos manuales', value: 'MANUAL_DATA' }
  ];

  readonly membershipDialogVisible = signal(false);
  readonly selectedCompany = signal<Company | null>(null);
  readonly memberCandidates = signal<UserListItem[]>([]);
  readonly roleCodes: { label: string; value: RoleCode }[] = [
    { label: 'Director de proyectos', value: 'GERENTE_PROYECTOS' },
    { label: 'Presupuestador', value: 'PRESUPUESTADOR' },
    { label: 'Auditor', value: 'AUDITOR' }
  ];

  readonly membershipForm = this.fb.nonNullable.group({
    userId: ['', Validators.required],
    roleCode: ['PRESUPUESTADOR' as RoleCode, Validators.required]
  });

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

  constructor() {
    this.catalogService.countries().subscribe((res) => this.countries.set(res.items));
    this.catalogService.documentTypes().subscribe((res) => this.documentTypes.set(res.items));
  }

  load(event: TableLazyLoadEvent) {
    this.lastLazyEvent = event;
    this.loading.set(true);
    const pageSize = (event.rows ?? 10) as 10 | 50 | 100;
    const pageNumber = Math.floor((event.first ?? 0) / pageSize) + 1;

    this.companyService.list({ pageNumber, pageSize }).subscribe({
      next: (response) => {
        this.companies.set(response.items);
        this.totalRecords.set(response.total_items);
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
    this.catalogService.departments(countryId).subscribe((res) => this.departments.set(res.items));
  }

  onDepartmentChange(departmentId: string) {
    this.form.patchValue({ municipalityId: '' });
    if (!departmentId) {
      this.municipalities.set([]);
      return;
    }
    this.catalogService.municipalities(departmentId).subscribe((res) => this.municipalities.set(res.items));
  }

  openCreate() {
    this.editingCompanyId.set(null);
    this.form.reset({ companyType: 'PRIVADA', representativeMode: 'MANUAL_DATA' });
    this.departments.set([]);
    this.municipalities.set([]);
    this.dialogVisible.set(true);
  }

  openEdit(company: Company) {
    this.editingCompanyId.set(company.company_id);
    this.form.setValue({
      businessName: company.business_name,
      taxId: company.tax_id,
      companyType: company.company_type,
      territorialLevel: company.territorial_level ?? '',
      countryId: company.country_id,
      departmentId: company.department_id,
      municipalityId: company.municipality_id,
      representativeMode: company.representative_mode,
      representativeUserId: company.representative_user_id ?? '',
      representativeFullName: company.representative_full_name ?? '',
      representativeDocumentTypeId: company.representative_document_type_id ?? '',
      representativeDocumentNumber: company.representative_document_number ?? ''
    });
    this.catalogService.departments(company.country_id).subscribe((res) => this.departments.set(res.items));
    this.catalogService.municipalities(company.department_id).subscribe((res) => this.municipalities.set(res.items));
    this.dialogVisible.set(true);
  }

  submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const payload = {
      business_name: value.businessName,
      tax_id: value.taxId,
      company_type: value.companyType,
      territorial_level: value.companyType === 'PUBLICA' ? value.territorialLevel : undefined,
      country_id: value.countryId,
      department_id: value.departmentId,
      municipality_id: value.municipalityId,
      representative: {
        mode: value.representativeMode,
        user_id: value.representativeMode === 'USER_REFERENCE' ? value.representativeUserId : undefined,
        full_name: value.representativeMode === 'MANUAL_DATA' ? value.representativeFullName : undefined,
        document_type_id: value.representativeMode === 'MANUAL_DATA' ? value.representativeDocumentTypeId : undefined,
        document_number: value.representativeMode === 'MANUAL_DATA' ? value.representativeDocumentNumber : undefined
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
      error: () => {
        this.saving.set(false);
        this.messageService.add({ severity: 'error', summary: 'No se pudo guardar la compañía' });
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
      header: company.is_active ? 'Desactivar compañía' : 'Activar compañía',
      message: `¿Confirmas ${company.is_active ? 'desactivar' : 'activar'} "${company.business_name}"?`,
      accept: () => {
        const request = company.is_active
          ? this.companyService.disable(company.company_id, 'Desactivada desde administración')
          : this.companyService.enable(company.company_id);

        request.subscribe({
          next: () => {
            this.messageService.add({ severity: 'success', summary: 'Estado actualizado' });
            this.reload();
          },
          error: () => this.messageService.add({ severity: 'error', summary: 'No se pudo cambiar el estado' })
        });
      }
    });
  }

  openMemberships(company: Company) {
    this.selectedCompany.set(company);
    this.membershipForm.reset({ roleCode: 'PRESUPUESTADOR' });
    this.userService.listAll().subscribe((users) => this.memberCandidates.set(users));
    this.membershipDialogVisible.set(true);
  }

  assignMembership() {
    const company = this.selectedCompany();
    if (!company || this.membershipForm.invalid) {
      this.membershipForm.markAllAsTouched();
      return;
    }

    const { userId, roleCode } = this.membershipForm.getRawValue();
    this.companyService.assignMembership(company.company_id, userId, roleCode).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Usuario vinculado a la compañía' });
        this.membershipForm.reset({ roleCode: 'PRESUPUESTADOR' });
      },
      error: () => this.messageService.add({ severity: 'error', summary: 'No se pudo vincular el usuario' })
    });
  }
}
