import { HttpErrorResponse } from '@angular/common/http';

/** Extrae un mensaje legible de un error `ProblemDetails` de la Business API. */
export function apiErrorMessage(err: unknown): string {
  if (err instanceof HttpErrorResponse) {
    const body = err.error;
    if (body && typeof body === 'object') {
      const errors = (body as { errors?: Record<string, string[]> }).errors;
      if (errors) {
        return Object.values(errors).flat().join(' · ');
      }
      const title = (body as { title?: string }).title;
      if (title) {
        return title;
      }
    }
    if (err.status === 403) {
      return 'No tienes permiso para esta acción.';
    }
  }
  return 'No se pudo completar la operación.';
}
