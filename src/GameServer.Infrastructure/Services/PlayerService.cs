using GameServer.Application.DTOs.Player;
using GameServer.Application.Interfaces;
using GameServer.Domain.Entities;
using GameServer.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GameServer.Infrastructure.Services;

public class PlayerService : IPlayerService
{
    private readonly IUnitOfWork _uow;
    public PlayerService(IUnitOfWork uow) => _uow = uow;

    public async Task<PlayerProfileDto?> GetProfileAsync(Guid userId)
    {
        var user = await _uow.Repository<User>().Query()
            .Include(u => u.PlayerProfile)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.PlayerProfile == null) return null;
        return MapToDto(user, user.PlayerProfile);
    }

    public async Task<PlayerProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileDto req)
    {
        var user = await _uow.Repository<User>().Query()
            .Include(u => u.PlayerProfile)
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException("User not found.");

        if (req.FirstName != null) user.FirstName = req.FirstName;
        if (req.LastName != null) user.LastName = req.LastName;
        if (req.AvatarUrl != null) user.AvatarUrl = req.AvatarUrl;
        if (req.Country != null) user.Country = req.Country;
        if (user.PlayerProfile != null && req.Bio != null) user.PlayerProfile.Bio = req.Bio;

        await _uow.Repository<User>().UpdateAsync(user);
        await _uow.SaveChangesAsync();
        return MapToDto(user, user.PlayerProfile!);
    }

    public async Task<LeaderboardDto> GetLeaderboardAsync(int page, int pageSize)
    {
        var query = _uow.Repository<PlayerProfile>().Query()
            .Include(p => p.User)
            .OrderByDescending(p => p.RankPoints);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new LeaderboardDto
        {
            Entries = items.Select((p, i) => new LeaderboardEntryDto
            {
                Rank = (page - 1) * pageSize + i + 1,
                UserId = p.UserId,
                Username = p.User.Username,
                AvatarUrl = p.User.AvatarUrl,
                Country = p.User.Country,
                Level = p.Level,
                RankPoints = p.RankPoints,
                TotalWins = p.TotalWins,
                WinRate = p.WinRate
            }),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<AchievementDto>> GetAchievementsAsync(Guid userId)
    {
        var profile = await _uow.Repository<PlayerProfile>().Query()
            .Include(p => p.Achievements).ThenInclude(pa => pa.Achievement)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        var all = await _uow.Repository<Achievement>().GetAllAsync();
        var earned = profile?.Achievements.ToDictionary(a => a.AchievementId, a => a.EarnedAt)
                     ?? new Dictionary<Guid, DateTime>();

        return all.Select(a => new AchievementDto
        {
            Id = a.Id,
            Name = a.Name,
            Description = a.Description,
            IconUrl = a.IconUrl,
            RewardCoins = a.RewardCoins,
            RewardGems = a.RewardGems,
            IsUnlocked = earned.ContainsKey(a.Id),
            EarnedAt = earned.TryGetValue(a.Id, out var dt) ? dt : null
        });
    }

    public async Task<PlayerStatsDto> GetStatsAsync(Guid userId)
    {
        var user = await _uow.Repository<User>().Query()
            .Include(u => u.PlayerProfile)
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException("User not found.");

        var p = user.PlayerProfile ?? new PlayerProfile();
        return new PlayerStatsDto
        {
            UserId = user.Id, Username = user.Username,
            TotalGamesPlayed = p.TotalGamesPlayed, TotalWins = p.TotalWins,
            TotalLosses = p.TotalLosses, TotalDraws = p.TotalDraws,
            TotalKills = p.TotalKills, TotalDeaths = p.TotalDeaths,
            WinRate = p.WinRate, KDRatio = p.KDRatio,
            BestStreak = p.BestStreak, CurrentStreak = p.CurrentStreak,
            RankPoints = p.RankPoints, Rank = p.Rank,
            Level = p.Level, Experience = p.Experience
        };
    }

    private static PlayerProfileDto MapToDto(User u, PlayerProfile p) => new()
    {
        UserId = u.Id, Username = u.Username, Email = u.Email,
        FirstName = u.FirstName, LastName = u.LastName, AvatarUrl = u.AvatarUrl,
        Country = u.Country, CreatedAt = u.CreatedAt, LastLoginAt = u.LastLoginAt,
        Bio = p.Bio, Level = p.Level, Experience = p.Experience,
        Coins = p.Coins, Gems = p.Gems, TotalGamesPlayed = p.TotalGamesPlayed,
        TotalWins = p.TotalWins, TotalLosses = p.TotalLosses,
        TotalKills = p.TotalKills, TotalDeaths = p.TotalDeaths,
        WinRate = p.WinRate, KDRatio = p.KDRatio, RankPoints = p.RankPoints,
        Rank = p.Rank, CurrentStreak = p.CurrentStreak, BestStreak = p.BestStreak
    };
}
