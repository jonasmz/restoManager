/** DTOs de los endpoints /api/v1/reports/* (Fase 9). Solo lectura, sucursal activa. */

export interface SalesSummary {
  totalSales: number;
  orderCount: number;
  averageTicket: number;
  paidCount: number;
  closedCount: number;
  discountTotal: number;
}

export interface SalesBucket {
  key: string;
  label: string;
  orderCount: number;
  totalSales: number;
}

export interface TopProduct {
  menuItemId: number;
  name: string;
  units: number;
  amount: number;
}

export interface PaymentMethodTotal {
  method: string;
  count: number;
  amount: number;
}

export interface DiscountApplied {
  discountId: number;
  name: string;
  timesApplied: number;
  totalAmount: number;
}

export interface LowStockLine {
  ingredientId: number;
  name: string;
  unit: string;
  stockQuantity: number;
  reorderPoint: number;
}

export interface PurchasingCost {
  totalCost: number;
  receivedOrders: number;
}

export interface TableTurnover {
  tableId: number;
  tableNumber: number;
  sessions: number;
  avgDurationMinutes: number;
  avgGuests: number;
}

export interface DashboardKpis {
  sales: number;
  orders: number;
  averageTicket: number;
  purchases: number;
  lowStockCount: number;
}

export interface DayPoint {
  date: string;
  sales: number;
  purchases: number;
}

export interface RecentSale {
  orderId: number;
  channel: string;
  status: string;
  totalAmount: number;
  orderTime: string;
}

export interface DashboardPayload {
  kpis: DashboardKpis;
  salesVsPurchase: DayPoint[];
  salesByChannel: SalesBucket[];
  topSelling: TopProduct[];
  lowStock: LowStockLine[];
  recentSales: RecentSale[];
}

export type SalesGroupBy = 'channel' | 'employee' | 'category' | 'day';

export const CHANNEL_LABELS: Record<string, string> = {
  MESA: 'Mesa', BARRA: 'Barra', TAKEAWAY: 'Para llevar', DELIVERY: 'Delivery',
};

export const METHOD_LABELS: Record<string, string> = {
  CASH: 'Efectivo', CARD: 'Tarjeta', TRANSFER: 'Transferencia', GIFT_CARD: 'Tarjeta regalo', OTHER: 'Otro',
};
