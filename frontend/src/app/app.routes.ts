import { Routes } from '@angular/router';

/**
 * Rutas de Fase 0: shell del template (Layout) con páginas placeholder.
 * Las rutas reales de cada módulo se añaden en su fase del roadmap.
 */
export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./layout/layout').then((m) => m.Layout),
    children: [
      {
        path: '',
        loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.Dashboard),
        title: 'Panel',
      },
      {
        path: 'inventory',
        loadComponent: () => import('./features/inventory/inventory').then((m) => m.Inventory),
        title: 'Inventario',
      },
      {
        path: 'reports',
        loadComponent: () => import('./features/reports/reports').then((m) => m.Reports),
        title: 'Reportes',
      },
    ],
  },
  {
    path: 'auth/signin',
    loadComponent: () => import('./features/auth/sign-in').then((m) => m.SignIn),
    title: 'Iniciar sesión',
  },
  {
    path: 'auth/signup',
    loadComponent: () => import('./features/auth/sign-up').then((m) => m.SignUp),
    title: 'Registro',
  },
  {
    path: '**',
    loadComponent: () => import('./features/not-found/not-found').then((m) => m.NotFound),
    title: 'Página no encontrada',
  },
];
