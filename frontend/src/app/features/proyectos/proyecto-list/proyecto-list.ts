import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';

import { ProjectService } from '../../../core/services/project.service';
import { ProjectListItem, ProjectStatus } from '../../../core/models/project.model';

const STATUS_SEVERITY: Record<ProjectStatus, 'success' | 'warn' | 'danger' | 'info' | 'secondary'> = {
  POR_COTIZAR: 'info',
  FALTA_INFORMACION: 'warn',
  COTIZADO: 'success',
  FINALIZADO: 'secondary',
  INHABILITADO: 'danger'
};

@Component({
  selector: 'app-proyecto-list',
  imports: [TableModule, TagModule, ButtonModule, InputTextModule, RouterLink],
  templateUrl: './proyecto-list.html'
})
export class ProyectoList {
  private readonly projectService = inject(ProjectService);

  readonly projects = signal<ProjectListItem[]>([]);
  readonly totalRecords = signal(0);
  readonly loading = signal(false);
  readonly search = signal('');

  statusSeverity(status: ProjectStatus) {
    return STATUS_SEVERITY[status];
  }

  load(event: TableLazyLoadEvent) {
    this.loading.set(true);
    const pageSize = (event.rows ?? 10) as 10 | 50 | 100;
    const pageNumber = Math.floor((event.first ?? 0) / pageSize) + 1;

    this.projectService.list({ pageNumber, pageSize, search: this.search() }).subscribe({
      next: (response) => {
        this.projects.set(response.items);
        this.totalRecords.set(response.total_items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
