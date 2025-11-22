namespace Domain.Enums;

public enum ReportType
{
    /// <summary>
    /// PDF-отчет по шаблону ГОСТ (Акт)
    /// </summary>
    PdfAct,
    
    /// <summary>
    /// Экспорт данных в Excel для KPI
    /// </summary>
    ExcelKpi
}