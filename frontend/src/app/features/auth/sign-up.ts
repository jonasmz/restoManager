import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

/**
 * Placeholder de Fase 0 (portado de `requirements/inapp/src/signup.html`).
 * El registro real se implementa en la Fase 1.
 */
@Component({
  selector: 'app-sign-up',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-vh-100 d-flex align-items-center justify-content-center bg-light p-3">
      <div class="card shadow-sm w-100" style="max-width: 26rem">
        <div class="card-body p-4">
          <div class="text-center mb-4">
            <img src="assets/images/logo.svg" alt="Resto Manager" height="28" />
          </div>
          <h1 class="h5 mb-3">Crear cuenta</h1>
          <form>
            <div class="mb-3">
              <label class="form-label" for="name">Nombre</label>
              <input id="name" type="text" class="form-control" autocomplete="name" disabled />
            </div>
            <div class="mb-3">
              <label class="form-label" for="email">Correo</label>
              <input id="email" type="email" class="form-control" autocomplete="username" disabled />
            </div>
            <div class="mb-3">
              <label class="form-label" for="password">Contraseña</label>
              <input id="password" type="password" class="form-control" autocomplete="new-password" disabled />
            </div>
            <button type="button" class="btn btn-primary w-100" disabled>Registrarme</button>
          </form>
          <p class="small text-secondary mt-3 mb-0">
            ¿Ya tienes cuenta? <a routerLink="/auth/signin">Inicia sesión</a>
          </p>
        </div>
      </div>
    </div>
  `,
})
export class SignUp {}
