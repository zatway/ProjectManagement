using Application.DTOs.Input_DTO;
using Application.DTOs.Output_DTO;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.IO; 
using System.Text.Json; 
using System.Threading;
using System.Threading.Tasks;
using System; 
using System.Linq;
using Infrastructure.Contexts;
using OfficeOpenXml;
using OfficeOpenXml.Style; 
using iText.Kernel.Pdf; 
using iText.Layout;
using iText.Layout.Element; 
using iText.Layout.Properties;
using iText.IO.Font;
using Microsoft.AspNetCore.Hosting;

namespace Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly ProjectManagementDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly string _reportsDirectory;

    public ReportService(ProjectManagementDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;

        _reportsDirectory = Path.Combine(_environment.ContentRootPath, "ReportsStorage");

        if (!Directory.Exists(_reportsDirectory))
        {
            Directory.CreateDirectory(_reportsDirectory);
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
        cancellationToken.ThrowIfCancellationRequested();

        if (!Enum.TryParse<ReportType>(request.ReportType, true, out var reportTypeEnum))
        {
            throw new ArgumentException(
                $"Тип отчета '{request.ReportType}' не является корректным значением для ReportType.");
        }

        var project = await _context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProjectId == request.ProjectId, cancellationToken);

        if (project == null)
        {
            throw new KeyNotFoundException($"Проект с ID {request.ProjectId} не найден.");
        }
        
        var newReport = new Report
        {
            ProjectId = request.ProjectId,
            StageId = request.StageId,
            ReportType = reportTypeEnum,
            Status = ReportStatus.Pending,
            GeneratedAt = DateTime.UtcNow,
            GeneratedByUserId = userId,
            ReportConfig = request.ReportConfig,
            TargetFileName = request.TargetFileName
        };

        await _context.Reports.AddAsync(newReport, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _ = Task.Run(async () => { await GenerateAndSaveReport(newReport.ReportId); });

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
    public async Task GenerateAndSaveReport(int reportId)
    {
        var report = await _context.Reports
            .Include(r => r.Project).ThenInclude(p => p.Stages)
            .Include(r => r.GeneratedBy)
            .FirstOrDefaultAsync(r => r.ReportId == reportId);

        if (report == null) return;

        report.Status = ReportStatus.InProgress;
        // Используем новый контекст для сохранения
        await _context.SaveChangesAsync();

        try
        {
            string fileExtension;
            byte[] fileBytes;

            var config = report.ReportConfig is null
                ? new { IncludeProgress = true, IncludeDeadline = true }
                : JsonSerializer.Deserialize<dynamic>(report.ReportConfig) ??
                  new { IncludeProgress = true, IncludeDeadline = true };

            switch (report.ReportType)
            {
                case ReportType.PdfAct:
                    fileBytes = GeneratePdfAct(report, config);
                    fileExtension = "pdf";
                    break;
                case ReportType.ExcelKpi:
                    fileBytes = await GenerateExcelKpiAsync(report, config);
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

            // Добавляем ID отчета, чтобы гарантировать уникальность
            string fileName = $"{baseFileName}_{report.ReportId}.{fileExtension}";
            
            string fullPath = Path.Combine(_reportsDirectory, fileName);

            await File.WriteAllBytesAsync(fullPath, fileBytes);

            report.FilePath = fullPath;
            report.Status = ReportStatus.Complete;
        }
        catch (Exception ex)
        {
            report.Status = ReportStatus.Failed;
            report.FilePath = null;
        }
        finally
        {
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Получает список кратких моделей отчетов для данного проекта.
    /// </summary>
    public async Task<IEnumerable<ShortReportResponse>> GetShortReportsByProjectAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        var projectExists = await _context.Projects
            .AnyAsync(p => p.ProjectId == projectId, cancellationToken);

        if (!projectExists)
        {
            throw new KeyNotFoundException($"Проект с ID {projectId} не найден.");
        }

        var reports = await _context.Reports
            .AsNoTracking()
            .Where(r => r.ProjectId == projectId)
            .OrderByDescending(r => r.GeneratedAt) // Сортируем по дате генерации
            .Select(r => new ShortReportResponse
            {
                ReportId = r.ReportId,
                ProjectName = r.Project.Name, // Навигационное свойство доступно
                ReportType = r.ReportType.ToString(),
                Status = r.Status.ToString(),
                GeneratedAt = r.GeneratedAt,
                TargetFileName = r.TargetFileName
            })
            .ToListAsync(cancellationToken);

        return reports;
    }
    private byte[] GeneratePdfAct(Report report, dynamic config)
    {
        using var ms = new MemoryStream();

        using (var writer = new PdfWriter(ms))
        {
            writer.SetSmartMode(true);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);

            document.Add(new Paragraph("_________________________________").SetFontSize(10)
                .SetTextAlignment(TextAlignment.CENTER));
            document.Add(new Paragraph($"АКТ СДАЧИ-ПРИЕМКИ РАБОТ №{report.ReportId}").SetFontSize(16).SimulateBold()
                .SetTextAlignment(TextAlignment.CENTER).SetMarginTop(20));
            document.Add(new Paragraph($"от {report.GeneratedAt:«dd» MMMM yyyy г.}").SetFontSize(12)
                .SetTextAlignment(TextAlignment.CENTER).SetMarginBottom(30));

            document.Add(new Paragraph("г. Москва").SetFontSize(12)
                .SetTextAlignment(TextAlignment.RIGHT).SetMarginBottom(10));

            document.Add(new Paragraph($"\t— Исполнитель: **{report.GeneratedBy.FullName}**")
                .SetFontSize(12).SetMarginLeft(30).SetMarginBottom(20));

            document.Add(new Paragraph($"составили настоящий Акт о том, что Исполнитель выполнил работы по проекту:")
                .SetFontSize(12).SetFirstLineIndent(30));

            document.Add(new Paragraph($"«**{report.Project.Name}**»").SetFontSize(14).SimulateBold()
                .SetTextAlignment(TextAlignment.CENTER).SetMarginTop(10).SetMarginBottom(20));

            var table = new iText.Layout.Element.Table(UnitValue.CreatePercentArray(new float[] { 5, 55, 20, 20 }))
                .UseAllAvailableWidth().SetMarginBottom(20);

            table.AddHeaderCell(new Cell().Add(new Paragraph("№").SimulateBold())
                .SetTextAlignment(TextAlignment.CENTER));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Наименование этапа (работы)").SimulateBold())
                .SetTextAlignment(TextAlignment.CENTER));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Статус").SimulateBold())
                .SetTextAlignment(TextAlignment.CENTER));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Срок сдачи").SimulateBold())
                .SetTextAlignment(TextAlignment.CENTER));

            var stages = report.Project.Stages.OrderBy(s => s.StageId).ToList(); 
            int i = 1;
            foreach (var stage in stages)
            {
                table.AddCell(new Cell().Add(new Paragraph(i.ToString())));
                table.AddCell(new Cell().Add(new Paragraph(stage.Name)));
                table.AddCell(new Cell().Add(new Paragraph(stage.Status.ToString()))
                    .SetTextAlignment(TextAlignment.CENTER));

                if (config.IncludeDeadline)
                {
                    table.AddCell(new Cell().Add(new Paragraph(stage.Deadline.ToShortDateString()))
                        .SetTextAlignment(TextAlignment.CENTER));
                }
                else
                {
                    table.AddCell(new Cell().Add(new Paragraph("—"))
                        .SetTextAlignment(TextAlignment.CENTER));
                }

                i++;
            }

            document.Add(table);

            // --- 5. ЗАКЛЮЧЕНИЕ ---
            document.Add(
                new Paragraph(
                        "Работы выполнены в полном объеме и соответствуют техническому заданию. Стороны претензий не имеют.")
                    .SetFontSize(12).SetFirstLineIndent(30).SetMarginBottom(30));

            // --- 6. ПОДПИСИ ---
            document.Add(new Paragraph("Заказчик:")
                .SetFontSize(12).SetMarginLeft(50));
            document.Add(new Paragraph("\n___________________ / (Подпись)")
                .SetFontSize(12).SetMarginLeft(50));

            document.Add(new Paragraph("\nИсполнитель:")
                .SetFontSize(12).SetMarginLeft(350).SetMarginTop(-40));
            document.Add(new Paragraph("\n___________________ / (Подпись)")
                .SetFontSize(12).SetMarginLeft(350));
        }

        return ms.ToArray();
    }

    private async Task<byte[]> GenerateExcelKpiAsync(Report report, dynamic config)
    {
        // Запрос данных этапов
        var stages = await _context.Stages
            .AsNoTracking()
            .Where(s => s.ProjectId == report.ProjectId)
            .OrderBy(s => s.StageId)
            .Select(s => new { s.StageId, s.Name, s.ProgressPercent, s.Deadline, s.Status })
            .ToListAsync();

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("KPI Summary");

        // --- 1. СТИЛИЗАЦИЯ (для красоты) ---
        var headerStyle = worksheet.Workbook.Styles.CreateNamedStyle("HeaderStyle");
        headerStyle.Style.Font.Bold = true;
        headerStyle.Style.Fill.PatternType = ExcelFillStyle.Solid;
        headerStyle.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
        headerStyle.Style.Border.BorderAround(ExcelBorderStyle.Thin);

        // --- 2. ШАПКА ОТЧЕТА ---
        worksheet.Cells["A1"].Value = "Отчет по ключевым показателям проекта (KPI)";
        worksheet.Cells["A1:D1"].Merge = true;
        worksheet.Cells["A1"].Style.Font.Size = 16;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        worksheet.Cells["A3"].Value = "Проект:";
        worksheet.Cells["B3"].Value = report.Project.Name;
        worksheet.Cells["A4"].Value = "Дата генерации:";
        worksheet.Cells["B4"].Value = report.GeneratedAt.ToShortDateString();

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
                worksheet.Cells[row, col].Value = stage.ProgressPercent;
                worksheet.Cells[row, col++].Style.Numberformat.Format = "0%"; // Форматирование
            }

            if (config.IncludeDeadline)
            {
                worksheet.Cells[row, col].Value = stage.Deadline;
                worksheet.Cells[row, col++].Style.Numberformat.Format = "yyyy-mm-dd"; // Форматирование даты
            }

            row++;
        }

        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        return package.GetAsByteArray();
    }

    public async Task<(byte[] FileBytes, string ContentType, string FileName)> DownloadReportAsync(
        int reportId,
        CancellationToken cancellationToken)
    {
        var report = await _context.Reports
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

        return (fileBytes, contentType, fileName);
    }
}