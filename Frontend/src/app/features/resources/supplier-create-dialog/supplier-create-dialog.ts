import { Component, computed, inject, input, model, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { TextareaModule } from 'primeng/textarea';
import { Observable } from 'rxjs';

import { resolveApiError } from '../../../core/http/api-error';
import { SupplierDetail, SupplierSummary, UpdateSupplierRequest } from '../../../core/models/resource.model';
import { SupplierService } from '../../../core/services/supplier.service';

/**
 * Alta y edición rápida de proveedor desde el formulario de insumo (`REQ-PROVEEDORES-01`,
 * `REQ-PROVEEDORES-02`). Solo la razón social es obligatoria; al guardar emite el proveedor para
 * seleccionarlo en el insumo. Con `supplierId` carga el proveedor y lo actualiza con su `rowVersion`.
 */
@Component({
  selector: 'app-supplier-create-dialog',
  imports: [ReactiveFormsModule, DialogModule, ButtonModule, InputTextModule, TextareaModule, MessageModule],
  templateUrl: './supplier-create-dialog.html',
  styleUrl: './supplier-create-dialog.scss'
})
export class SupplierCreateDialog {
  private readonly fb = inject(FormBuilder);
  private readonly supplierService = inject(SupplierService);

  readonly visible = model(false);
  /** Proveedor seleccionado en el insumo; `null` crea uno nuevo. */
  readonly supplierId = input<string | null>(null);
  readonly saved = output<SupplierSummary>();

  readonly saving = signal(false);
  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  private readonly loadedSupplier = signal<SupplierDetail | null>(null);

  readonly isEdit = computed(() => this.supplierId() !== null);

  readonly form = this.fb.nonNullable.group({
    businessName: ['', [Validators.required, Validators.maxLength(200)]],
    contactName: ['', [Validators.maxLength(150)]],
    contactPhone: ['', [Validators.maxLength(30)]],
    email: ['', [Validators.maxLength(254), Validators.email]],
    address: ['', [Validators.maxLength(300)]],
    observations: ['', [Validators.maxLength(1000)]]
  });

  prepare(): void {
    this.errorMessage.set(null);
    this.saving.set(false);
    this.loadedSupplier.set(null);
    this.form.reset();

    const supplierId = this.supplierId();
    if (!supplierId) {
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.supplierService.getById(supplierId).subscribe({
      next: (supplier) => {
        this.loadedSupplier.set(supplier);
        this.form.reset({
          businessName: supplier.businessName,
          contactName: supplier.contactName ?? '',
          contactPhone: supplier.contactPhone ?? '',
          email: supplier.email ?? '',
          address: supplier.address ?? '',
          observations: supplier.observations ?? ''
        });
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.errorMessage.set(resolveApiError(error).message);
      }
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const request = {
      businessName: raw.businessName.trim(),
      contactName: raw.contactName.trim() || null,
      contactPhone: raw.contactPhone.trim() || null,
      email: raw.email.trim() || null,
      address: raw.address.trim() || null,
      observations: raw.observations.trim() || null
    };

    this.saving.set(true);
    this.errorMessage.set(null);

    const supplier = this.loadedSupplier();
    const request$: Observable<SupplierSummary> = supplier
      ? this.supplierService.update(supplier.supplierId, {
          ...request,
          countryId: supplier.countryId,
          departmentId: supplier.departmentId,
          municipalityId: supplier.municipalityId,
          rowVersion: supplier.rowVersion
        } satisfies UpdateSupplierRequest)
      : this.supplierService.create(request);

    request$.subscribe({
      next: (stored) => {
        this.saving.set(false);
        this.visible.set(false);
        this.saved.emit(stored);
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.errorMessage.set(resolveApiError(error).message);
      }
    });
  }
}
