using GameServer.Application.DTOs.Player;
using GameServer.Application.Interfaces;
using GameServer.Domain.Entities;
using GameServer.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GameServer.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;
    public NotificationService(IUnitOfWork uow)=>_uow=uow;

    public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid userId,bool unreadOnly=false)
    {
        var q=_uow.Repository<Notification>().Query().Where(n=>n.UserId==userId);
        if(unreadOnly)q=q.Where(n=>!n.IsRead);
        var items=await q.OrderByDescending(n=>n.CreatedAt).Take(50).ToListAsync();
        return items.Select(n=>new NotificationDto{Id=n.Id,Title=n.Title,Message=n.Message,Type=n.Type,IsRead=n.IsRead,ReadAt=n.ReadAt,ActionUrl=n.ActionUrl,CreatedAt=n.CreatedAt});
    }

    public async Task<bool> MarkAsReadAsync(Guid userId,Guid notifId)
    {
        var n=await _uow.Repository<Notification>().FirstOrDefaultAsync(x=>x.Id==notifId&&x.UserId==userId);
        if(n==null)return false;
        n.IsRead=true;n.ReadAt=DateTime.UtcNow;
        await _uow.Repository<Notification>().UpdateAsync(n);
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkAllAsReadAsync(Guid userId)
    {
        var items=await _uow.Repository<Notification>().FindAsync(n=>n.UserId==userId&&!n.IsRead);
        foreach(var n in items){n.IsRead=true;n.ReadAt=DateTime.UtcNow;await _uow.Repository<Notification>().UpdateAsync(n);}
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task CreateNotificationAsync(Guid userId,string title,string message,string type="info")
    {
        await _uow.Repository<Notification>().AddAsync(new Notification{UserId=userId,Title=title,Message=message,Type=type});
        await _uow.SaveChangesAsync();
    }
}
