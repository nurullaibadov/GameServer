using GameServer.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GameServer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _svc;
    public NotificationController(INotificationService svc)=>_svc=svc;
    private Guid UserId=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Get my notifications</summary>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery]bool unreadOnly=false)=>Ok(new{success=true,data=await _svc.GetUserNotificationsAsync(UserId,unreadOnly)});

    /// <summary>Mark a notification as read</summary>
    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id){var ok=await _svc.MarkAsReadAsync(UserId,id);return ok?Ok(new{success=true}):NotFound();}

    /// <summary>Mark all notifications as read</summary>
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(){await _svc.MarkAllAsReadAsync(UserId);return Ok(new{success=true,message="All notifications marked as read."});}
}
