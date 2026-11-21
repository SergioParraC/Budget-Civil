import { DecimalPipe, NgClass } from '@angular/common';
import { Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CdkDrag, CdkDragDrop, CdkDropList, DragDropModule } from '@angular/cdk/drag-drop';
import { FormsModule } from '@angular/forms';
import {
  ApiError,
  alertTitleFor,
  errorNeedsRefresh,
  resolveApiError
} from '../../../core/http/api-error';
import { ConfirmationService, MenuItem, MessageService, TreeNode } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { MenuModule } from 'primeng/menu';
import { MessageModule } from 'primeng/message';
import { SkeletonModule } from 'primeng/skeleton';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { TreeTableModule } from 'primeng/treetable';

import { AuthService } from '../../../core/auth/auth.service';
import { Permission } from '../../../core/auth/permissions';
import { ProjectBudget } from '../../../core/models/project.model';
import {
  WbsActivityResponse,
  WbsChapterOption,
  WbsItemApuResponse,
  WbsNode
} from '../../../core/models/wbs.model';
import { ProjectContextService } from '../../../core/services/project-context.service';
import { ProjectService } from '../../../core/services/project.service';
import { WbsService } from '../../../core/services/wbs.service';
import { ProjectUnavailable } from '../../proyectos/project-unavailable/project-unavailable';
import { ActivityCard } from './activity-card/activity-card';
import { ChapterFormDialog } from './chapter-form-dialog/chapter-form-dialog';
import { ConflictDialog } from './conflict-dialog/conflict-dialog';
import { ItemFormDialog } from './item-form-dialog/item-form-dialog';
import { MoveNodeDialog, MoveTargetOption } from './move-node-dialog/move-node-dialog';
import { SearchPanel } from './search-panel/search-panel';

type DropPosition = 'before' | 'inside' | 'after';

interface ConflictAlert {
  title: string;
  message: string;
  code: string | null;
  traceId: string | null;
  refresh: boolean;
}

/**
 * Pantalla única del EDT (`REQ-EDT-25`…`REQ-EDT-35`): árbol con capítulos e ítems raíz, alta de capítulo,
 * edición de ítem, bajas, búsqueda, actividad y movimiento por arrastrar y soltar con acciones explícitas.
 * Ningún costo ni estado se calcula aquí: todo viene del servidor (`D-06`, `D-121`).
 */
@Component({
  selector: 'app-edt',
  imports: [
    DragDropModule,
    FormsModule,
    TableModule,
    TreeTableModule,
    InputNumberModule,
    TagModule,
    ButtonModule,
    DialogModule,
    MenuModule,
    TooltipModule,
    MessageModule,
    SkeletonModule,
    DecimalPipe,
    NgClass,
    ProjectUnavailable,
    ActivityCard,
    ChapterFormDialog,
    ItemFormDialog,
    MoveNodeDialog,
    ConflictDialog,
    SearchPanel
  ],
  templateUrl: './edt.html',
  styleUrl: './edt.scss'
})
export class Edt {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly wbsService = inject(WbsService);
  private readonly projectService = inject(ProjectService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  readonly projectContext = inject(ProjectContextService);

  readonly projectId = this.route.snapshot.paramMap.get('id');

  readonly nodes = signal<WbsNode[]>([]);
  readonly loading = signal(false);
  readonly loadError = signal(false);
  readonly saving = signal(false);

  readonly expandedIds = signal<Set<string>>(new Set());
  readonly selectedNodeId = signal<string | null>(null);
  readonly highlightedNodeId = signal<string | null>(null);
  readonly draggingNode = signal<WbsNode | null>(null);

  readonly activity = signal<WbsActivityResponse | null>(null);
  readonly activityLoading = signal(false);
  readonly activityHidden = signal(false);
  readonly activityModalVisible = signal(false);

  readonly budget = signal<ProjectBudget | null>(null);
  readonly budgetLoading = signal(true);

  readonly canWrite = computed(() => this.authService.can(Permission.wbsWrite));
  readonly canWriteApu = computed(() => this.authService.can(Permission.apusWrite));
  readonly canCreateApu = computed(() => this.canWrite() && this.canWriteApu());
  /** La edición en línea usa el `PUT` unificado, que exige ambos permisos. */
  readonly canEditQuantity = computed(() => this.canWrite() && this.canWriteApu());
  readonly quantityDecimals = computed(
    () => this.budget()?.quantityDecimals ?? 7
  );

  /** Borradores de cantidad por ítem mientras el usuario escribe. */
  readonly quantityDrafts = signal<Record<string, number | null>>({});
  /** Ítem cuya cantidad se está guardando; bloquea su input y evita envíos dobles. */
  readonly savingQuantityId = signal<string | null>(null);

  readonly chapterDialogVisible = signal(false);
  readonly chapterToEdit = signal<WbsNode | null>(null);
  readonly chapterParentId = signal<string | null>(null);

  readonly itemDialogVisible = signal(false);
  readonly itemToEdit = signal<WbsNode | null>(null);

  readonly moveDialogVisible = signal(false);
  readonly nodeToMove = signal<WbsNode | null>(null);

  readonly searchVisible = signal(false);

  readonly alertVisible = signal(false);
  readonly alert = signal<ConflictAlert>({
    title: 'No se pudo completar la operación',
    message: '',
    code: null,
    traceId: null,
    refresh: false
  });

  readonly projectReady = computed(() => this.projectContext.project() !== null);

  /** Capítulo seleccionado (o el padre del ítem seleccionado) para preseleccionar el alta. */
  readonly selectedChapterId = computed<string | null>(() => {
    const id = this.selectedNodeId();
    if (!id) {
      return null;
    }

    const node = this.findNode(this.nodes(), id);
    if (!node) {
      return null;
    }

    return node.nodeType === 'CHAPTER' ? node.id : node.parentChapterId;
  });

  /** Ancho del código según el código más largo y la profundidad visible del árbol. */
  readonly codeColumnWidth = computed(() => {
    let maxCodeLength = 6;
    let maxLevel = 0;

    const visit = (nodes: WbsNode[], level: number) => {
      for (const node of nodes) {
        maxCodeLength = Math.max(maxCodeLength, node.code.length);
        maxLevel = Math.max(maxLevel, level);

        if (this.expandedIds().has(node.id)) {
          visit(node.children, level + 1);
        }
      }
    };

    visit(this.nodes(), 0);
    return `calc(${maxCodeLength + 1}ch + ${maxLevel}rem + 4.5rem)`;
  });

  readonly parentMap = computed(() => {
    const map = new Map<string, string | null>();
    const visit = (list: WbsNode[]) =>
      list.forEach((node) => {
        map.set(node.id, node.parentChapterId);
        visit(node.children);
      });
    visit(this.nodes());
    return map;
  });

  readonly chapterOptions = computed<WbsChapterOption[]>(() => {
    const options: WbsChapterOption[] = [];
    const visit = (list: WbsNode[]) =>
      list.forEach((node) => {
        if (node.nodeType === 'CHAPTER') {
          options.push({ id: node.id, code: node.code, name: node.name ?? '' });
        }
        visit(node.children);
      });
    visit(this.nodes());
    return options.sort((a, b) => a.code.localeCompare(b.code));
  });

  readonly treeNodes = computed<TreeNode<WbsNode>[]>(() => this.toTreeNodes(this.nodes()));

  readonly moveTargets = computed<MoveTargetOption[]>(() => {
    const dragged = this.nodeToMove();
    const targets: MoveTargetOption[] = [
      {
        id: '',
        label: 'Raíz del presupuesto',
        maxIndex: this.childrenOf(null).filter((node) => node.id !== dragged?.id).length
      }
    ];

    this.chapterOptions().forEach((chapter) => {
      if (dragged && (chapter.id === dragged.id || this.isInSubtree(chapter.id, dragged.id))) {
        return;
      }

      targets.push({
        id: chapter.id,
        label: `${chapter.code} · ${chapter.name}`,
        maxIndex: this.childrenOf(chapter.id).filter((node) => node.id !== dragged?.id).length
      });
    });

    return targets;
  });

  constructor() {
    this.projectContext.load(this.projectId);

    effect(() => {
      const project = this.projectContext.project();
      if (!project) {
        return;
      }

      untracked(() => {
        this.loadTree();
        this.loadActivity();
        this.loadBudget();
      });
    });
  }

  loadTree(onDone?: () => void, onError?: () => void, preserveExpansion = false): void {
    const projectId = this.projectId;
    if (!projectId) {
      return;
    }

    this.loading.set(true);
    this.loadError.set(false);

    this.wbsService.getTree(projectId).subscribe({
      next: (response) => {
        this.nodes.set(response.nodes);
        if (!preserveExpansion) {
          this.expandedIds.set(this.chapterIds(response.nodes));
        }
        this.loading.set(false);
        onDone?.();
      },
      error: (error: unknown) => {
        this.nodes.set([]);
        this.loading.set(false);
        this.loadError.set(true);
        onError?.();
      }
    });
  }

  loadActivity(): void {
    const projectId = this.projectId;
    if (!projectId) {
      return;
    }

    this.activityLoading.set(true);
    this.wbsService.getActivity(projectId).subscribe({
      next: (activity) => {
        this.activity.set(activity);
        this.activityHidden.set(false);
        this.activityLoading.set(false);
      },
      error: () => {
        this.activity.set(null);
        this.activityLoading.set(false);
        this.activityHidden.set(true);
      }
    });
  }

  loadBudget(): void {
    const projectId = this.projectId;
    if (!projectId) {
      return;
    }

    this.budgetLoading.set(true);
    this.projectService.getBudget(projectId).subscribe({
      next: (budget) => {
        this.budget.set(budget);
        this.budgetLoading.set(false);
      },
      error: () => {
        this.budget.set(null);
        this.budgetLoading.set(false);
      }
    });
  }

  refreshAll(): void {
    this.loadTree();
    this.loadActivity();
    this.loadBudget();
  }

  childrenOf(parentId: string | null, source: WbsNode[] = this.nodes()): WbsNode[] {
    if (parentId === null) {
      return source;
    }

    return this.findNode(source, parentId)?.children ?? [];
  }

  findNode(nodes: WbsNode[], id: string): WbsNode | null {
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

  isInSubtree(candidateId: string, ancestorId: string): boolean {
    const parents = this.parentMap();
    let current: string | null | undefined = candidateId;

    while (current) {
      if (current === ancestorId) {
        return true;
      }
      current = parents.get(current) ?? null;
    }

    return false;
  }

  private chapterIds(nodes: WbsNode[]): Set<string> {
    const ids = new Set<string>();

    const visit = (list: WbsNode[]) =>
      list.forEach((node) => {
        if (node.nodeType === 'CHAPTER') {
          ids.add(node.id);
        }
        visit(node.children);
      });

    visit(nodes);
    return ids;
  }

  private toTreeNodes(nodes: WbsNode[]): TreeNode<WbsNode>[] {
    return nodes.map((node) => ({
      key: node.id,
      data: node,
      label: node.code,
      leaf: node.children.length === 0,
      expanded: this.expandedIds().has(node.id),
      selectable: true,
      children: node.children.length > 0 ? this.toTreeNodes(node.children) : undefined,
      styleClass: this.rowClass(node)
    }));
  }

  private rowClass(node: WbsNode): string {
    const classes = ['edt__row'];

    if (this.selectedNodeId() === node.id) {
      classes.push('edt__row--selected');
    }

    if (this.highlightedNodeId() === node.id) {
      classes.push('edt__row--highlight');
    }

    if (this.draggingNode() && this.isInvalidTarget(node)) {
      classes.push('edt__row--invalid');
    }

    return classes.join(' ');
  }

  isInvalidTarget(node: WbsNode): boolean {
    const dragged = this.draggingNode();
    if (!dragged) {
      return false;
    }

    return dragged.id === node.id || (dragged.nodeType === 'CHAPTER' && this.isInSubtree(node.id, dragged.id));
  }

  onNodeExpand(event: { node: TreeNode<WbsNode> }): void {
    const node = event.node.data;
    if (!node) {
      return;
    }
    this.expandedIds.update((ids) => new Set(ids).add(node.id));
  }

  onNodeCollapse(event: { node: TreeNode<WbsNode> }): void {
    const node = event.node.data;
    if (!node) {
      return;
    }
    this.expandedIds.update((ids) => {
      const next = new Set(ids);
      next.delete(node.id);
      return next;
    });
  }

  expandAll(): void {
    const ids = new Set<string>();
    const visit = (list: WbsNode[]) =>
      list.forEach((node) => {
        if (node.children.length > 0) {
          ids.add(node.id);
          visit(node.children);
        }
      });
    visit(this.nodes());
    this.expandedIds.set(ids);
  }

  collapseAll(): void {
    this.expandedIds.set(new Set());
  }

  selectNode(node: WbsNode): void {
    this.selectedNodeId.set(node.id);
    this.highlightedNodeId.set(null);
  }

  private revealNode(nodeId: string): void {
    const parents = this.parentMap();
    const ancestors = new Set<string>();
    let current = parents.get(nodeId) ?? null;

    while (current) {
      ancestors.add(current);
      current = parents.get(current) ?? null;
    }

    this.expandedIds.set(new Set([...this.chapterIds(this.nodes()), ...ancestors]));
    this.selectedNodeId.set(nodeId);
    this.highlightedNodeId.set(nodeId);
  }

  startDrag(node: WbsNode): void {
    if (!this.canWrite() || this.saving()) {
      return;
    }
    this.draggingNode.set(node);
  }

  endDrag(): void {
    this.draggingNode.set(null);
  }

  readonly canDrop = (drag: CdkDrag, drop: CdkDropList): boolean => {
    const dragged = drag.data as WbsNode | undefined;
    const target = drop.data as WbsNode | undefined;

    if (!dragged || !target || !this.canWrite() || this.saving()) {
      return false;
    }

    if (dragged.id === target.id) {
      return false;
    }

    return !(dragged.nodeType === 'CHAPTER' && this.isInSubtree(target.id, dragged.id));
  };

  readonly canDropOnRoot = (drag: CdkDrag): boolean => {
    const dragged = drag.data as WbsNode | undefined;
    return !!dragged && this.canWrite() && !this.saving();
  };

  onRowDrop(event: CdkDragDrop<WbsNode>, target: WbsNode): void {
    const dragged = event.item.data as WbsNode | undefined;
    if (!dragged || dragged.id === target.id || this.saving()) {
      return;
    }

    const host = event.container.element.nativeElement;
    const rect = host.getBoundingClientRect();
    const ratio = rect.height > 0 ? (event.dropPoint.y - rect.top) / rect.height : 0.5;

    let position: DropPosition;
    if (target.nodeType === 'CHAPTER' && ratio >= 0.3 && ratio <= 0.7) {
      position = 'inside';
    } else {
      position = ratio < 0.5 ? 'before' : 'after';
    }

    this.applyDrop(dragged, target, position);
  }

  onRootDrop(event: CdkDragDrop<WbsNode>): void {
    const dragged = event.item.data as WbsNode | undefined;
    if (!dragged || this.saving()) {
      return;
    }

    const index = this.childrenOf(null).filter((node) => node.id !== dragged.id).length;
    this.commitMove(dragged, null, index, null);
  }

  private applyDrop(dragged: WbsNode, target: WbsNode, position: DropPosition): void {
    if (dragged.nodeType === 'CHAPTER' && this.isInSubtree(target.id, dragged.id)) {
      return;
    }

    if (position === 'inside') {
      const index = this.childrenOf(target.id).filter((node) => node.id !== dragged.id).length;
      this.commitMove(dragged, target.id, index, null);
      return;
    }

    const parentId = target.parentChapterId;
    const siblings = this.childrenOf(parentId).filter((node) => node.id !== dragged.id);
    const targetIndex = siblings.findIndex((node) => node.id === target.id);
    const index = position === 'before' ? Math.max(targetIndex, 0) : targetIndex + 1;

    this.commitMove(dragged, parentId, index, index);
  }

  private commitMove(
    dragged: WbsNode,
    parentId: string | null,
    index: number,
    serverIndex: number | null
  ): void {
    if (dragged.parentChapterId === parentId) {
      const currentIndex = this.childrenOf(parentId).findIndex((node) => node.id === dragged.id);
      if (currentIndex === index) {
        return;
      }

      this.reorderTo(dragged, parentId, index);
      return;
    }

    this.moveTo(dragged, parentId, index, serverIndex);
  }

  private moveTo(
    dragged: WbsNode,
    parentId: string | null,
    index: number,
    serverIndex: number | null
  ): void {
    const projectId = this.projectId;
    if (!projectId) {
      return;
    }

    const previous = this.nodes();
    this.nodes.set(this.withNodeMoved(previous, dragged, parentId, index));
    this.saving.set(true);

    this.wbsService
      .move(projectId, {
        nodeType: dragged.nodeType,
        nodeId: dragged.id,
        targetParentChapterId: parentId,
        targetIndex: serverIndex,
        rowVersion: dragged.rowVersion
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.draggingNode.set(null);
          this.messageService.add({
            severity: 'success',
            summary: 'Nodo movido',
            detail: `“${dragged.code}” se movió correctamente.`
          });
          this.refreshAll();
        },
        error: (error: unknown) => {
          this.saving.set(false);
          this.draggingNode.set(null);
          this.nodes.set(previous);
          this.handleFailure(resolveApiError(error));
        }
      });
  }

  private reorderTo(dragged: WbsNode, parentId: string | null, index: number): void {
    const projectId = this.projectId;
    if (!projectId) {
      return;
    }

    const previous = this.nodes();
    const optimistic = this.withNodeMoved(previous, dragged, parentId, index);
    this.nodes.set(optimistic);
    this.saving.set(true);

    const payload = this.childrenOf(parentId, optimistic).map((node) => ({
      nodeType: node.nodeType,
      nodeId: node.id
    }));

    this.wbsService.reorder(projectId, { parentChapterId: parentId, nodes: payload }).subscribe({
      next: () => {
        this.saving.set(false);
        this.draggingNode.set(null);
        this.refreshAll();
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.draggingNode.set(null);
        this.nodes.set(previous);
        this.handleFailure(resolveApiError(error));
      }
    });
  }

  private withNodeMoved(
    source: WbsNode[],
    dragged: WbsNode,
    parentId: string | null,
    index: number
  ): WbsNode[] {
    const roots = this.cloneNodes(source);
    const removed = this.detach(roots, dragged.id);
    if (!removed) {
      return source;
    }

    const target = parentId === null ? null : this.findNode(roots, parentId);
    if (parentId !== null && !target) {
      return source;
    }

    const list = parentId === null ? roots : target!.children;
    const position = Math.min(Math.max(index, 0), list.length);
    removed.parentChapterId = parentId;
    list.splice(position, 0, removed);
    this.reindex(roots);
    return roots;
  }

  private cloneNodes(nodes: WbsNode[]): WbsNode[] {
    return nodes.map((node) => ({ ...node, children: this.cloneNodes(node.children) }));
  }

  private detach(nodes: WbsNode[], id: string): WbsNode | null {
    const index = nodes.findIndex((node) => node.id === id);
    if (index >= 0) {
      return nodes.splice(index, 1)[0];
    }

    for (const node of nodes) {
      const removed = this.detach(node.children, id);
      if (removed) {
        return removed;
      }
    }

    return null;
  }

  private reindex(nodes: WbsNode[]): void {
    nodes.forEach((node, index) => {
      node.orderIndex = index;
      this.reindex(node.children);
    });
  }

  canMoveUp(node: WbsNode): boolean {
    const siblings = this.childrenOf(node.parentChapterId);
    return siblings.findIndex((sibling) => sibling.id === node.id) > 0;
  }

  canMoveDown(node: WbsNode): boolean {
    const siblings = this.childrenOf(node.parentChapterId);
    const index = siblings.findIndex((sibling) => sibling.id === node.id);
    return index >= 0 && index < siblings.length - 1;
  }

  moveUp(node: WbsNode): void {
    const index = this.childrenOf(node.parentChapterId).findIndex((sibling) => sibling.id === node.id);
    if (index > 0) {
      this.reorderTo(node, node.parentChapterId, index - 1);
    }
  }

  moveDown(node: WbsNode): void {
    const siblings = this.childrenOf(node.parentChapterId);
    const index = siblings.findIndex((sibling) => sibling.id === node.id);
    if (index >= 0 && index < siblings.length - 1) {
      this.reorderTo(node, node.parentChapterId, index + 1);
    }
  }

  openMove(node: WbsNode): void {
    this.nodeToMove.set(node);
    this.moveDialogVisible.set(true);
  }

  onMoveConfirmed(event: { parentId: string | null; index: number | null }): void {
    const dragged = this.nodeToMove();
    if (!dragged) {
      return;
    }

    const index =
      event.index ?? this.childrenOf(event.parentId).filter((node) => node.id !== dragged.id).length;

    this.commitMove(dragged, event.parentId, index, event.index);
    this.nodeToMove.set(null);
  }

  openNewChapter(parentId: string | null): void {
    this.chapterToEdit.set(null);
    this.chapterParentId.set(parentId);
    this.chapterDialogVisible.set(true);
  }

  openEditChapter(node: WbsNode): void {
    this.chapterToEdit.set(node);
    this.chapterParentId.set(node.parentChapterId);
    this.chapterDialogVisible.set(true);
  }

  onChapterSaved(node: WbsNode): void {
    this.messageService.add({
      severity: 'success',
      summary: 'Capítulo guardado',
      detail: `“${node.code}” quedó disponible en el EDT.`
    });
    this.loadActivity();
    this.loadBudget();
    this.loadTree(() => this.revealNode(node.id));
  }

  openItemEdit(node: WbsNode): void {
    this.itemToEdit.set(node);
    this.itemDialogVisible.set(true);
  }

  onItemSaved(response: WbsItemApuResponse): void {
    this.messageService.add({ severity: 'success', summary: 'Ítem actualizado' });
    this.itemToEdit.set(null);
    this.loadActivity();
    this.loadBudget();
    this.loadTree(() => this.revealNode(response.id));
  }

  /** Valor visible en la celda: el borrador local mientras se escribe o el vigente del servidor. */
  quantityValue(node: WbsNode): number | null {
    const drafts = this.quantityDrafts();

    return Object.prototype.hasOwnProperty.call(drafts, node.id)
      ? drafts[node.id]
      : node.quantityWork;
  }

  onQuantityChange(node: WbsNode, value: number | null): void {
    this.quantityDrafts.update((drafts) => ({ ...drafts, [node.id]: value ?? null }));
  }

  onQuantityBlur(node: WbsNode): void {
    this.commitQuantity(node);
  }

  onQuantityKeyDown(event: KeyboardEvent, node: WbsNode): void {
    if (event.key === 'Enter') {
      event.preventDefault();
      event.stopPropagation();
      this.commitQuantity(node);
      return;
    }

    if (event.key === 'Escape') {
      event.preventDefault();
      event.stopPropagation();
      this.cancelQuantity(node);
    }
  }

  /**
   * Guarda la cantidad editada en línea. Envía solo `quantityWork` y `rowVersion` (sin bloque `apu`) y,
   * al confirmar el servidor, refresca el presupuesto y el árbol para recibir los costos recalculados.
   */
  private commitQuantity(node: WbsNode): void {
    if (!this.canEditQuantity() || node.nodeType !== 'ITEM' || !node.apu) {
      return;
    }

    if (this.savingQuantityId() !== null) {
      return;
    }

    const drafts = this.quantityDrafts();
    if (!Object.prototype.hasOwnProperty.call(drafts, node.id)) {
      return;
    }

    const draft = drafts[node.id];

    if (draft === null) {
      this.clearQuantityDraft(node.id);
      this.messageService.add({
        severity: 'error',
        summary: 'Cantidad obligatoria',
        detail: 'La cantidad de obra no puede estar vacía.'
      });
      return;
    }

    if (draft < 0) {
      this.clearQuantityDraft(node.id);
      this.messageService.add({
        severity: 'error',
        summary: 'Cantidad inválida',
        detail: 'La cantidad de obra no puede ser negativa.'
      });
      return;
    }

    if (draft === node.quantityWork) {
      this.clearQuantityDraft(node.id);
      return;
    }

    const projectId = this.projectId;
    if (!projectId) {
      this.clearQuantityDraft(node.id);
      return;
    }

    this.savingQuantityId.set(node.id);
    this.wbsService
      .updateItem(projectId, node.id, {
        quantityWork: draft,
        rowVersion: node.rowVersion
      })
      .subscribe({
        next: (response) => {
          // La respuesta trae la versión nueva: evita un segundo guardado con datos obsoletos.
          this.clearQuantityDraft(node.id);
          node.quantityWork = response.quantityWork;
          node.rowVersion = response.rowVersion;
          this.nodes.set([...this.nodes()]);
          this.loadBudget();
          this.loadTree(
            () => this.savingQuantityId.set(null),
            () => this.savingQuantityId.set(null),
            true
          );
        },
        error: (error: unknown) => {
          this.clearQuantityDraft(node.id);
          this.savingQuantityId.set(null);
          this.handleFailure(resolveApiError(error));
        }
      });
  }

  private cancelQuantity(node: WbsNode): void {
    this.clearQuantityDraft(node.id);
  }

  private clearQuantityDraft(nodeId: string): void {
    const drafts = { ...this.quantityDrafts() };
    if (!Object.prototype.hasOwnProperty.call(drafts, nodeId)) {
      return;
    }

    delete drafts[nodeId];
    this.quantityDrafts.set(drafts);
  }

  /** Vuelve a la ficha del proyecto (`/proyectos/:id`), donde están los accesos a los módulos. */
  backToProject(): void {
    const projectId = this.projectId;
    if (!projectId) {
      return;
    }

    void this.router.navigate(['/proyectos', projectId]);
  }

  goToNewApu(chapterId?: string | null): void {
    const projectId = this.projectId;
    if (!projectId) {
      return;
    }

    void this.router.navigate(['/proyectos', projectId, 'edt', 'items', 'nuevo'], {
      queryParams: chapterId ? { chapterId } : {}
    });
  }

  openApu(node: WbsNode): void {
    const projectId = this.projectId;
    if (!projectId) {
      return;
    }

    if (!node.apu) {
      this.messageService.add({
        severity: 'warn',
        summary: 'El ítem no tiene APU',
        detail: 'Crea el APU desde “Nuevo APU”.'
      });
      return;
    }

    void this.router.navigate(['/proyectos', projectId, 'edt', 'items', node.id, 'apu']);
  }

  confirmDelete(node: WbsNode): void {
    const isChapter = node.nodeType === 'CHAPTER';

    this.confirmationService.confirm({
      header: isChapter ? 'Eliminar capítulo' : 'Eliminar ítem',
      message: isChapter
        ? 'Solo se elimina un capítulo vacío, sin subcapítulos ni ítems. ¿Confirmas la baja?'
        : 'Se eliminará el ítem y también su APU. Los insumos se conservan. ¿Confirmas la baja?',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Eliminar',
      rejectLabel: 'Cancelar',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => this.deleteNode(node)
    });
  }

  private deleteNode(node: WbsNode): void {
    const projectId = this.projectId;
    if (!projectId) {
      return;
    }

    this.saving.set(true);

    const request$ =
      node.nodeType === 'CHAPTER'
        ? this.wbsService.deleteChapter(projectId, node.id, node.rowVersion)
        : this.wbsService.deleteItem(projectId, node.id, node.rowVersion);

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.messageService.add({ severity: 'success', summary: 'Nodo eliminado' });
        if (this.selectedNodeId() === node.id) {
          this.selectedNodeId.set(null);
        }
        this.refreshAll();
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.handleFailure(resolveApiError(error));
      }
    });
  }

  rowMenuItems(node: WbsNode): MenuItem[] {
    const items: MenuItem[] = [];
    const isChapter = node.nodeType === 'CHAPTER';

    if (this.canCreateApu() && isChapter) {
      items.push({
        label: 'Nuevo ítem aquí',
        icon: 'pi pi-plus',
        command: () => this.goToNewApu(node.id)
      });
    }

    if (this.canWrite() && isChapter) {
      items.push({
        label: 'Nuevo capítulo aquí',
        icon: 'pi pi-folder',
        command: () => this.openNewChapter(node.id)
      });
    }

    if (this.canWrite()) {
      items.push({
        label: 'Editar',
        icon: 'pi pi-pencil',
        command: () => (isChapter ? this.openEditChapter(node) : this.openItemEdit(node))
      });
    }

    if (this.canWriteApu() && !isChapter) {
      items.push({
        label: 'Abrir APU',
        icon: 'pi pi-calculator',
        command: () => this.openApu(node)
      });
    }

    if (this.canWrite()) {
      items.push({ separator: true });
      items.push({
        label: 'Mover a…',
        icon: 'pi pi-arrow-right-arrow-left',
        command: () => this.openMove(node)
      });
      items.push({
        label: 'Subir',
        icon: 'pi pi-arrow-up',
        disabled: !this.canMoveUp(node),
        command: () => this.moveUp(node)
      });
      items.push({
        label: 'Bajar',
        icon: 'pi pi-arrow-down',
        disabled: !this.canMoveDown(node),
        command: () => this.moveDown(node)
      });
      items.push({ separator: true });
      items.push({
        label: 'Eliminar',
        icon: 'pi pi-trash',
        command: () => this.confirmDelete(node)
      });
    }

    return items;
  }

  onSearchSelected(nodeId: string): void {
    this.revealNode(nodeId);
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
    this.refreshAll();
  }

  onDialogFailure(apiError: ApiError): void {
    this.handleFailure(apiError);
  }

  trackNode(_index: number, node: WbsNode): string {
    return node.id;
  }
}
