using GameServer.Domain.Common;
using GameServer.Domain.Enums;

namespace GameServer.Domain.Entities;

public class Achievement : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;
    public int RewardCoins { get; set; }
    public int RewardGems { get; set; }
    public bool IsSecret { get; set; } = false;
    public ICollection<PlayerAchievement> PlayerAchievements { get; set; } = new List<PlayerAchievement>();
}

public class PlayerAchievement : BaseEntity
{
    public Guid PlayerProfileId { get; set; }
    public Guid AchievementId { get; set; }
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
    public PlayerProfile PlayerProfile { get; set; } = null!;
    public Achievement Achievement { get; set; } = null!;
}

public class CurrencyTransaction : BaseEntity
{
    public Guid PlayerProfileId { get; set; }
    public TransactionType Type { get; set; }
    public int CoinsAmount { get; set; } = 0;
    public int GemsAmount { get; set; } = 0;
    public string Description { get; set; } = string.Empty;
    public Guid? CreatedByAdminId { get; set; }
    public PlayerProfile PlayerProfile { get; set; } = null!;
}

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Type { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public string? ActionUrl { get; set; }
    public User User { get; set; } = null!;
}

public class UserAuditLog : BaseEntity
{
    public Guid UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? Details { get; set; }
    public Guid? PerformedByAdminId { get; set; }
    public User User { get; set; } = null!;
}

public class SystemSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublic { get; set; } = false;
}
