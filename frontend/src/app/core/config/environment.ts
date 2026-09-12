/**
 * Configuración de entorno. En Fase 0 hay un único archivo; la Fase 1 añade
 * `environment.production.ts` y su `fileReplacements` en angular.json.
 *
 * Las URLs de las APIs se leen de `window.__env` (inyectada en runtime por
 * `public/env.js`, ver `deploy/docker-compose.yml` y `deploy/nginx/nginx.conf`) en
 * vez de compilarse fijas en el bundle: el mismo build de Angular sirve para
 * cualquier host/dominio sin recompilar.
 *
 * Por defecto las llamadas son relativas al origin actual — nginx enruta por path
 * (`/api/auth` → Auth API; `/api/v1`, `/media` → Business API; el resto → frontend)
 * bajo un único origin, así que funciona sin configurar nada desde `localhost`, la
 * IP LAN del host o cualquier dominio que apunte a nginx (ver
 * `deploy/nginx/nginx.conf`). `authApiUrl` por defecto es `/api/auth` (no vacío,
 * y con el segmento `/auth` incluido) por dos motivos: el frontend tiene su propia
 * ruta `/auth/signin` (nginx expone la Auth API en `/api/auth/` para no pisarla,
 * `AuthService` le agrega solo `/login` etc.), y además `/api/auth` no es prefijo
 * de las rutas de la Business API (`/api/v1/...`) — si lo fuera, `authInterceptor`
 * (que compara con `startsWith`) confundiría las llamadas a la Business API con
 * llamadas a la Auth API y les saltearía el Bearer token. Solo hace falta un valor
 * absoluto en `apiBaseUrl`/`authBaseUrl` cuando el frontend se sirve desde un origin
 * distinto al de las APIs (p. ej. sin nginx delante).
 */
declare global {
  interface Window {
    __env?: { apiBaseUrl?: string; authBaseUrl?: string };
  }
}

const runtimeEnv = typeof window !== 'undefined' ? window.__env : undefined;

export const environment = {
  production: false,
  authApiUrl: runtimeEnv?.authBaseUrl ?? '/api/auth',
  businessApiUrl: runtimeEnv?.apiBaseUrl ?? '',
} as const;
