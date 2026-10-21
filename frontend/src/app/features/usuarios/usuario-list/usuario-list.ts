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
import { Company } from '../../../core/models/company.model';

const ROLE_OPTIONS = [
  { label: 'Administrador', value: 'ADMINISTRADOR' },
  { label: 'Director de proyectos', value: 'GERENTE_PROYECTOS' },
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
  private readonly fb = inject(FormBuilder);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  readonly users = signal<UserListItem[]>([]);
  readonly loading = signal(false);

  readonly roleOptions = ROLE_OPTIONS;
  readonly companies = signal<Company[]>([]);

  readonly roleDialogVisible = signal(false);
  readonly companyDialogVisible = signal(false);
  readonly selectedUser = signal<UserListItem | null>(null);

  readonly roleForm = this.fb.nonNullable.group({
    rol_asignado: ['PRESUPUESTADOR', Validators.required]
  });

  readonly companyForm = this.fb.nonNullable.group({
    company_id: ['', Validators.required]
  });

  constructor() {
    this.companyService.list({ pageSize: 100, isActive: true }).subscribe((res) => this.companies.set(res.items));
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
    this.roleForm.setValue({ rol_asignado: user.role_code });
    this.roleDialogVisible.set(true);
  }

  submitRole() {
    const user = this.selectedUser();
    if (!user || this.roleForm.invalid) {
      return;
    }
    this.userService.changeRole(user.user_id, this.roleForm.getRawValue().rol_asignado).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Rol actualizado' });
        this.roleDialogVisible.set(false);
        this.load();
      },
      error: () => this.messageService.add({ severity: 'error', summary: 'No se pudo cambiar el rol' })
    });
  }

  openCompanyDialog(user: UserListItem) {
    this.selectedUser.set(user);
    this.companyForm.reset({ company_id: user.company_id ?? '' });
    this.companyDialogVisible.set(true);
  }

  submitCompany() {
    const user = this.selectedUser();
    if (!user || this.companyForm.invalid) {
      return;
    }
    this.userService.changeCompany(user.user_id, this.companyForm.getRawValue().company_id).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Compañía actualizada' });
        this.companyDialogVisible.set(false);
        this.load();
      },
      error: () => this.messageService.add({ severity: 'error', summary: 'No se pudo cambiar la compañía' })
    });
  }

  deactivate(user: UserListItem) {
    this.confirmationService.confirm({
      header: 'Desactivar usuario',
      message: `¿Confirmas desactivar a "${user.username}"?`,
      accept: () => {
        this.userService.deactivate(user.user_id, 'Desactivado desde administración').subscribe({
          next: () => {
            this.messageService.add({ severity: 'success', summary: 'Usuario desactivado' });
            this.load();
          },
          error: () => this.messageService.add({ severity: 'error', summary: 'No se pudo desactivar el usuario' })
        });
      }
    });
  }
}
