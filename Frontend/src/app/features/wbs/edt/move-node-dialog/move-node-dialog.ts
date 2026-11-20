import { Component, computed, effect, input, model, output, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';

import { WbsNode } from '../../../../core/models/wbs.model';

/** Destino disponible en “Mover a…”: la raíz o un capítulo, con el máximo índice posible. */
export interface MoveTargetOption {
  id: string;
  label: string;
  maxIndex: number;
}

/**
 * Movimiento explícito equivalente al arrastrar y soltar (`REQ-EDT-28`): destino y posición dentro
 * del padre, con `targetIndex` nulo para dejar el nodo al final.
 */
@Component({
  selector: 'app-move-node-dialog',
  imports: [FormsModule, DialogModule, ButtonModule, SelectModule, InputNumberModule],
  templateUrl: './move-node-dialog.html',
  styleUrl: './move-node-dialog.scss'
})
export class MoveNodeDialog {
  readonly visible = model(false);
  readonly node = input<WbsNode | null>(null);
  readonly targets = input<MoveTargetOption[]>([]);
  readonly confirmed = output<{ parentId: string | null; index: number | null }>();

  readonly targetId = signal('');
  readonly position = signal<number | null>(null);

  readonly maxIndex = computed(
    () => this.targets().find((target) => target.id === this.targetId())?.maxIndex ?? 0
  );

  constructor() {
    effect(() => {
      if (!this.visible()) {
        return;
      }

      untracked(() => {
        this.targetId.set('');
        this.position.set(null);
      });
    });
  }

  submit(): void {
    this.confirmed.emit({
      parentId: this.targetId() || null,
      index: this.position() ?? null
    });
    this.visible.set(false);
  }
}
