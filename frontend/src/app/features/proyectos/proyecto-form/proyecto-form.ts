import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { InputNumberModule } from 'primeng/inputnumber';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { CardModule } from 'primeng/card';
import { MessageModule } from 'primeng/message';

import { AuthService } from '../../../core/auth/auth.service';
import { ProjectService } from '../../../core/services/project.service';
import { CatalogService } from '../../../core/services/catalog.service';
import { Country, Currency, Department, Municipality } from '../../../core/models/catalog.model';
import { SigningMode } from '../../../core/models/project.model';
import { resolveApiError } from '../../../core/http/api-error';

@Component({
  selector: 'app-proyecto-form',
  imports: [
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    TextareaModule,
    SelectModule,
    InputNumberModule,
    ToggleSwitchModule,
    CardModule,
    MessageModule
  ],
  templateUrl: './proyecto-form.html',
  styleUrl: './proyecto-form.scss'
})
export class ProyectoForm {
  private readonly fb = inject(FormBuilder);
  private readonly projectService = inject(ProjectService);
  private readonly catalogService = inject(CatalogService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly currentUser = this.authService.currentUser;

  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly countries = signal<Country[]>([]);
  readonly departments = signal<Department[]>([]);
  readonly municipalities = signal<Municipality[]>([]);
  readonly currencies = signal<Currency[]>([]);

  readonly signingModes: { label: string; value: SigningMode }[] = [
    { label: 'Sin firma', value: 'SIN_FIRMA' },
    { label: 'Única por presupuestador asignado', value: 'UNICA_POR_PRESUPUESTADOR_ASIGNADO' },
    { label: 'Independiente por profesional responsable', value: 'INDEPENDIENTE_POR_PROFESIONAL_RESPONSABLE' }
  ];

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(255)]],
    description: ['', Validators.maxLength(1000)],
    objective: ['', Validators.maxLength(1000)],
    countryId: ['', Validators.required],
    departmentId: ['', Validators.required],
    municipalityId: ['', Validators.required],
    baseCurrencyId: ['', Validators.required],
    budgetLimit: this.fb.control<number | null>(null, Validators.min(0.01)),
    signingMode: ['UNICA_POR_PRESUPUESTADOR_ASIGNADO' as SigningMode, Validators.required],
    requiresAuditor: [false],
    adminPercent: [0, [Validators.min(0), Validators.max(100)]],
    unforeseenPercent: [0, [Validators.min(0), Validators.max(100)]],
    profitPercent: [0, [Validators.min(0), Validators.max(100)]]
  });

  constructor() {
    this.catalogService.countries().subscribe({
      next: (res) => this.countries.set(res.items),
      error: (error: unknown) => this.errorMessage.set(resolveApiError(error).message)
    });
    this.catalogService.currencies().subscribe({
      next: (res) => this.currencies.set(res.items),
      error: (error: unknown) => this.errorMessage.set(resolveApiError(error).message)
    });
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
      error: (error: unknown) => this.errorMessage.set(resolveApiError(error).message)
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
      error: (error: unknown) => this.errorMessage.set(resolveApiError(error).message)
    });
  }

  submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorMessage.set(
        'Completa los campos obligatorios: nombre, país, departamento, municipio, divisa y modo de firma.'
      );
      return;
    }

    const value = this.form.getRawValue();
    this.saving.set(true);
    this.errorMessage.set(null);

    this.projectService
      .create({
        name: value.name,
        description: value.description,
        objective: value.objective,
        countryId: value.countryId,
        departmentId: value.departmentId,
        municipalityId: value.municipalityId,
        baseCurrencyId: value.baseCurrencyId,
        budgetLimit: value.budgetLimit ?? undefined,
        signingMode: value.signingMode,
        requiresAuditor: value.requiresAuditor,
        aiuAdminPercent: value.adminPercent,
        aiuUnforeseenPercent: value.unforeseenPercent,
        aiuProfitPercent: value.profitPercent
      })
      .subscribe({
        next: (response) => {
          this.saving.set(false);
          this.router.navigate(['/proyectos', response.projectId]);
        },
        error: (error: unknown) => {
          this.saving.set(false);
          this.errorMessage.set(resolveApiError(error).message);
        }
      });
  }
}
