import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';

import { downloadCsv, downloadXlsx, ExportColumn } from '../../core/export/table-export';

/**
 * Botones de exportación para una tabla de reporte: CSV y XLSX se arman en el
 * cliente con los datos ya cargados; PDF (opcional) delega en un callback que
 * descarga el endpoint server-side (Fase 10).
 */
@Component({
  selector: 'app-export-buttons',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="btn-group btn-group-sm">
      <button type="button" class="btn btn-outline-secondary" [disabled]="empty() || busy()"
        (click)="csv()"><i class="ti ti-file-text me-1"></i>CSV</button>
      <button type="button" class="btn btn-outline-secondary" [disabled]="empty() || busy()"
        (click)="xlsx()"><i class="ti ti-file-spreadsheet me-1"></i>XLSX</button>
      @if (pdf()) {
        <button type="button" class="btn btn-outline-secondary" [disabled]="busy()"
          (click)="runPdf()"><i class="ti ti-file-type-pdf me-1"></i>PDF</button>
      }
    </div>
  `,
})
export class ExportButtons<T> {
  readonly name = input.required<string>();
  readonly sheet = input<string>('Reporte');
  readonly columns = input.required<ExportColumn<T>[]>();
  readonly rows = input.required<readonly T[]>();
  /** Descarga el PDF server-side; ausente = sin botón PDF. */
  readonly pdf = input<(() => Promise<void>) | null>(null);

  protected readonly busy = signal(false);
  protected readonly empty = computed(() => this.rows().length === 0);

  protected csv(): void {
    downloadCsv(this.name(), this.columns(), this.rows());
  }

  protected async xlsx(): Promise<void> {
    await this.withBusy(() => downloadXlsx(this.name(), this.sheet(), this.columns(), this.rows()));
  }

  protected async runPdf(): Promise<void> {
    const fn = this.pdf();
    if (fn) {
      await this.withBusy(fn);
    }
  }

  private async withBusy(fn: () => Promise<void>): Promise<void> {
    this.busy.set(true);
    try {
      await fn();
    } finally {
      this.busy.set(false);
    }
  }
}
