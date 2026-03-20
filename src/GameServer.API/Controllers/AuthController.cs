using GameServer.Application.DTOs.Auth;
using GameServer.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GameServer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth)=>_auth=auth;
    private string Ip=>HttpContext.Connection.RemoteIpAddress?.ToString()??"unknown";
    private Guid UserId=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Register a new player account</summary>
    [HttpPost("register")][AllowAnonymous]
    public async Task<IActionResult> Register([FromBody]RegisterRequestDto req){var r=await _auth.RegisterAsync(req,Ip);return r.Success?Ok(r):BadRequest(r);}

    /// <summary>Login with username or email</summary>
    [HttpPost("login")][AllowAnonymous]
    public async Task<IActionResult> Login([FromBody]LoginRequestDto req){var r=await _auth.LoginAsync(req,Ip);return r.Success?Ok(r):Unauthorized(r);}

    /// <summary>Get a new access token using refresh token</summary>
    [HttpPost("refresh-token")][AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody]RefreshTokenRequestDto req){var r=await _auth.RefreshTokenAsync(req.RefreshToken,Ip);return r.Success?Ok(r):Unauthorized(r);}

    /// <summary>Logout and invalidate tokens</summary>
    [HttpPost("logout")][Authorize]
    public async Task<IActionResult> Logout(){await _auth.LogoutAsync(UserId);return Ok(new{success=true,message="Logged out."});}

    /// <summary>Send password reset email</summary>
    [HttpPost("forgot-password")][AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody]ForgotPasswordRequestDto req){await _auth.ForgotPasswordAsync(req);return Ok(new{success=true,message="If account exists, reset email sent."});}

    /// <summary>Reset password with token from email</summary>
    [HttpPost("reset-password")][AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody]ResetPasswordRequestDto req){var ok=await _auth.ResetPasswordAsync(req);return ok?Ok(new{success=true,message="Password reset successfully."}):BadRequest(new{success=false,message="Invalid or expired token."});}

    /// <summary>Verify email address with token</summary>
    [HttpGet("verify-email")][AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromQuery]string token){var ok=await _auth.VerifyEmailAsync(token);return ok?Ok(new{success=true,message="Email verified."}):BadRequest(new{success=false,message="Invalid or expired token."});}

    /// <summary>Resend email verification</summary>
    [HttpPost("resend-verification")][AllowAnonymous]
    public async Task<IActionResult> ResendVerification([FromBody]ForgotPasswordRequestDto req){await _auth.ResendVerificationEmailAsync(req.Email);return Ok(new{success=true,message="Verification email sent if account exists."});}

    /// <summary>Change password (must be logged in)</summary>
    [HttpPost("change-password")][Authorize]
    public async Task<IActionResult> ChangePassword([FromBody]ChangePasswordRequestDto req){var ok=await _auth.ChangePasswordAsync(UserId,req);return ok?Ok(new{success=true,message="Password changed."}):BadRequest(new{success=false,message="Current password is incorrect."});}
}
