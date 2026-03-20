using GameServer.Application.DTOs.Admin;
using GameServer.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GameServer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles="Admin,SuperAdmin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _admin;
    public AdminController(IAdminService admin)=>_admin=admin;
    private Guid AdminId=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Get dashboard statistics</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()=>Ok(new{success=true,data=await _admin.GetDashboardStatsAsync()});

    // ── USERS ──────────────────────────────────────────────
    /// <summary>Get paginated user list with filters</summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery]AdminUserFilterDto filter)=>Ok(new{success=true,data=await _admin.GetUsersAsync(filter)});

    /// <summary>Get single user by ID</summary>
    [HttpGet("users/{userId:guid}")]
    public async Task<IActionResult> GetUser(Guid userId){var u=await _admin.GetUserByIdAsync(userId);return u==null?NotFound(new{success=false,message="User not found."}):Ok(new{success=true,data=u});}

    /// <summary>Ban a user</summary>
    [HttpPost("users/{userId:guid}/ban")]
    public async Task<IActionResult> BanUser(Guid userId,[FromBody]BanUserDto req){var ok=await _admin.BanUserAsync(AdminId,userId,req);return ok?Ok(new{success=true,message="User banned."}):NotFound();}

    /// <summary>Remove ban from user</summary>
    [HttpPost("users/{userId:guid}/unban")]
    public async Task<IActionResult> UnbanUser(Guid userId){var ok=await _admin.UnbanUserAsync(AdminId,userId);return ok?Ok(new{success=true,message="User unbanned."}):NotFound();}

    /// <summary>Update user role (SuperAdmin only)</summary>
    [HttpPut("users/{userId:guid}/role")][Authorize(Roles="SuperAdmin")]
    public async Task<IActionResult> UpdateRole(Guid userId,[FromBody]UpdateRoleRequest req){var ok=await _admin.UpdateUserRoleAsync(AdminId,userId,req.Role);return ok?Ok(new{success=true,message="Role updated."}):BadRequest(new{success=false,message="Invalid role."});}

    /// <summary>Credit coins or gems to a user</summary>
    [HttpPost("users/credit")]
    public async Task<IActionResult> CreditCurrency([FromBody]CreditCurrencyDto req){var ok=await _admin.CreditCurrencyAsync(AdminId,req);return ok?Ok(new{success=true,message="Currency credited."}):NotFound();}

    // ── GAMES ──────────────────────────────────────────────
    /// <summary>Get all games (including inactive)</summary>
    [HttpGet("games")]
    public async Task<IActionResult> GetGames()=>Ok(new{success=true,data=await _admin.GetAllGamesAsync()});

    /// <summary>Create a new game</summary>
    [HttpPost("games")]
    public async Task<IActionResult> CreateGame([FromBody]CreateGameDto req){var g=await _admin.CreateGameAsync(req);return Ok(new{success=true,data=g});}

    /// <summary>Update an existing game</summary>
    [HttpPut("games/{gameId:guid}")]
    public async Task<IActionResult> UpdateGame(Guid gameId,[FromBody]UpdateGameDto req){var g=await _admin.UpdateGameAsync(gameId,req);return Ok(new{success=true,data=g});}

    /// <summary>Soft-delete a game</summary>
    [HttpDelete("games/{gameId:guid}")]
    public async Task<IActionResult> DeleteGame(Guid gameId){var ok=await _admin.DeleteGameAsync(gameId);return ok?Ok(new{success=true,message="Game deleted."}):NotFound();}

    // ── NOTIFICATIONS ──────────────────────────────────────
    /// <summary>Send notification to one user or all users</summary>
    [HttpPost("notifications")]
    public async Task<IActionResult> SendNotification([FromBody]SendNotificationDto req){await _admin.SendNotificationAsync(req);return Ok(new{success=true,message="Notification sent."});}

    // ── AUDIT LOGS ─────────────────────────────────────────
    /// <summary>Get paginated audit logs</summary>
    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs([FromQuery]int page=1,[FromQuery]int pageSize=50)=>Ok(new{success=true,data=await _admin.GetAuditLogsAsync(page,pageSize)});

    // ── SETTINGS ───────────────────────────────────────────
    /// <summary>Get all system settings</summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()=>Ok(new{success=true,data=await _admin.GetSettingsAsync()});

    /// <summary>Update a system setting by key</summary>
    [HttpPut("settings/{key}")]
    public async Task<IActionResult> UpdateSetting(string key,[FromBody]UpdateSettingRequest req){var ok=await _admin.UpdateSettingAsync(key,req.Value);return ok?Ok(new{success=true,message="Setting updated."}):NotFound();}
}

public record UpdateRoleRequest(string Role);
public record UpdateSettingRequest(string Value);
