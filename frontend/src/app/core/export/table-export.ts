import { Workbook } from 'exceljs';

/** Una columna exportable: encabezado visible + función que extrae el valor de la fila. */
export interface ExportColumn<T> {
  header: string;
  value: (row: T) => string | number | null | undefined;
}

function triggerDownload(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  a.remove();
  setTimeout(() => URL.revokeObjectURL(url), 0);
}

function stamp(): string {
  return new Date().toISOString().slice(0, 10);
}

function csvCell(v: string | number | null | undefined): string {
  const s = v == null ? '' : String(v);
  return /[";\n]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s;
}

/** Descarga las filas como CSV (separador `;`, BOM para Excel-es). */
export function downloadCsv<T>(name: string, columns: ExportColumn<T>[], rows: readonly T[]): void {
  const head = columns.map((c) => csvCell(c.header)).join(';');
  const body = rows.map((r) => columns.map((c) => csvCell(c.value(r))).join(';')).join('\n');
  const blob = new Blob(['﻿' + head + '\n' + body], { type: 'text/csv;charset=utf-8' });
  triggerDownload(blob, `${name}-${stamp()}.csv`);
}

/** Descarga las filas como XLSX (una hoja, encabezados en negrita). */
export async function downloadXlsx<T>(
  name: string, sheetName: string, columns: ExportColumn<T>[], rows: readonly T[],
): Promise<void> {
  const wb = new Workbook();
  const ws = wb.addWorksheet(sheetName.slice(0, 31));

  ws.columns = columns.map((c) => ({ header: c.header, key: c.header, width: Math.max(12, c.header.length + 2) }));
  ws.getRow(1).font = { bold: true };

  for (const r of rows) {
    ws.addRow(columns.map((c) => c.value(r) ?? ''));
  }

  const buffer = await wb.xlsx.writeBuffer();
  const blob = new Blob([buffer], {
    type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  });
  triggerDownload(blob, `${name}-${stamp()}.xlsx`);
}
