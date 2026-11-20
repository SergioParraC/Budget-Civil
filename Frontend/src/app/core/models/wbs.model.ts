import { ApuResourceGroup } from './apu.model';

/** Tipos de nodo del EDT (`WbsNodeTypeCodes`). */
export type WbsNodeType = 'CHAPTER' | 'ITEM';

/** Estados del APU tal como los devuelve el servidor. */
export type ApuStatus = 'COMPLETO' | 'FALTA_INFORMACION';

/** Referencia del APU dentro de un nodo del árbol (`WbsApuReferenceDto`). */
export interface WbsApuReference {
  id: string;
  status: ApuStatus | string;
  description: string;
  unit: string;
  unitPrice: number | null;
  rowVersion: string;
}

/**
 * Nodo del árbol v1 (`WbsNodeDto`). Los ítems raíz comparten la colección raíz con los capítulos
 * (`D-118`); `name` solo existe en capítulos y la descripción visible del ítem es `apu.description`.
 */
export interface WbsNode {
  id: string;
  projectId: string;
  nodeType: WbsNodeType;
  code: string;
  parentChapterId: string | null;
  name: string | null;
  quantityWork: number | null;
  orderIndex: number;
  rowVersion: string;
  lastModifiedBy: string;
  lastModifiedAt: string;
  apu: WbsApuReference | null;
  unitCost: number | null;
  directCost: number | null;
  children: WbsNode[];
}

export interface WbsTreeResponse {
  projectId: string;
  nodes: WbsNode[];
}

/** Resultado de `GET /wbs/search`: sin costos. */
export interface WbsSearchItem {
  nodeId: string;
  nodeType: WbsNodeType;
  code: string;
  name: string | null;
  quantityWork: number | null;
  parentChapterId: string | null;
  orderIndex: number;
}

export interface WbsSearchResponse {
  items: WbsSearchItem[];
  page: number;
  pageSize: number;
  totalItems: number;
}

export interface WbsActivityParticipant {
  userId: string;
  userName: string;
  lastModifiedAt: string;
}

export interface WbsActivityResponse {
  projectId: string;
  lastActivity: WbsActivityParticipant | null;
  participants: WbsActivityParticipant[];
}

export interface WbsCodeValidation {
  code: string;
  isValid: boolean;
  isAvailable: boolean;
  reason: string | null;
}

/** Opción de capítulo para los selectores de ubicación y de movimiento. */
export interface WbsChapterOption {
  id: string;
  code: string;
  name: string;
}

export interface CreateWbsChapterRequest {
  code: string;
  name: string;
  parentChapterId?: string | null;
}

export interface UpdateWbsChapterRequest {
  code?: string;
  name: string;
  rowVersion: string;
}

export interface WbsItemApuResourceDraft {
  resourceId: string;
  consumption: number;
  wastePercentage: number;
}

export interface WbsItemApuDraft {
  description: string;
  unit: string;
  resources: WbsItemApuResourceDraft[];
}

export interface CreateWbsItemRequest {
  code: string;
  wbsChapterId?: string | null;
  quantityWork: number;
  apu: WbsItemApuDraft;
}

export interface WbsItemApuResourceUpdateDraft {
  lineId?: string | null;
  resourceId: string;
  consumption: number;
  wastePercentage: number;
}

export interface UpdateWbsItemApuDraft {
  description?: string;
  unit?: string;
  apuRowVersion: string;
  resources?: WbsItemApuResourceUpdateDraft[];
}

export interface UpdateWbsItemRequest {
  code?: string;
  quantityWork: number;
  rowVersion: string;
  apu?: UpdateWbsItemApuDraft;
}

export interface MoveWbsNodeRequest {
  nodeType: WbsNodeType;
  nodeId: string;
  targetParentChapterId?: string | null;
  targetIndex?: number | null;
  rowVersion: string;
}

export interface WbsNodeRef {
  nodeType: WbsNodeType;
  nodeId: string;
}

export interface ReorderWbsNodesRequest {
  parentChapterId?: string | null;
  nodes: WbsNodeRef[];
}

export interface WbsNodeRefWithOrder extends WbsNodeRef {
  orderIndex: number;
}

export interface ReorderWbsNodesResponse {
  parentChapterId: string | null;
  nodes: WbsNodeRefWithOrder[];
}

/** Respuesta plana de alta, edición y copia unificadas de ítem + APU (`WbsItemApuResponse`). */
export interface WbsItemApuResponse {
  id: string;
  projectId: string;
  nodeType: WbsNodeType;
  code: string;
  parentChapterId: string | null;
  name: string | null;
  quantityWork: number;
  orderIndex: number;
  rowVersion: string;
  lastModifiedBy: string;
  lastModifiedAt: string;
  children: WbsNode[];
  apuId: string;
  apuStatus: ApuStatus | string;
  apuRowVersion: string;
  apuLastModifiedBy: string;
  apuLastModifiedAt: string;
  description: string;
  unit: string;
  resources: ApuResourceGroup[];
}
