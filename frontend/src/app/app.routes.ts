import { Routes } from '@angular/router';

import { authGuard } from './core/auth/auth.guard';
import { roleGuard } from './core/auth/role.guard';

const orgRoles = roleGuard('ADMIN', 'BRANCH_MANAGER');
const invRoles = roleGuard('ADMIN', 'BRANCH_MANAGER', 'INVENTORY');
const menuRoles = roleGuard('ADMIN', 'BRANCH_MANAGER');
const salonRoles = roleGuard('ADMIN', 'BRANCH_MANAGER', 'WAITER');
const salesRoles = roleGuard('ADMIN', 'BRANCH_MANAGER', 'WAITER');
const discountRoles = roleGuard('ADMIN', 'BRANCH_MANAGER');
const deliveryRoles = roleGuard('ADMIN', 'BRANCH_MANAGER', 'WAITER');
const driverRoles = roleGuard('ADMIN', 'BRANCH_MANAGER');
const customerRoles = roleGuard('ADMIN', 'BRANCH_MANAGER', 'WAITER');

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
        path: 'salon/floor',
        canActivate: [salonRoles],
        loadComponent: () => import('./features/salon/floor-page').then((m) => m.FloorPage),
        title: 'Tablero de salón',
      },
      {
        path: 'salon/tables',
        canActivate: [salonRoles],
        loadComponent: () => import('./features/salon/tables-page').then((m) => m.TablesPage),
        title: 'Mesas',
      },
      {
        path: 'salon/reservations',
        canActivate: [salonRoles],
        loadComponent: () => import('./features/salon/reservations-page').then((m) => m.ReservationsPage),
        title: 'Reservas',
      },
      {
        path: 'pos',
        canActivate: [salesRoles],
        loadComponent: () => import('./features/sales/pos-page').then((m) => m.PosPage),
        title: 'Punto de venta',
      },
      {
        path: 'pos/order/:id',
        canActivate: [salesRoles],
        loadComponent: () => import('./features/sales/order-page').then((m) => m.OrderPage),
        title: 'Pedido',
      },
      {
        path: 'sales/orders',
        canActivate: [salesRoles],
        loadComponent: () => import('./features/sales/orders-page').then((m) => m.OrdersPage),
        title: 'Pedidos',
      },
      {
        path: 'sales/discounts',
        canActivate: [discountRoles],
        loadComponent: () => import('./features/sales/discounts-page').then((m) => m.DiscountsPage),
        title: 'Descuentos',
      },
      {
        path: 'delivery/board',
        canActivate: [deliveryRoles],
        loadComponent: () => import('./features/delivery/board-page').then((m) => m.BoardPage),
        title: 'Despacho de delivery',
      },
      {
        path: 'delivery/drivers',
        canActivate: [driverRoles],
        loadComponent: () => import('./features/delivery/drivers-page').then((m) => m.DriversPage),
        title: 'Repartidores',
      },
      {
        path: 'customers',
        canActivate: [customerRoles],
        loadComponent: () => import('./features/customers/customers-page').then((m) => m.CustomersPage),
        title: 'Clientes',
      },
      {
        path: 'customers/:id',
        canActivate: [customerRoles],
        loadComponent: () => import('./features/customers/customer-detail-page').then((m) => m.CustomerDetailPage),
        title: 'Cliente',
      },
      {
        path: 'reviews',
        canActivate: [customerRoles],
        loadComponent: () => import('./features/customers/reviews-page').then((m) => m.ReviewsPage),
        title: 'Reseñas',
      },
      {
        path: 'reports',
        canActivate: [roleGuard('ADMIN', 'BRANCH_MANAGER')],
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
    // Carta pública / QR (Fase 11): sin shell ni guard.
    path: 'carta/:slug',
    loadComponent: () => import('./features/catalog/catalog-page').then((m) => m.CatalogPage),
    title: 'Carta',
  },
  {
    path: '**',
    loadComponent: () => import('./features/not-found/not-found').then((m) => m.NotFound),
    title: 'Página no encontrada',
  },
];
