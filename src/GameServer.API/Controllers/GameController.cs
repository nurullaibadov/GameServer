using GameServer.Application.DTOs.Game;
using GameServer.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GameServer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GameController : ControllerBase
{
    private readonly IGameService _game;
    public GameController(IGameService game)=>_game=game;
    private Guid UserId=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>List all active games</summary>
    [HttpGet][AllowAnonymous]
    public async Task<IActionResult> GetGames(){return Ok(new{success=true,data=await _game.GetAvailableGamesAsync()});}

    /// <summary>List all active sessions</summary>
    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions(){return Ok(new{success=true,data=await _game.GetActiveSessionsAsync()});}

    /// <summary>Get session by ID</summary>
    [HttpGet("sessions/{id:guid}")]
    public async Task<IActionResult> GetSession(Guid id){var s=await _game.GetSessionAsync(id);return s==null?NotFound(new{success=false,message="Session not found."}):Ok(new{success=true,data=s});}

    /// <summary>Create a new game session</summary>
    [HttpPost("sessions")]
    public async Task<IActionResult> CreateSession([FromBody]CreateSessionDto req){var s=await _game.CreateSessionAsync(UserId,req);return CreatedAtAction(nameof(GetSession),new{id=s.Id},new{success=true,data=s});}

    /// <summary>Join session by code</summary>
    [HttpPost("sessions/join/{code}")]
    public async Task<IActionResult> JoinSession(string code){var s=await _game.JoinSessionAsync(UserId,code);return Ok(new{success=true,data=s});}

    /// <summary>Leave current session</summary>
    [HttpPost("sessions/{id:guid}/leave")]
    public async Task<IActionResult> LeaveSession(Guid id){var ok=await _game.LeaveSessionAsync(UserId,id);return ok?Ok(new{success=true,message="Left session."}):NotFound();}

    /// <summary>Start session (host only)</summary>
    [HttpPost("sessions/{id:guid}/start")]
    public async Task<IActionResult> StartSession(Guid id){var s=await _game.StartSessionAsync(UserId,id);return Ok(new{success=true,data=s});}

    /// <summary>End session and submit results (Admin only)</summary>
    [HttpPost("sessions/{id:guid}/end")][Authorize(Roles="Admin,SuperAdmin")]
    public async Task<IActionResult> EndSession(Guid id,[FromBody]EndSessionDto req){var s=await _game.EndSessionAsync(id,req);return Ok(new{success=true,data=s});}

    /// <summary>Get my game history</summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetMyHistory([FromQuery]int page=1,[FromQuery]int pageSize=20){var r=await _game.GetPlayerHistoryAsync(UserId,page,pageSize);return Ok(new{success=true,data=r});}

    /// <summary>Get game history for any player</summary>
    [HttpGet("history/{userId:guid}")]
    public async Task<IActionResult> GetPlayerHistory(Guid userId,[FromQuery]int page=1,[FromQuery]int pageSize=20){var r=await _game.GetPlayerHistoryAsync(userId,page,pageSize);return Ok(new{success=true,data=r});}
}
