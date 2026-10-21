import { Component, inject } from '@angular/core';
import { CardModule } from 'primeng/card';

import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-dashboard',
  imports: [CardModule],
  templateUrl: './dashboard.html'
})
export class Dashboard {
  private readonly authService = inject(AuthService);

  readonly currentUser = this.authService.currentUser;
}
