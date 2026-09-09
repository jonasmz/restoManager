/** DTOs de la Business API para el módulo de Ventas (Fase 6). */

export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export const ORDER_CHANNELS = ['MESA', 'BARRA', 'TAKEAWAY', 'DELIVERY'] as const;
export type OrderChannel = (typeof ORDER_CHANNELS)[number];

export const ORDER_STATUSES = ['OPEN', 'PAID', 'CLOSED', 'CANCELLED'] as const;
export type OrderStatus = (typeof ORDER_STATUSES)[number];

export const PAYMENT_METHODS = ['CASH', 'CARD', 'TRANSFER', 'GIFT_CARD', 'OTHER'] as const;
export type PaymentMethod = (typeof PAYMENT_METHODS)[number];

export const PAYMENT_STATUSES = ['PENDING', 'CONFIRMED', 'FAILED', 'REFUNDED'] as const;
export type PaymentStatus = (typeof PAYMENT_STATUSES)[number];

export const DISCOUNT_TYPES = ['PERCENTAGE', 'FIXED_AMOUNT'] as const;
export type DiscountType = (typeof DISCOUNT_TYPES)[number];

export interface OrderItem {
  id: number;
  menuItemId: number;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  notes: string | null;
}

export interface OrderDiscount {
  id: number;
  discountId: number;
  appliedAmount: number;
}

export interface Payment {
  id: number;
  paymentMethod: PaymentMethod;
  amount: number;
  paymentTime: string;
  status: PaymentStatus;
}

export interface Order {
  id: number;
  branchId: number;
  channel: OrderChannel;
  status: OrderStatus;
  tableId: number | null;
  tableSessionId: number | null;
  customerId: number | null;
  employeeId: number;
  orderTime: string;
  itemsSubtotal: number;
  discountTotal: number;
  totalAmount: number;
  confirmedPaid: number;
  balance: number;
  items: OrderItem[];
  discounts: OrderDiscount[];
  payments: Payment[];
}

export interface Discount {
  id: number;
  name: string;
  type: DiscountType;
  value: number;
  startDate: string;
  endDate: string;
}

export interface CreateOrderBody {
  channel: OrderChannel;
  tableId?: number | null;
  tableSessionId?: number | null;
  customerId?: number | null;
}

export interface SaveDiscountBody {
  name: string;
  type: DiscountType;
  value: number;
  startDate: string;
  endDate: string;
}
