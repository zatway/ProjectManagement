namespace Domain.Enums;

public enum ReportType
{
    /// <summary>
    /// PDF-отчет по шаблону ГОСТ (Акт)
    /// </summary>
    PdfAct,
    
    /// <summary>
    /// PDF-отчет по шаблону ГОСТ (Протокол)
    /// </summary>
    PdfProtocol,
    
    /// <summary>
    /// Экспорт данных в Excel для KPI
    /// </summary>
    ExcelKpi
}