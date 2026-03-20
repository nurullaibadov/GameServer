using GameServer.Domain.Common;

namespace GameServer.Domain.Entities;

public class PlayerProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public int Level { get; set; } = 1;
    public long Experience { get; set; } = 0;
    public int Coins { get; set; } = 0;
    public int Gems { get; set; } = 0;
    public int TotalGamesPlayed { get; set; } = 0;
    public int TotalWins { get; set; } = 0;
    public int TotalLosses { get; set; } = 0;
    public int TotalDraws { get; set; } = 0;
    public int TotalKills { get; set; } = 0;
    public int TotalDeaths { get; set; } = 0;
    public double WinRate => TotalGamesPlayed == 0 ? 0 : Math.Round((double)TotalWins / TotalGamesPlayed * 100, 2);
    public double KDRatio => TotalDeaths == 0 ? TotalKills : Math.Round((double)TotalKills / TotalDeaths, 2);
    public int RankPoints { get; set; } = 0;
    public string Rank { get; set; } = "Bronze";
    public int CurrentStreak { get; set; } = 0;
    public int BestStreak { get; set; } = 0;
    public string? Bio { get; set; }
    public User User { get; set; } = null!;
    public ICollection<PlayerAchievement> Achievements { get; set; } = new List<PlayerAchievement>();
    public ICollection<CurrencyTransaction> Transactions { get; set; } = new List<CurrencyTransaction>();
}
