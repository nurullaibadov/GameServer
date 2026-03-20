using GameServer.Application.DTOs.Game;
using GameServer.Application.Interfaces;
using GameServer.Domain.Entities;
using GameServer.Domain.Enums;
using GameServer.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GameServer.Infrastructure.Services;

public class GameService : IGameService
{
    private readonly IUnitOfWork _uow;
    public GameService(IUnitOfWork uow) => _uow = uow;

    public async Task<IEnumerable<GameDto>> GetAvailableGamesAsync()
    {
        var games = await _uow.Repository<Game>().FindAsync(g => g.IsActive);
        return games.Select(MapGame);
    }

    public async Task<GameSessionDto> CreateSessionAsync(Guid userId, CreateSessionDto req)
    {
        var game = await _uow.Repository<Game>().GetByIdAsync(req.GameId)
            ?? throw new KeyNotFoundException("Game not found.");

        var session = new GameSession
        {
            GameId = req.GameId,
            SessionCode = GenerateCode(),
            Status = GameStatus.Waiting,
            ServerRegion = req.ServerRegion,
            HostUserId = userId,
            CurrentPlayers = 1
        };
        await _uow.Repository<GameSession>().AddAsync(session);
        await _uow.Repository<GameParticipant>().AddAsync(new GameParticipant
        {
            SessionId = session.Id, UserId = userId, IsHost = true
        });
        await _uow.SaveChangesAsync();
        return await GetSessionDtoAsync(session.Id);
    }

    public async Task<GameSessionDto?> GetSessionAsync(Guid sessionId)
    {
        try { return await GetSessionDtoAsync(sessionId); }
        catch (KeyNotFoundException) { return null; }
    }

    public async Task<GameSessionDto> JoinSessionAsync(Guid userId, string code)
    {
        var session = await _uow.Repository<GameSession>().Query()
            .Include(s => s.Game)
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.SessionCode == code)
            ?? throw new KeyNotFoundException("Session not found.");

        if (session.Status != GameStatus.Waiting)
            throw new InvalidOperationException("Session is not accepting players.");
        if (session.CurrentPlayers >= session.Game.MaxPlayers)
            throw new InvalidOperationException("Session is full.");
        if (session.Participants.Any(p => p.UserId == userId))
            throw new InvalidOperationException("Already in this session.");

        await _uow.Repository<GameParticipant>().AddAsync(new GameParticipant
        {
            SessionId = session.Id, UserId = userId
        });
        session.CurrentPlayers++;
        await _uow.Repository<GameSession>().UpdateAsync(session);
        await _uow.SaveChangesAsync();
        return await GetSessionDtoAsync(session.Id);
    }

    public async Task<bool> LeaveSessionAsync(Guid userId, Guid sessionId)
    {
        var participant = await _uow.Repository<GameParticipant>()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.SessionId == sessionId);
        if (participant == null) return false;

        participant.LeftAt = DateTime.UtcNow;
        await _uow.Repository<GameParticipant>().UpdateAsync(participant);

        var session = await _uow.Repository<GameSession>().GetByIdAsync(sessionId);
        if (session != null)
        {
            session.CurrentPlayers = Math.Max(0, session.CurrentPlayers - 1);
            await _uow.Repository<GameSession>().UpdateAsync(session);
        }
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<GameSessionDto> StartSessionAsync(Guid userId, Guid sessionId)
    {
        var session = await _uow.Repository<GameSession>().Query()
            .Include(s => s.Game)
            .FirstOrDefaultAsync(s => s.Id == sessionId)
            ?? throw new KeyNotFoundException("Session not found.");

        if (session.HostUserId != userId)
            throw new InvalidOperationException("Only host can start the session.");
        if (session.Status != GameStatus.Waiting)
            throw new InvalidOperationException("Session cannot be started.");

        session.Status = GameStatus.InProgress;
        session.StartedAt = DateTime.UtcNow;
        await _uow.Repository<GameSession>().UpdateAsync(session);
        await _uow.SaveChangesAsync();
        return await GetSessionDtoAsync(sessionId);
    }

    public async Task<GameSessionDto> EndSessionAsync(Guid sessionId, EndSessionDto req)
    {
        var session = await _uow.Repository<GameSession>().Query()
            .Include(s => s.Game)
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.Id == sessionId)
            ?? throw new KeyNotFoundException("Session not found.");

        session.Status = GameStatus.Finished;
        session.EndedAt = DateTime.UtcNow;

        foreach (var result in req.Results)
        {
            var participant = session.Participants.FirstOrDefault(p => p.UserId == result.UserId);
            if (participant == null) continue;

            participant.Score = result.Score;
            participant.Kills = result.Kills;
            participant.Deaths = result.Deaths;
            participant.Result = Enum.TryParse<MatchResult>(result.Result, out var mr) ? mr : MatchResult.Loss;
            participant.CoinsEarned = participant.Result == MatchResult.Win
                ? session.Game.RewardCoins : session.Game.RewardCoins / 2;
            participant.ExperienceEarned = participant.Result == MatchResult.Win
                ? session.Game.RewardExperience : session.Game.RewardExperience / 2;

            await _uow.Repository<GameParticipant>().UpdateAsync(participant);

            var profile = await _uow.Repository<PlayerProfile>()
                .FirstOrDefaultAsync(p => p.UserId == result.UserId);
            if (profile != null)
            {
                profile.TotalGamesPlayed++;
                profile.TotalKills += result.Kills;
                profile.TotalDeaths += result.Deaths;
                profile.Coins += participant.CoinsEarned;
                profile.Experience += participant.ExperienceEarned;

                if (participant.Result == MatchResult.Win)
                {
                    profile.TotalWins++;
                    profile.RankPoints += 25;
                    profile.CurrentStreak++;
                    if (profile.CurrentStreak > profile.BestStreak)
                        profile.BestStreak = profile.CurrentStreak;
                }
                else if (participant.Result == MatchResult.Loss)
                {
                    profile.TotalLosses++;
                    profile.RankPoints = Math.Max(0, profile.RankPoints - 15);
                    profile.CurrentStreak = 0;
                }
                else
                {
                    profile.TotalDraws++;
                }

                profile.Level = (int)(profile.Experience / 1000) + 1;
                profile.Rank = profile.RankPoints switch
                {
                    >= 2000 => "Diamond",
                    >= 1500 => "Platinum",
                    >= 1000 => "Gold",
                    >= 500  => "Silver",
                    _       => "Bronze"
                };
                await _uow.Repository<PlayerProfile>().UpdateAsync(profile);
            }
        }

        await _uow.Repository<GameSession>().UpdateAsync(session);
        await _uow.SaveChangesAsync();
        return await GetSessionDtoAsync(sessionId);
    }

    public async Task<IEnumerable<GameSessionDto>> GetActiveSessionsAsync()
    {
        var sessions = await _uow.Repository<GameSession>().Query()
            .Include(s => s.Game)
            .Include(s => s.Participants).ThenInclude(p => p.User)
            .Where(s => s.Status == GameStatus.Waiting || s.Status == GameStatus.InProgress)
            .ToListAsync();
        return sessions.Select(MapSession);
    }

    public async Task<PagedResultDto<GameHistoryDto>> GetPlayerHistoryAsync(Guid userId, int page, int pageSize)
    {
        var query = _uow.Repository<GameParticipant>().Query()
            .Include(p => p.Session).ThenInclude(s => s.Game)
            .Where(p => p.UserId == userId && p.LeftAt == null)
            .OrderByDescending(p => p.JoinedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResultDto<GameHistoryDto>
        {
            Items = items.Select(p => new GameHistoryDto
            {
                SessionId = p.SessionId,
                GameName = p.Session.Game.Name,
                MapName = p.Session.Game.MapName,
                Result = p.Result?.ToString() ?? "Unknown",
                Score = p.Score, Kills = p.Kills, Deaths = p.Deaths,
                CoinsEarned = p.CoinsEarned, ExperienceEarned = p.ExperienceEarned,
                PlayedAt = p.JoinedAt,
                DurationSeconds = (p.Session.EndedAt.HasValue && p.Session.StartedAt.HasValue)
                    ? (int)(p.Session.EndedAt.Value - p.Session.StartedAt.Value).TotalSeconds : 0
            }),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    private async Task<GameSessionDto> GetSessionDtoAsync(Guid id)
    {
        var session = await _uow.Repository<GameSession>().Query()
            .Include(s => s.Game)
            .Include(s => s.Participants).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new KeyNotFoundException("Session not found.");
        return MapSession(session);
    }

    private static GameSessionDto MapSession(GameSession s) => new()
    {
        Id = s.Id, GameId = s.GameId, GameName = s.Game.Name,
        SessionCode = s.SessionCode, Status = s.Status.ToString(),
        CurrentPlayers = s.CurrentPlayers, MaxPlayers = s.Game.MaxPlayers,
        ServerRegion = s.ServerRegion, StartedAt = s.StartedAt,
        EndedAt = s.EndedAt, CreatedAt = s.CreatedAt,
        Participants = s.Participants.Select(p => new ParticipantDto
        {
            UserId = p.UserId,
            Username = p.User.Username,
            AvatarUrl = p.User.AvatarUrl,
            Score = p.Score, Kills = p.Kills, Deaths = p.Deaths,
            Result = p.Result?.ToString(), IsHost = p.IsHost
        })
    };

    private static GameDto MapGame(Game g) => new()
    {
        Id = g.Id, Name = g.Name, Description = g.Description,
        Type = g.Type.ToString(), MaxPlayers = g.MaxPlayers,
        MinPlayers = g.MinPlayers, IsActive = g.IsActive,
        MapName = g.MapName, DurationSeconds = g.DurationSeconds,
        RewardCoins = g.RewardCoins, RewardExperience = g.RewardExperience,
        IconUrl = g.IconUrl
    };

    private static string GenerateCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Range(0, 8)
            .Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
    }
}
