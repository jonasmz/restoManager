export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export interface Ingredient {
  id: number;
  name: string;
  unit: string;
  unitPrice: number;
}

export interface StockLine {
  ingredientId: number;
  ingredientName: string;
  unit: string;
  stockQuantity: number;
  unitPrice: number;
  stockValue: number;
}

export interface Movement {
  id: number;
  ingredientId: number;
  movementType: string;
  quantity: number;
  movementTime: string;
  referenceType: string | null;
  referenceId: number | null;
  employeeId: number | null;
}

export interface Supplier {
  id: number;
  name: string;
  contactName: string;
  phone: string;
  email: string;
  address: string;
}

export interface PurchaseOrderLine {
  ingredientId: number;
  quantity: number;
  unitPrice: number;
}

export interface PurchaseOrder {
  id: number;
  supplierId: number;
  branchId: number;
  orderDate: string;
  totalAmount: number;
  status: string;
  items: PurchaseOrderLine[];
}

export const MOVEMENT_TYPES = ['PURCHASE', 'SALE', 'WASTE', 'ADJUSTMENT'] as const;
