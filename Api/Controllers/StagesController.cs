using Application.DTOs.Input_DTO;
using Application.DTOs.Output_DTO;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Authorize(Roles = "Administrator,Specialist")]
[ApiController]
[Route("api/stages")]
public class StagesController : ControllerBase
{
    private readonly IStageService _stageService;

    public StagesController(IStageService stageService)
    {
        _stageService = stageService;
    }

    [HttpPost("~/api/projects/{projectId}/stages")]
    [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateStage(
        int projectId,
        [FromBody] CreateStageRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var newStageId = await _stageService.CreateStageAsync(request, projectId, cancellationToken);

            return CreatedAtAction(nameof(GetStageDetail), new { stageId = newStageId }, newStageId);
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

    [HttpGet("~/api/projects/{projectId}/stages")]
    [ProducesResponseType(typeof(IEnumerable<ShortStageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAllStages(int projectId, CancellationToken cancellationToken)
    {
        try
        {
            var stages = await _stageService.GetAllStagesByProjectAsync(projectId, cancellationToken);
            return Ok(stages);
        }
        catch (KeyNotFoundException ex)
        {
            // Проект не найден
            return NotFound(ex.Message);
        }
    }

    [HttpGet("{stageId}")]
    [ProducesResponseType(typeof(StageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStageDetail(int stageId, CancellationToken cancellationToken)
    {
        try
        {
            var stage = await _stageService.GetStageDetailAsync(stageId, cancellationToken);
            return Ok(stage);
        }
        catch (KeyNotFoundException)
        {
            // Этап не найден
            return NotFound();
        }
    }

    [HttpPatch("{stageId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStage(int stageId, [FromBody] UpdateStageRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _stageService.UpdateStageAsync(stageId, request, cancellationToken);
            return NoContent();
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

    [HttpDelete("{stageId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStage(int stageId, CancellationToken cancellationToken)
    {
        try
        {
            await _stageService.DeleteProjectAsync(stageId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}