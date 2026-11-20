import { Component, effect, inject, input, model, output, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';

import { ApiError, resolveApiError } from '../../../core/http/api-error';
import { Resource } from '../../../core/models/resource.model';
import { ProjectService } from '../../../core/services/project.service';
import { ResourceService } from '../../../core/services/resource.service';

interface SourceProject {
  projectId: string;
  name: string;
}

/**
 * Copia de un insumo desde otro proyecto de la compañía (`REQ-INSUMOS-30`). Usa `GET /projects` para
 * elegir el origen y `POST /resources/copy` para copiar; no existe búsqueda global de insumos.
 */
@Component({
  selector: 'app-resource-copy-dialog',
  imports: [FormsModule, DialogModule, ButtonModule, InputTextModule, SelectModule, TableModule, MessageModule],
  templateUrl: './resource-copy-dialog.html',
  styleUrl: './resource-copy-dialog.scss'
})
export class ResourceCopyDialog {
  private readonly resourceService = inject(ResourceService);
  private readonly projectService = inject(ProjectService);

  readonly visible = model(false);
  readonly projectId = input.required<string>();
  readonly copyRequested = output<Resource>();
  readonly failed = output<ApiError>();

  readonly projects = signal<SourceProject[]>([]);
  readonly sourceProjectId = signal('');
  readonly term = signal('');
  readonly items = signal<Resource[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly selected = signal<Resource | null>(null);

  constructor() {
    effect(() => {
      if (!this.visible()) {
        return;
      }

      untracked(() => {
        this.sourceProjectId.set('');
        this.term.set('');
        this.items.set([]);
        this.selected.set(null);
        this.errorMessage.set(null);
        this.loadProjects();
      });
    });
  }

  private loadProjects(): void {
    this.projectService.list({ pageNumber: 1, pageSize: 100 }).subscribe({
      next: (response) =>
        this.projects.set(
          response.items
            .filter((project) => project.projectId !== this.projectId())
            .map((project) => ({ projectId: project.projectId, name: project.name }))
        ),
      error: () => this.projects.set([])
    });
  }

  onProjectChange(projectId: string): void {
    this.selected.set(null);
    this.items.set([]);
    this.term.set('');

    if (projectId) {
      this.search();
    }
  }

  search(): void {
    const sourceProjectId = this.sourceProjectId();
    if (!sourceProjectId) {
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);

    this.resourceService
      .list(sourceProjectId, { search: this.term(), page: 1, pageSize: 100 })
      .subscribe({
        next: (response) => {
          this.items.set(response.items);
          this.loading.set(false);
        },
        error: () => {
          this.items.set([]);
          this.loading.set(false);
          this.errorMessage.set('El insumo de origen no está disponible.');
        }
      });
  }

  select(resource: Resource): void {
    this.selected.set(resource);
  }

  copy(): void {
    const selected = this.selected();
    if (!selected) {
      return;
    }

    this.saving.set(true);
    this.errorMessage.set(null);

    this.resourceService.copy(this.projectId(), { sourceResourceId: selected.resourceId }).subscribe({
      next: (stored) => {
        this.saving.set(false);
        this.visible.set(false);
        this.copyRequested.emit(stored);
      },
      error: (error: unknown) => {
        this.saving.set(false);
        const apiError = resolveApiError(error);
        this.errorMessage.set(apiError.message);
        this.failed.emit(apiError);
      }
    });
  }
}
