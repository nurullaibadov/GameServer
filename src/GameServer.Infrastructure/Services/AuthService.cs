using System.Security.Cryptography;
using System.Text;
using GameServer.Application.DTOs.Auth;
using GameServer.Application.Interfaces;
using GameServer.Domain.Entities;
using GameServer.Domain.Enums;
using GameServer.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GameServer.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly IJwtService _jwt;
    private readonly IEmailService _email;

    public AuthService(IUnitOfWork uow, IJwtService jwt, IEmailService email)
    {
        _uow = uow;
        _jwt = jwt;
        _email = email;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto req, string ip)
    {
        var repo = _uow.Repository<User>();

        if (await repo.AnyAsync(u => u.Email == req.Email.ToLower()))
            return Fail("Email already registered.");

        if (await repo.AnyAsync(u => u.Username == req.Username))
            return Fail("Username already taken.");

        var (hash, salt) = HashPassword(req.Password);
        var verifyToken = GenerateToken();

        var user = new User
        {
            Username = req.Username,
            Email = req.Email.ToLower(),
            PasswordHash = hash,
            PasswordSalt = salt,
            FirstName = req.FirstName,
            LastName = req.LastName,
            Country = req.Country,
            DateOfBirth = req.DateOfBirth,
            EmailVerificationToken = verifyToken,
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24),
            Status = UserStatus.PendingVerification,
            LastLoginIp = ip
        };

        await repo.AddAsync(user);
        await _uow.Repository<PlayerProfile>().AddAsync(new PlayerProfile { UserId = user.Id });

        var access = _jwt.GenerateAccessToken(user.Id, user.Username, user.Email, user.Role.ToString());
        var refresh = _jwt.GenerateRefreshToken();
        user.RefreshToken = refresh;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(30);
        await _uow.SaveChangesAsync();

        _ = _email.SendWelcomeEmailAsync(user.Email, user.Username, verifyToken);

        return new AuthResponseDto
        {
            Success = true,
            Message = "Registration successful. Please verify your email.",
            AccessToken = access,
            RefreshToken = refresh,
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(60),
            User = ToUserInfo(user)
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto req, string ip)
    {
        var user = await _uow.Repository<User>().Query()
            .Include(u => u.PlayerProfile)
            .FirstOrDefaultAsync(u => u.Email == req.UsernameOrEmail.ToLower()
                                   || u.Username == req.UsernameOrEmail);

        if (user == null) return Fail("Invalid credentials.");
        if (user.Status == UserStatus.Banned) return Fail($"Account banned: {user.BanReason}");
        if (user.LockoutUntil.HasValue && user.LockoutUntil > DateTime.UtcNow)
            return Fail($"Account locked until {user.LockoutUntil:f}.");

        if (!VerifyPassword(req.Password, user.PasswordHash, user.PasswordSalt))
        {
            user.LoginFailedCount++;
            if (user.LoginFailedCount >= 5)
                user.LockoutUntil = DateTime.UtcNow.AddMinutes(15);
            await _uow.Repository<User>().UpdateAsync(user);
            await _uow.SaveChangesAsync();
            return Fail("Invalid credentials.");
        }

        user.LoginFailedCount = 0;
        user.LockoutUntil = null;
        user.LastLoginAt = DateTime.UtcNow;
        user.LastLoginIp = ip;

        var access = _jwt.GenerateAccessToken(user.Id, user.Username, user.Email, user.Role.ToString());
        var refresh = _jwt.GenerateRefreshToken();
        user.RefreshToken = refresh;
        user.RefreshTokenExpiry = req.RememberMe ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddDays(7);
        await _uow.Repository<User>().UpdateAsync(user);

        await _uow.Repository<UserAuditLog>().AddAsync(new UserAuditLog
        {
            UserId = user.Id,
            Action = "LOGIN",
            IpAddress = ip
        });
        await _uow.SaveChangesAsync();

        return new AuthResponseDto
        {
            Success = true,
            Message = "Login successful.",
            AccessToken = access,
            RefreshToken = refresh,
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(60),
            User = ToUserInfo(user)
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, string ip)
    {
        var user = await _uow.Repository<User>()
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

        if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
            return Fail("Invalid or expired refresh token.");

        var access = _jwt.GenerateAccessToken(user.Id, user.Username, user.Email, user.Role.ToString());
        var refresh = _jwt.GenerateRefreshToken();
        user.RefreshToken = refresh;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _uow.Repository<User>().UpdateAsync(user);
        await _uow.SaveChangesAsync();

        return new AuthResponseDto
        {
            Success = true,
            AccessToken = access,
            RefreshToken = refresh,
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(60),
            User = ToUserInfo(user)
        };
    }

    public async Task<bool> LogoutAsync(Guid userId)
    {
        var user = await _uow.Repository<User>().GetByIdAsync(userId);
        if (user == null) return false;
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        await _uow.Repository<User>().UpdateAsync(user);
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequestDto req)
    {
        var user = await _uow.Repository<User>()
            .FirstOrDefaultAsync(u => u.Email == req.Email.ToLower());
        if (user == null) return true; // security: don't reveal if email exists

        user.PasswordResetToken = GenerateToken();
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        await _uow.Repository<User>().UpdateAsync(user);
        await _uow.SaveChangesAsync();
        _ = _email.SendPasswordResetEmailAsync(user.Email, user.Username, user.PasswordResetToken!);
        return true;
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequestDto req)
    {
        var user = await _uow.Repository<User>().FirstOrDefaultAsync(u =>
            u.PasswordResetToken == req.Token && u.PasswordResetTokenExpiry > DateTime.UtcNow);
        if (user == null) return false;

        var (hash, salt) = HashPassword(req.NewPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.RefreshToken = null;
        await _uow.Repository<User>().UpdateAsync(user);
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        var user = await _uow.Repository<User>().FirstOrDefaultAsync(u =>
            u.EmailVerificationToken == token && u.EmailVerificationTokenExpiry > DateTime.UtcNow);
        if (user == null) return false;

        user.IsEmailVerified = true;
        user.Status = UserStatus.Active;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiry = null;
        await _uow.Repository<User>().UpdateAsync(user);
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResendVerificationEmailAsync(string email)
    {
        var user = await _uow.Repository<User>()
            .FirstOrDefaultAsync(u => u.Email == email.ToLower() && !u.IsEmailVerified);
        if (user == null) return false;

        user.EmailVerificationToken = GenerateToken();
        user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
        await _uow.Repository<User>().UpdateAsync(user);
        await _uow.SaveChangesAsync();
        _ = _email.SendEmailVerificationAsync(user.Email, user.Username, user.EmailVerificationToken!);
        return true;
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto req)
    {
        var user = await _uow.Repository<User>().GetByIdAsync(userId);
        if (user == null || !VerifyPassword(req.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            return false;

        var (hash, salt) = HashPassword(req.NewPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.RefreshToken = null;
        await _uow.Repository<User>().UpdateAsync(user);
        await _uow.SaveChangesAsync();
        return true;
    }

    private static AuthResponseDto Fail(string message) => new() { Success = false, Message = message };

    private static UserInfoDto ToUserInfo(User u) => new()
    {
        Id = u.Id,
        Username = u.Username,
        Email = u.Email,
        Role = u.Role.ToString(),
        AvatarUrl = u.AvatarUrl,
        IsEmailVerified = u.IsEmailVerified
    };

    private static (string hash, string salt) HashPassword(string password)
    {
        var saltBytes = new byte[32];
        RandomNumberGenerator.Fill(saltBytes);
        var salt = Convert.ToBase64String(saltBytes);
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(salt));
        var hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
        return (hash, salt);
    }

    private static bool VerifyPassword(string password, string hash, string salt)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(salt));
        var computed = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
        return computed == hash;
    }

    private static string GenerateToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLower();
    }
}
