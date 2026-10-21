import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { InputNumberModule } from 'primeng/inputnumber';
import { CardModule } from 'primeng/card';

import { ProjectService } from '../../../core/services/project.service';
import { CatalogService } from '../../../core/services/catalog.service';
import { Country, Currency, Department, Municipality } from '../../../core/models/catalog.model';
import { SigningMode } from '../../../core/models/project.model';

@Component({
  selector: 'app-proyecto-form',
  imports: [ReactiveFormsModule, ButtonModule, InputTextModule, TextareaModule, SelectModule, InputNumberModule, CardModule],
  templateUrl: './proyecto-form.html',
  styleUrl: './proyecto-form.scss'
})
export class ProyectoForm {
  private readonly fb = inject(FormBuilder);
  private readonly projectService = inject(ProjectService);
  private readonly catalogService = inject(CatalogService);
  private readonly router = inject(Router);

  readonly saving = signal(false);
  readonly countries = signal<Country[]>([]);
  readonly departments = signal<Department[]>([]);
  readonly municipalities = signal<Municipality[]>([]);
  readonly currencies = signal<Currency[]>([]);

  readonly signingModes: { label: string; value: SigningMode }[] = [
    { label: 'Única por presupuestador asignado', value: 'UNICA_POR_PRESUPUESTADOR_ASIGNADO' },
    { label: 'Independiente por profesional responsable', value: 'INDEPENDIENTE_POR_PROFESIONAL_RESPONSABLE' }
  ];

  readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    description: [''],
    objective: [''],
    countryId: ['', Validators.required],
    departmentId: ['', Validators.required],
    municipalityId: ['', Validators.required],
    baseCurrencyId: ['', Validators.required],
    signingMode: ['UNICA_POR_PRESUPUESTADOR_ASIGNADO' as SigningMode, Validators.required],
    adminPercent: [0],
    unforeseenPercent: [0],
    profitPercent: [0]
  });

  constructor() {
    this.catalogService.countries().subscribe((res) => this.countries.set(res.items));
    this.catalogService.currencies().subscribe((res) => this.currencies.set(res.items));
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

  submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.saving.set(true);

    this.projectService
      .create({
        name: value.name,
        description: value.description,
        objective: value.objective,
        country_id: value.countryId,
        department_id: value.departmentId,
        municipality_id: value.municipalityId,
        base_currency_id: value.baseCurrencyId,
        signing_mode: value.signingMode,
        aiu_admin_percent: value.adminPercent,
        aiu_unforeseen_percent: value.unforeseenPercent,
        aiu_profit_percent: value.profitPercent
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.router.navigate(['/proyectos']);
        },
        error: () => this.saving.set(false)
      });
  }
}
