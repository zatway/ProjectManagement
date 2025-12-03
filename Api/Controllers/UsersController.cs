using Application.DTOs.Output_DTO;
using Application.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

public class UsersController : ControllerBase
{
    private readonly IUsersService _usersService;

    /// <summary>
    /// Контроллер для работы с пользователями.
    /// </summary>
    public UsersController(IUsersService usersService)
    {
        _usersService = usersService;
    }
    
    /// <summary>
    /// Возвращает список всех пользователей системы.
    /// </summary>
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
            return NotFound(ex.Message);
        }
    }
}