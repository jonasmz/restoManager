/** DTOs de la Business API para el módulo de Menú, Cocina y Fiscal (Fase 4). */

export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export interface Category {
  id: number;
  name: string;
  description: string;
}

export interface RecipeLine {
  ingredientId: number;
  quantityRequired: number;
}

export interface MenuItem {
  id: number;
  categoryId: number;
  name: string;
  description: string;
  price: number;
  isAvailable: boolean;
  recipe: RecipeLine[];
  taxRateIds: number[];
  /** Ruta relativa a la Business API (`/media/menu/…`) o `null` (Fase 11). */
  imageUrl: string | null;
}

/** Cuerpo de alta/edición: receta e impuestos van inline (se persiste en una operación). */
export interface SaveMenuItemBody {
  categoryId: number;
  name: string;
  description: string;
  price: number;
  isAvailable: boolean;
  recipe: RecipeLine[];
  taxRateIds: number[];
}

export interface RecipeCostLine {
  ingredientId: number;
  ingredientName: string;
  quantityRequired: number;
  unitPrice: number;
  lineCost: number;
}

export interface MenuItemCost {
  menuItemId: number;
  cost: number;
  lines: RecipeCostLine[];
}

/** Disponibilidad del plato en la sucursal activa (X-Branch-Id). */
export interface MenuItemAvailability {
  menuItemId: number;
  branchId: number;
  isAvailable: boolean;
  /** true si hay fila propia en la sucursal; false si hereda de menu_items.is_available. */
  isOverride: boolean;
}

export interface KitchenStation {
  id: number;
  branchId: number;
  name: string;
  description: string;
  menuItemIds: number[];
}

export interface TaxRate {
  id: number;
  name: string;
  /** Porcentaje 0–100 (impuestos inclusivos en el precio del plato). */
  rate: number;
}
