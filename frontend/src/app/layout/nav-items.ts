/** Ítems del sidebar. Cada fase del roadmap añade su grupo/enlaces. */
export interface NavItem {
  readonly label: string;
  readonly icon: string; // clase de Tabler Icons, p. ej. 'ti-home'
  readonly route: string;
  readonly exact?: boolean;
  /** Roles que ven el ítem. Vacío/ausente = visible para cualquier usuario autenticado. */
  readonly roles?: readonly string[];
}

export interface NavGroup {
  readonly title: string;
  readonly items: readonly NavItem[];
}

const INV_ROLES = ['ADMIN', 'BRANCH_MANAGER', 'INVENTORY'] as const;
const MENU_ROLES = ['ADMIN', 'BRANCH_MANAGER'] as const;
const SALON_ROLES = ['ADMIN', 'BRANCH_MANAGER', 'WAITER'] as const;
const SALES_ROLES = ['ADMIN', 'BRANCH_MANAGER', 'WAITER'] as const;

export const NAV_GROUPS: readonly NavGroup[] = [
  {
    title: 'Principal',
    items: [
      { label: 'Panel', icon: 'ti-home', route: '/', exact: true },
      {
        label: 'Reportes',
        icon: 'ti-receipt',
        route: '/reports',
        roles: ['ADMIN', 'BRANCH_MANAGER'],
      },
    ],
  },
  {
    title: 'Inventario',
    items: [
      { label: 'Existencias', icon: 'ti-box-seam', route: '/inventory', exact: true, roles: INV_ROLES },
      { label: 'Ingredientes', icon: 'ti-carrot', route: '/inventory/ingredients', roles: INV_ROLES },
      { label: 'Movimientos', icon: 'ti-arrows-exchange', route: '/inventory/movements', roles: INV_ROLES },
    ],
  },
  {
    title: 'Compras',
    items: [
      { label: 'Proveedores', icon: 'ti-truck-delivery', route: '/purchasing/suppliers', roles: INV_ROLES },
      { label: 'Órdenes de compra', icon: 'ti-clipboard-list', route: '/purchasing/orders', roles: INV_ROLES },
    ],
  },
  {
    title: 'Salón',
    items: [
      { label: 'Tablero', icon: 'ti-layout-grid', route: '/salon/floor', roles: SALON_ROLES },
      { label: 'Mesas', icon: 'ti-armchair', route: '/salon/tables', roles: SALON_ROLES },
      { label: 'Reservas', icon: 'ti-calendar-event', route: '/salon/reservations', roles: SALON_ROLES },
    ],
  },
  {
    title: 'Ventas',
    items: [
      { label: 'Punto de venta', icon: 'ti-cash-register', route: '/pos', exact: true, roles: SALES_ROLES },
      { label: 'Pedidos', icon: 'ti-receipt-2', route: '/sales/orders', roles: SALES_ROLES },
      { label: 'Descuentos', icon: 'ti-discount', route: '/sales/discounts', roles: MENU_ROLES },
    ],
  },
  {
    title: 'Menú',
    items: [
      { label: 'Categorías', icon: 'ti-category', route: '/menu/categories', roles: MENU_ROLES },
      { label: 'Platos', icon: 'ti-tools-kitchen-2', route: '/menu/items', roles: MENU_ROLES },
    ],
  },
  {
    title: 'Cocina',
    items: [
      { label: 'Estaciones', icon: 'ti-flame', route: '/kitchen/stations', roles: MENU_ROLES },
    ],
  },
  {
    title: 'Fiscal',
    items: [
      { label: 'Tasas de impuesto', icon: 'ti-percentage', route: '/fiscal/tax-rates', roles: MENU_ROLES },
    ],
  },
  {
    title: 'Organización',
    items: [
      { label: 'Empresa', icon: 'ti-building-store', route: '/org/restaurant', roles: ['ADMIN', 'BRANCH_MANAGER'] },
      { label: 'Sucursales', icon: 'ti-map-pin', route: '/org/branches', roles: ['ADMIN', 'BRANCH_MANAGER'] },
      { label: 'Puestos', icon: 'ti-briefcase', route: '/org/roles', roles: ['ADMIN'] },
      { label: 'Departamentos', icon: 'ti-users-group', route: '/org/departments', roles: ['ADMIN', 'BRANCH_MANAGER'] },
      { label: 'Empleados', icon: 'ti-users', route: '/org/employees', roles: ['ADMIN', 'BRANCH_MANAGER'] },
    ],
  },
];
