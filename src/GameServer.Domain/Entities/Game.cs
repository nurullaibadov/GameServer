using GameServer.Domain.Common;
using GameServer.Domain.Enums;

namespace GameServer.Domain.Entities;

public class Game : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public GameType Type { get; set; }
    public int MaxPlayers { get; set; }
    public int MinPlayers { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public string? MapName { get; set; }
    public int DurationSeconds { get; set; }
    public int RewardCoins { get; set; }
    public int RewardExperience { get; set; }
    public string? IconUrl { get; set; }
    public ICollection<GameSession> Sessions { get; set; } = new List<GameSession>();
}

public class GameSession : BaseEntity
{
    public Guid GameId { get; set; }
    public string SessionCode { get; set; } = string.Empty;
    public GameStatus Status { get; set; } = GameStatus.Waiting;
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int CurrentPlayers { get; set; } = 0;
    public string? ServerRegion { get; set; }
    public Guid? HostUserId { get; set; }
    public Game Game { get; set; } = null!;
    public ICollection<GameParticipant> Participants { get; set; } = new List<GameParticipant>();
}

public class GameParticipant : BaseEntity
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
    public MatchResult? Result { get; set; }
    public int Kills { get; set; } = 0;
    public int Deaths { get; set; } = 0;
    public int Score { get; set; } = 0;
    public int CoinsEarned { get; set; } = 0;
    public int ExperienceEarned { get; set; } = 0;
    public bool IsHost { get; set; } = false;
    public GameSession Session { get; set; } = null!;
    public User User { get; set; } = null!;
}
