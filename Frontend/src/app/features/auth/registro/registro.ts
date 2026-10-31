import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { PasswordModule } from 'primeng/password';
import { SelectModule } from 'primeng/select';

import { AuthService } from '../../../core/auth/auth.service';
import { resolveApiError } from '../../../core/http/api-error';
import { RegisterResponse } from '../../../core/models/auth.model';
import { Country, Department, DocumentType, Municipality } from '../../../core/models/catalog.model';
import { CatalogService } from '../../../core/services/catalog.service';

@Component({
  selector: 'app-registro',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    ButtonModule,
    CardModule,
    InputTextModule,
    MessageModule,
    PasswordModule,
    SelectModule
  ],
  templateUrl: './registro.html',
  styleUrl: './registro.scss'
})
export class Registro {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly catalogService = inject(CatalogService);

  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly registered = signal<RegisterResponse | null>(null);

  readonly documentTypes = signal<DocumentType[]>([]);
  readonly countries = signal<Country[]>([]);
  readonly departments = signal<Department[]>([]);
  readonly municipalities = signal<Municipality[]>([]);

  readonly form = this.fb.nonNullable.group({
    documentType: ['', Validators.required],
    documentNumber: ['', Validators.required],
    firstName: ['', Validators.required],
    middleName: [''],
    lastName: ['', Validators.required],
    secondLastName: [''],
    birthDate: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    username: ['', [Validators.required, Validators.minLength(4)]],
    password: [
      '',
      [
        Validators.required,
        Validators.minLength(8),
        Validators.pattern(/^(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).+$/)
      ]
    ],
    countryId: ['', Validators.required],
    departmentId: ['', Validators.required],
    municipalityId: ['', Validators.required]
  });

  constructor() {
    this.catalogService.documentTypes().subscribe((res) => this.documentTypes.set(res.items));
    this.catalogService.countries().subscribe((res) => this.countries.set(res.items));
  }

  onCountryChange(countryId: string) {
    this.form.patchValue({ departmentId: '', municipalityId: '' });
    this.departments.set([]);
    this.municipalities.set([]);
    if (!countryId) {
      return;
    }
    this.catalogService.departments(countryId).subscribe((res) => this.departments.set(res.items));
  }

  onDepartmentChange(departmentId: string) {
    this.form.patchValue({ municipalityId: '' });
    this.municipalities.set([]);
    if (!departmentId) {
      return;
    }
    this.catalogService.municipalities(departmentId).subscribe((res) => this.municipalities.set(res.items));
  }

  submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorMessage.set(
        'Completa los campos obligatorios. La contraseña requiere 8 caracteres, una mayúscula, un número y un símbolo.'
      );
      return;
    }

    this.saving.set(true);
    this.errorMessage.set(null);
    this.authService.register(this.form.getRawValue()).subscribe({
      next: (response) => {
        this.saving.set(false);
        this.registered.set(response);
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.errorMessage.set(resolveApiError(error).message);
      }
    });
  }
}
