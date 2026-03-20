using System.Linq.Expressions;
using GameServer.Domain.Interfaces;
using GameServer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GameServer.Infrastructure.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly AppDbContext _ctx;
    protected readonly DbSet<T> _set;
    public GenericRepository(AppDbContext ctx) { _ctx = ctx; _set = ctx.Set<T>(); }
    public async Task<T?> GetByIdAsync(Guid id) => await _set.FindAsync(id);
    public async Task<IEnumerable<T>> GetAllAsync() => await _set.ToListAsync();
    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T,bool>> p) => await _set.Where(p).ToListAsync();
    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T,bool>> p) => await _set.FirstOrDefaultAsync(p);
    public async Task<bool> AnyAsync(Expression<Func<T,bool>> p) => await _set.AnyAsync(p);
    public async Task<int> CountAsync(Expression<Func<T,bool>>? p=null) => p==null?await _set.CountAsync():await _set.CountAsync(p);
    public async Task<T> AddAsync(T e) { await _set.AddAsync(e); return e; }
    public async Task AddRangeAsync(IEnumerable<T> e) => await _set.AddRangeAsync(e);
    public Task UpdateAsync(T e) { _set.Update(e); return Task.CompletedTask; }
    public Task DeleteAsync(T e) { _set.Remove(e); return Task.CompletedTask; }
    public IQueryable<T> Query() => _set.AsQueryable();
}
