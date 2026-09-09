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
];
