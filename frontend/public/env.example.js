// Config runtime leída por environment.ts (window.__env) antes de que arranque
// Angular. En Docker, `deploy/docker-compose.yml` regenera `env.js` (gitignored) a
// partir de las variables de entorno API_BASE_URL / AUTH_BASE_URL al levantar el
// contenedor — no lo edites a mano ahí.
//
// Para correr `ng serve` fuera de Docker, copiá este archivo a `env.js` una vez.
// Rutas relativas al origin actual (funciona detrás de nginx sin más — ver
// deploy/nginx/nginx.conf). authBaseUrl usa "/api/auth" (no "/api" a secas) por
// dos motivos: el frontend tiene su propia ruta /auth/signin (AuthService le
// agrega solo "/login" etc. después), y para no ser prefijo de las rutas de la
// Business API ("/api/v1/...") — si lo fuera, el interceptor de auth confundiría
// esas llamadas con llamadas a la Auth API y no les adjuntaría el token.
window.__env = {
  apiBaseUrl: '',
  authBaseUrl: '/api/auth',
};
