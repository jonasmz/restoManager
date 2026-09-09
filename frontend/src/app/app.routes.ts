import { Routes } from '@angular/router';

import { authGuard } from './core/auth/auth.guard';
import { roleGuard } from './core/auth/role.guard';

const orgRoles = roleGuard('ADMIN', 'BRANCH_MANAGER');

/**
 * El shell (Layout) queda tras `authGuard`. Las páginas de módulos se añaden por
 * fase; la Fase 2 aporta las de Organización.
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
      {
        path: 'org/restaurant',
        canActivate: [orgRoles],
        loadComponent: () => import('./features/org/restaurant/restaurant-page').then((m) => m.RestaurantPage),
        title: 'Empresa',
      },
      {
        path: 'org/branches',
        canActivate: [orgRoles],
        loadComponent: () => import('./features/org/branches/branches-page').then((m) => m.BranchesPage),
        title: 'Sucursales',
      },
      {
        path: 'org/roles',
        canActivate: [roleGuard('ADMIN')],
        loadComponent: () => import('./features/org/roles/roles-page').then((m) => m.RolesPage),
        title: 'Puestos',
      },
      {
        path: 'org/departments',
        canActivate: [orgRoles],
        loadComponent: () => import('./features/org/departments/departments-page').then((m) => m.DepartmentsPage),
        title: 'Departamentos',
      },
      {
        path: 'org/employees',
        canActivate: [orgRoles],
        loadComponent: () => import('./features/org/employees/employees-page').then((m) => m.EmployeesPage),
        title: 'Empleados',
      },
      {
        path: 'org/employees/:id',
        canActivate: [orgRoles],
        loadComponent: () => import('./features/org/employees/employee-detail-page').then((m) => m.EmployeeDetailPage),
        title: 'Empleado',
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
