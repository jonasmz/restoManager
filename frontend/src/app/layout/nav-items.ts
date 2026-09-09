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

export const NAV_GROUPS: readonly NavGroup[] = [
  {
    title: 'Principal',
    items: [
      { label: 'Panel', icon: 'ti-home', route: '/', exact: true },
      {
        label: 'Inventario',
        icon: 'ti-box-seam',
        route: '/inventory',
        roles: ['ADMIN', 'BRANCH_MANAGER', 'INVENTORY'],
      },
      {
        label: 'Reportes',
        icon: 'ti-receipt',
        route: '/reports',
        roles: ['ADMIN', 'BRANCH_MANAGER'],
      },
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
