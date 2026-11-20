import { Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { SelectModule } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';

import { AuthService } from '../../../core/auth/auth.service';
import { Permission } from '../../../core/auth/permissions';
import { nonNegativeNumber } from '../../../core/forms/validators';
import {
  ApiError,
  alertTitleFor,
  errorNeedsRefresh,
  resolveApiError
} from '../../../core/http/api-error';
import { CompositionLine, LABOR_RESOURCE_TYPE_CODE, MATERIAL_RESOURCE_TYPE_CODE } from '../../../core/models/apu.model';
import { Unit } from '../../../core/models/unit.model';
import { WbsChapterOption, WbsNode } from '../../../core/models/wbs.model';
import { ApuPreviewDefaultsService } from '../../../core/services/apu-preview-defaults.service';
import { ProjectContextService } from '../../../core/services/project-context.service';
import { UnitService } from '../../../core/services/unit.service';
import { WbsService } from '../../../core/services/wbs.service';
import { ProjectUnavailable } from '../../proyectos/project-unavailable/project-unavailable';
import { CompositionEditor } from '../composition-editor/composition-editor';
import { UnitCreateDialog } from '../composition-editor/unit-create-dialog/unit-create-dialog';
import { ConflictDialog } from '../edt/conflict-dialog/conflict-dialog';

/**
 * Página “Nuevo APU” (`REQ-APUS-23`…`REQ-APUS-25`): crea el ítem del EDT y su APU en una sola llamada a
 * `POST /wbs/items`, porque no existe alta individual de APU.
 */
@Component({
  selector: 'app-apu-create',
  imports: [
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    SelectModule,
    TextareaModule,
    MessageModule,
    ProjectUnavailable,
    CompositionEditor,
    UnitCreateDialog,
    ConflictDialog
  ],
  templateUrl: './apu-create.html',
  styleUrl: './apu-create.scss'
})
export class ApuCreate {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly wbsService = inject(WbsService);
  private readonly unitService = inject(UnitService);
  private readonly previewDefaults = inject(ApuPreviewDefaultsService);
  readonly projectContext = inject(ProjectContextService);

  readonly projectId = this.route.snapshot.paramMap.get('id');

  readonly canWrite = this.authService.can(Permission.wbsWrite);
  readonly canWriteApu = this.authService.can(Permission.apusWrite);
  readonly canCreateUnit = this.authService.can(Permission.catalogsWrite);
  readonly authorized = computed(() => this.canWrite && this.canWriteApu);

  readonly saving = signal(false);
  readonly loadError = signal(false);
  readonly chapterOptions = signal<WbsChapterOption[]>([]);
  readonly units = signal<Unit[]>([]);
  readonly lines = signal<CompositionLine[]>([]);
  readonly materialWaste = signal(0);
  readonly laborToolsPercent = signal(this.previewDefaults.laborToolsPercent());
  readonly unitDialogVisible = signal(false);

  readonly codeState = signal<'idle' | 'checking' | 'valid' | 'invalid'>('idle');
  readonly codeMessage = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);

  readonly alertVisible = signal(false);
  readonly alert = signal({
    title: '',
    message: '',
    code: null as string | null,
    traceId: null as string | null,
    refresh: false
  });

  readonly form = this.fb.nonNullable.group({
    code: ['', [Validators.required, Validators.maxLength(10)]],
    wbsChapterId: [this.route.snapshot.queryParamMap.get('chapterId') ?? ''],
    quantityWork: [1, [nonNegativeNumber]],
    description: ['', [Validators.required, Validators.maxLength(255)]],
    unit: ['', [Validators.required, Validators.maxLength(10)]]
  });

  readonly unitOptions = computed(() =>
    this.units().map((unit) => ({ label: `${unit.shortUnit} — ${unit.unit}`, value: unit.shortUnit }))
  );

  constructor() {
    this.projectContext.load(this.projectId);

    effect(() => {
      if (!this.projectContext.project()) {
        return;
      }

      untracked(() => this.loadOptions());
    });
  }

  private loadOptions(): void {
    const projectId = this.projectId;
    if (!projectId) {
      return;
    }

    this.loadError.set(false);

    this.wbsService.getTree(projectId).subscribe({
      next: (response) => this.chapterOptions.set(this.flattenChapters(response.nodes)),
      error: () => this.loadError.set(true)
    });

    this.unitService.list().subscribe({
      next: (units) => this.units.set(units),
      error: () => this.units.set([])
    });
  }

  private flattenChapters(nodes: WbsNode[]): WbsChapterOption[] {
    const chapters: WbsChapterOption[] = [];
    const visit = (list: WbsNode[]) =>
      list.forEach((node) => {
        if (node.nodeType === 'CHAPTER') {
          chapters.push({ id: node.id, code: node.code, name: node.name ?? '' });
        }
        visit(node.children);
      });
    visit(nodes);
    return chapters.sort((a, b) => a.code.localeCompare(b.code));
  }

  readonly chapterSelectOptions = computed(() => [
    { id: '', label: 'Raíz del presupuesto' },
    ...this.chapterOptions().map((chapter) => ({
      id: chapter.id,
      label: `${chapter.code} · ${chapter.name}`
    }))
  ]);

  onCodeBlur(): void {
    const code = this.form.controls.code.value.trim();
    if (!code) {
      this.codeState.set('invalid');
      this.codeMessage.set('El código es obligatorio.');
      return;
    }

    this.codeState.set('checking');
    this.codeMessage.set(null);

    this.wbsService.validateCode(this.projectId ?? '', code).subscribe({
      next: (validation) => {
        if (validation.isValid && validation.isAvailable) {
          this.codeState.set('valid');
          return;
        }

        this.codeState.set('invalid');
        this.codeMessage.set(
          validation.isValid
            ? 'Ese código ya está en uso en el proyecto (sin distinguir mayúsculas). Elige otro.'
            : 'El código debe tener entre 1 y 10 caracteres; se permiten letras, números y símbolos como . , - _.'
        );
      },
      error: (error: unknown) => {
        this.codeState.set('invalid');
        this.codeMessage.set(resolveApiError(error).message);
      }
    });
  }

  onUnitCreated(unit: Unit): void {
    this.units.update((units) =>
      units.some((candidate) => candidate.id === unit.id) ? units : [...units, unit]
    );
    this.form.controls.unit.setValue(unit.shortUnit);
  }

  submit(): void {
    const projectId = this.projectId;
    if (!projectId || !this.authorized()) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const lines = this.lines();
    const invalidLine = lines.some((line) => !(line.consumption > 0));

    if (invalidLine) {
      this.errorMessage.set('Revisa los insumos: la cantidad debe ser mayor que cero.');
      return;
    }

    const raw = this.form.getRawValue();
    this.saving.set(true);
    this.errorMessage.set(null);

    this.wbsService
      .createItem(projectId, {
        code: raw.code.trim(),
        wbsChapterId: raw.wbsChapterId || null,
        quantityWork: Number(raw.quantityWork),
        apu: {
          description: raw.description.trim(),
          unit: raw.unit.trim(),
          resources: lines.map((line) => ({
            resourceId: line.resourceId,
            consumption: Number(line.consumption),
            wastePercentage:
              line.resourceTypeCode === MATERIAL_RESOURCE_TYPE_CODE
                ? this.materialWaste()
                : line.resourceTypeCode === LABOR_RESOURCE_TYPE_CODE
                  ? this.laborToolsPercent()
                  : 0
          }))
        }
      })
      .subscribe({
        next: (response) => {
          this.saving.set(false);
          void this.router.navigate(
            ['/proyectos', projectId, 'edt', 'items', response.id, 'apu'],
            { queryParams: { created: '1' } }
          );
        },
        error: (error: unknown) => {
          this.saving.set(false);
          const apiError = resolveApiError(error);
          this.errorMessage.set(apiError.message);

          if (
            apiError.code === 'WBS_CODE_DUPLICATED' ||
            apiError.code === 'WBS_CODE_INVALID'
          ) {
            this.codeState.set('invalid');
            this.codeMessage.set(apiError.message);
          }

          this.handleFailure(apiError);
        }
      });
  }

  goToEdt(): void {
    if (!this.projectId) {
      return;
    }

    void this.router.navigate(['/proyectos', this.projectId, 'edt']);
  }

  private handleFailure(apiError: ApiError): void {
    this.alert.set({
      title: alertTitleFor(apiError.code),
      message: apiError.message,
      code: apiError.code,
      traceId: apiError.traceId,
      refresh: errorNeedsRefresh(apiError.code)
    });
    this.alertVisible.set(true);
  }

  onAlertRefresh(): void {
    this.loadOptions();
  }
}
