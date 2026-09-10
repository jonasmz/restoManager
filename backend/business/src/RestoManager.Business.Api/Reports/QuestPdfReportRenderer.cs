using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RestoManager.Business.Application.Reports;

namespace RestoManager.Business.Api.Reports;

/// <summary>
/// Renderiza un <see cref="ReportDocument"/> a PDF con QuestPDF (Fase 10).
/// Encabezado: restaurante, sucursal, CUIT/NIF, período y fecha de emisión.
/// Pie: número de página. Cifras y fechas en formato es-AR (ya vienen formateadas
/// en el documento).
/// </summary>
public sealed class QuestPdfReportRenderer : IReportRenderer
{
    private static readonly Color Accent = Color.FromHex("#E66239");
    private static readonly Color HeaderBg = Color.FromHex("#F1F5F9");
    private static readonly Color Line = Color.FromHex("#E2E8F0");

    public byte[] ToPdf(ReportDocument doc)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(t => t.FontSize(9).FontColor(Colors.Black));

                page.Header().Element(e => Header(e, doc));
                page.Content().PaddingVertical(12).Element(e => Content(e, doc));
                page.Footer().Element(e => Footer(e, doc));
            });
        }).GeneratePdf();
    }

    private static void Header(IContainer container, ReportDocument doc)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(doc.Header.RestaurantName).Bold().FontSize(13);
                    c.Item().Text($"Sucursal: {doc.Header.BranchName}").FontSize(9);
                    c.Item().Text($"CUIT/NIF: {doc.Header.TaxNumber}").FontSize(8).FontColor(Colors.Grey.Darken1);
                });
                row.ConstantItem(200).AlignRight().Column(c =>
                {
                    c.Item().Text(doc.Title).Bold().FontSize(12).FontColor(Accent);
                    c.Item().AlignRight().Text($"Período: {doc.PeriodLabel}").FontSize(8);
                    c.Item().AlignRight().Text($"Emitido: {doc.GeneratedAt:dd/MM/yyyy HH:mm}").FontSize(8)
                        .FontColor(Colors.Grey.Darken1);
                });
            });
            col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Accent);
        });
    }

    private static void Footer(IContainer container, ReportDocument doc)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(Line);
            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text($"{doc.Header.RestaurantName} · {doc.Title}")
                    .FontSize(7).FontColor(Colors.Grey.Darken1);
                row.ConstantItem(120).AlignRight().Text(t =>
                {
                    t.DefaultTextStyle(s => s.FontSize(7).FontColor(Colors.Grey.Darken1));
                    t.Span("Página ");
                    t.CurrentPageNumber();
                    t.Span(" de ");
                    t.TotalPages();
                });
            });
        });
    }

    private static void Content(IContainer container, ReportDocument doc)
    {
        container.Column(col =>
        {
            col.Spacing(14);

            if (doc.Kpis.Count > 0)
            {
                col.Item().Row(row =>
                {
                    row.Spacing(8);
                    foreach (var kpi in doc.Kpis)
                    {
                        row.RelativeItem().Background(HeaderBg).Padding(8).Column(c =>
                        {
                            c.Item().Text(kpi.Label).FontSize(7.5f).FontColor(Colors.Grey.Darken2);
                            c.Item().Text(kpi.Value).Bold().FontSize(11);
                        });
                    }
                });
            }

            foreach (var section in doc.Sections)
            {
                col.Item().Column(c =>
                {
                    c.Item().PaddingBottom(4).Text(section.Heading).Bold().FontSize(10).FontColor(Accent);
                    c.Item().Element(e => Table(e, section));
                });
            }

            if (doc.Kpis.Count == 0 && doc.Sections.Count == 0)
            {
                col.Item().Text("Sin datos para el período seleccionado.").Italic().FontColor(Colors.Grey.Darken1);
            }
        });
    }

    private static void Table(IContainer container, ReportSection section)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(2); // primera columna más ancha
                for (var i = 1; i < section.Columns.Count; i++)
                {
                    cols.RelativeColumn();
                }
            });

            table.Header(header =>
            {
                for (var i = 0; i < section.Columns.Count; i++)
                {
                    var cell = header.Cell().Background(HeaderBg).Padding(4);
                    var aligned = i == 0 ? cell : cell.AlignRight();
                    aligned.Text(section.Columns[i]).Bold().FontSize(8);
                }
            });

            if (section.Rows.Count == 0)
            {
                table.Cell().ColumnSpan((uint)section.Columns.Count).Padding(6)
                    .Text("Sin datos.").Italic().FontColor(Colors.Grey.Darken1);
                return;
            }

            foreach (var row in section.Rows)
            {
                for (var i = 0; i < section.Columns.Count; i++)
                {
                    var value = i < row.Count ? row[i] : string.Empty;
                    var cell = table.Cell().BorderBottom(0.5f).BorderColor(Line).Padding(4);
                    var aligned = i == 0 ? cell : cell.AlignRight();
                    aligned.Text(value).FontSize(8);
                }
            }
        });
    }
}
