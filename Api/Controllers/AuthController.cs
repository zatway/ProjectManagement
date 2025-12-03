using Application.DTOs.Input_DTO;
using Application.DTOs.Output_DTO;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
namespace Api.Controllers;

public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    
    /// <summary>
    /// Контроллер для операций аутентификации и регистрации пользователей.
    /// </summary>
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Аутентифицирует пользователя и выдает JWT-токен и Refresh Token.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var userInfo = await _authService.Login(request, cancellationToken);
            return Ok(userInfo);
        }
        catch (UnauthorizedAccessException ex) 
        {
            return Unauthorized(ex.Message);
        }
    }
    
    /// <summary>
    /// Регистрирует нового пользователя в системе.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _authService.Register(request, cancellationToken);
            return Ok();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    /// <summary>
    /// Обновляет пару токенов доступа по действующему Refresh Token.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var newTokens = await _authService.RefreshToken(request, cancellationToken);
            return Ok(newTokens);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }
}