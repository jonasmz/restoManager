import { HttpInterceptorFn } from '@angular/common/http';

/**
 * Stub de Fase 0. En fases siguientes este interceptor traduce las respuestas
 * `ProblemDetails` de las APIs a notificaciones de UI y centraliza el logging.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req);
};
