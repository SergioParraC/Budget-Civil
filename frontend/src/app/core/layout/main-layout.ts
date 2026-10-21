import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MenuItem } from 'primeng/api';
import { MenuModule } from 'primeng/menu';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';

import { AuthService } from '../auth/auth.service';

@Component({
  selector: 'app-main-layout',
  imports: [RouterOutlet, MenuModule, ButtonModule, ToastModule, ConfirmDialogModule],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.scss'
})
export class MainLayout {
  private readonly authService = inject(AuthService);

  readonly currentUser = this.authService.currentUser;

  readonly menuItems: MenuItem[] = [
    { label: 'Dashboard', icon: 'pi pi-home', routerLink: '/dashboard' },
    { label: 'Proyectos', icon: 'pi pi-briefcase', routerLink: '/proyectos' },
    { label: 'Compañías', icon: 'pi pi-building', routerLink: '/companias' },
    { label: 'Usuarios', icon: 'pi pi-users', routerLink: '/usuarios' },
    { label: 'APU', icon: 'pi pi-calculator', routerLink: '/apu' },
    { label: 'Mi perfil', icon: 'pi pi-user', routerLink: '/perfil' }
  ];

  logout() {
    this.authService.logout();
  }
}
