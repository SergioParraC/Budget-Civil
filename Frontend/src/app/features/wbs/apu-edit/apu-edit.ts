import { Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
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
import {
  ApuDetail,
  CompositionLine,
  LABOR_RESOURCE_TYPE_CODE,
  MATERIAL_RESOURCE_TYPE_CODE,
  compositionFactorsFromDetail,
  resourceTypeLabel
} from '../../../core/models/apu.model';
import { Unit } from '../../../core/models/unit.model';
import { WbsNode } from '../../../core/models/wbs.model';
import { ApuPreviewDefaultsService } from '../../../core/services/apu-preview-defaults.service';
import { ApuService } from '../../../core/services/apu.service';
import { PendingChangesService } from '../../../core/services/pending-changes.service';
import { ProjectContextService } from '../../../core/services/project-context.service';
import { UnitService } from '../../../core/services/unit.service';
import { WbsService } from '../../../core/services/wbs.service';
import { ProjectUnavailable } from '../../proyectos/project-unavailable/project-unavailable';
import { CompositionEditor } from '../composition-editor/composition-editor';
import { UnitCreateDialog } from '../composition-editor/unit-create-dialog/unit-create-dialog';
import { ConflictDialog } from '../edt/conflict-dialog/conflict-dialog';

/**
 * Página de edición del APU del ítem (`REQ-APUS-26`…`REQ-APUS-29`). Envía `quantityWork`, `rowVersion` y el
 * bloque `apu` con `apuRowVersion`; solo envía `resources` cuando los insumos cambiaron.
 */
@Component({
  selector: 'app-apu-edit',
  imports: [
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    SelectModule,
    TextareaModule,
    MessageModule,
    TagModule,
    ProjectUnavailable,
    CompositionEditor,
    UnitCreateDialog,
    ConflictDialog
  ],
  templateUrl: './apu-edit.html',
  styleUrl: './apu-edit.scss'
})
export class ApuEdit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly wbsService = inject(WbsService);
  private readonly apuService = inject(ApuService);
  private readonly unitService = inject(UnitService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly pendingChanges = inject(PendingChangesService);
  private readonly previewDefaults = inject(ApuPreviewDefaultsService);
  readonly projectContext = inject(ProjectContextService);

  readonly projectId = this.route.snapshot.paramMap.get('id');
  readonly itemId = this.route.snapshot.paramMap.get('itemId');

  readonly canWrite = this.authService.can(Permission.wbsWrite);
  readonly canWriteApu = this.authService.can(Permission.apusWrite);
  readonly canCreateUnit = this.authService.can(Permission.catalogsWrite);
  readonly editable = computed(() => this.canWrite && this.canWriteApu);

  readonly node = signal<WbsNode | null>(null);
  readonly detail = signal<ApuDetail | null>(null);
  readonly loading = signal(false);
  readonly loadError = signal(false);
  readonly itemWithoutApu = signal(false);
  readonly saving = signal(false);
  readonly compositionDirty = signal(false);
  readonly lines = signal<CompositionLine[]>([]);
  readonly materialWaste = signal(0);
  readonly laborToolsPercent = signal(0);
  readonly rememberedLaborToolsPercent = this.previewDefaults.laborToolsPercent;
  readonly hasLaborLines = computed(() =>
    this.lines().some((line) => line.resourceTypeCode === LABOR_RESOURCE_TYPE_CODE)
  );
  readonly showMissingLaborTools = computed(
    () =>
      this.editable() &&
      this.hasLaborLines() &&
      this.laborToolsPercent() === 0
  );
  readonly units = signal<Unit[]>([]);
  readonly unitDialogVisible = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly createdNotice = signal(false);
  readonly codeState = signal<'idle' | 'checking' | 'valid' | 'invalid'>('idle');
  readonly codeMessage = signal<string | null>(null);

  readonly itemRowVersion = signal<string | null>(null);
  readonly apuRowVersion = signal<string | null>(null);

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
    quantityWork: [1, [nonNegativeNumber]],
    description: ['', [Validators.required, Validators.maxLength(255)]],
    unit: ['', [Validators.required, Validators.maxLength(10)]]
  });

  readonly unitOptions = computed(() =>
    this.units().map((unit) => ({ label: `${unit.shortUnit} — ${unit.unit}`, value: unit.shortUnit }))
  );

  private applyingServerState = false;

  constructor() {
    if (this.route.snapshot.queryParamMap.get('created') === '1') {
      this.createdNotice.set(true);

      // Evita que el aviso reaparezca al refrescar la página.
      const cleanUrl = this.router.url.split('?')[0];
      history.replaceState(history.state, '', cleanUrl);
    }

    this.projectContext.load(this.projectId);

    this.form.valueChanges.subscribe(() => {
      if (!this.applyingServerState) {
        this.pendingChanges.setPendingChanges(true);
      }
    });

    effect(() => {
      if (!this.projectContext.project()) {
        return;
      }

      untracked(() => this.load());
    });
  }

  private load(): void {
    const projectId = this.projectId;
    const itemId = this.itemId;

    if (!projectId || !itemId) {
      this.loadError.set(true);
      return;
    }

    this.loading.set(true);
    this.loadError.set(false);
    this.itemWithoutApu.set(false);

    this.unitService.list().subscribe({
      next: (units) => this.units.set(units),
      error: () => this.units.set([])
    });

    this.wbsService.getTree(projectId).subscribe({
      next: (response) => {
        const node = this.findNode(response.nodes, itemId);

        if (!node) {
          this.loading.set(false);
          this.loadError.set(true);
          return;
        }

        this.node.set(node);
        this.itemRowVersion.set(node.rowVersion);

        if (!node.apu) {
          this.loading.set(false);
          this.itemWithoutApu.set(true);
          return;
        }

        this.apuRowVersion.set(node.apu.rowVersion);

        this.apuService.getById(projectId, node.apu.id).subscribe({
          next: (detail) => {
            this.applyDetail(detail, node);
            this.loading.set(false);
          },
          error: (error: unknown) => {
            this.loading.set(false);
            this.loadError.set(true);
            this.handleFailure(resolveApiError(error));
          }
        });
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.loadError.set(true);
        this.handleFailure(resolveApiError(error));
      }
    });
  }

  private findNode(nodes: WbsNode[], id: string): WbsNode | null {
    for (const node of nodes) {
      if (node.id === id) {
        return node;
      }

      const found = this.findNode(node.children, id);
      if (found) {
        return found;
      }
    }

    return null;
  }

  private applyDetail(detail: ApuDetail, node: WbsNode): void {
    this.detail.set(detail);
    this.apuRowVersion.set(detail.rowVersion);
    this.itemRowVersion.set(node.rowVersion);

    this.applyingServerState = true;
    this.form.reset({
      code: node.code,
      quantityWork: node.quantityWork ?? 1,
      description: detail.description,
      unit: detail.unit
    });
    this.applyingServerState = false;

    this.codeState.set('idle');
    this.codeMessage.set(null);

    this.lines.set(this.toLines(detail));
    const factors = compositionFactorsFromDetail(detail);
    this.materialWaste.set(factors.materialWaste);
    this.laborToolsPercent.set(factors.laborToolsPercent);
    this.compositionDirty.set(false);
    this.pendingChanges.clear();
  }

  private toLines(detail: ApuDetail): CompositionLine[] {
    return detail.resources.flatMap((group) =>
      group.items.map((item) => ({
        key: item.lineId,
        lineId: item.lineId,
        resourceId: item.resourceId,
        description: item.description,
        unit: item.unit,
        unitPrice: item.unitPrice,
        resourceType: resourceTypeLabel(group.resourceType),
        resourceTypeCode: group.resourceType,
        matrixResourceId: item.matrixResourceId ?? null,
        consumption: item.consumption,
        wastePercentage: item.wastePercentage
      }))
    );
  }

  onCompositionDirty(): void {
    this.compositionDirty.set(true);
    this.pendingChanges.setPendingChanges(true);
  }

  /** Valida el código del ítem al salir del campo, ignorando el propio nodo en la comprobación de duplicados. */
  onCodeBlur(): void {
    if (!this.editable()) {
      return;
    }

    const node = this.node();
    const code = this.form.controls.code.value.trim();

    if (!code) {
      this.codeState.set('invalid');
      this.codeMessage.set('El código es obligatorio.');
      return;
    }

    if (node && code === node.code) {
      this.codeState.set('valid');
      this.codeMessage.set(null);
      return;
    }

    this.codeState.set('checking');
    this.codeMessage.set(null);

    this.wbsService.validateCode(this.projectId ?? '', code, node?.id).subscribe({
      next: (validation) => {
        if (this.form.controls.code.value.trim() !== code) {
          return;
        }

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
        if (this.form.controls.code.value.trim() !== code) {
          return;
        }

        this.codeState.set('invalid');
        this.codeMessage.set(resolveApiError(error).message);
      }
    });
  }

  applyRememberedLaborTools(): void {
    const remembered = this.rememberedLaborToolsPercent();
    if (!(remembered > 0)) {
      return;
    }

    this.laborToolsPercent.set(remembered);
    this.onCompositionDirty();
  }

  onUnitCreated(unit: Unit): void {
    this.units.update((units) =>
      units.some((candidate) => candidate.id === unit.id) ? units : [...units, unit]
    );
    this.form.controls.unit.setValue(unit.shortUnit);
  }

  submit(): void {
    if (this.saving()) {
      return;
    }

    const projectId = this.projectId;
    const node = this.node();
    const itemRowVersion = this.itemRowVersion();
    const apuRowVersion = this.apuRowVersion();

    if (!projectId || !node || !this.editable() || !itemRowVersion || !apuRowVersion) {
      return;
    }

    const raw = this.form.getRawValue();
    const code = raw.code.trim();

    if (this.form.invalid || !code) {
      this.form.markAllAsTouched();
      return;
    }

    if (this.codeState() === 'invalid') {
      this.errorMessage.set(this.codeMessage() ?? 'Revisa el código del ítem.');
      return;
    }

    const lines = this.lines();
    const invalidLine = lines.some((line) => !(line.consumption > 0));

    if (invalidLine) {
      this.errorMessage.set('Revisa los insumos: la cantidad debe ser mayor que cero.');
      return;
    }

    this.saving.set(true);
    this.errorMessage.set(null);

    this.wbsService
      .updateItem(projectId, node.id, {
        ...(code !== node.code ? { code } : {}),
        quantityWork: Number(raw.quantityWork),
        rowVersion: itemRowVersion,
        apu: {
          description: raw.description.trim(),
          unit: raw.unit.trim(),
          apuRowVersion,
          ...(this.compositionDirty()
            ? {
                resources: lines.map((line) => ({
                  lineId: line.lineId,
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
            : {})
        }
      })
      .subscribe({
        next: (response) => {
          this.saving.set(false);
          this.itemRowVersion.set(response.rowVersion);
          this.apuRowVersion.set(response.apuRowVersion);
          this.compositionDirty.set(false);
          this.pendingChanges.clear();
          this.messageService.add({ severity: 'success', summary: 'APU guardado' });
          this.load();
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

  discardChanges(): void {
    this.confirmationService.confirm({
      header: 'Descartar cambios',
      message: '¿Confirmas descartar los cambios sin guardar y volver al estado del servidor?',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Descartar',
      rejectLabel: 'Seguir editando',
      accept: () => {
        this.pendingChanges.clear();
        this.load();
      }
    });
  }

  goToEdt(): void {
    if (!this.projectId) {
      return;
    }

    void this.router.navigate(['/proyectos', this.projectId, 'edt']);
  }

  goToCreate(): void {
    if (!this.projectId) {
      return;
    }

    void this.router.navigate(['/proyectos', this.projectId, 'edt', 'items', 'nuevo']);
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
    this.load();
  }
}
