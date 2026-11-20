import { Routes } from '@angular/router';

import { authGuard } from './core/auth/auth.guard';
import { permissionGuard } from './core/auth/permission.guard';
import { Permission } from './core/auth/permissions';
import { MainLayout } from './core/layout/main-layout';
import { unsavedChangesGuard } from './features/wbs/apu-edit/unsaved-changes.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login)
  },
  {
    path: 'registro',
    loadComponent: () => import('./features/auth/registro/registro').then((m) => m.Registro)
  },
  {
    path: 'recuperar-contrasena',
    loadComponent: () =>
      import('./features/auth/recuperar-contrasena/recuperar-contrasena').then((m) => m.RecuperarContrasena)
  },
  {
    path: '',
    component: MainLayout,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.Dashboard)
      },
      {
        path: 'proyectos',
        loadComponent: () => import('./features/proyectos/proyecto-list/proyecto-list').then((m) => m.ProyectoList)
      },
      {
        path: 'proyectos/nuevo',
        loadComponent: () => import('./features/proyectos/proyecto-form/proyecto-form').then((m) => m.ProyectoForm)
      },
      {
        path: 'proyectos/:id/edt',
        canActivate: [permissionGuard(Permission.wbsRead)],
        loadComponent: () => import('./features/wbs/edt/edt').then((m) => m.Edt)
      },
      {
        path: 'proyectos/:id/edt/items/nuevo',
        canActivate: [permissionGuard(Permission.wbsRead)],
        loadComponent: () => import('./features/wbs/apu-create/apu-create').then((m) => m.ApuCreate)
      },
      {
        path: 'proyectos/:id/edt/items/:itemId/apu',
        canActivate: [permissionGuard(Permission.apusRead)],
        canDeactivate: [unsavedChangesGuard],
        loadComponent: () => import('./features/wbs/apu-edit/apu-edit').then((m) => m.ApuEdit)
      },
      {
        path: 'proyectos/:id/insumos',
        canActivate: [permissionGuard(Permission.resourcesRead)],
        loadComponent: () =>
          import('./features/resources/resource-list/resource-list').then((m) => m.ResourceList)
      },
      {
        path: 'proyectos/:id',
        loadComponent: () => import('./features/proyectos/proyecto-detalle/proyecto-detalle').then((m) => m.ProyectoDetalle)
      },
      {
        path: 'companias',
        canActivate: [permissionGuard(Permission.companiesRead)],
        loadComponent: () => import('./features/companias/compania-list/compania-list').then((m) => m.CompaniaList)
      },
      {
        path: 'usuarios',
        canActivate: [permissionGuard(Permission.usersRead)],
        loadComponent: () => import('./features/usuarios/usuario-list/usuario-list').then((m) => m.UsuarioList)
      },
      {
        path: 'perfil',
        loadComponent: () => import('./features/perfil/perfil').then((m) => m.Perfil)
      }
    ]
  },
  { path: '**', redirectTo: 'dashboard' }
];
