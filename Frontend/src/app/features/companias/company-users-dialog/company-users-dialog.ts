import { Component, computed, effect, inject, input, model, output, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ConfirmationService, MessageService } from 'primeng/api';

import { RoleCode } from '../../../core/models/company.model';
import { DocumentType } from '../../../core/models/catalog.model';
import { UserListItem, UserSearchFilter } from '../../../core/models/user.model';
import { resolveApiError } from '../../../core/http/api-error';
import { CatalogService } from '../../../core/services/catalog.service';
import { CompanyService } from '../../../core/services/company.service';
import { UserService } from '../../../core/services/user.service';

export type CompanyUsersDialogMode = 'director' | 'admin';

const ROLE_LABELS: Record<string, string> = {
  DIRECTOR: 'Director de proyectos',
  PRESUPUESTADOR: 'Presupuestador',
  AUDITOR: 'Auditor',
  ADMINISTRADOR: 'Administrador'
};

const ROLE_PLURALS: Record<string, string> = {
  DIRECTOR: 'directores',
  PRESUPUESTADOR: 'presupuestadores',
  AUDITOR: 'auditores'
};

/**
 * Modal reutilizable de gestión de usuarios de una compañía. Lo usan el Director de proyectos
 * (presupuestadores y auditores de su compañía) y el Administrador (solo directores).
 */
@Component({
  selector: 'app-company-users-dialog',
  imports: [DialogModule, TableModule, ButtonModule, InputTextModule, SelectModule, TagModule, FormsModule, DatePipe],
  templateUrl: './company-users-dialog.html',
  styleUrl: './company-users-dialog.scss'
})
export class CompanyUsersDialog {
  private readonly userService = inject(UserService);
  private readonly companyService = inject(CompanyService);
  private readonly catalogService = inject(CatalogService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  readonly visible = model(false);
  readonly companyId = input<string | null>(null);
  readonly companyName = input('');
  readonly roleCode = input<RoleCode>('PRESUPUESTADOR');
  readonly mode = input<CompanyUsersDialogMode>('director');
  readonly changed = output<void>();

  readonly members = signal<UserListItem[]>([]);
  readonly candidates = signal<UserListItem[]>([]);
  readonly loading = signal(false);
  readonly searching = signal(false);
  readonly searchAttempted = signal(false);
  readonly searchTerm = signal('');
  readonly assigningUserId = signal<string | null>(null);
  readonly filter = signal<UserSearchFilter>('all');
  readonly documentTypes = signal<DocumentType[]>([]);
  readonly selectedDocumentType = signal('');

  readonly filterOptions: { label: string; value: UserSearchFilter }[] = [
    { label: 'Todos los campos', value: 'all' },
    { label: 'Correo electrónico', value: 'email' },
    { label: 'Nombre de usuario', value: 'username' },
    { label: 'Nombre y apellidos', value: 'fullName' },
    { label: 'Tipo y número de documento', value: 'document' }
  ];

  readonly searchPlaceholder = computed(() => {
    switch (this.filter()) {
      case 'email':
        return 'correo@dominio.com';
      case 'username':
        return 'Nombre de usuario';
      case 'fullName':
        return 'Nombres y apellidos';
      default:
        return 'Correo, usuario, nombre o documento';
    }
  });

  /** El Administrador ve todos los roles de la compañía; el Director solo el rol gestionado. */
  readonly showRoleColumn = computed(() => this.mode() === 'admin');

  readonly title = computed(() => {
    if (this.mode() === 'admin') {
      return `Usuarios de ${this.companyName() || 'la compañía'}`;
    }

    return `Usuarios ${ROLE_PLURALS[this.roleCode()] ?? ''} de mi compañía`;
  });

  readonly searchHint = computed(() => {
    if (this.mode() === 'admin') {
      return 'Busca usuarios con rol Director de proyectos que no pertenezcan a ninguna compañía.';
    }

    return `Busca usuarios con rol ${ROLE_LABELS[this.roleCode()] ?? this.roleCode()} que no pertenezcan a ninguna compañía.`;
  });

  constructor() {
    effect(() => {
      const visible = this.visible();
      this.companyId();
      this.roleCode();
      this.mode();

      if (!visible) {
        return;
      }

      // Se reinicia la búsqueda cada vez que se abre el modal sin re-disparar el efecto.
      untracked(() => {
        this.filter.set('all');
        this.searchTerm.set('');
        this.selectedDocumentType.set('');
        this.candidates.set([]);
        this.searchAttempted.set(false);
        this.loadDocumentTypes();
        this.loadMembers();
        this.search('');
      });
    });
  }

  private loadDocumentTypes() {
    if (this.documentTypes().length > 0) {
      return;
    }

    this.catalogService.documentTypes().subscribe({
      next: (response) => this.documentTypes.set(response.items),
      error: () => this.documentTypes.set([])
    });
  }

  onFilterChange(filter: UserSearchFilter) {
    this.filter.set(filter);
    this.searchTerm.set('');
    this.selectedDocumentType.set('');
    this.search('');
  }

  roleLabel(roleCode: string): string {
    return ROLE_LABELS[roleCode] ?? roleCode;
  }

  canRemove(user: UserListItem): boolean {
    return this.mode() === 'admin'
      ? user.roleCode === 'DIRECTOR'
      : user.roleCode === this.roleCode();
  }

  loadMembers() {
    const companyId = this.companyId();
    if (!companyId) {
      this.members.set([]);
      return;
    }

    this.loading.set(true);
    const request = this.mode() === 'admin'
      ? this.userService.listByCompany(companyId, { pageSize: 100 })
      : this.userService.listByCompany(companyId, { pageSize: 100, roleCode: this.roleCode() });

    request.subscribe({
      next: (response) => {
        this.members.set(response.items);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'No se pudieron cargar los usuarios de la compañía',
          detail: resolveApiError(error).message
        });
      }
    });
  }

  search(term: string) {
    this.searchTerm.set(term);
    this.searching.set(true);
    this.searchAttempted.set(true);

    this.userService.listAssignable(this.roleCode(), this.buildFilters(term)).subscribe({
      next: (users) => {
        this.candidates.set(users);
        this.searching.set(false);
      },
      error: (error: unknown) => {
        this.searching.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'No se pudo buscar usuarios',
          detail: resolveApiError(error).message
        });
      }
    });
  }

  /** Traduce el filtro elegido al query param correspondiente de `GET /api/v1/users`. */
  private buildFilters(term: string) {
    const value = term.trim();

    if (this.filter() === 'document') {
      // Tipo y número se combinan con AND en el endpoint; se admite solo uno de los dos.
      const documentType = this.selectedDocumentType();
      if (!documentType && !value) {
        return {};
      }

      return {
        ...(documentType ? { documentType } : {}),
        ...(value ? { documentNumber: value } : {})
      };
    }

    if (!value) {
      return {};
    }

    switch (this.filter()) {
      case 'email':
        return { email: value };
      case 'username':
        return { username: value };
      case 'fullName':
        return { fullName: value };
      default:
        return { search: value };
    }
  }

  assign(user: UserListItem) {
    const companyId = this.companyId();
    if (!companyId) {
      return;
    }

    this.assigningUserId.set(user.userId);
    this.companyService.assignMembership(companyId, user.userId, user.roleCode as RoleCode).subscribe({
      next: () => {
        this.assigningUserId.set(null);
        this.messageService.add({ severity: 'success', summary: 'Usuario vinculado a la compañía' });
        this.loadMembers();
        this.search(this.searchTerm());
        this.changed.emit();
      },
      error: (error: unknown) => {
        this.assigningUserId.set(null);
        this.messageService.add({
          severity: 'error',
          summary: 'No se pudo vincular el usuario',
          detail: resolveApiError(error).message
        });
      }
    });
  }

  remove(user: UserListItem) {
    const companyId = this.companyId();
    if (!companyId) {
      return;
    }

    this.confirmationService.confirm({
      header: 'Quitar de la compañía',
      message: `¿Confirmas quitar a "${user.firstName} ${user.lastName}" de la compañía?`,
      accept: () => {
        this.companyService.deactivateMembership(companyId, user.userId).subscribe({
          next: () => {
            this.messageService.add({ severity: 'success', summary: 'Usuario desvinculado de la compañía' });
            this.loadMembers();
            this.search(this.searchTerm());
            this.changed.emit();
          },
          error: (error: unknown) =>
            this.messageService.add({
              severity: 'error',
              summary: 'No se pudo desvincular el usuario',
              detail: resolveApiError(error).message
            })
        });
      }
    });
  }
}
