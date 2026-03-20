using GameServer.Application.DTOs.Admin;
using GameServer.Application.DTOs.Game;
using GameServer.Application.Interfaces;
using GameServer.Domain.Entities;
using GameServer.Domain.Enums;
using GameServer.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GameServer.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly IUnitOfWork _uow;
    private readonly IEmailService _email;
    public AdminService(IUnitOfWork uow,IEmailService email){_uow=uow;_email=email;}

    public async Task<PagedResultDto<AdminUserDto>> GetUsersAsync(AdminUserFilterDto f)
    {
        var q=_uow.Repository<User>().Query().Include(u=>u.PlayerProfile).AsQueryable();
        if(!string.IsNullOrEmpty(f.Search))q=q.Where(u=>u.Username.Contains(f.Search)||u.Email.Contains(f.Search));
        if(!string.IsNullOrEmpty(f.Role)&&Enum.TryParse<UserRole>(f.Role,out var r))q=q.Where(u=>u.Role==r);
        if(!string.IsNullOrEmpty(f.Status)&&Enum.TryParse<UserStatus>(f.Status,out var s))q=q.Where(u=>u.Status==s);
        var total=await q.CountAsync();
        q=f.SortDirection=="asc"?q.OrderBy(u=>u.CreatedAt):q.OrderByDescending(u=>u.CreatedAt);
        var users=await q.Skip((f.Page-1)*f.PageSize).Take(f.PageSize).ToListAsync();
        return new PagedResultDto<AdminUserDto>{Items=users.Select(MU),TotalCount=total,Page=f.Page,PageSize=f.PageSize};
    }

    public async Task<AdminUserDto?> GetUserByIdAsync(Guid id){var u=await _uow.Repository<User>().Query().Include(x=>x.PlayerProfile).FirstOrDefaultAsync(x=>x.Id==id);return u==null?null:MU(u);}

    public async Task<bool> BanUserAsync(Guid adminId,Guid userId,BanUserDto req)
    {
        var u=await _uow.Repository<User>().GetByIdAsync(userId);if(u==null)return false;
        u.Status=UserStatus.Banned;u.BanReason=req.Reason;u.BannedAt=DateTime.UtcNow;u.BannedByUserId=adminId;u.RefreshToken=null;
        await _uow.Repository<User>().UpdateAsync(u);
        await _uow.Repository<UserAuditLog>().AddAsync(new UserAuditLog{UserId=userId,Action="BAN",PerformedByAdminId=adminId,Details=req.Reason});
        await _uow.SaveChangesAsync();_=_email.SendBanNotificationAsync(u.Email,u.Username,req.Reason);return true;
    }

    public async Task<bool> UnbanUserAsync(Guid adminId,Guid userId)
    {
        var u=await _uow.Repository<User>().GetByIdAsync(userId);if(u==null)return false;
        u.Status=UserStatus.Active;u.BanReason=null;u.BannedAt=null;u.BannedByUserId=null;
        await _uow.Repository<User>().UpdateAsync(u);
        await _uow.Repository<UserAuditLog>().AddAsync(new UserAuditLog{UserId=userId,Action="UNBAN",PerformedByAdminId=adminId});
        await _uow.SaveChangesAsync();return true;
    }

    public async Task<bool> UpdateUserRoleAsync(Guid adminId,Guid userId,string role)
    {
        var u=await _uow.Repository<User>().GetByIdAsync(userId);
        if(u==null||!Enum.TryParse<UserRole>(role,out var ur))return false;
        u.Role=ur;await _uow.Repository<User>().UpdateAsync(u);
        await _uow.Repository<UserAuditLog>().AddAsync(new UserAuditLog{UserId=userId,Action="ROLE_CHANGE",PerformedByAdminId=adminId,Details=$"Role set to {role}"});
        await _uow.SaveChangesAsync();return true;
    }

    public async Task<AdminDashboardDto> GetDashboardStatsAsync()
    {
        var today=DateTime.UtcNow.Date;
        return new AdminDashboardDto
        {
            TotalUsers=await _uow.Repository<User>().CountAsync(),
            ActiveUsers=await _uow.Repository<User>().CountAsync(u=>u.Status==UserStatus.Active),
            BannedUsers=await _uow.Repository<User>().CountAsync(u=>u.Status==UserStatus.Banned),
            TotalGames=await _uow.Repository<Game>().CountAsync(),
            ActiveSessions=await _uow.Repository<GameSession>().CountAsync(s=>s.Status==GameStatus.InProgress||s.Status==GameStatus.Waiting),
            NewUsersToday=await _uow.Repository<User>().CountAsync(u=>u.CreatedAt>=today),
            NewUsersThisWeek=await _uow.Repository<User>().CountAsync(u=>u.CreatedAt>=today.AddDays(-7)),
            NewUsersThisMonth=await _uow.Repository<User>().CountAsync(u=>u.CreatedAt>=today.AddDays(-30)),
            TopPlayers=(await _uow.Repository<PlayerProfile>().Query().Include(p=>p.User).OrderByDescending(p=>p.RankPoints).Take(10).ToListAsync()).Select(p=>new TopPlayerDto{UserId=p.UserId,Username=p.User.Username,Level=p.Level,RankPoints=p.RankPoints,TotalWins=p.TotalWins})
        };
    }

    public async Task<bool> CreditCurrencyAsync(Guid adminId,CreditCurrencyDto req)
    {
        var prof=await _uow.Repository<PlayerProfile>().FirstOrDefaultAsync(p=>p.UserId==req.UserId);if(prof==null)return false;
        prof.Coins+=req.CoinsAmount;prof.Gems+=req.GemsAmount;
        await _uow.Repository<PlayerProfile>().UpdateAsync(prof);
        await _uow.Repository<CurrencyTransaction>().AddAsync(new CurrencyTransaction{PlayerProfileId=prof.Id,Type=TransactionType.AdminCredit,CoinsAmount=req.CoinsAmount,GemsAmount=req.GemsAmount,Description=req.Reason,CreatedByAdminId=adminId});
        await _uow.SaveChangesAsync();return true;
    }

    public async Task<PagedResultDto<AuditLogDto>> GetAuditLogsAsync(int page,int pageSize)
    {
        var q=_uow.Repository<UserAuditLog>().Query().Include(a=>a.User).OrderByDescending(a=>a.CreatedAt);
        var total=await q.CountAsync();
        var items=await q.Skip((page-1)*pageSize).Take(pageSize).ToListAsync();
        return new PagedResultDto<AuditLogDto>{Items=items.Select(a=>new AuditLogDto{Id=a.Id,UserId=a.UserId,Username=a.User.Username,Action=a.Action,IpAddress=a.IpAddress,Details=a.Details,PerformedByAdminId=a.PerformedByAdminId,CreatedAt=a.CreatedAt}),TotalCount=total,Page=page,PageSize=pageSize};
    }

    public async Task<bool> SendNotificationAsync(SendNotificationDto req)
    {
        if(req.UserId.HasValue)
            await _uow.Repository<Notification>().AddAsync(new Notification{UserId=req.UserId.Value,Title=req.Title,Message=req.Message,Type=req.Type,ActionUrl=req.ActionUrl});
        else
        {
            var users=await _uow.Repository<User>().FindAsync(u=>u.Status==UserStatus.Active);
            await _uow.Repository<Notification>().AddRangeAsync(users.Select(u=>new Notification{UserId=u.Id,Title=req.Title,Message=req.Message,Type=req.Type,ActionUrl=req.ActionUrl}));
        }
        await _uow.SaveChangesAsync();return true;
    }

    public async Task<IEnumerable<GameDto>> GetAllGamesAsync()=>(await _uow.Repository<Game>().GetAllAsync()).Select(MG);

    public async Task<GameDto> CreateGameAsync(CreateGameDto req)
    {
        var g=new Game{Name=req.Name,Description=req.Description,Type=Enum.Parse<GameType>(req.Type),MaxPlayers=req.MaxPlayers,MinPlayers=req.MinPlayers,MapName=req.MapName,DurationSeconds=req.DurationSeconds,RewardCoins=req.RewardCoins,RewardExperience=req.RewardExperience,IconUrl=req.IconUrl};
        await _uow.Repository<Game>().AddAsync(g);await _uow.SaveChangesAsync();return MG(g);
    }

    public async Task<GameDto> UpdateGameAsync(Guid id,UpdateGameDto req)
    {
        var g=await _uow.Repository<Game>().GetByIdAsync(id)??throw new KeyNotFoundException("Game not found.");
        g.Name=req.Name;g.Description=req.Description;g.Type=Enum.Parse<GameType>(req.Type);g.MaxPlayers=req.MaxPlayers;g.MinPlayers=req.MinPlayers;g.MapName=req.MapName;g.DurationSeconds=req.DurationSeconds;g.RewardCoins=req.RewardCoins;g.RewardExperience=req.RewardExperience;g.IconUrl=req.IconUrl;g.IsActive=req.IsActive;
        await _uow.Repository<Game>().UpdateAsync(g);await _uow.SaveChangesAsync();return MG(g);
    }

    public async Task<bool> DeleteGameAsync(Guid id){var g=await _uow.Repository<Game>().GetByIdAsync(id);if(g==null)return false;g.IsDeleted=true;await _uow.Repository<Game>().UpdateAsync(g);await _uow.SaveChangesAsync();return true;}

    public async Task<IEnumerable<SystemSettingDto>> GetSettingsAsync()=>(await _uow.Repository<SystemSetting>().GetAllAsync()).Select(s=>new SystemSettingDto{Id=s.Id,Key=s.Key,Value=s.Value,Description=s.Description,IsPublic=s.IsPublic});

    public async Task<bool> UpdateSettingAsync(string key,string value){var s=await _uow.Repository<SystemSetting>().FirstOrDefaultAsync(x=>x.Key==key);if(s==null)return false;s.Value=value;await _uow.Repository<SystemSetting>().UpdateAsync(s);await _uow.SaveChangesAsync();return true;}

    private static AdminUserDto MU(User u)=>new(){Id=u.Id,Username=u.Username,Email=u.Email,Role=u.Role.ToString(),Status=u.Status.ToString(),FirstName=u.FirstName,LastName=u.LastName,Country=u.Country,IsEmailVerified=u.IsEmailVerified,LastLoginAt=u.LastLoginAt,LastLoginIp=u.LastLoginIp,CreatedAt=u.CreatedAt,BanReason=u.BanReason,BannedAt=u.BannedAt,TotalGamesPlayed=u.PlayerProfile?.TotalGamesPlayed??0,Level=u.PlayerProfile?.Level??0,Coins=u.PlayerProfile?.Coins??0,Gems=u.PlayerProfile?.Gems??0};
    private static GameDto MG(Game g)=>new(){Id=g.Id,Name=g.Name,Description=g.Description,Type=g.Type.ToString(),MaxPlayers=g.MaxPlayers,MinPlayers=g.MinPlayers,IsActive=g.IsActive,MapName=g.MapName,DurationSeconds=g.DurationSeconds,RewardCoins=g.RewardCoins,RewardExperience=g.RewardExperience,IconUrl=g.IconUrl};
}
