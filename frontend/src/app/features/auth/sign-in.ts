import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';

/** Inicio de sesión. Portado de `requirements/inapp/src/signin.html`. */
@Component({
  selector: 'app-sign-in',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-vh-100 d-flex align-items-center justify-content-center bg-light p-3">
      <div class="card shadow-sm w-100" style="max-width: 26rem">
        <div class="card-body p-4">
          <div class="text-center mb-4">
            <img src="assets/images/logo.svg" alt="Resto Manager" height="28" />
          </div>
          <h1 class="h5 mb-3">Iniciar sesión</h1>

          @if (error()) {
            <div class="alert alert-danger py-2 small" role="alert">{{ error() }}</div>
          }

          <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
            <div class="mb-3">
              <label class="form-label" for="email">Correo</label>
              <input
                id="email"
                type="email"
                class="form-control"
                formControlName="email"
                autocomplete="username"
                [class.is-invalid]="invalid('email')"
              />
              <div class="invalid-feedback">Introduce un correo válido.</div>
            </div>

            <div class="mb-3">
              <label class="form-label" for="password">Contraseña</label>
              <input
                id="password"
                type="password"
                class="form-control"
                formControlName="password"
                autocomplete="current-password"
                [class.is-invalid]="invalid('password')"
              />
              <div class="invalid-feedback">Introduce tu contraseña.</div>
            </div>

            <button type="submit" class="btn btn-primary w-100" [disabled]="submitting()">
              {{ submitting() ? 'Entrando…' : 'Entrar' }}
            </button>
          </form>
        </div>
      </div>
    </div>
  `,
})
export class SignIn {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  protected invalid(control: 'email' | 'password'): boolean {
    const c = this.form.controls[control];
    return c.invalid && (c.dirty || c.touched);
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    this.error.set(null);

    const { email, password } = this.form.getRawValue();
    this.auth.login(email, password).subscribe({
      next: () => {
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/';
        void this.router.navigateByUrl(returnUrl);
      },
      error: (err: unknown) => {
        this.submitting.set(false);
        this.error.set(
          err instanceof HttpErrorResponse && err.status === 401
            ? 'Correo o contraseña inválidos.'
            : 'No se pudo iniciar sesión. Inténtalo de nuevo.',
        );
      },
    });
  }
}
