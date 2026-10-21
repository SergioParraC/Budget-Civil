import { Component, inject } from '@angular/core';
import { CardModule } from 'primeng/card';

import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-perfil',
  imports: [CardModule],
  templateUrl: './perfil.html'
})
export class Perfil {
  private readonly authService = inject(AuthService);

  readonly currentUser = this.authService.currentUser;
}
