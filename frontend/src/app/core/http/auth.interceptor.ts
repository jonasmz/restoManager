import { HttpInterceptorFn } from '@angular/common/http';

/**
 * Stub de Fase 0. En Fase 1 este interceptor:
 *  - añade `Authorization: Bearer <accessToken>` a las llamadas a la Business API,
 *  - ante un 401 intenta `POST /auth/refresh` una vez y reintenta,
 *  - si el refresh falla, redirige a `/auth/signin`.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req);
};
