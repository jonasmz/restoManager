import { computed, Injectable, signal } from '@angular/core';

import { CartItemInput } from './cart.types';
import { CartLine } from './catalog.models';

const KEY_PREFIX = 'rm.cart.';

/**
 * Carrito de estimación de la carta pública (Fase 11). Solo vive en el cliente: suma
 * un total estimado, no crea pedidos. Se persiste en `localStorage` por slug de carta
 * para que sobreviva a recargas.
 */
@Injectable({ providedIn: 'root' })
export class CartService {
  private slug = '';
  private readonly _lines = signal<CartLine[]>([]);

  readonly lines = this._lines.asReadonly();
  readonly count = computed(() => this._lines().reduce((n, l) => n + l.qty, 0));
  readonly total = computed(() => this._lines().reduce((sum, l) => sum + l.price * l.qty, 0));

  /** Vincula el carrito a una carta y carga su estado guardado. */
  use(slug: string): void {
    this.slug = slug;
    this._lines.set(readStored(slug));
  }

  add(item: CartItemInput): void {
    this._lines.update((lines) => {
      const existing = lines.find((l) => l.itemId === item.id);
      const next = existing
        ? lines.map((l) => (l.itemId === item.id ? { ...l, qty: l.qty + 1 } : l))
        : [...lines, { itemId: item.id, name: item.name, price: item.price, qty: 1 }];
      return next;
    });
    this.persist();
  }

  inc(itemId: number): void {
    this._lines.update((lines) => lines.map((l) => (l.itemId === itemId ? { ...l, qty: l.qty + 1 } : l)));
    this.persist();
  }

  dec(itemId: number): void {
    this._lines.update((lines) =>
      lines
        .map((l) => (l.itemId === itemId ? { ...l, qty: l.qty - 1 } : l))
        .filter((l) => l.qty > 0),
    );
    this.persist();
  }

  remove(itemId: number): void {
    this._lines.update((lines) => lines.filter((l) => l.itemId !== itemId));
    this.persist();
  }

  clear(): void {
    this._lines.set([]);
    this.persist();
  }

  private persist(): void {
    if (!this.slug) {
      return;
    }
    try {
      localStorage.setItem(KEY_PREFIX + this.slug, JSON.stringify(this._lines()));
    } catch {
      /* almacenamiento no disponible */
    }
  }
}

function readStored(slug: string): CartLine[] {
  try {
    const raw = localStorage.getItem(KEY_PREFIX + slug);
    if (!raw) {
      return [];
    }
    const parsed: unknown = JSON.parse(raw);
    if (!Array.isArray(parsed)) {
      return [];
    }
    return parsed
      .filter((l): l is CartLine =>
        !!l &&
        typeof l === 'object' &&
        typeof (l as CartLine).itemId === 'number' &&
        typeof (l as CartLine).name === 'string' &&
        typeof (l as CartLine).price === 'number' &&
        typeof (l as CartLine).qty === 'number' &&
        (l as CartLine).qty > 0);
  } catch {
    return [];
  }
}
