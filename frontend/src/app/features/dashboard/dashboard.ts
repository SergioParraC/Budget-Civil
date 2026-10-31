import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { ConfirmationService, MessageService } from 'primeng/api';

import { AuthService } from '../../core/auth/auth.service';
import { CompanyService } from '../../core/services/company.service';
import { MyCompany, RoleCode } from '../../core/models/company.model';
import { resolveApiError } from '../../core/http/api-error';
import { CompanyFormDialog } from '../companias/company-form-dialog/company-form-dialog';
import { CompanyUsersDialog } from '../companias/company-users-dialog/company-users-dialog';

const ROLE_LABELS: Record<string, string> = {
  ADMINISTRADOR: 'Administrador',
  DIRECTOR: 'Director de proyectos',
  PRESUPUESTADOR: 'Presupuestador',
  AUDITOR: 'Auditor'
};

@Component({
  selector: 'app-dashboard',
  imports: [CardModule, ButtonModule, MessageModule, DatePipe, CompanyFormDialog, CompanyUsersDialog],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard {
  private readonly authService = inject(AuthService);
  private readonly companyService = inject(CompanyService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  readonly currentUser = this.authService.currentUser;
  readonly roleCode = this.authService.roleCode;

  readonly company = signal<MyCompany | null>(null);
  readonly loading = signal(false);
  readonly companyFormVisible = signal(false);
  readonly membersVisible = signal(false);
  readonly membersRole = signal<RoleCode>('PRESUPUESTADOR');

  readonly isDirector = computed(() => this.roleCode() === 'DIRECTOR');
  readonly isEstimator = computed(() => this.roleCode() === 'PRESUPUESTADOR');
  readonly isAuditor = computed(() => this.roleCode() === 'AUDITOR');
  readonly canLeaveCompany = computed(() => this.isEstimator() || this.isAuditor());

  constructor() {
    this.reloadCompany();
  }

  roleLabel(): string {
    const roleCode = this.roleCode();
    return roleCode ? (ROLE_LABELS[roleCode] ?? roleCode) : '—';
  }

  companyTypeLabel(companyType: string | null): string {
    switch (companyType) {
      case 'PUBLICA':
        return 'Pública';
      case 'PRIVADA':
        return 'Privada';
      default:
        return '—';
    }
  }

  location(company: MyCompany): string {
    const parts = [company.municipalityName, company.departmentName, company.countryName].filter(
      (value): value is string => !!value
    );

    return parts.length > 0 ? parts.join(', ') : '—';
  }

  reloadCompany() {
    this.loading.set(true);
    this.companyService.getMyCompany().subscribe({
      next: (company) => {
        this.company.set(company);
        this.loading.set(false);
      },
      error: () => {
        this.company.set(null);
        this.loading.set(false);
      }
    });
  }

  openCompanyForm() {
    this.companyFormVisible.set(true);
  }

  onCompanySaved() {
    const hadCompany = this.company() !== null;
    this.reloadCompany();

    if (!hadCompany) {
      // El token vigente no incluye el claim `company_id`: hay que volver a iniciar sesión.
      this.messageService.add({
        severity: 'success',
        summary: 'Compañía creada',
        detail: 'Vuelve a iniciar sesión para operar con tu compañía.',
        life: 6000
      });

      setTimeout(() => this.authService.logout(), 1800);
    }
  }

  openMembers(roleCode: RoleCode) {
    this.membersRole.set(roleCode);
    this.membersVisible.set(true);
  }

  confirmLeaveCompany() {
    const message = this.isAuditor()
      ? 'ALERTA: El usuario perderá acceso a los proyectos y compañías a auditar, por favor confirme su salida'
      : 'ALERTA: Si usted sale de la compañía actual, dejará de participar en los proyectos que tiene vinculados actualmente';

    this.confirmationService.confirm({
      header: 'Salir de la compañía',
      message,
      acceptLabel: 'Sí, salir',
      rejectLabel: 'Cancelar',
      accept: () => this.leaveCompany()
    });
  }

  private leaveCompany() {
    this.companyService.leaveMyCompany().subscribe({
      next: (response) => {
        this.messageService.add({
          severity: 'success',
          summary: 'Salida registrada',
          detail: `Se retiraron ${response.projectsDetached} participación(es) en proyectos. Vuelve a iniciar sesión.`,
          life: 6000
        });

        // El token vigente conserva el claim `company_id`: hay que autenticarse de nuevo.
        setTimeout(() => this.authService.logout(), 1500);
      },
      error: (error: unknown) =>
        this.messageService.add({
          severity: 'error',
          summary: 'No se pudo salir de la compañía',
          detail: resolveApiError(error).message
        })
    });
  }
}
