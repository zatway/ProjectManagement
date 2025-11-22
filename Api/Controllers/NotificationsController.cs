using Application.DTOs.Output_DTO;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers;

[Authorize(Roles = "Administrator,Specialist")]
[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }
    
    /// <summary>
    /// Получает все уведомления (прочитанные и непрочитанные) для текущего авторизованного пользователя.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<NotificationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetNotifications(CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            var notifications = await _notificationService.GetNotificationsByUserIdAsync(userId, cancellationToken);
            
            return Ok(notifications);
        }
        catch (UnauthorizedAccessException ex)
        {
            // На случай, если токен невалиден, хотя [Authorize] должен сработать раньше
            return Unauthorized(ex.Message);
        }
    }
    
    /// <summary>
    /// Отмечает конкретное уведомление как прочитанное.
    /// </summary>
    [HttpPatch("{notificationId}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(int notificationId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            await _notificationService.MarkAsReadAsync(notificationId, userId, cancellationToken);
            
            return NoContent(); // Успешное выполнение, нет содержимого для возврата
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message); // Попытка изменить чужое уведомление
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Удаляет конкретное уведомление.
    /// </summary>
    [HttpDelete("{notificationId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteNotification(int notificationId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            await _notificationService.DeleteNotificationAsync(notificationId, userId, cancellationToken);
            
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message); // Попытка удалить чужое уведомление
        }
    }
    
    private int GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (!int.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Не удалось определить ID пользователя из токена.");
        }
        return userId;
    }

}