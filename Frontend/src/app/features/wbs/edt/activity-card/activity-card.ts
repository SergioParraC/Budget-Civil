import { DatePipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { CardModule } from 'primeng/card';
import { MessageModule } from 'primeng/message';

import { WbsActivityResponse } from '../../../../core/models/wbs.model';

/** Tarjeta de actividad y participantes del EDT (`REQ-EDT-19`, `REQ-EDT-33`). */
@Component({
  selector: 'app-activity-card',
  imports: [CardModule, MessageModule, DatePipe],
  templateUrl: './activity-card.html',
  styleUrl: './activity-card.scss'
})
export class ActivityCard {
  readonly title = input('Actividad del EDT');
  readonly loading = input(false);
  readonly activity = input<WbsActivityResponse | null>(null);
  /** El Auditor puede recibir `403`: la tarjeta se oculta con un aviso y la pantalla sigue usable. */
  readonly hidden = input(false);
}
