/** DTOs de la Business API para el módulo de Delivery (Fase 7). */

export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export const VEHICLE_TYPES = ['MOTORCYCLE', 'BICYCLE', 'CAR', 'ON_FOOT'] as const;
export type VehicleType = (typeof VEHICLE_TYPES)[number];

export const DELIVERY_STATUSES = [
  'PENDING', 'ASSIGNED', 'IN_TRANSIT', 'DELIVERED', 'FAILED', 'CANCELLED',
] as const;
export type DeliveryStatus = (typeof DELIVERY_STATUSES)[number];

export interface Driver {
  id: number;
  employeeId: number;
  vehicleType: VehicleType;
  licensePlate: string;
}

export interface Delivery {
  id: number;
  orderId: number;
  driverId: number;
  deliveryAddress: string;
  estimatedTime: string;
  actualTime: string | null;
  status: DeliveryStatus;
  orderChannel: string;
  orderStatus: string;
  orderTime: string;
  orderTotal: number;
  customerId: number | null;
}

export interface SaveDriverBody {
  employeeId: number;
  vehicleType: VehicleType;
  licensePlate: string;
}
