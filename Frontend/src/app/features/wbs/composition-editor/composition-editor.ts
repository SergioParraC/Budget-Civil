import { DecimalPipe } from '@angular/common';
import { Component, computed, inject, input, model, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputNumberModule } from 'primeng/inputnumber';
import { MessageModule } from 'primeng/message';
import { TableModule } from 'primeng/table';

import {
  CompositionLine,
  LABOR_RESOURCE_TYPE_CODE,
  MATERIAL_RESOURCE_TYPE_CODE,
  RESOURCE_TYPE_ORDER,
  resourceTypeLabel
} from '../../../core/models/apu.model';
import { Resource } from '../../../core/models/resource.model';
import { ApuPreviewDefaultsService } from '../../../core/services/apu-preview-defaults.service';
import { ResourcePickerDialog } from './resource-picker-dialog/resource-picker-dialog';

interface CompositionGroup {
  code: string;
  name: string;
  lines: CompositionLine[];
}

/**
 * Insumos editables compartidos por el alta y la edición del APU (`REQ-APUS-24`). Se agrupan por tipo;
 * el desperdicio de materiales y la herramienta menor de mano de obra se envían como `wastePercentage`
 * de cada línea, según corresponda.
 */
@Component({
  selector: 'app-composition-editor',
  imports: [
    FormsModule,
    TableModule,
    ButtonModule,
    InputNumberModule,
    MessageModule,
    DecimalPipe,
    ResourcePickerDialog
  ],
  templateUrl: './composition-editor.html',
  styleUrl: './composition-editor.scss'
})
export class CompositionEditor {
  private readonly previewDefaults = inject(ApuPreviewDefaultsService);

  readonly projectId = input.required<string>();
  readonly apuUnit = input<string>('');
  readonly editable = input(true);
  readonly lines = model<CompositionLine[]>([]);
  /** Desperdicio único aplicado a todos los insumos de tipo Materiales. */
  readonly materialWaste = model(0);
  /** Porcentaje de herramienta menor; se guarda como desperdicio de las líneas de mano de obra. */
  readonly laborToolsPercent = model(0);
  /** AIU del proyecto, usada solo para el cálculo previo del frontend. */
  readonly aiuAdminPercent = input(0);
  readonly aiuUnforeseenPercent = input(0);
  readonly aiuProfitPercent = input(0);
  readonly dirty = output<void>();

  readonly pickerVisible = signal(false);

  private pendingQuantityFocusKey: string | null = null;

  /** Identificador estable del cuadro de cantidad de cada línea. */
  readonly quantityInputId = (key: string): string => `apu-quantity-${key}`;

  /** Total de la línea: únicamente precio unitario × cantidad, sin factores adicionales. */
  readonly lineTotal = (line: CompositionLine): number =>
    roundCurrency((line.unitPrice ?? 0) * (line.consumption ?? 0));

  /** Costo directo preliminar: líneas del APU más los valores de desperdicio y herramienta menor. */
  readonly directCost = computed(() =>
    roundCurrency(
      this.lines().reduce((total, line) => total + this.lineTotal(line), 0) +
        (this.groupExtra(MATERIAL_RESOURCE_TYPE_CODE)?.total ?? 0) +
        (this.groupExtra(LABOR_RESOURCE_TYPE_CODE)?.total ?? 0)
    )
  );

  /** Costo indirecto preliminar: AIU del proyecto aplicado al costo directo, componente por componente. */
  readonly indirectCost = computed(() => {
    const directCost = this.directCost();

    return roundCurrency(
      roundCurrency((directCost * this.aiuAdminPercent()) / 100) +
        roundCurrency((directCost * this.aiuUnforeseenPercent()) / 100) +
        roundCurrency((directCost * this.aiuProfitPercent()) / 100)
    );
  });

  readonly preliminaryTotal = computed(() => roundCurrency(this.directCost() + this.indirectCost()));

  readonly aiuSummary = computed(() => {
    const parts: [string, number][] = [
      ['A', this.aiuAdminPercent()],
      ['I', this.aiuUnforeseenPercent()],
      ['U', this.aiuProfitPercent()]
    ];

    const configured = parts.filter(([, value]) => value > 0).map(([label, value]) => `${label} ${value} %`);
    return configured.length > 0 ? configured.join(' + ') : 'sin AIU configurado';
  });

  /**
   * Fila extra del grupo cuando el desperdicio o la herramienta menor son mayores que cero. El precio
   * unitario muestra el total del grupo sobre el que se aplica el porcentaje (materiales o mano de
   * obra) y el total, el valor adicional que ese porcentaje aporta al APU. Cálculo del frontend.
   */
  readonly groupExtra = (
    code: string
  ): { percentage: number; unitPrice: number; total: number } | null => {
    const percentage =
      code === MATERIAL_RESOURCE_TYPE_CODE
        ? this.materialWaste()
        : code === LABOR_RESOURCE_TYPE_CODE
          ? this.laborToolsPercent()
          : 0;

    if (!(percentage > 0)) {
      return null;
    }

    const groupLines = this.lines().filter((line) => line.resourceTypeCode === code);
    if (groupLines.length === 0) {
      return null;
    }

    const base = groupLines.reduce((total, line) => total + this.lineTotal(line), 0);
    const total = roundCurrency((base * percentage) / 100);

    return { percentage, unitPrice: roundCurrency(base), total };
  };

  /** Subtotal del tipo: líneas del grupo más su fila de desperdicio o herramienta menor. */
  readonly groupSubtotal = (code: string): number => {
    const base = this.lines()
      .filter((line) => line.resourceTypeCode === code)
      .reduce((total, line) => total + this.lineTotal(line), 0);

    return roundCurrency(base + (this.groupExtra(code)?.total ?? 0));
  };

  readonly groups = computed<CompositionGroup[]>(() => {
    const grouped = new Map<string, CompositionLine[]>();
    for (const line of this.lines()) {
      const code = normalizeType(line.resourceTypeCode || line.resourceType);
      grouped.set(code, [...(grouped.get(code) ?? []), line]);
    }

    return [...grouped.entries()]
      .sort(([left], [right]) => typeOrder(left) - typeOrder(right))
      .map(([code, lines]) => ({ code, name: resourceTypeLabel(code), lines }));
  });

  /** Conserva el DOM de cada fila al escribir para no perder el foco del campo. */
  readonly trackLine = (_index: number, line: CompositionLine): string => line.key;

  openPicker(): void {
    this.pickerVisible.set(true);
  }

  addLine(resource: Resource): void {
    const resourceTypeCode = normalizeType(resource.resourceTypeCode);
    const key = `${Date.now()}-${Math.random().toString(16).slice(2)}`;

    this.lines.update((lines) => [
      ...lines,
      {
        key,
        lineId: null,
        resourceId: resource.resourceId,
        description: resource.description,
        unit: resource.unit,
        unitPrice: resource.unitPrice,
        resourceType: resource.resourceTypeName,
        resourceTypeCode,
        matrixResourceId: resourceTypeCode === 'MATRIX' ? resource.resourceId : null,
        consumption: null,
        wastePercentage: 0
      }
    ]);
    this.pendingQuantityFocusKey = key;
    this.dirty.emit();
  }

  /** El diálogo ya se cerró: lleva el cursor a la cantidad de la línea recién agregada. */
  onPickerClosed(): void {
    const key = this.pendingQuantityFocusKey;
    if (!key) {
      return;
    }

    this.pendingQuantityFocusKey = null;
    document.getElementById(this.quantityInputId(key))?.focus();
  }

  updateConsumption(line: CompositionLine, value: number | null): void {
    this.patch(line, { consumption: value ?? null });
  }

  onWasteChange(value: number | null): void {
    this.materialWaste.set(value ?? 0);
    this.dirty.emit();
  }

  onLaborToolsChange(value: number | null): void {
    this.laborToolsPercent.set(value ?? 0);
    // Se recuerda como valor por defecto para los siguientes APUs.
    this.previewDefaults.setLaborToolsPercent(this.laborToolsPercent());
    this.dirty.emit();
  }

  removeLine(line: CompositionLine): void {
    this.lines.update((lines) => lines.filter((candidate) => candidate.key !== line.key));
    this.dirty.emit();
  }

  private patch(line: CompositionLine, changes: Partial<CompositionLine>): void {
    this.lines.update((lines) =>
      lines.map((candidate) =>
        candidate.key === line.key ? { ...candidate, ...changes } : candidate
      )
    );
    this.dirty.emit();
  }
}

function normalizeType(code: string | null | undefined): string {
  return (code ?? '').trim().toUpperCase();
}

function typeOrder(code: string): number {
  const index = RESOURCE_TYPE_ORDER.indexOf(code);
  return index >= 0 ? index : RESOURCE_TYPE_ORDER.length;
}

function roundCurrency(value: number): number {
  return Math.round(value * 100) / 100;
}
