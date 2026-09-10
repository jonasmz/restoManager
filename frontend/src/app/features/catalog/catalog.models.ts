/** DTOs de la carta pública (`GET /api/v1/public/catalog/{slug}`, Fase 11). */

export interface PublicCatalog {
  restaurant: { name: string };
  branch: { name: string; address: string };
  categories: CatalogCategory[];
  items: CatalogItem[];
}

export interface CatalogCategory {
  id: number;
  name: string;
}

export interface CatalogItem {
  id: number;
  categoryId: number;
  categoryName: string;
  name: string;
  description: string;
  price: number;
  /** Ruta relativa a la Business API (`/media/menu/…`) o `null`. */
  imageUrl: string | null;
  /** Nombres de ingredientes, sin unidades ni cantidades. */
  ingredients: string[];
}

/** Línea del carrito de estimación (solo cliente, no crea pedidos). */
export interface CartLine {
  itemId: number;
  name: string;
  price: number;
  qty: number;
}
