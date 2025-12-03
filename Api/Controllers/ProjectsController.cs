using System.Security.Claims;
using Application.DTOs.Input_DTO;
using Application.DTOs.Output_DTO;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Authorize(Roles = "Administrator,Specialist")]
[ApiController]
[Route("/api/projects")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet("~/api/projects/{id}/detail")]
    [ProducesResponseType(typeof(ProjectResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetDetailsProject(int id, CancellationToken cancellationToken)
    {
        try
        {
            var project = await _projectService.GetProjectByIdAsync(id, cancellationToken);
            return Ok(project);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("~/api/projects/all")]
    [ProducesResponseType(typeof(ShortProjectResponse[]), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetAllShortProjects(CancellationToken cancellationToken)
    {
        try
        {
            var projects = await _projectService.GetAllProjectsAsync(cancellationToken);
            return Ok(projects);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPatch("~/api/projects/{id}/update")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateProject(int id, UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _projectService.UpdateProjectAsync(id, request, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("~/api/projects/create")]
    [ProducesResponseType(typeof(int), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateProject(
        [FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var createdByUserId))
        {
            return Unauthorized("Не удалось определить ID пользователя.");
        }

        try
        {
            var newProjectId = await _projectService.CreateProjectAsync(
                request,
                createdByUserId,
                cancellationToken);

            return CreatedAtAction(nameof(GetDetailsProject), new { id = newProjectId }, newProjectId);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("~/api/projects/{id}/delete")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> DeleteProject(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _projectService.DeleteProjectAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}