import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';

import { downloadCsv, downloadXlsx, ExportColumn } from '../../core/export/table-export';

/**
 * Par de botones "CSV / XLSX" para una tabla de reporte. Recibe las columnas y las
 * filas ya cargadas en la página; no vuelve a llamar al backend.
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
    </div>
  `,
})
export class ExportButtons<T> {
  readonly name = input.required<string>();
  readonly sheet = input<string>('Reporte');
  readonly columns = input.required<ExportColumn<T>[]>();
  readonly rows = input.required<readonly T[]>();

  protected readonly busy = signal(false);
  protected readonly empty = computed(() => this.rows().length === 0);

  protected csv(): void {
    downloadCsv(this.name(), this.columns(), this.rows());
  }

  protected async xlsx(): Promise<void> {
    this.busy.set(true);
    try {
      await downloadXlsx(this.name(), this.sheet(), this.columns(), this.rows());
    } finally {
      this.busy.set(false);
    }
  }
}
