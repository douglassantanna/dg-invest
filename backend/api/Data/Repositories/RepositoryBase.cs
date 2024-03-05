
using System.Linq.Expressions;
using api.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace api.Data.Repositories;
public class RepositoryBase<T> : IBaseRepository<T> where T : Entity
{
    public readonly DbSet<T> _DbSet;
    private readonly DataContext _context;

    public RepositoryBase(DataContext context)
    {
        _DbSet = context.Set<T>();
        _context = context;
    }

    public async Task AddAsync(T entity)
    {
        await _DbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public void Delete(T entity)
    {
        _DbSet.Remove(entity);
        _context.SaveChanges();
    }

    public async Task<IEnumerable<T>> GetAll(Expression<Func<T, bool>> filter = null,
                                                   Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null)
    {
        var query = _DbSet.AsQueryable();

        if (filter != null)
            query = query
                .Where(filter)
                .AsNoTracking();

        if (include != null)
            query = include(query);

        return await query.ToListAsync();
    }

    public async Task<T?> GetByIdAsync(int id, Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null)
    {
        IQueryable<T> query = _context.Set<T>();

        if (include != null)
        {
            query = include(query);
        }

        return await query.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task UpdateAsync(T entity)
    {
        _context.Update(entity);
        await _context.SaveChangesAsync();
    }
}
