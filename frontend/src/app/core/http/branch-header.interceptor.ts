import { inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';

import { environment } from '../config/environment';
import { BranchContextService } from '../branch/branch-context.service';

/**
 * Adjunta `X-Branch-Id` (sucursal activa) a las llamadas a la Business API.
 * El backend lo valida contra los claims del token.
 */
export const branchHeaderInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith(environment.businessApiUrl)) {
    return next(req);
  }

  const branchId = inject(BranchContextService).activeId();
  if (branchId === null) {
    return next(req);
  }

  return next(req.clone({ setHeaders: { 'X-Branch-Id': String(branchId) } }));
};
