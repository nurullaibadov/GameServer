using GameServer.Domain.Interfaces;
using GameServer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace GameServer.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _ctx;
    private readonly Dictionary<Type,object> _repos = new();
    private IDbContextTransaction? _tx;
    public UnitOfWork(AppDbContext ctx) => _ctx = ctx;
    public IGenericRepository<T> Repository<T>() where T : class
    {
        if(!_repos.TryGetValue(typeof(T),out var r)) { r=new GenericRepository<T>(_ctx); _repos[typeof(T)]=r; }
        return (IGenericRepository<T>)r;
    }
    public async Task<int> SaveChangesAsync() => await _ctx.SaveChangesAsync();
    public async Task BeginTransactionAsync() => _tx=await _ctx.Database.BeginTransactionAsync();
    public async Task CommitTransactionAsync() { await _ctx.SaveChangesAsync(); if(_tx!=null)await _tx.CommitAsync(); }
    public async Task RollbackTransactionAsync() { if(_tx!=null)await _tx.RollbackAsync(); }
    public void Dispose() { _tx?.Dispose(); _ctx.Dispose(); }
}
