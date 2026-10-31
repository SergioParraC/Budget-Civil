import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from './auth.service';
import { PermissionKey } from './permissions';

/** Evita abrir pantallas para las que el rol del usuario no tiene permiso en el backend. */
export const permissionGuard = (permission: PermissionKey): CanActivateFn => () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    return router.createUrlTree(['/login']);
  }

  return authService.can(permission) ? true : router.createUrlTree(['/dashboard']);
};
