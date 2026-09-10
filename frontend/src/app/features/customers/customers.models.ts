/** DTOs de la Business API para el módulo de Clientes y fidelización (Fase 8). */

export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export interface Customer {
  id: number;
  firstName: string;
  lastName: string;
  phone: string;
  email: string;
  loyaltyPoints: number;
}

export interface SaveCustomerBody {
  firstName: string;
  lastName: string;
  phone: string;
  email: string;
}

export interface CustomerOrder {
  id: number;
  branchId: number;
  channel: string;
  status: string;
  totalAmount: number;
  orderTime: string;
}

export interface LoyaltyBalance {
  customerId: number;
  loyaltyPoints: number;
}

export interface LoyaltyRedemptionResult {
  customerId: number;
  orderId: number;
  pointsRedeemed: number;
  remainingPoints: number;
  discountAmount: number;
  orderTotal: number;
}

export interface GiftCard {
  id: number;
  customerId: number;
  cardNumber: string;
  balance: number;
  expiryDate: string;
  expired: boolean;
}

export interface IssueGiftCardBody {
  customerId: number;
  cardNumber: string;
  initialBalance: number;
  expiryDate: string;
}

export interface Review {
  id: number;
  customerId: number;
  branchId: number;
  rating: number;
  comment: string | null;
  reviewDate: string;
}

export interface CreateReviewBody {
  customerId: number;
  rating: number;
  comment: string | null;
}
