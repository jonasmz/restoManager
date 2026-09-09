import { Routes } from '@angular/router';

import { authGuard } from './core/auth/auth.guard';
import { roleGuard } from './core/auth/role.guard';

const orgRoles = roleGuard('ADMIN', 'BRANCH_MANAGER');
const invRoles = roleGuard('ADMIN', 'BRANCH_MANAGER', 'INVENTORY');
const menuRoles = roleGuard('ADMIN', 'BRANCH_MANAGER');

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
        canActivate: [invRoles],
        loadComponent: () => import('./features/inventory/stock-page').then((m) => m.StockPage),
        title: 'Existencias',
      },
      {
        path: 'inventory/ingredients',
        canActivate: [invRoles],
        loadComponent: () => import('./features/inventory/ingredients-page').then((m) => m.IngredientsPage),
        title: 'Ingredientes',
      },
      {
        path: 'inventory/movements',
        canActivate: [invRoles],
        loadComponent: () => import('./features/inventory/movements-page').then((m) => m.MovementsPage),
        title: 'Movimientos de inventario',
      },
      {
        path: 'purchasing/suppliers',
        canActivate: [invRoles],
        loadComponent: () => import('./features/purchasing/suppliers-page').then((m) => m.SuppliersPage),
        title: 'Proveedores',
      },
      {
        path: 'purchasing/orders',
        canActivate: [invRoles],
        loadComponent: () => import('./features/purchasing/purchase-orders-page').then((m) => m.PurchaseOrdersPage),
        title: 'Órdenes de compra',
      },
      {
        path: 'menu/categories',
        canActivate: [menuRoles],
        loadComponent: () => import('./features/menu/categories-page').then((m) => m.CategoriesPage),
        title: 'Categorías',
      },
      {
        path: 'menu/items',
        canActivate: [menuRoles],
        loadComponent: () => import('./features/menu/menu-items-page').then((m) => m.MenuItemsPage),
        title: 'Platos',
      },
      {
        path: 'menu/items/:id',
        canActivate: [menuRoles],
        loadComponent: () => import('./features/menu/menu-item-detail-page').then((m) => m.MenuItemDetailPage),
        title: 'Plato',
      },
      {
        path: 'kitchen/stations',
        canActivate: [menuRoles],
        loadComponent: () => import('./features/kitchen/stations-page').then((m) => m.StationsPage),
        title: 'Estaciones de cocina',
      },
      {
        path: 'fiscal/tax-rates',
        canActivate: [menuRoles],
        loadComponent: () => import('./features/fiscal/tax-rates-page').then((m) => m.TaxRatesPage),
        title: 'Tasas de impuesto',
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
