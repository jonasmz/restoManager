/**
 * Configuración de entorno. En Fase 0 hay un único archivo; la Fase 1 añade
 * `environment.production.ts` y su `fileReplacements` en angular.json.
 */
export const environment = {
  production: false,
  authApiUrl: 'http://localhost:5001',
  businessApiUrl: 'http://localhost:5002',
} as const;
