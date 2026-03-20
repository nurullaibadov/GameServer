using GameServer.Application.DTOs.Admin;
using GameServer.Application.DTOs.Auth;
using GameServer.Application.DTOs.Game;
using GameServer.Application.DTOs.Player;

namespace GameServer.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, string ipAddress);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, string ipAddress);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, string ipAddress);
    Task<bool> LogoutAsync(Guid userId);
    Task<bool> ForgotPasswordAsync(ForgotPasswordRequestDto request);
    Task<bool> ResetPasswordAsync(ResetPasswordRequestDto request);
    Task<bool> VerifyEmailAsync(string token);
    Task<bool> ResendVerificationEmailAsync(string email);
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request);
}
public interface IPlayerService
{
    Task<PlayerProfileDto?> GetProfileAsync(Guid userId);
    Task<PlayerProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileDto request);
    Task<LeaderboardDto> GetLeaderboardAsync(int page, int pageSize);
    Task<IEnumerable<AchievementDto>> GetAchievementsAsync(Guid userId);
    Task<PlayerStatsDto> GetStatsAsync(Guid userId);
}
public interface IGameService
{
    Task<GameSessionDto> CreateSessionAsync(Guid userId, CreateSessionDto request);
    Task<GameSessionDto?> GetSessionAsync(Guid sessionId);
    Task<GameSessionDto> JoinSessionAsync(Guid userId, string sessionCode);
    Task<bool> LeaveSessionAsync(Guid userId, Guid sessionId);
    Task<GameSessionDto> StartSessionAsync(Guid userId, Guid sessionId);
    Task<GameSessionDto> EndSessionAsync(Guid sessionId, EndSessionDto request);
    Task<IEnumerable<GameSessionDto>> GetActiveSessionsAsync();
    Task<PagedResultDto<GameHistoryDto>> GetPlayerHistoryAsync(Guid userId, int page, int pageSize);
    Task<IEnumerable<GameDto>> GetAvailableGamesAsync();
}
public interface IAdminService
{
    Task<PagedResultDto<AdminUserDto>> GetUsersAsync(AdminUserFilterDto filter);
    Task<AdminUserDto?> GetUserByIdAsync(Guid userId);
    Task<bool> BanUserAsync(Guid adminId, Guid userId, BanUserDto request);
    Task<bool> UnbanUserAsync(Guid adminId, Guid userId);
    Task<bool> UpdateUserRoleAsync(Guid adminId, Guid userId, string role);
    Task<AdminDashboardDto> GetDashboardStatsAsync();
    Task<bool> CreditCurrencyAsync(Guid adminId, CreditCurrencyDto request);
    Task<PagedResultDto<AuditLogDto>> GetAuditLogsAsync(int page, int pageSize);
    Task<bool> SendNotificationAsync(SendNotificationDto request);
    Task<IEnumerable<GameDto>> GetAllGamesAsync();
    Task<GameDto> CreateGameAsync(CreateGameDto request);
    Task<GameDto> UpdateGameAsync(Guid gameId, UpdateGameDto request);
    Task<bool> DeleteGameAsync(Guid gameId);
    Task<IEnumerable<SystemSettingDto>> GetSettingsAsync();
    Task<bool> UpdateSettingAsync(string key, string value);
}
public interface IEmailService
{
    Task SendWelcomeEmailAsync(string toEmail, string username, string verificationToken);
    Task SendPasswordResetEmailAsync(string toEmail, string username, string resetToken);
    Task SendEmailVerificationAsync(string toEmail, string username, string token);
    Task SendGenericEmailAsync(string toEmail, string subject, string htmlBody);
    Task SendBanNotificationAsync(string toEmail, string username, string reason);
}
public interface IJwtService
{
    string GenerateAccessToken(Guid userId, string username, string email, string role);
    string GenerateRefreshToken();
    Guid? GetUserIdFromToken(string token);
    bool ValidateToken(string token);
}
public interface INotificationService
{
    Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid userId, bool unreadOnly = false);
    Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId);
    Task<bool> MarkAllAsReadAsync(Guid userId);
    Task CreateNotificationAsync(Guid userId, string title, string message, string type = "info");
}
