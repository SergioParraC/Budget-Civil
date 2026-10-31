import { Component, computed, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MenuItem } from 'primeng/api';
import { MenuModule } from 'primeng/menu';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';

import { AuthService } from '../auth/auth.service';
import { Permission } from '../auth/permissions';

@Component({
  selector: 'app-main-layout',
  imports: [RouterOutlet, MenuModule, ButtonModule, ToastModule, ConfirmDialogModule],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.scss'
})
export class MainLayout {
  private readonly authService = inject(AuthService);

  readonly currentUser = this.authService.currentUser;

  /** El menú se filtra con los permisos reales del token para no provocar 403. */
  readonly menuItems = computed<MenuItem[]>(() => {
    const items: MenuItem[] = [{ label: 'Dashboard', icon: 'pi pi-home', routerLink: '/dashboard' }];

    if (this.authService.can(Permission.projectsRead)) {
      items.push({ label: 'Proyectos', icon: 'pi pi-briefcase', routerLink: '/proyectos' });
    }

    if (this.authService.can(Permission.companiesRead)) {
      items.push({ label: 'Compañías', icon: 'pi pi-building', routerLink: '/companias' });
    }

    if (this.authService.can(Permission.usersRead)) {
      items.push({ label: 'Usuarios', icon: 'pi pi-users', routerLink: '/usuarios' });
    }

    if (this.authService.can(Permission.apuPublicRead)) {
      items.push({ label: 'APU', icon: 'pi pi-calculator', routerLink: '/apu' });
    }

    items.push({ label: 'Mi perfil', icon: 'pi pi-user', routerLink: '/perfil' });

    return items;
  });

  logout() {
    this.authService.logout();
  }
}
