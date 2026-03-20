using GameServer.Domain.Common;
using GameServer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameServer.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<PlayerProfile> PlayerProfiles => Set<PlayerProfile>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<GameParticipant> GameParticipants => Set<GameParticipant>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<PlayerAchievement> PlayerAchievements => Set<PlayerAchievement>();
    public DbSet<CurrencyTransaction> CurrencyTransactions => Set<CurrencyTransaction>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<UserAuditLog> UserAuditLogs => Set<UserAuditLog>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        base.OnModelCreating(m);
        m.Entity<User>(e => { e.HasKey(x=>x.Id); e.HasIndex(x=>x.Email).IsUnique(); e.HasIndex(x=>x.Username).IsUnique(); e.Property(x=>x.Email).HasMaxLength(255).IsRequired(); e.Property(x=>x.Username).HasMaxLength(50).IsRequired(); e.Property(x=>x.PasswordHash).HasMaxLength(500).IsRequired(); e.Property(x=>x.PasswordSalt).HasMaxLength(500).IsRequired(); e.HasQueryFilter(x=>!x.IsDeleted); });
        m.Entity<PlayerProfile>(e => { e.HasKey(x=>x.Id); e.HasOne(x=>x.User).WithOne(x=>x.PlayerProfile).HasForeignKey<PlayerProfile>(x=>x.UserId).OnDelete(DeleteBehavior.Cascade); e.HasQueryFilter(x=>!x.IsDeleted); e.Ignore(x=>x.WinRate); e.Ignore(x=>x.KDRatio); });
        m.Entity<Game>(e => { e.HasKey(x=>x.Id); e.Property(x=>x.Name).HasMaxLength(100).IsRequired(); e.HasQueryFilter(x=>!x.IsDeleted); });
        m.Entity<GameSession>(e => { e.HasKey(x=>x.Id); e.HasIndex(x=>x.SessionCode).IsUnique(); e.HasOne(x=>x.Game).WithMany(x=>x.Sessions).HasForeignKey(x=>x.GameId).OnDelete(DeleteBehavior.Restrict); e.HasQueryFilter(x=>!x.IsDeleted); });
        m.Entity<GameParticipant>(e => { e.HasKey(x=>x.Id); e.HasOne(x=>x.Session).WithMany(x=>x.Participants).HasForeignKey(x=>x.SessionId).OnDelete(DeleteBehavior.Cascade); e.HasOne(x=>x.User).WithMany(x=>x.GameSessions).HasForeignKey(x=>x.UserId).OnDelete(DeleteBehavior.Restrict); });
        m.Entity<Achievement>(e => { e.HasKey(x=>x.Id); e.Property(x=>x.Name).HasMaxLength(100); });
        m.Entity<PlayerAchievement>(e => { e.HasKey(x=>x.Id); e.HasOne(x=>x.PlayerProfile).WithMany(x=>x.Achievements).HasForeignKey(x=>x.PlayerProfileId).OnDelete(DeleteBehavior.Cascade); e.HasOne(x=>x.Achievement).WithMany(x=>x.PlayerAchievements).HasForeignKey(x=>x.AchievementId).OnDelete(DeleteBehavior.Restrict); });
        m.Entity<CurrencyTransaction>(e => { e.HasKey(x=>x.Id); e.HasOne(x=>x.PlayerProfile).WithMany(x=>x.Transactions).HasForeignKey(x=>x.PlayerProfileId).OnDelete(DeleteBehavior.Cascade); });
        m.Entity<Notification>(e => { e.HasKey(x=>x.Id); e.HasOne(x=>x.User).WithMany(x=>x.Notifications).HasForeignKey(x=>x.UserId).OnDelete(DeleteBehavior.Cascade); });
        m.Entity<UserAuditLog>(e => { e.HasKey(x=>x.Id); e.HasOne(x=>x.User).WithMany(x=>x.AuditLogs).HasForeignKey(x=>x.UserId).OnDelete(DeleteBehavior.Cascade); });
        m.Entity<SystemSetting>(e => { e.HasKey(x=>x.Id); e.HasIndex(x=>x.Key).IsUnique(); e.Property(x=>x.Key).HasMaxLength(100).IsRequired();
            e.HasData(
                new SystemSetting { Id=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Key="MaintenanceMode", Value="false", Description="Enable maintenance mode", IsPublic=true, CreatedAt=new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc) },
                new SystemSetting { Id=Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), Key="RegistrationEnabled", Value="true", Description="Allow new registrations", IsPublic=true, CreatedAt=new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc) }
            );
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var e in ChangeTracker.Entries<BaseEntity>())
            if (e.State == EntityState.Modified)
                e.Entity.UpdatedAt = DateTime.UtcNow;
        return await base.SaveChangesAsync(ct);
    }
}
