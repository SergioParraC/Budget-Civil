import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import { Project } from '../models/project.model';
import { ProjectService } from './project.service';

/**
 * Contexto del proyecto de la ruta (`/proyectos/:id/...`). Valida el proyecto con `GET /projects/{id}`
 * y expone el estado “Proyecto no disponible” para `404 PROJECT_NOT_FOUND` o `403`.
 */
@Injectable({ providedIn: 'root' })
export class ProjectContextService {
  private readonly projectService = inject(ProjectService);
  private readonly router = inject(Router);

  private readonly currentProjectId = signal<string | null>(null);
  readonly project = signal<Project | null>(null);
  readonly loading = signal(false);
  readonly unavailable = signal(false);

  /** Carga el proyecto la primera vez que se abre su contexto. */
  load(projectId: string | null): void {
    if (!projectId) {
      this.project.set(null);
      this.loading.set(false);
      this.unavailable.set(true);
      return;
    }

    if (this.currentProjectId() === projectId && (this.project() || this.unavailable())) {
      return;
    }

    this.fetch(projectId);
  }

  /** Fuerza una nueva validación del mismo proyecto (reintento tras un error). */
  reload(projectId: string | null): void {
    if (!projectId) {
      this.unavailable.set(true);
      return;
    }

    this.fetch(projectId);
  }

  goToList(): void {
    void this.router.navigate(['/proyectos']);
  }

  private fetch(projectId: string): void {
    this.currentProjectId.set(projectId);
    this.project.set(null);
    this.unavailable.set(false);
    this.loading.set(true);

    this.projectService.getById(projectId).subscribe({
      next: (project) => {
        this.project.set(project);
        this.loading.set(false);
      },
      error: () => {
        this.project.set(null);
        this.unavailable.set(true);
        this.loading.set(false);
      }
    });
  }
}
