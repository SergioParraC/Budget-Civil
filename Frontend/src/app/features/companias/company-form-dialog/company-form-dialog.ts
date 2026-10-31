import { Component, effect, inject, input, model, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { MessageService } from 'primeng/api';

import {
  Company,
  CompanyFormValue,
  CompanyType,
  MyCompany,
  RepresentativeMode
} from '../../../core/models/company.model';
import { Country, Department, DocumentType, Municipality } from '../../../core/models/catalog.model';
import { UserListItem } from '../../../core/models/user.model';
import { resolveApiError } from '../../../core/http/api-error';
import { CatalogService } from '../../../core/services/catalog.service';
import { CompanyService } from '../../../core/services/company.service';
import { UserService } from '../../../core/services/user.service';

type CompanyFormSource = Company | MyCompany;

/**
 * Formulario de creación/edición de compañía reutilizable: lo usa el Administrador en la lista de
 * compañías y el Director de proyectos desde el dashboard (configurar o crear su compañía).
 */
@Component({
  selector: 'app-company-form-dialog',
  imports: [DialogModule, ReactiveFormsModule, InputTextModule, SelectModule, ButtonModule],
  templateUrl: './company-form-dialog.html',
  styleUrl: './company-form-dialog.scss'
})
export class CompanyFormDialog {
  private readonly companyService = inject(CompanyService);
  private readonly catalogService = inject(CatalogService);
  private readonly userService = inject(UserService);
  private readonly fb = inject(FormBuilder);
  private readonly messageService = inject(MessageService);

  readonly visible = model(false);
  readonly company = input<CompanyFormSource | null>(null);
  readonly saved = output<void>();

  readonly saving = signal(false);
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

  /** Búsqueda del representante legal por documento (tipo y número obligatorios, `D-87`). */
  readonly representativeSearchForm = this.fb.nonNullable.group({
    documentType: ['', Validators.required],
    documentNumber: ['', Validators.required],
    username: [''],
    email: ['']
  });

  constructor() {
    this.form.controls.representativeMode.valueChanges.subscribe((mode) => this.syncRepresentativeMode(mode));

    effect(() => {
      if (!this.visible()) {
        return;
      }

      // Se recargan al abrir: si el catálogo falló al crear el dashboard, el modal se recupera.
      this.loadCatalogs();
      this.prepare(this.company());
    });
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

  private prepare(source: CompanyFormSource | null) {
    this.resetRepresentativeSearch();
    this.departments.set([]);
    this.municipalities.set([]);

    if (!source) {
      this.form.reset({ companyType: 'PRIVADA', representativeMode: 'MANUAL_DATA' });
      this.syncRepresentativeMode('MANUAL_DATA');
      return;
    }

    this.form.setValue({
      businessName: source.businessName,
      taxId: source.taxId,
      companyType: source.companyType ?? 'PRIVADA',
      territorialLevel: source.territorialLevel ?? '',
      countryId: source.countryId ?? '',
      departmentId: source.departmentId ?? '',
      municipalityId: source.municipalityId ?? '',
      representativeMode: source.representativeMode ?? 'MANUAL_DATA',
      representativeUserId: source.representativeUserId ?? '',
      representativeFullName: source.representativeFullName ?? '',
      representativeDocumentTypeId: source.representativeDocumentTypeId ?? '',
      representativeDocumentNumber: source.representativeDocumentNumber ?? ''
    });

    this.syncRepresentativeMode(source.representativeMode ?? 'MANUAL_DATA');

    if (source.representativeMode === 'USER_REFERENCE' && source.representativeUserId) {
      this.userService.getById(source.representativeUserId).subscribe({
        next: (user) => this.selectedRepresentative.set(user),
        error: () => this.selectedRepresentative.set(null)
      });
    }

    if (source.countryId) {
      this.catalogService.departments(source.countryId).subscribe({
        next: (res) => this.departments.set(res.items),
        error: (error: unknown) => this.showCatalogError('los departamentos', error)
      });
    }

    if (source.departmentId) {
      this.catalogService.municipalities(source.departmentId).subscribe({
        next: (res) => this.municipalities.set(res.items),
        error: (error: unknown) => this.showCatalogError('los municipios', error)
      });
    }
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

  submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const payload: CompanyFormValue = {
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

    const editingId = this.company()?.companyId ?? null;
    this.saving.set(true);

    const request: Observable<unknown> = editingId
      ? this.companyService.update(editingId, payload)
      : this.companyService.create(payload);

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.visible.set(false);
        this.messageService.add({
          severity: 'success',
          summary: editingId ? 'Compañía actualizada' : 'Compañía creada'
        });
        this.saved.emit();
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'No se pudo guardar la compañía',
          detail: resolveApiError(error).message
        });
      }
    });
  }
}
