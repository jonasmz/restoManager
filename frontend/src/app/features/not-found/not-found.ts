import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

/** Portado de `requirements/inapp/src/404-error.html`. */
@Component({
  selector: 'app-not-found',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-vh-100 d-flex flex-column align-items-center justify-content-center text-center p-3">
      <img src="assets/images/logo.svg" alt="Resto Manager" height="28" class="mb-4" />
      <p class="display-1 fw-bold text-primary mb-0">404</p>
      <h1 class="h4 mb-2">Página no encontrada</h1>
      <p class="text-secondary mb-4">La ruta solicitada no existe.</p>
      <a routerLink="/" class="btn btn-primary">Ir al panel</a>
    </div>
  `,
})
export class NotFound {}
