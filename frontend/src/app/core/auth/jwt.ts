/** Claims del access token emitido por la Auth API. */
export interface JwtPayload {
  sub?: string;
  email?: string;
  role?: string | string[];
  employee_id?: string;
  branch_id?: string | string[];
  exp?: number;
  iat?: number;
}

/** Decodifica el payload de un JWT sin verificar la firma (eso lo hace el backend). */
export function decodeJwt(token: string): JwtPayload | null {
  const parts = token.split('.');
  if (parts.length !== 3) {
    return null;
  }
  try {
    const json = atob(parts[1].replace(/-/g, '+').replace(/_/g, '/'));
    return JSON.parse(json) as JwtPayload;
  } catch {
    return null;
  }
}

/** Segundos epoch actuales. */
export function nowSeconds(): number {
  return Math.floor(Date.now() / 1000);
}

/** `true` si el token no tiene `exp` o ya expiró (con margen de 10 s). */
export function isExpired(payload: JwtPayload | null): boolean {
  return !payload?.exp || payload.exp <= nowSeconds() + 10;
}

export function toStringArray(value: string | string[] | undefined): string[] {
  if (value === undefined) {
    return [];
  }
  return Array.isArray(value) ? value : [value];
}
