using Application.DTOs.Input_DTO;
using Application.DTOs.Output_DTO;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers;

[Authorize(Roles = "Administrator,Specialist")]
[ApiController]
[Route("api/reports")]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;

    /// <summary>
    /// Контроллер для работы с отчетами.
    /// </summary>
    public ReportController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Запускает асинхронную генерацию нового отчета по проекту.
    /// </summary>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(ReportResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateReport(
        [FromBody] GenerateReportRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Не удалось определить ID пользователя из токена.");
            }

            var response = await _reportService.GenerateReportAsync(request, userId, cancellationToken);

            return AcceptedAtAction(
                nameof(DownloadReport),
                new { reportId = response.ReportId }, 
                response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Скачивает готовый файл отчета по его идентификатору.
    /// </summary>
    [HttpGet("{reportId}/download")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)] // Для статуса InProgress/Failed
    public async Task<IActionResult> DownloadReport(int reportId, CancellationToken cancellationToken)
    {
        try
        {
            var (fileBytes, contentType, fileName) = 
                await _reportService.DownloadReportAsync(reportId, cancellationToken);
            
            return File(fileBytes, contentType, fileName);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }
    
    /// <summary>
    /// Возвращает список кратких сведений об отчетах по указанному проекту.
    /// </summary>
    [HttpGet("{projectId}/projects")]
    [ProducesResponseType(typeof(IEnumerable<ShortReportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReportsByProject(int projectId, CancellationToken cancellationToken)
    {
        try
        {
            var reports = await _reportService.GetShortReportsByProjectAsync(projectId, cancellationToken);
            
            return Ok(reports);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}