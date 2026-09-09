import { decodeJwt, JwtPayload, toStringArray } from './jwt';

export interface CurrentUser {
  readonly userId: number;
  readonly email: string;
  readonly roles: readonly string[];
  readonly employeeId: number;
  readonly branchIds: readonly number[];
}

export function currentUserFromPayload(payload: JwtPayload | null): CurrentUser | null {
  if (!payload?.sub) {
    return null;
  }
  return {
    userId: Number(payload.sub),
    email: payload.email ?? '',
    roles: toStringArray(payload.role),
    employeeId: Number(payload.employee_id ?? 0),
    branchIds: toStringArray(payload.branch_id)
      .map((b) => Number(b))
      .filter((n) => Number.isFinite(n) && n > 0),
  };
}

export function currentUserFromToken(accessToken: string): CurrentUser | null {
  return currentUserFromPayload(decodeJwt(accessToken));
}
