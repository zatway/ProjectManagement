using System.Security.Claims;
using Application.DTOs.Input_DTO;
using Application.DTOs.Output_DTO;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Authorize(Roles = "Administrator,Specialist")]
[Route("api/[controller]")]
[ApiController]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet("{id}")]
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

    [HttpGet("all")]
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

    [HttpPatch("update/{id}")]
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

    [HttpPost("create")]
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

    [HttpDelete("delete/{id}")]
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