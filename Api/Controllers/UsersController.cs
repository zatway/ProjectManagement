using Application.DTOs.Output_DTO;
using Application.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

public class UsersController : ControllerBase
{
    private readonly IUsersService _usersService;

    public UsersController(IUsersService usersService)
    {
        _usersService = usersService;
    }
    
    [HttpGet("~/api/users/all")]
    [ProducesResponseType(typeof(IEnumerable<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAllStages(CancellationToken cancellationToken)
    {
        try
        {
            var users = await _usersService.GetAllUsersAsync(cancellationToken);
            return Ok(users);
        }
        catch (KeyNotFoundException ex)
        {
            // Проект не найден
            return NotFound(ex.Message);
        }
    }
}