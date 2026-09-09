import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from './auth.service';

/** Restringe una ruta a los roles indicados; si no, redirige al panel. */
export function roleGuard(...roles: string[]): CanActivateFn {
  return () => {
    const auth = inject(AuthService);
    return auth.hasAnyRole(roles) ? true : inject(Router).createUrlTree(['/']);
  };
}
