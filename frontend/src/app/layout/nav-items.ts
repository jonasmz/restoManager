/** Ítems del sidebar. Cada fase del roadmap añade su grupo/enlaces. */
export interface NavItem {
  readonly label: string;
  readonly icon: string; // clase de Tabler Icons, p. ej. 'ti-home'
  readonly route: string;
  readonly exact?: boolean;
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
      { label: 'Inventario', icon: 'ti-box-seam', route: '/inventory' },
      { label: 'Reportes', icon: 'ti-receipt', route: '/reports' },
    ],
  },
  {
    title: 'Cuenta',
    items: [
      { label: 'Iniciar sesión', icon: 'ti-logout', route: '/auth/signin' },
      { label: 'Registro', icon: 'ti-user-plus', route: '/auth/signup' },
    ],
  },
];
