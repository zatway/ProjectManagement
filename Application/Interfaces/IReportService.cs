using Application.DTOs.Input_DTO;
using Application.DTOs.Output_DTO;

namespace Application.Interfaces;

public interface IReportService
{
    /// <summary>
    /// Запускает асинхронную генерацию отчета и возвращает его метаданные.
    /// </summary>
    Task<ReportResponse> GenerateReportAsync(GenerateReportRequest request, int userId, CancellationToken cancellationToken);
    
    /// <summary>
    /// Читает сгенерированный файл с диска и возвращает его байты для скачивания.
    /// </summary>
    Task<(byte[] FileBytes, string ContentType, string FileName)> DownloadReportAsync(int reportId, CancellationToken cancellationToken);
    
    /// <summary>
    /// Внутренний метод. Выыполняет реальную генерацию и сохранение файла.
    /// </summary>
    Task GenerateAndSaveReport(int reportId);
    
    /// <summary>
    /// Получает список кратких моделей отчетов для данного проекта.
    /// </summary>
    Task<IEnumerable<ShortReportResponse>> GetShortReportsByProjectAsync(
        int projectId,
        CancellationToken cancellationToken);
}