using System.Drawing;
using System.Linq;
using Application.DTOs.Input_DTO;
using Application.DTOs.Output_DTO;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Infrastructure.Contexts;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Services;

/// <summary>
/// Конфигурация для генерации отчета.
/// </summary>
internal class ReportConfig
{
    public bool IncludeProgress { get; set; } = true;
    public bool IncludeDeadline { get; set; } = true;
    public List<int>? StageIds { get; set; }
}

public class ReportService : IReportService
{
    private readonly IDbContextFactory<ProjectManagementDbContext> _contextFactory;
    private readonly INotificationService _notificationService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<ReportService> _logger;
    private readonly string _reportsDirectory;

    public ReportService(
        IDbContextFactory<ProjectManagementDbContext> contextFactory,
        IHostEnvironment environment,
        INotificationService notificationService,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ReportService> logger)
    {
        _contextFactory = contextFactory;
        _notificationService = notificationService;
        _serviceScopeFactory = serviceScopeFactory;
        _environment = environment;
        _logger = logger;

        ExcelPackage.License.SetNonCommercialPersonal("zatway");

        // Используем переменную окружения или путь по умолчанию
        var reportsPath = Environment.GetEnvironmentVariable("REPORTS_STORAGE_PATH");
        _reportsDirectory = !string.IsNullOrWhiteSpace(reportsPath)
            ? reportsPath
            : Path.Combine(_environment.ContentRootPath, "ReportsStorage");

        if (!Directory.Exists(_reportsDirectory))
        {
            Directory.CreateDirectory(_reportsDirectory);
            _logger.LogInformation("Создана директория для отчетов: {ReportsDirectory}", _reportsDirectory);
        }
        else
        {
            _logger.LogDebug("Директория для отчетов уже существует: {ReportsDirectory}", _reportsDirectory);
        }
    }

    /// <summary>
    /// Запускает асинхронную генерацию отчета.
    /// </summary>
    public async Task<ReportResponse> GenerateReportAsync(
        GenerateReportRequest request,
        int userId,
        CancellationToken cancellationToken)
    {
        using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (!Enum.TryParse<ReportType>(request.ReportType, true, out var reportTypeEnum))
        {
            throw new ArgumentException(
                $"Тип отчета '{request.ReportType}' не является корректным значением для ReportType.");
        }

        var project = await context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProjectId == request.ProjectId, cancellationToken);

        if (project == null)
        {
            throw new KeyNotFoundException($"Проект с ID {request.ProjectId} не найден.");
        }

        var stageIdsToInclude = new List<int>();
        if (request.StageIds != null && request.StageIds.Any())
        {
            stageIdsToInclude = request.StageIds;
        }
        else if (request.StageId.HasValue)
        {
            stageIdsToInclude.Add(request.StageId.Value);
        }

        // Объединяем StageIds с существующей конфигурацией
        var reportConfigDict = new Dictionary<string, object>();
        if (!string.IsNullOrWhiteSpace(request.ReportConfig))
        {
            try
            {
                var existingConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(request.ReportConfig);
                if (existingConfig != null)
                {
                    foreach (var kvp in existingConfig)
                    {
                        reportConfigDict[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch
            {
            }
        }

        // Добавляем StageIds в конфигурацию
        if (stageIdsToInclude.Any())
        {
            reportConfigDict["StageIds"] = stageIdsToInclude;
        }

        var finalReportConfig = reportConfigDict.Any()
            ? JsonSerializer.Serialize(reportConfigDict)
            : request.ReportConfig;

        var newReport = new Report
        {
            ProjectId = request.ProjectId,
            StageId = request.StageId,
            ReportType = reportTypeEnum,
            Status = ReportStatus.Pending,
            GeneratedAt = DateTime.UtcNow,
            GeneratedByUserId = userId,
            ReportConfig = finalReportConfig,
            TargetFileName = request.TargetFileName,
            FilePath = null
        };

        await context.Reports.AddAsync(newReport, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        try
        {
            await _notificationService.CreateAndSendNotificationAsync(
                userId,
                request.ProjectId,
                $"Создан отчет '{reportTypeEnum}' по проекту '{project.Name}'. Генерация началась.",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось отправить уведомление о создании отчета {ReportId}", newReport.ReportId);
        }

        _ = Task.Run(async () => { await GenerateAndSaveReport(newReport.ReportId, _serviceScopeFactory); });

        return new ReportResponse
        {
            ReportId = newReport.ReportId,
            ProjectId = newReport.ProjectId,
            ReportType = newReport.ReportType.ToString(),
            Status = newReport.Status.ToString(),
            GeneratedAt = newReport.GeneratedAt,
            ProjectName = project.Name
        };
    }

    /// <summary>
    /// Внутренний метод: выполняет генерацию и сохранение файла.
    /// </summary>
    public async Task GenerateAndSaveReport(int reportId, IServiceScopeFactory serviceScopeFactory)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var report = await context.Reports
            .Include(r => r.Project).ThenInclude(p => p.Stages)
            .Include(r => r.GeneratedBy)
            .FirstOrDefaultAsync(r => r.ReportId == reportId);

        if (report == null)
        {
            _logger?.LogWarning("Отчёт с ID {ReportId} не найден для генерации.", reportId);
            return;
        }

        if (report.Status == ReportStatus.Complete || report.Status == ReportStatus.Failed)
        {
            _logger?.LogInformation("Отчёт {ReportId} уже обработан (статус: {Status}).", reportId, report.Status);
            return;
        }

        report.Status = ReportStatus.InProgress;
        await context.SaveChangesAsync();

        // Уведомление о начале генерации
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            await notificationService.CreateAndSendNotificationAsync(
                report.GeneratedByUserId,
                report.ProjectId,
                $"Генерация отчета '{report.ReportType}' по проекту '{report.Project.Name}' началась.",
                CancellationToken.None);

            _logger.LogInformation("Уведомление о начале генерации отчета {ReportId} отправлено", reportId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось отправить уведомление о начале генерации отчета {ReportId}", reportId);
        }

        try
        {
            string fileExtension;
            byte[] fileBytes;

            ReportConfig config;
            if (report.ReportConfig is null)
            {
                config = new ReportConfig();
            }
            else
            {
                try
                {
                    var configDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(report.ReportConfig);
                    config = new ReportConfig
                    {
                        IncludeProgress = configDict?.ContainsKey("IncludeProgress") == true &&
                                          configDict["IncludeProgress"].ValueKind == JsonValueKind.True,
                        IncludeDeadline = configDict?.ContainsKey("IncludeDeadline") == true &&
                                          configDict["IncludeDeadline"].ValueKind == JsonValueKind.True,
                        StageIds = configDict?.ContainsKey("StageIds") == true
                            ? JsonSerializer.Deserialize<List<int>>(configDict["StageIds"].GetRawText())
                            : null
                    };
                }
                catch
                {
                    config = new ReportConfig();
                }
            }

            switch (report.ReportType)
            {
                case ReportType.PdfAct:
                    _logger.LogDebug("Начало генерации PDF отчета {ReportId}", reportId);
                    fileBytes = GeneratePdfAct(report, config);
                    _logger.LogDebug("PDF отчет {ReportId} успешно сгенерирован, размер: {Size} байт", reportId,
                        fileBytes.Length);
                    fileExtension = "pdf";
                    break;
                case ReportType.ExcelKpi:
                    _logger.LogDebug("Начало генерации Excel отчета {ReportId}", reportId);
                    fileBytes = await GenerateExcelKpiAsync(report, config, context);
                    _logger.LogDebug("Excel отчет {ReportId} успешно сгенерирован, размер: {Size} байт", reportId,
                        fileBytes.Length);
                    fileExtension = "xlsx";
                    break;
                default:
                    throw new NotSupportedException($"Тип отчета {report.ReportType} не поддерживается.");
            }

            string baseFileName;

            if (!string.IsNullOrWhiteSpace(report.TargetFileName))
            {
                baseFileName = Path.GetFileNameWithoutExtension(report.TargetFileName);
            }
            else
            {
                baseFileName = $"{report.Project.Name}_{report.ReportType.ToString()}";
            }

            string fileName = $"{baseFileName}_{report.ReportId}.{fileExtension}";
            string fullPath = Path.Combine(_reportsDirectory, fileName);

            await File.WriteAllBytesAsync(fullPath, fileBytes);

            report.FilePath = fullPath;
            report.Status = ReportStatus.Complete;
            await context.SaveChangesAsync();

            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                await notificationService.CreateAndSendNotificationAsync(
                    report.GeneratedByUserId,
                    report.ProjectId,
                    $"Отчет '{report.ReportType}' по проекту '{report.Project.Name}' готов к скачиванию.",
                    CancellationToken.None);

                _logger.LogInformation("Уведомление о готовности отчета {ReportId} отправлено", reportId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось отправить уведомление о готовности отчета {ReportId}", reportId);
            }

            _logger.LogInformation("Отчёт {ReportId} успешно сгенерирован: {FilePath}", reportId, fullPath);
        }
        catch (Exception ex)
        {
            report.Status = ReportStatus.Failed;
            report.FilePath = null;
            await context.SaveChangesAsync();

            // Уведомление об ошибке
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                await notificationService.CreateAndSendNotificationAsync(
                    report.GeneratedByUserId,
                    report.ProjectId,
                    $"Ошибка генерации отчета '{report.ReportType}' по проекту '{report.Project.Name}': {ex.Message}",
                    CancellationToken.None);

                _logger.LogInformation("Уведомление об ошибке генерации отчета {ReportId} отправлено", reportId);
            }
            catch (Exception notifyEx)
            {
                _logger.LogError(notifyEx, "Не удалось отправить уведомление об ошибке генерации отчета {ReportId}",
                    reportId);
            }

            _logger.LogError(ex,
                "Ошибка генерации отчёта {ReportId}: {Message}. Тип отчета: {ReportType}. StackTrace: {StackTrace}",
                reportId,
                ex.Message,
                report.ReportType,
                ex.StackTrace);
        }
        finally
        {
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Получает список кратких моделей отчетов для данного проекта.
    /// </summary>
    public async Task<IEnumerable<ShortReportResponse>> GetShortReportsByProjectAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var projectExists = await context.Projects
            .AnyAsync(p => p.ProjectId == projectId, cancellationToken);

        if (!projectExists)
        {
            throw new KeyNotFoundException($"Проект с ID {projectId} не найден.");
        }

        var reports = await context.Reports
            .AsNoTracking()
            .Include(r => r.Project)
            .Where(r => r.ProjectId == projectId)
            .OrderByDescending(r => r.GeneratedAt)
            .Select(r => new ShortReportResponse
            {
                ReportId = r.ReportId,
                ProjectName = r.Project.Name,
                ReportType = r.ReportType.ToString(),
                Status = r.Status.ToString(),
                GeneratedAt = r.GeneratedAt,
                TargetFileName = r.TargetFileName
            })
            .ToListAsync(cancellationToken);

        return reports;
    }

    /// <summary>
    /// Генерирует PDF-документ акта сдачи-приемки работ.
    /// </summary>
    private byte[] GeneratePdfAct(Report report, ReportConfig config)
    {
        if (report.GeneratedBy == null)
        {
            throw new InvalidOperationException(
                $"Не удалось загрузить данные пользователя для отчета {report.ReportId}");
        }

        if (report.Project == null)
        {
            throw new InvalidOperationException($"Не удалось загрузить данные проекта для отчета {report.ReportId}");
        }

        using var ms = new MemoryStream();

        using (var writer = new PdfWriter(ms))
        {
            writer.SetSmartMode(true);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);

            // Настройка шрифтов
            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // Заголовок документа
            document.Add(new Paragraph("АКТ СДАЧИ-ПРИЕМКИ РАБОТ")
                .SetFont(boldFont)
                .SetFontSize(18)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(20)
                .SetMarginBottom(5));

            document.Add(new Paragraph($"№ {report.ReportId}")
                .SetFont(boldFont)
                .SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(5));

            document.Add(new Paragraph($"от {report.GeneratedAt.ToString("dd.MM.yyyy")} г.")
                .SetFont(regularFont)
                .SetFontSize(12)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(30));

            // Город
            document.Add(new Paragraph("г. Москва")
                .SetFont(regularFont)
                .SetFontSize(12)
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetMarginBottom(20));

            // Исполнитель
            document.Add(new Paragraph($"Исполнитель: {report.GeneratedBy.FullName}")
                .SetFont(regularFont)
                .SetFontSize(12)
                .SetMarginLeft(30)
                .SetMarginBottom(15));

            // Преамбула
            document.Add(new Paragraph("составили настоящий Акт о том, что Исполнитель выполнил работы по проекту:")
                .SetFont(regularFont)
                .SetFontSize(12)
                .SetFirstLineIndent(30)
                .SetMarginBottom(10));

            // Название проекта
            document.Add(new Paragraph($"«{report.Project.Name}»")
                .SetFont(boldFont)
                .SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(10)
                .SetMarginBottom(25));

            // Таблица этапов
            var table = new Table(UnitValue.CreatePercentArray(new float[] { 8, 50, 20, 22 }))
                .UseAllAvailableWidth()
                .SetMarginBottom(25);

            // Заголовки таблицы
            var headerCellStyle = new Style()
                .SetFont(boldFont)
                .SetFontSize(11)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetPadding(8);

            table.AddHeaderCell(new Cell().Add(new Paragraph("№").SetFont(boldFont)).AddStyle(headerCellStyle));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Наименование этапа (работы)").SetFont(boldFont))
                .AddStyle(headerCellStyle));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Статус").SetFont(boldFont)).AddStyle(headerCellStyle));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Срок сдачи").SetFont(boldFont))
                .AddStyle(headerCellStyle));

            // Данные этапов с фильтрацией по StageIds
            var stages = report.Project.Stages?.AsEnumerable() ?? Enumerable.Empty<Stage>();

            if (config.StageIds != null && config.StageIds.Any())
            {
                stages = stages.Where(s => config.StageIds.Contains(s.StageId));
            }

            var stagesList = stages.OrderBy(s => s.StageId).ToList();

            if (!stagesList.Any())
            {
                _logger.LogWarning("Не найдено этапов для включения в PDF отчет {ReportId}. StageIds: {StageIds}",
                    report.ReportId,
                    config.StageIds != null ? string.Join(", ", config.StageIds) : "не указаны");
            }

            int i = 1;
            foreach (var stage in stagesList)
            {
                var cellStyle = new Style()
                    .SetFont(regularFont)
                    .SetFontSize(10)
                    .SetPadding(6);

                table.AddCell(new Cell().Add(new Paragraph(i.ToString())).AddStyle(cellStyle)
                    .SetTextAlignment(TextAlignment.CENTER));

                table.AddCell(new Cell().Add(new Paragraph(stage.Name)).AddStyle(cellStyle));

                table.AddCell(new Cell().Add(new Paragraph(stage.Status.ToString())).AddStyle(cellStyle)
                    .SetTextAlignment(TextAlignment.CENTER));

                if (config.IncludeDeadline)
                {
                    table.AddCell(new Cell().Add(new Paragraph(stage.Deadline.ToString("dd.MM.yyyy")))
                        .AddStyle(cellStyle)
                        .SetTextAlignment(TextAlignment.CENTER));
                }
                else
                {
                    table.AddCell(new Cell().Add(new Paragraph("—")).AddStyle(cellStyle)
                        .SetTextAlignment(TextAlignment.CENTER));
                }

                i++;
            }

            document.Add(table);

            // Заключение
            document.Add(
                new Paragraph(
                        "Работы выполнены в полном объеме и соответствуют техническому заданию. Стороны претензий не имеют.")
                    .SetFont(regularFont)
                    .SetFontSize(12)
                    .SetFirstLineIndent(30)
                    .SetMarginTop(30)
                    .SetMarginBottom(50));

            // Подписи
            var signatureStyle = new Style()
                .SetFont(regularFont)
                .SetFontSize(12);

            document.Add(new Paragraph("Заказчик:")
                .SetFont(regularFont)
                .SetFontSize(12)
                .SetMarginLeft(50)
                .SetMarginBottom(40));

            document.Add(new Paragraph("___________________ / (Подпись)")
                .SetFont(regularFont)
                .SetFontSize(11)
                .SetMarginLeft(50)
                .SetMarginBottom(20));

            document.Add(new Paragraph("Исполнитель:")
                .SetFont(regularFont)
                .SetFontSize(12)
                .SetMarginLeft(350)
                .SetMarginTop(-60)
                .SetMarginBottom(40));

            document.Add(new Paragraph("___________________ / (Подпись)")
                .SetFont(regularFont)
                .SetFontSize(11)
                .SetMarginLeft(350));
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Генерирует Excel-файл с отчетом по ключевым показателям проекта.
    /// </summary>
    private async Task<byte[]> GenerateExcelKpiAsync(Report report, ReportConfig config,
        ProjectManagementDbContext context)
    {
        try
        {
            if (report.Project == null)
            {
                throw new InvalidOperationException(
                    $"Не удалось загрузить данные проекта для отчета {report.ReportId}");
            }

            _logger.LogInformation(
                "Начало генерации Excel отчета {ReportId}. ProjectId: {ProjectId}, StageIds: {StageIds}",
                report.ReportId,
                report.ProjectId,
                config.StageIds != null ? string.Join(", ", config.StageIds) : "не указаны");

            var stagesQuery = context.Stages
                .AsNoTracking()
                .Where(s => s.ProjectId == report.ProjectId);

            // Фильтрация по StageIds, если указаны
            if (config.StageIds != null && config.StageIds.Any())
            {
                _logger.LogDebug("Применяется фильтрация по StageIds: {StageIds}", string.Join(", ", config.StageIds));
                stagesQuery = stagesQuery.Where(s => config.StageIds.Contains(s.StageId));
            }

            var stages = await stagesQuery
                .OrderBy(s => s.StageId)
                .Select(s => new { s.StageId, s.Name, s.ProgressPercent, s.Deadline, s.Status })
                .ToListAsync();

            _logger.LogDebug("Найдено этапов для Excel отчета {ReportId}: {Count}", report.ReportId, stages.Count);

            if (!stages.Any())
            {
                _logger.LogWarning("Не найдено этапов для включения в Excel отчет {ReportId}. StageIds: {StageIds}",
                    report.ReportId,
                    config.StageIds != null ? string.Join(", ", config.StageIds) : "не указаны");
            }

            _logger.LogDebug("Создание Excel пакета для отчета {ReportId}", report.ReportId);
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("KPI Summary");

            // --- 1. СТИЛИЗАЦИЯ (для красоты) ---
            var headerStyle = worksheet.Workbook.Styles.CreateNamedStyle("HeaderStyle");
            headerStyle.Style.Font.Bold = true;
            headerStyle.Style.Fill.PatternType = ExcelFillStyle.Solid;
            headerStyle.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            headerStyle.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            headerStyle.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            headerStyle.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            headerStyle.Style.Border.Right.Style = ExcelBorderStyle.Thin;

            // --- 2. ШАПКА ОТЧЕТА ---
            worksheet.Cells["A1"].Value = "Отчет по ключевым показателям проекта (KPI)";
            worksheet.Cells["A1:D1"].Merge = true;
            worksheet.Cells["A1"].Style.Font.Size = 16;
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            worksheet.Cells["A3"].Value = "Проект:";
            worksheet.Cells["B3"].Value = report.Project.Name;
            worksheet.Cells["A4"].Value = "Дата генерации:";
            worksheet.Cells["B4"].Value = report.GeneratedAt.ToString("dd.MM.yyyy");

            // --- 3. ТАБЛИЦА ДАННЫХ ---
            int startRow = 6;
            int col = 1;

            // Заголовки
            worksheet.Cells[startRow, col++].Value = "ID";
            worksheet.Cells[startRow, col++].Value = "Название этапа";
            worksheet.Cells[startRow, col++].Value = "Статус";

            // 💡 Динамическое формирование заголовков на основе конфигурации
            if (config.IncludeProgress)
            {
                worksheet.Cells[startRow, col++].Value = "Прогресс, %";
            }

            if (config.IncludeDeadline)
            {
                worksheet.Cells[startRow, col++].Value = "Плановая дата";
            }

            // Применяем стиль к заголовкам
            worksheet.Cells[startRow, 1, startRow, col - 1].StyleName = "HeaderStyle";

            // Заполнение данными
            int row = startRow + 1;
            foreach (var stage in stages)
            {
                col = 1;
                worksheet.Cells[row, col++].Value = stage.StageId;
                worksheet.Cells[row, col++].Value = stage.Name;
                worksheet.Cells[row, col++].Value = stage.Status.ToString();

                if (config.IncludeProgress)
                {
                    worksheet.Cells[row, col].Value = stage.ProgressPercent / 100.0;
                    worksheet.Cells[row, col++].Style.Numberformat.Format = "0.00%";
                }

                if (config.IncludeDeadline)
                {
                    worksheet.Cells[row, col].Value = stage.Deadline;
                    worksheet.Cells[row, col++].Style.Numberformat.Format = "dd.mm.yyyy";
                }

                row++;
            }

            if (worksheet.Dimension != null)
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            else
            {
                _logger.LogWarning("Лист пуст для отчета {ReportId}, пропускаем AutoFitColumns", report.ReportId);
            }

            _logger.LogInformation("Excel пакет для отчета {ReportId} подготовлен, получение байтов", report.ReportId);
            var result = package.GetAsByteArray();
            _logger.LogInformation("Excel отчет {ReportId} успешно сгенерирован, размер: {Size} байт", report.ReportId,
                result.Length);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Ошибка при генерации Excel отчета {ReportId}: {Message}. StackTrace: {StackTrace}",
                report.ReportId,
                ex.Message,
                ex.StackTrace);
            throw;
        }
    }

    /// <summary>
    /// Скачивает готовый файл отчета по его идентификатору.
    /// </summary>
    /// <param name="reportId">Идентификатор отчета.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Кортеж с байтами файла, типом контента и именем файла.</returns>
    /// <exception cref="KeyNotFoundException">Отчет или файл не найден.</exception>
    /// <exception cref="InvalidOperationException">Отчет не готов к скачиванию.</exception>
    public async Task<(byte[] FileBytes, string ContentType, string FileName)> DownloadReportAsync(
        int reportId,
        CancellationToken cancellationToken)
    {
        using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var report = await context.Reports
            .AsNoTracking()
            .Include(r => r.Project)
            .FirstOrDefaultAsync(r => r.ReportId == reportId, cancellationToken);

        if (report == null)
        {
            throw new KeyNotFoundException($"Отчет ID {reportId} не найден.");
        }

        if (report.Status != ReportStatus.Complete || string.IsNullOrEmpty(report.FilePath))
        {
            throw new InvalidOperationException(
                $"Отчет ID {reportId} находится в статусе '{report.Status}', скачивание невозможно.");
        }

        // 💡 Реальное чтение файла с диска
        if (!File.Exists(report.FilePath))
        {
            _logger?.LogWarning("Файл отчёта {ReportId} не найден: {FilePath}", reportId, report.FilePath);
            throw new KeyNotFoundException($"Файл отчета не найден по пути: {report.FilePath}");
        }

        byte[] fileBytes = await File.ReadAllBytesAsync(report.FilePath, cancellationToken);

        string contentType;

        if (report.ReportType == ReportType.PdfAct)
        {
            contentType = "application/pdf";
        }
        else if (report.ReportType == ReportType.ExcelKpi)
        {
            contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        }
        else
        {
            contentType = "application/octet-stream";
        }

        // Возвращаем имя файла, которое будет отображаться у пользователя
        string fileName = Path.GetFileName(report.FilePath);

        _logger?.LogInformation("Отчёт {ReportId} успешно скачан.", reportId);
        return (fileBytes, contentType, fileName);
    }
}