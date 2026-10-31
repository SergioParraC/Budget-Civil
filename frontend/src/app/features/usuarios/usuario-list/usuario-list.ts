import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { SelectModule } from 'primeng/select';
import { InputTextModule } from 'primeng/inputtext';
import { ConfirmationService, MessageService } from 'primeng/api';

import { UserService } from '../../../core/services/user.service';
import { CompanyService } from '../../../core/services/company.service';
import { UserListItem } from '../../../core/models/user.model';
import { Company, RoleCode } from '../../../core/models/company.model';
import { AuthService } from '../../../core/auth/auth.service';
import { Permission } from '../../../core/auth/permissions';
import { resolveApiError } from '../../../core/http/api-error';

const ROLE_OPTIONS: { label: string; value: RoleCode }[] = [
  { label: 'Administrador', value: 'ADMINISTRADOR' },
  { label: 'Director de proyectos', value: 'DIRECTOR' },
  { label: 'Presupuestador', value: 'PRESUPUESTADOR' },
  { label: 'Auditor', value: 'AUDITOR' }
];

@Component({
  selector: 'app-usuario-list',
  imports: [TableModule, TagModule, ButtonModule, DialogModule, SelectModule, InputTextModule, ReactiveFormsModule],
  templateUrl: './usuario-list.html',
  styleUrl: './usuario-list.scss'
})
export class UsuarioList {
  private readonly userService = inject(UserService);
  private readonly companyService = inject(CompanyService);
  private readonly authService = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  readonly users = signal<UserListItem[]>([]);
  readonly loading = signal(false);

  readonly roleOptions = ROLE_OPTIONS;
  readonly companies = signal<Company[]>([]);

  readonly canChangeRole = this.authService.can(Permission.usersRoleChange);
  readonly canChangeCompany = this.authService.can(Permission.usersCompanyChange);
  readonly canDeactivate = this.authService.can(Permission.usersDeactivate);
  /** Se reutiliza el permiso de inhabilitación: no existe un permiso separado para reactivar. */
  readonly canReactivate = this.authService.can(Permission.usersDeactivate);
  /** `COMPANIES_READ` solo lo tiene ADMINISTRADOR; sin él hay que digitar el GUID de la compañía. */
  readonly canListCompanies = this.authService.can(Permission.companiesRead);

  readonly roleDialogVisible = signal(false);
  readonly companyDialogVisible = signal(false);
  readonly selectedUser = signal<UserListItem | null>(null);

  readonly roleForm = this.fb.nonNullable.group({
    roleCode: ['PRESUPUESTADOR' as RoleCode, Validators.required]
  });

  readonly companyForm = this.fb.nonNullable.group({
    companyId: ['', Validators.required]
  });

  constructor() {
    if (this.canListCompanies) {
      this.companyService.list({ pageSize: 100, isActive: true }).subscribe((res) => this.companies.set(res.items));
    }
    this.load();
  }

  load() {
    this.loading.set(true);
    this.userService.listAll().subscribe({
      next: (users) => {
        this.users.set(users);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  openRoleDialog(user: UserListItem) {
    this.selectedUser.set(user);
    this.roleForm.setValue({ roleCode: user.roleCode as RoleCode });
    this.roleDialogVisible.set(true);
  }

  submitRole() {
    const user = this.selectedUser();
    if (!user || this.roleForm.invalid) {
      return;
    }
    this.userService.changeRole(user.userId, this.roleForm.getRawValue().roleCode).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Rol actualizado' });
        this.roleDialogVisible.set(false);
        this.load();
      },
      error: (error: unknown) =>
        this.messageService.add({
          severity: 'error',
          summary: 'No se pudo cambiar el rol',
          detail: resolveApiError(error).message
        })
    });
  }

  openCompanyDialog(user: UserListItem) {
    this.selectedUser.set(user);
    this.companyForm.reset({ companyId: user.companyId ?? '' });
    this.companyDialogVisible.set(true);
  }

  submitCompany() {
    const user = this.selectedUser();
    if (!user || this.companyForm.invalid) {
      return;
    }
    this.userService.changeCompany(user.userId, this.companyForm.getRawValue().companyId).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Compañía actualizada' });
        this.companyDialogVisible.set(false);
        this.load();
      },
      error: (error: unknown) =>
        this.messageService.add({
          severity: 'error',
          summary: 'No se pudo cambiar la compañía',
          detail: resolveApiError(error).message
        })
    });
  }

  deactivate(user: UserListItem) {
    this.confirmationService.confirm({
      header: 'Desactivar usuario',
      message: `¿Confirmas desactivar a "${user.username}"?`,
      accept: () => {
        this.userService.deactivate(user.userId, 'Desactivado desde administración').subscribe({
          next: () => {
            this.messageService.add({ severity: 'success', summary: 'Usuario desactivado' });
            this.load();
          },
          error: (error: unknown) =>
            this.messageService.add({
              severity: 'error',
              summary: 'No se pudo desactivar el usuario',
              detail: resolveApiError(error).message
            })
        });
      }
    });
  }

  enable(user: UserListItem) {
    this.confirmationService.confirm({
      header: 'Habilitar usuario',
      message: `¿Confirmas habilitar de nuevo a "${user.username}"?`,
      accept: () => {
        this.userService.enable(user.userId).subscribe({
          next: () => {
            this.messageService.add({ severity: 'success', summary: 'Usuario habilitado' });
            this.load();
          },
          error: (error: unknown) =>
            this.messageService.add({
              severity: 'error',
              summary: 'No se pudo habilitar el usuario',
              detail: resolveApiError(error).message
            })
        });
      }
    });
  }
}
