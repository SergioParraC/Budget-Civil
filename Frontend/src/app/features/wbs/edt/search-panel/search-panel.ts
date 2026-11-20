import { Component, effect, inject, input, model, output, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { TableLazyLoadEvent, TableModule } from 'primeng/table';

import { resolveApiError } from '../../../../core/http/api-error';
import { WbsSearchItem } from '../../../../core/models/wbs.model';
import { WbsService } from '../../../../core/services/wbs.service';

/** Búsqueda parcial del EDT (`GET /wbs/search`): resultados sin costos (`REQ-EDT-33`). */
@Component({
  selector: 'app-search-panel',
  imports: [FormsModule, DialogModule, ButtonModule, InputTextModule, TableModule, MessageModule],
  templateUrl: './search-panel.html',
  styleUrl: './search-panel.scss'
})
export class SearchPanel {
  private readonly wbsService = inject(WbsService);

  readonly visible = model(false);
  readonly projectId = input.required<string>();
  readonly selected = output<string>();

  readonly term = signal('');
  readonly items = signal<WbsSearchItem[]>([]);
  readonly totalRecords = signal(0);
  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly pageSize = signal<10 | 50 | 100>(10);

  constructor() {
    effect(() => {
      if (!this.visible()) {
        return;
      }

      untracked(() => this.search(1));
    });
  }

  search(page: number): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.wbsService
      .search(this.projectId(), { search: this.term(), page, pageSize: this.pageSize() })
      .subscribe({
        next: (response) => {
          this.items.set(response.items);
          this.totalRecords.set(response.totalItems);
          this.loading.set(false);
        },
        error: (error: unknown) => {
          this.items.set([]);
          this.totalRecords.set(0);
          this.loading.set(false);
          this.errorMessage.set(resolveApiError(error).message);
        }
      });
  }

  load(event: TableLazyLoadEvent): void {
    const size = (event.rows ?? 10) as 10 | 50 | 100;
    this.pageSize.set(size);
    this.search(Math.floor((event.first ?? 0) / size) + 1);
  }

  choose(item: WbsSearchItem): void {
    this.selected.emit(item.nodeId);
    this.visible.set(false);
  }
}
