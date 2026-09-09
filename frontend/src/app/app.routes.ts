import { Routes } from '@angular/router';

import { authGuard } from './core/auth/auth.guard';

/**
 * El shell (Layout) queda tras `authGuard`. Las páginas siguen siendo placeholder
 * hasta su fase del roadmap. El registro es solo por administrador (Fase 2), por eso
 * no hay ruta de alta pública.
 */
export const routes: Routes = [
  {
    path: '',
    canActivate: [authGuard],
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
    path: '**',
    loadComponent: () => import('./features/not-found/not-found').then((m) => m.NotFound),
    title: 'Página no encontrada',
  },
];
