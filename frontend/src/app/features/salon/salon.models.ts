/** DTOs de la Business API para el módulo de Salón (Fase 5). */

export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export const TABLE_OPERATIONAL_STATUSES = ['ACTIVE', 'CLEANING', 'OUT_OF_SERVICE'] as const;
export type TableOperationalStatus = (typeof TABLE_OPERATIONAL_STATUSES)[number];

export interface Table {
  id: number;
  branchId: number;
  number: number;
  capacity: number;
  operationalStatus: TableOperationalStatus;
}

/** Estado de uso derivado (Anexo B), nunca persistido. */
export type TableDisplayStatus =
  | 'AVAILABLE'
  | 'RESERVED'
  | 'OCCUPIED'
  | 'CLEANING'
  | 'OUT_OF_SERVICE';

export interface FloorTable {
  tableId: number;
  number: number;
  capacity: number;
  operationalStatus: TableOperationalStatus;
  displayStatus: TableDisplayStatus;
  openSessionId: number | null;
  openedAt: string | null;
  nextReservationTime: string | null;
}

export const RESERVATION_STATUSES = ['PENDING', 'CONFIRMED', 'SEATED', 'CANCELLED', 'NO_SHOW'] as const;
export type ReservationStatus = (typeof RESERVATION_STATUSES)[number];

export interface Reservation {
  id: number;
  customerId: number;
  branchId: number;
  tableId: number;
  reservationTime: string;
  partySize: number;
  status: ReservationStatus;
}

export interface Customer {
  id: number;
  firstName: string;
  lastName: string;
  phone: string;
  email: string;
  loyaltyPoints: number;
}
