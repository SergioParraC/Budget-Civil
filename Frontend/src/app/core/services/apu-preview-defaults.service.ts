import { Injectable, signal } from '@angular/core';

const LABOR_TOOLS_STORAGE_KEY = 'budgetcivil.apu.laborToolsPercent';

/**
 * Valor recordado para precargar la herramienta menor en los siguientes APUs. El porcentaje se guarda
 * en el `wastePercentage` de las líneas de mano de obra al crear o editar el APU.
 */
@Injectable({ providedIn: 'root' })
export class ApuPreviewDefaultsService {
  readonly laborToolsPercent = signal(this.readLaborToolsPercent());

  setLaborToolsPercent(value: number): void {
    const normalized = normalizePercent(value);
    this.laborToolsPercent.set(normalized);

    try {
      localStorage.setItem(LABOR_TOOLS_STORAGE_KEY, String(normalized));
    } catch {
      // Sin almacenamiento disponible se conserva el valor en memoria.
    }
  }

  private readLaborToolsPercent(): number {
    try {
      const stored = localStorage.getItem(LABOR_TOOLS_STORAGE_KEY);
      return stored === null ? 0 : normalizePercent(Number(stored));
    } catch {
      return 0;
    }
  }
}

function normalizePercent(value: number): number {
  if (!Number.isFinite(value)) {
    return 0;
  }

  return Math.min(100, Math.max(0, value));
}
