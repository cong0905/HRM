using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using HRM.DAL.Context;

namespace HRM.DAL.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly HrmDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(HrmDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<List<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        var entry = await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entry.Entity;
    }

    public virtual async Task UpdateAsync(T entity)
    {
        var entry = _context.Entry(entity);
        if (entry.State == EntityState.Detached)
            _dbSet.Attach(entity);
        entry.State = EntityState.Modified;
        await _context.SaveChangesAsync();
        entry.State = EntityState.Detached;
    }

    public virtual async Task DeleteAsync(T entity)
    {
        var entry = _context.Entry(entity);
        if (entry.State == EntityState.Detached)
            _dbSet.Attach(entity);
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        // Entity is auto-detached after deletion
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        return predicate == null
            ? await _dbSet.CountAsync()
            : await _dbSet.CountAsync(predicate);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
