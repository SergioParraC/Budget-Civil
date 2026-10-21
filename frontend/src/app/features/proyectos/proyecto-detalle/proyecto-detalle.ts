import { Component, computed, inject, signal } from '@angular/core';
import { JsonPipe } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { ButtonModule } from 'primeng/button';
import { TabsModule } from 'primeng/tabs';
import { TableModule } from 'primeng/table';
import { DialogModule } from 'primeng/dialog';
import { SelectModule } from 'primeng/select';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { ConfirmationService, MessageService } from 'primeng/api';

import { ProjectService } from '../../../core/services/project.service';
import { UserService } from '../../../core/services/user.service';
import { Project, ProjectStatus } from '../../../core/models/project.model';
import { RoleCode } from '../../../core/models/company.model';
import { UserListItem } from '../../../core/models/user.model';

const STATUS_SEVERITY: Record<ProjectStatus, 'success' | 'warn' | 'danger' | 'info' | 'secondary'> = {
  POR_COTIZAR: 'info',
  FALTA_INFORMACION: 'warn',
  COTIZADO: 'success',
  FINALIZADO: 'secondary',
  INHABILITADO: 'danger'
};

const NEXT_STATUS: Partial<Record<ProjectStatus, { target: ProjectStatus; label: string }>> = {
  POR_COTIZAR: { target: 'FALTA_INFORMACION', label: 'Marcar información faltante' },
  FALTA_INFORMACION: { target: 'COTIZADO', label: 'Marcar como cotizado' },
  COTIZADO: { target: 'FINALIZADO', label: 'Finalizar proyecto' },
  FINALIZADO: { target: 'FALTA_INFORMACION', label: 'Reabrir (falta información)' }
};

@Component({
  selector: 'app-proyecto-detalle',
  imports: [
    ReactiveFormsModule,
    FormsModule,
    JsonPipe,
    CardModule,
    TagModule,
    ButtonModule,
    TabsModule,
    TableModule,
    DialogModule,
    SelectModule,
    InputTextModule,
    TextareaModule,
    ToggleSwitchModule
  ],
  templateUrl: './proyecto-detalle.html',
  styleUrl: './proyecto-detalle.scss'
})
export class ProyectoDetalle {
  private readonly route = inject(ActivatedRoute);
  private readonly projectService = inject(ProjectService);
  private readonly userService = inject(UserService);
  private readonly fb = inject(FormBuilder);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  private readonly projectId = this.route.snapshot.paramMap.get('id')!;

  readonly project = signal<Project | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly members = signal<unknown[]>([]);

  readonly nextStatusAction = computed(() => {
    const project = this.project();
    return project ? NEXT_STATUS[project.status] : undefined;
  });

  readonly membershipDialogVisible = signal(false);
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

  readonly editForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
    description: [''],
    objective: ['']
  });

  constructor() {
    this.load();
  }

  statusSeverity(status: ProjectStatus) {
    return STATUS_SEVERITY[status];
  }

  private load() {
    this.loading.set(true);
    this.projectService.getById(this.projectId).subscribe({
      next: (project) => {
        this.project.set(project);
        this.editForm.setValue({
          name: project.name,
          description: project.description ?? '',
          objective: project.objective ?? ''
        });
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });

    this.projectService.listMembers(this.projectId).subscribe((res) => this.members.set(res.items));
  }

  saveEdit() {
    const project = this.project();
    if (!project || this.editForm.invalid) {
      return;
    }

    this.saving.set(true);
    this.projectService
      .update(this.projectId, {
        name: this.editForm.getRawValue().name,
        description: this.editForm.getRawValue().description,
        objective: this.editForm.getRawValue().objective,
        country_id: project.country_id,
        department_id: project.department_id,
        municipality_id: project.municipality_id,
        base_currency_id: project.base_currency_id,
        aiu_admin_percent: project.aiu_admin_percent ?? undefined,
        aiu_unforeseen_percent: project.aiu_unforeseen_percent ?? undefined,
        aiu_profit_percent: project.aiu_profit_percent ?? undefined
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.messageService.add({ severity: 'success', summary: 'Proyecto actualizado' });
          this.load();
        },
        error: () => {
          this.saving.set(false);
          this.messageService.add({ severity: 'error', summary: 'No se pudo actualizar el proyecto' });
        }
      });
  }

  advanceStatus() {
    const action = this.nextStatusAction();
    if (!action) {
      return;
    }
    this.projectService.changeStatus(this.projectId, { target_status: action.target as never }).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Estado actualizado' });
        this.load();
      },
      error: () => this.messageService.add({ severity: 'error', summary: 'No se pudo cambiar el estado' })
    });
  }

  disableProject() {
    this.confirmationService.confirm({
      header: 'Deshabilitar proyecto',
      message: '¿Confirmas deshabilitar este proyecto?',
      accept: () => {
        this.projectService.changeStatus(this.projectId, { target_status: 'INHABILITADO', reason: 'Deshabilitado desde administración' }).subscribe({
          next: () => {
            this.messageService.add({ severity: 'success', summary: 'Proyecto deshabilitado' });
            this.load();
          },
          error: () => this.messageService.add({ severity: 'error', summary: 'No se pudo deshabilitar' })
        });
      }
    });
  }

  reactivateProject() {
    this.projectService.reactivate(this.projectId).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Proyecto reactivado' });
        this.load();
      },
      error: () => this.messageService.add({ severity: 'error', summary: 'No se pudo reactivar' })
    });
  }

  toggleVisibility(isPublic: boolean) {
    this.projectService.changeVisibility(this.projectId, isPublic).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Visibilidad actualizada' });
        this.load();
      },
      error: () => this.messageService.add({ severity: 'error', summary: 'No se pudo cambiar la visibilidad' })
    });
  }

  openMemberships() {
    this.membershipForm.reset({ roleCode: 'PRESUPUESTADOR' });
    this.userService.listAll().subscribe((users) => this.memberCandidates.set(users));
    this.membershipDialogVisible.set(true);
  }

  assignMembership() {
    if (this.membershipForm.invalid) {
      this.membershipForm.markAllAsTouched();
      return;
    }
    const { userId, roleCode } = this.membershipForm.getRawValue();
    this.projectService.assignMembership(this.projectId, userId, roleCode).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Usuario vinculado al proyecto' });
        this.membershipDialogVisible.set(false);
        this.load();
      },
      error: () => this.messageService.add({ severity: 'error', summary: 'No se pudo vincular el usuario' })
    });
  }
}
