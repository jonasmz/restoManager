namespace RestoManager.Business.Application.Reports;

// Modelo de documento de reporte, agnóstico del formato de salida (Fase 10).
// La capa Api lo renderiza a PDF con QuestPDF; en el futuro podría reusarse para otros formatos.

public sealed record ReportKpi(string Label, string Value);

public sealed record ReportSection(
    string Heading,
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<string>> Rows);

public sealed record ReportDocument(
    string Title,
    ReportHeader Header,
    string PeriodLabel,
    DateTime GeneratedAt,
    IReadOnlyList<ReportKpi> Kpis,
    IReadOnlyList<ReportSection> Sections);

/// <summary>Renderizador de un <see cref="ReportDocument"/> a bytes de PDF (adaptador en la capa Api).</summary>
public interface IReportRenderer
{
    byte[] ToPdf(ReportDocument document);
}
