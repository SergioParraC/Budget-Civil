import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
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

/**
 * Política de contraseña alineada con `RegisterUserRequestValidator` del backend:
 * mínimo 8 caracteres, una mayúscula (incluye Á, É, Í, Ó, Ú, Ñ, Ü), un número y un símbolo.
 */
const PASSWORD_PATTERN = /^(?=.*\p{Lu})(?=.*\p{Nd})(?=.*[^\p{L}\p{N}\s]).+$/u;

/** Etiquetas en español para nombrar los campos inválidos en el mensaje de resumen. */
const FIELD_LABELS: Record<string, string> = {
  documentType: 'tipo de documento',
  documentNumber: 'número de documento',
  firstName: 'primer nombre',
  lastName: 'primer apellido',
  birthDate: 'fecha de nacimiento',
  email: 'correo electrónico',
  username: 'nombre de usuario',
  password: 'contraseña',
  countryId: 'país',
  departmentId: 'departamento',
  municipalityId: 'municipio'
};

const PASSWORD_HINT = 'La contraseña requiere 8 caracteres, una mayúscula, un número y un símbolo.';

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
      [Validators.required, Validators.minLength(8), Validators.pattern(PASSWORD_PATTERN)]
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

  /** Marca visualmente el campo solo cuando el usuario ya lo tocó o intentó enviar el formulario. */
  isInvalid(control: AbstractControl): boolean {
    return control.invalid && control.touched;
  }

  get passwordError(): string | null {
    const control = this.form.controls.password;
    if (!this.isInvalid(control)) {
      return null;
    }

    if (control.hasError('required')) {
      return 'La contraseña es obligatoria.';
    }

    if (control.hasError('minlength')) {
      return 'La contraseña debe tener al menos 8 caracteres.';
    }

    return 'La contraseña debe incluir una mayúscula, un número y un símbolo.';
  }

  get usernameError(): string | null {
    const control = this.form.controls.username;
    if (!this.isInvalid(control)) {
      return null;
    }

    return control.hasError('required')
      ? 'El nombre de usuario es obligatorio.'
      : 'El nombre de usuario debe tener al menos 4 caracteres.';
  }

  get emailError(): string | null {
    const control = this.form.controls.email;
    if (!this.isInvalid(control)) {
      return null;
    }

    return control.hasError('required')
      ? 'El correo electrónico es obligatorio.'
      : 'Ingresa un correo electrónico válido.';
  }

  submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorMessage.set(this.buildValidationMessage());
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

  /** Resume qué campos fallan y solo menciona la política de contraseña si ese campo es el inválido. */
  private buildValidationMessage(): string {
    const invalidLabels = Object.entries(FIELD_LABELS)
      .filter(([name]) => this.form.controls[name as keyof typeof this.form.controls].invalid)
      .map(([, label]) => label);

    const summary =
      invalidLabels.length > 0
        ? `Revisa los campos marcados en rojo: ${invalidLabels.join(', ')}.`
        : 'Revisa los datos del formulario.';

    return this.form.controls.password.invalid ? `${summary} ${PASSWORD_HINT}` : summary;
  }
}
