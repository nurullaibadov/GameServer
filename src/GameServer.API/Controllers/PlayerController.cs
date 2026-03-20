using GameServer.Application.DTOs.Player;
using GameServer.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GameServer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlayerController : ControllerBase
{
    private readonly IPlayerService _player;
    public PlayerController(IPlayerService player)=>_player=player;
    private Guid UserId=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Get my profile</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile(){var p=await _player.GetProfileAsync(UserId);return p==null?NotFound():Ok(new{success=true,data=p});}

    /// <summary>Update my profile</summary>
    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody]UpdateProfileDto req){var p=await _player.UpdateProfileAsync(UserId,req);return Ok(new{success=true,data=p});}

    /// <summary>Get my statistics</summary>
    [HttpGet("me/stats")]
    public async Task<IActionResult> GetMyStats(){var s=await _player.GetStatsAsync(UserId);return Ok(new{success=true,data=s});}

    /// <summary>Get my achievements</summary>
    [HttpGet("me/achievements")]
    public async Task<IActionResult> GetMyAchievements(){var a=await _player.GetAchievementsAsync(UserId);return Ok(new{success=true,data=a});}

    /// <summary>Get another player profile by ID</summary>
    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetProfile(Guid userId){var p=await _player.GetProfileAsync(userId);return p==null?NotFound(new{success=false,message="Player not found."}):Ok(new{success=true,data=p});}

    /// <summary>Get another player stats by ID</summary>
    [HttpGet("{userId:guid}/stats")]
    public async Task<IActionResult> GetStats(Guid userId){var s=await _player.GetStatsAsync(userId);return Ok(new{success=true,data=s});}

    /// <summary>Global leaderboard (public)</summary>
    [HttpGet("leaderboard")][AllowAnonymous]
    public async Task<IActionResult> GetLeaderboard([FromQuery]int page=1,[FromQuery]int pageSize=20){if(pageSize>100)pageSize=100;var r=await _player.GetLeaderboardAsync(page,pageSize);return Ok(new{success=true,data=r});}
}
