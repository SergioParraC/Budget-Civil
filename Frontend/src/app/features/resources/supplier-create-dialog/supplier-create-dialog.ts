import { Component, inject, model, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { TextareaModule } from 'primeng/textarea';

import { resolveApiError } from '../../../core/http/api-error';
import { SupplierSummary } from '../../../core/models/resource.model';
import { SupplierService } from '../../../core/services/supplier.service';

/**
 * Alta rápida de proveedor desde el formulario de insumo (`REQ-PROVEEDORES-01`). Solo la razón social
 * es obligatoria; al guardar emite el proveedor para seleccionarlo en el insumo.
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
  readonly created = output<SupplierSummary>();

  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    businessName: ['', [Validators.required, Validators.maxLength(200)]],
    contactName: ['', [Validators.maxLength(150)]],
    contactPhone: ['', [Validators.maxLength(30)]],
    email: ['', [Validators.maxLength(254), Validators.email]],
    address: ['', [Validators.maxLength(300)]],
    observations: ['', [Validators.maxLength(1000)]]
  });

  prepare(): void {
    this.form.reset();
    this.errorMessage.set(null);
    this.saving.set(false);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    this.saving.set(true);
    this.errorMessage.set(null);

    this.supplierService
      .create({
        businessName: raw.businessName.trim(),
        contactName: raw.contactName.trim() || null,
        contactPhone: raw.contactPhone.trim() || null,
        email: raw.email.trim() || null,
        address: raw.address.trim() || null,
        observations: raw.observations.trim() || null
      })
      .subscribe({
        next: (supplier) => {
          this.saving.set(false);
          this.visible.set(false);
          this.created.emit(supplier);
        },
        error: (error: unknown) => {
          this.saving.set(false);
          this.errorMessage.set(resolveApiError(error).message);
        }
      });
  }
}
