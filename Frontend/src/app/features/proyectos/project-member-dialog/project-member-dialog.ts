import { Component, computed, effect, inject, input, model, output, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { MessageService } from 'primeng/api';

import { RoleCode } from '../../../core/models/company.model';
import { DocumentType } from '../../../core/models/catalog.model';
import { UserListItem, UserSearchFilter } from '../../../core/models/user.model';
import { resolveApiError } from '../../../core/http/api-error';
import { CatalogService } from '../../../core/services/catalog.service';
import { ProjectService } from '../../../core/services/project.service';
import { UserService } from '../../../core/services/user.service';

/**
 * Buscador y alta de miembros del proyecto para un rol concreto (presupuestador o auditor).
 * Solo ofrece usuarios de la compañía del proyecto que aún no estén vinculados.
 */
@Component({
  selector: 'app-project-member-dialog',
  imports: [DialogModule, ButtonModule, InputTextModule, SelectModule, FormsModule],
  templateUrl: './project-member-dialog.html',
  styleUrl: './project-member-dialog.scss'
})
export class ProjectMemberDialog {
  private readonly userService = inject(UserService);
  private readonly projectService = inject(ProjectService);
  private readonly catalogService = inject(CatalogService);
  private readonly messageService = inject(MessageService);

  readonly visible = model(false);
  readonly projectId = input<string | null>(null);
  readonly companyId = input<string | null>(null);
  readonly roleCode = input<RoleCode>('PRESUPUESTADOR');
  readonly assignedUserIds = input<string[]>([]);
  readonly title = input('Asignar miembro');
  readonly hint = input('Busca usuarios de la compañía del proyecto que aún no estén vinculados.');
  /** `assign` vincula el usuario al proyecto; `select` solo lo devuelve al componente padre. */
  readonly mode = input<'assign' | 'select'>('assign');
  readonly assigned = output<void>();
  readonly selected = output<UserListItem>();

  readonly candidates = signal<UserListItem[]>([]);
  readonly documentTypes = signal<DocumentType[]>([]);
  readonly searching = signal(false);
  readonly searchAttempted = signal(false);
  readonly assigningUserId = signal<string | null>(null);
  readonly filter = signal<UserSearchFilter>('all');
  readonly searchTerm = signal('');
  readonly selectedDocumentType = signal('');

  /** Altas hechas en esta sesión del modal: se excluyen aunque el padre aún no haya recargado. */
  private readonly assignedLocally = signal<string[]>([]);

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

  constructor() {
    effect(() => {
      const visible = this.visible();
      this.roleCode();
      this.companyId();

      if (!visible) {
        return;
      }

      untracked(() => {
        this.filter.set('all');
        this.searchTerm.set('');
        this.selectedDocumentType.set('');
        this.candidates.set([]);
        this.searchAttempted.set(false);
        this.assignedLocally.set([]);
        this.loadDocumentTypes();
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

  search(term: string) {
    this.searchTerm.set(term);
    this.searching.set(true);
    this.searchAttempted.set(true);

    this.userService
      .listAll({
        roleCode: this.roleCode(),
        companyId: this.companyId() ?? undefined,
        ...this.buildFilters(term)
      })
      .subscribe({
        next: (users) => {
          const alreadyMember = new Set([...this.assignedUserIds(), ...this.assignedLocally()]);
          this.candidates.set(users.filter((user) => !alreadyMember.has(user.userId)));
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

  assign(user: UserListItem) {
    if (this.mode() === 'select') {
      this.selected.emit(user);
      this.visible.set(false);
      return;
    }

    const projectId = this.projectId();
    if (!projectId) {
      return;
    }

    this.assigningUserId.set(user.userId);
    this.projectService.assignMembership(projectId, user.userId, this.roleCode()).subscribe({
      next: () => {
        this.assigningUserId.set(null);
        this.assignedLocally.update((ids) => [...ids, user.userId]);
        this.messageService.add({ severity: 'success', summary: 'Miembro vinculado al proyecto' });
        this.assigned.emit();
        this.search(this.searchTerm());
      },
      error: (error: unknown) => {
        this.assigningUserId.set(null);
        this.messageService.add({
          severity: 'error',
          summary: 'No se pudo vincular el miembro',
          detail: resolveApiError(error).message
        });
      }
    });
  }

  /** Traduce el filtro elegido al query param correspondiente de `GET /api/v1/users`. */
  private buildFilters(term: string) {
    const value = term.trim();

    if (this.filter() === 'document') {
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
}
