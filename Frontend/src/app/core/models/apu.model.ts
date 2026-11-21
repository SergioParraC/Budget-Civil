/** Referencia del ítem dentro del detalle del APU (`WbsItemReferenceDto`). */
export interface WbsItemReference {
  code: string;
  quantityWork: number | null;
  unit: string | null;
}

/** Línea de composición del APU (`ApuResourceItemDto`). */
export interface ApuResourceItem {
  lineId: string;
  resourceId: string;
  description: string;
  unit: string | null;
  unitPrice: number | null;
  /** Cantidad base de la línea, sin desperdicio ni herramienta menor. */
  consumption: number;
  wastePercentage: number;
  matrixResourceId?: string | null;
}

/** Insumos agrupados por tipo, en el orden del catálogo (`ApuResourceGroupDto`). */
export interface ApuResourceGroup {
  resourceType: string;
  items: ApuResourceItem[];
}

/** Detalle del APU (`ApuDetailResponse`). */
export interface ApuDetail {
  apuId: string;
  description: string;
  unit: string;
  status: string;
  wbsItem: WbsItemReference | null;
  resources: ApuResourceGroup[];
  rowVersion: string;
  lastModifiedBy: string;
  lastModifiedAt: string;
}

export interface ApuListItem {
  apuId: string;
  code: string;
  description: string;
  unit: string;
  unitPrice: number | null;
  projectId: string;
  projectName: string;
  municipalityName: string | null;
  departmentName: string | null;
}

export interface ApuListResponse {
  items: ApuListItem[];
  page: number;
  pageSize: number;
  totalItems: number;
}

/** Componente de una Matriz (`MatrixComponentItemDto`). */
export interface MatrixComponentItem {
  componentId: string;
  resourceId: string;
  description: string;
  unit: string | null;
  unitPrice: number | null;
  quantity: number;
  wastePercentage: number;
  subtotal: number | null;
}

export interface MatrixResourceGroup {
  resourceType: string;
  items: MatrixComponentItem[];
}

/** Composición de una Matriz (`MatrixDetailResponse`). */
export interface MatrixDetail {
  matrixResourceId: string;
  description: string;
  unit: string | null;
  status: string;
  resources: MatrixResourceGroup[];
  directCost: number | null;
  rowVersion: string;
}

export interface AddMatrixComponentRequest {
  componentResourceId: string;
  consumption: number;
  wastePercentage: number;
  rowVersion: string;
}

export interface UpdateMatrixComponentRequest {
  consumption: number;
  wastePercentage: number;
  rowVersion: string;
}

/**
 * Línea editable de composición del APU. `lineId` existe cuando la línea ya está guardada; el cliente
 * calcula la vista previa y el servidor solo entrega la composición base.
 */
export interface CompositionLine {
  key: string;
  lineId: string | null;
  resourceId: string;
  description: string;
  unit: string | null;
  unitPrice: number | null;
  /** Nombre visible del tipo (p. ej. "Materiales"). */
  resourceType: string;
  /** Código del catálogo (p. ej. `MATERIAL`), usado para agrupar y aplicar el desperdicio. */
  resourceTypeCode: string;
  matrixResourceId?: string | null;
  /** `null` mientras la línea recién agregada espera que el usuario escriba la cantidad. */
  consumption: number | null;
  wastePercentage: number;
}

/** Orden fijo del catálogo de tipos de insumo (`ResourceTypeCatalog`). */
export const RESOURCE_TYPE_ORDER = [
  'MATERIAL',
  'LABOR',
  'TOOL',
  'EQUIPMENT',
  'AUXILIARY',
  'WORK',
  'TRANSPORT',
  'MATRIX'
];

const RESOURCE_TYPE_LABELS: Record<string, string> = {
  MATERIAL: 'Materiales',
  LABOR: 'Mano de obra',
  TOOL: 'Herramienta',
  EQUIPMENT: 'Equipos',
  AUXILIARY: 'Auxiliares',
  WORK: 'Trabajos',
  TRANSPORT: 'Transporte',
  MATRIX: 'Matriz'
};

export const MATERIAL_RESOURCE_TYPE_CODE = 'MATERIAL';
export const LABOR_RESOURCE_TYPE_CODE = 'LABOR';

/** Nombre visible del tipo de insumo a partir de su código. */
export function resourceTypeLabel(code: string | null | undefined): string {
  const normalized = (code ?? '').trim().toUpperCase();
  return RESOURCE_TYPE_LABELS[normalized] ?? (normalized || 'Sin tipo');
}

/**
 * Porcentajes guardados por el backend para las filas globales del editor. En mano de obra el
 * `wastePercentage` representa la herramienta menor. Se toma la primera línea con valor mayor que cero
 * para no perder el dato cuando una línea antigua quedó en cero.
 */
export function compositionFactorsFromDetail(detail: ApuDetail): {
  materialWaste: number;
  laborToolsPercent: number;
} {
  return {
    materialWaste: storedWasteForType(detail, MATERIAL_RESOURCE_TYPE_CODE),
    laborToolsPercent: storedWasteForType(detail, LABOR_RESOURCE_TYPE_CODE)
  };
}

function storedWasteForType(detail: ApuDetail, typeCode: string): number {
  const group = detail.resources.find(
    (candidate) => (candidate.resourceType ?? '').trim().toUpperCase() === typeCode
  );

  if (!group || group.items.length === 0) {
    return 0;
  }

  const withStoredWaste = group.items.find((item) => (item.wastePercentage ?? 0) > 0);
  return withStoredWaste?.wastePercentage ?? group.items[0]?.wastePercentage ?? 0;
}
