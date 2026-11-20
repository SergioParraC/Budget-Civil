/** Unidad del catálogo global (`UnitDto`). El APU y el insumo guardan la sigla textual. */
export interface Unit {
  id: number;
  shortUnit: string;
  unit: string;
}

export interface CreateUnitRequest {
  shortUnit: string;
  unit: string;
}
