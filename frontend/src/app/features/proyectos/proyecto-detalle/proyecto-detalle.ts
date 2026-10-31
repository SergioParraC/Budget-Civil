import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import {
  AbstractControl,
  FormBuilder,
  FormsModule,
  ReactiveFormsModule,
  ValidationErrors,
  Validators
} from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { ButtonModule } from 'primeng/button';
import { TabsModule } from 'primeng/tabs';
import { TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { MessageModule } from 'primeng/message';
import { DialogModule } from 'primeng/dialog';
import { SelectModule } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { ConfirmationService, MessageService } from 'primeng/api';

import { ProjectService } from '../../../core/services/project.service';
import { UserService } from '../../../core/services/user.service';
import { CatalogService } from '../../../core/services/catalog.service';
import { Project, ProjectFormValue, ProjectMemberItem, ProjectStatus, SigningMode } from '../../../core/models/project.model';
import { Country, Currency, Department, Municipality } from '../../../core/models/catalog.model';
import { RoleCode } from '../../../core/models/company.model';
import { UserListItem } from '../../../core/models/user.model';
import { AuthService } from '../../../core/auth/auth.service';
import { Permission } from '../../../core/auth/permissions';
import { resolveApiError } from '../../../core/http/api-error';
import { ProjectMemberDialog } from '../project-member-dialog/project-member-dialog';

/** Máximo dos decimales para porcentajes y montos. */
function maxTwoDecimals(control: AbstractControl): ValidationErrors | null {
  const value = control.value;
  if (value === null || value === undefined || value === '') {
    return null;
  }

  const text = String(value);
  const decimals = text.includes('.') ? text.split('.')[1].length : 0;

  return decimals > 2 ? { maxTwoDecimals: true } : null;
}

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
    CardModule,
    TagModule,
    ButtonModule,
    TabsModule,
    TableModule,
    InputTextModule,
    InputNumberModule,
    SelectModule,
    MessageModule,
    DialogModule,
    TextareaModule,
    ToggleSwitchModule,
    DatePipe,
    ProjectMemberDialog
  ],
  templateUrl: './proyecto-detalle.html',
  styleUrl: './proyecto-detalle.scss'
})
export class ProyectoDetalle {
  private readonly route = inject(ActivatedRoute);
  private readonly projectService = inject(ProjectService);
  private readonly userService = inject(UserService);
  private readonly catalogService = inject(CatalogService);
  private readonly authService = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  readonly projectId = this.route.snapshot.paramMap.get('id')!;

  readonly project = signal<Project | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);

  readonly roleCode = this.authService.roleCode;
  readonly canEdit = this.authService.can(Permission.projectsUpdate);
  readonly canAssignMembers = this.authService.can(Permission.projectMembersAssign);
  readonly canChangeVisibility = computed(() => this.roleCode() === 'DIRECTOR');

  readonly nextStatusAction = computed(() => {
    const project = this.project();
    if (!project) {
      return undefined;
    }

    const action = NEXT_STATUS[project.status];
    if (!action) {
      return undefined;
    }

    return this.canPerformTransition(project.status, action.target) ? action : undefined;
  });

  readonly canDisable = computed(() => this.roleCode() === 'DIRECTOR' && this.project()?.status !== 'INHABILITADO');
  readonly canReactivate = computed(() => this.roleCode() === 'DIRECTOR' && this.project()?.status === 'INHABILITADO');

  readonly canSearchUsers = this.authService.can(Permission.usersRead);
  readonly canRemoveMembers = this.authService.can(Permission.projectMembersDeactivate);

  /** El Administrador no puede asignar ni retirar miembros de proyecto (`ProjectMembershipPolicy`). */
  readonly canManageMembers = computed(
    () => this.canAssignMembers && this.canSearchUsers && this.roleCode() !== 'ADMINISTRADOR'
  );

  readonly members = signal<ProjectMemberItem[]>([]);
  readonly membersLoading = signal(false);

  /** Auditores del proyecto; la pestaña solo se muestra si el proyecto los requiere. */
  readonly auditors = computed(() => this.members().filter((member) => member.roleCode === 'AUDITOR'));
  readonly memberUserIds = computed(() => this.members().map((member) => member.userId));

  readonly memberDialogVisible = signal(false);
  readonly memberDialogRole = signal<RoleCode>('PRESUPUESTADOR');
  readonly memberDialogTitle = computed(() =>
    this.memberDialogRole() === 'AUDITOR' ? 'Asignar auditor' : 'Asignar presupuestador'
  );
  readonly memberDialogHint = computed(() =>
    this.memberDialogRole() === 'AUDITOR'
      ? 'Busca auditores de la compañía del proyecto que aún no estén vinculados.'
      : 'Busca presupuestadores de la compañía del proyecto que aún no estén vinculados.'
  );

  readonly editForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
    description: [''],
    objective: ['']
  });

  /** Configuración del proyecto: ubicación, divisa, tope, AIU, firma y auditoría. */
  readonly configForm = this.fb.group({
    countryId: ['', Validators.required],
    departmentId: ['', Validators.required],
    municipalityId: ['', Validators.required],
    baseCurrencyId: ['', Validators.required],
    budgetLimit: this.fb.control<number | null>(null, [Validators.min(0.01), maxTwoDecimals]),
    aiuAdminPercent: this.fb.control<number | null>(null, [
      Validators.min(0),
      Validators.max(100),
      maxTwoDecimals
    ]),
    aiuUnforeseenPercent: this.fb.control<number | null>(null, [
      Validators.min(0),
      Validators.max(100),
      maxTwoDecimals
    ]),
    aiuProfitPercent: this.fb.control<number | null>(null, [
      Validators.min(0),
      Validators.max(100),
      maxTwoDecimals
    ]),
    signingMode: ['UNICA_POR_PRESUPUESTADOR_ASIGNADO' as SigningMode, Validators.required],
    signerUserId: [''],
    requiresAuditor: [false]
  });

  /** `D-112`: el Director es el único que configura los parámetros del proyecto. */
  readonly canConfigureProject = computed(() => this.roleCode() === 'DIRECTOR');

  /** Auditor en solo lectura; requiere el check de auditoría activo (`D-112`). */
  readonly isAuditor = computed(() => this.roleCode() === 'AUDITOR');
  readonly countries = signal<Country[]>([]);
  readonly departments = signal<Department[]>([]);
  readonly municipalities = signal<Municipality[]>([]);
  readonly currencies = signal<Currency[]>([]);
  readonly signers = signal<UserListItem[]>([]);

  /** Modo de firma vigente en el formulario; decide si se muestra el firmante. */
  readonly currentSigningMode = signal<SigningMode>('UNICA_POR_PRESUPUESTADOR_ASIGNADO');
  /** Usuario designado como firmante único del proyecto. */
  readonly selectedSigner = signal<UserListItem | null>(null);
  readonly signatureHelpVisible = signal(false);
  readonly signerDialogVisible = signal(false);

  readonly signingModes: { label: string; value: SigningMode }[] = [
    { label: 'Sin firma', value: 'SIN_FIRMA' },
    { label: 'Única por presupuestador asignado', value: 'UNICA_POR_PRESUPUESTADOR_ASIGNADO' },
    { label: 'Independiente por profesional responsable', value: 'INDEPENDIENTE_POR_PROFESIONAL_RESPONSABLE' }
  ];

  constructor() {
    this.catalogService.countries().subscribe({ next: (res) => this.countries.set(res.items) });
    this.catalogService.currencies().subscribe({ next: (res) => this.currencies.set(res.items) });

    this.configForm.controls.signingMode.valueChanges.subscribe((mode) => this.onSigningModeChange(mode));

    this.load();
  }

  statusSeverity(status: ProjectStatus) {
    return STATUS_SEVERITY[status];
  }

  private load() {
    this.loading.set(true);
    this.loadMembers();
    this.projectService.getById(this.projectId).subscribe({
      next: (project) => {
        this.project.set(project);
        this.editForm.setValue({
          name: project.name,
          description: project.description ?? '',
          objective: project.objective ?? ''
        });
        this.syncConfigForm(project);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'No se pudo cargar el proyecto',
          detail: resolveApiError(error).message
        });
      }
    });
  }

  saveEdit() {
    if (this.editForm.invalid) {
      this.editForm.markAllAsTouched();
      return;
    }

    this.persistProject('Proyecto actualizado');
  }

  saveConfiguration() {
    if (this.configForm.invalid) {
      this.configForm.markAllAsTouched();
      this.messageService.add({
        severity: 'warn',
        summary: 'Revisa la configuración',
        detail: 'Los porcentajes deben estar entre 0 y 100 y admiten máximo 2 decimales.'
      });
      return;
    }

    this.persistProject('Configuración actualizada');
  }

  onConfigCountryChange(countryId: string) {
    this.configForm.patchValue({ departmentId: '', municipalityId: '' });
    this.municipalities.set([]);
    this.loadDepartments(countryId);
  }

  onConfigDepartmentChange(departmentId: string) {
    this.configForm.patchValue({ municipalityId: '' });
    this.loadMunicipalities(departmentId);
  }

  onSigningModeChange(mode: SigningMode | null) {
    const control = this.configForm.controls.signerUserId;
    const resolved: SigningMode = mode ?? 'SIN_FIRMA';

    this.currentSigningMode.set(resolved);

    // El firmante único solo existe en el modo «Única por presupuestador asignado».
    if (resolved !== 'UNICA_POR_PRESUPUESTADOR_ASIGNADO') {
      control.setValue('', { emitEvent: false });
      control.disable({ emitEvent: false });
      this.selectedSigner.set(null);
      return;
    }

    if (this.canConfigureProject()) {
      control.enable({ emitEvent: false });
    } else {
      control.disable({ emitEvent: false });
    }
  }

  openSignatureHelp() {
    this.signatureHelpVisible.set(true);
  }

  openSignerDialog() {
    if (!this.canConfigureProject()) {
      return;
    }

    this.signerDialogVisible.set(true);
  }

  onSignerSelected(user: UserListItem) {
    this.configForm.controls.signerUserId.setValue(user.userId);
    this.selectedSigner.set(user);
  }

  clearSigner() {
    if (!this.canConfigureProject()) {
      return;
    }

    this.configForm.controls.signerUserId.setValue('');
    this.selectedSigner.set(null);
  }

  signerLabel(): string {
    const signer = this.selectedSigner();
    if (signer) {
      return `${signer.firstName} ${signer.lastName} (${signer.username})`;
    }

    return this.configForm.controls.signerUserId.value ? 'Firmante asignado' : 'Sin firmante';
  }

  private persistProject(successSummary: string) {
    const payload = this.buildUpdatePayload();
    if (!payload) {
      return;
    }

    this.saving.set(true);
    this.projectService.update(this.projectId, payload).subscribe({
      next: () => {
        this.saving.set(false);
        this.messageService.add({ severity: 'success', summary: successSummary });
        this.load();
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'No se pudo actualizar el proyecto',
          detail: resolveApiError(error).message
        });
      }
    });
  }

  private buildUpdatePayload(): ProjectFormValue | null {
    const project = this.project();
    if (!project) {
      return null;
    }

    const general = this.editForm.getRawValue();
    const config = this.configForm.getRawValue();
    const canConfigure = this.canConfigureProject();

    // `D-112`: el Presupuestador solo edita datos generales; el resto de parámetros se conservan.
    const signingMode = canConfigure ? (config.signingMode ?? 'SIN_FIRMA') : project.signatureMode;
    const singleSignerMode = 'UNICA_POR_PRESUPUESTADOR_ASIGNADO';
    const signerUserId = canConfigure
      ? signingMode === singleSignerMode
        ? config.signerUserId || undefined
        : undefined
      : project.signatureMode === singleSignerMode
        ? (project.signerUserId ?? undefined)
        : undefined;

    return {
      name: general.name,
      code: project.code ?? undefined,
      description: general.description,
      objective: general.objective,
      countryId: canConfigure ? (config.countryId ?? '') : project.countryId,
      departmentId: canConfigure ? (config.departmentId ?? '') : project.departmentId,
      municipalityId: canConfigure ? (config.municipalityId ?? '') : project.municipalityId,
      baseCurrencyId: canConfigure ? (config.baseCurrencyId ?? '') : project.baseCurrencyId,
      budgetLimit: canConfigure
        ? (config.budgetLimit ?? undefined)
        : (project.budgetLimit ?? undefined),
      aiuAdminPercent: canConfigure
        ? (config.aiuAdminPercent ?? undefined)
        : (project.aiuAdminPercent ?? undefined),
      aiuUnforeseenPercent: canConfigure
        ? (config.aiuUnforeseenPercent ?? undefined)
        : (project.aiuUnforeseenPercent ?? undefined),
      aiuProfitPercent: canConfigure
        ? (config.aiuProfitPercent ?? undefined)
        : (project.aiuProfitPercent ?? undefined),
      signingMode,
      signerUserId,
      requiresAuditor: canConfigure ? (config.requiresAuditor ?? false) : project.requiresAuditor
    };
  }

  private syncConfigForm(project: Project) {
    this.configForm.setValue({
      countryId: project.countryId,
      departmentId: project.departmentId,
      municipalityId: project.municipalityId,
      baseCurrencyId: project.baseCurrencyId,
      budgetLimit: project.budgetLimit,
      aiuAdminPercent: project.aiuAdminPercent,
      aiuUnforeseenPercent: project.aiuUnforeseenPercent,
      aiuProfitPercent: project.aiuProfitPercent,
      signingMode: project.signatureMode,
      signerUserId: project.signerUserId ?? '',
      requiresAuditor: project.requiresAuditor
    });

    this.loadDepartments(project.countryId);
    this.loadMunicipalities(project.departmentId);
    this.loadSigners(project.companyId);
    this.onSigningModeChange(project.signatureMode);
  }

  private loadDepartments(countryId: string) {
    if (!countryId) {
      this.departments.set([]);
      return;
    }

    this.catalogService.departments(countryId).subscribe({
      next: (response) => this.departments.set(response.items),
      error: () => this.departments.set([])
    });
  }

  private loadMunicipalities(departmentId: string) {
    if (!departmentId) {
      this.municipalities.set([]);
      return;
    }

    this.catalogService.municipalities(departmentId).subscribe({
      next: (response) => this.municipalities.set(response.items),
      error: () => this.municipalities.set([])
    });
  }

  private loadSigners(companyId: string) {
    if (!this.canSearchUsers) {
      this.signers.set([]);
      return;
    }

    this.userService.listAll({ roleCode: 'PRESUPUESTADOR', companyId }).subscribe({
      next: (users) => {
        this.signers.set(users);
        const signerId = this.configForm.controls.signerUserId.value;
        this.selectedSigner.set(users.find((user) => user.userId === signerId) ?? null);
      },
      error: () => this.signers.set([])
    });
  }

  advanceStatus() {
    const action = this.nextStatusAction();
    if (!action) {
      return;
    }

    this.projectService.changeStatus(this.projectId, { targetStatus: action.target }).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Estado actualizado' });
        this.load();
      },
      error: (error: unknown) =>
        this.messageService.add({
          severity: 'error',
          summary: 'No se pudo cambiar el estado',
          detail: resolveApiError(error).message
        })
    });
  }

  disableProject() {
    this.confirmationService.confirm({
      header: 'Deshabilitar proyecto',
      message: '¿Confirmas deshabilitar este proyecto?',
      accept: () => {
        this.projectService
          .changeStatus(this.projectId, {
            targetStatus: 'INHABILITADO',
            reason: 'Deshabilitado desde administración'
          })
          .subscribe({
            next: () => {
              this.messageService.add({ severity: 'success', summary: 'Proyecto deshabilitado' });
              this.load();
            },
            error: (error: unknown) =>
              this.messageService.add({
                severity: 'error',
                summary: 'No se pudo deshabilitar',
                detail: resolveApiError(error).message
              })
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
      error: (error: unknown) =>
        this.messageService.add({
          severity: 'error',
          summary: 'No se pudo reactivar',
          detail: resolveApiError(error).message
        })
    });
  }

  toggleVisibility(isPublic: boolean) {
    this.projectService.changeVisibility(this.projectId, isPublic).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Visibilidad actualizada' });
        this.load();
      },
      error: (error: unknown) =>
        this.messageService.add({
          severity: 'error',
          summary: 'No se pudo cambiar la visibilidad',
          detail: resolveApiError(error).message
        })
    });
  }

  loadMembers() {
    this.membersLoading.set(true);
    this.projectService.listMembers(this.projectId).subscribe({
      next: (response) => {
        this.members.set(response.items);
        this.membersLoading.set(false);
      },
      error: () => {
        this.members.set([]);
        this.membersLoading.set(false);
      }
    });
  }

  openMemberDialog(roleCode: RoleCode) {
    this.memberDialogRole.set(roleCode);
    this.memberDialogVisible.set(true);
  }

  removeMembership(member: ProjectMemberItem) {
    this.confirmationService.confirm({
      header: 'Retirar miembro',
      message: `¿Confirmas retirar a "${member.firstName} ${member.lastName}" del proyecto?`,
      accept: () => {
        this.projectService.deactivateMembership(this.projectId, member.membershipId).subscribe({
          next: () => {
            this.messageService.add({ severity: 'success', summary: 'Miembro retirado del proyecto' });
            this.loadMembers();
          },
          error: (error: unknown) =>
            this.messageService.add({
              severity: 'error',
              summary: 'No se pudo retirar al miembro',
              detail: resolveApiError(error).message
            })
        });
      }
    });
  }

  /** Réplica de `ProjectStatusTransitionPolicy.CanRolePerformTransition` para ocultar acciones inválidas. */
  private canPerformTransition(current: ProjectStatus, target: ProjectStatus): boolean {
    const roleCode = this.roleCode();

    if (roleCode === 'DIRECTOR') {
      return true;
    }

    if (roleCode === 'PRESUPUESTADOR') {
      if (current === 'FINALIZADO') {
        return false;
      }

      return target === 'FALTA_INFORMACION' || target === 'COTIZADO';
    }

    return false;
  }
}
