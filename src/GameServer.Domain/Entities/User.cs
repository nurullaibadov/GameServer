using GameServer.Domain.Common;
using GameServer.Domain.Enums;

namespace GameServer.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Player;
    public UserStatus Status { get; set; } = UserStatus.PendingVerification;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Country { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }
    public bool IsEmailVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpiry { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    public int LoginFailedCount { get; set; } = 0;
    public DateTime? LockoutUntil { get; set; }
    public string? BanReason { get; set; }
    public DateTime? BannedAt { get; set; }
    public Guid? BannedByUserId { get; set; }
    public PlayerProfile? PlayerProfile { get; set; }
    public ICollection<GameParticipant> GameSessions { get; set; } = new List<GameParticipant>();
    public ICollection<UserAuditLog> AuditLogs { get; set; } = new List<UserAuditLog>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
