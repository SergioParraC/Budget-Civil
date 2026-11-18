import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TooltipModule } from 'primeng/tooltip';

import { AuthService } from '../auth/auth.service';
import { Permission } from '../auth/permissions';

interface NavItem {
  label: string;
  icon: string;
  routerLink: string;
}

interface NavGroup {
  label: string;
  items: NavItem[];
}

const COLLAPSED_STORAGE_KEY = 'budget-civil-sidebar-collapsed';

@Component({
  selector: 'app-main-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ToastModule, ConfirmDialogModule, TooltipModule],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.scss'
})
export class MainLayout {
  private readonly authService = inject(AuthService);

  readonly currentUser = this.authService.currentUser;
  readonly collapsed = signal(this.readCollapsedState());

  readonly initials = computed(() => {
    const username = this.currentUser()?.username ?? '';
    return username.slice(0, 2).toUpperCase() || '?';
  });

  /** El menú se filtra con los permisos reales del token para no provocar 403. */
  readonly navGroups = computed<NavGroup[]>(() => {
    const operacion: NavItem[] = [{ label: 'Dashboard', icon: 'pi pi-home', routerLink: '/dashboard' }];

    if (this.authService.can(Permission.projectsRead)) {
      operacion.push({ label: 'Proyectos', icon: 'pi pi-briefcase', routerLink: '/proyectos' });
    }

    if (this.authService.can(Permission.apuPublicRead)) {
      operacion.push({ label: 'APU', icon: 'pi pi-calculator', routerLink: '/apu' });
    }

    const administracion: NavItem[] = [];

    if (this.authService.can(Permission.companiesRead)) {
      administracion.push({ label: 'Compañías', icon: 'pi pi-building', routerLink: '/companias' });
    }

    if (this.authService.can(Permission.usersRead)) {
      administracion.push({ label: 'Usuarios', icon: 'pi pi-users', routerLink: '/usuarios' });
    }

    const groups: NavGroup[] = [{ label: 'Operación', items: operacion }];

    if (administracion.length > 0) {
      groups.push({ label: 'Administración', items: administracion });
    }

    return groups;
  });

  toggleCollapsed(): void {
    this.collapsed.update((value) => !value);
    localStorage.setItem(COLLAPSED_STORAGE_KEY, String(this.collapsed()));
  }

  logout() {
    this.authService.logout();
  }

  private readCollapsedState(): boolean {
    return localStorage.getItem(COLLAPSED_STORAGE_KEY) === 'true';
  }
}
