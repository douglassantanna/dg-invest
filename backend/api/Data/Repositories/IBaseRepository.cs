using System.Linq.Expressions;
using api.Shared;
using Microsoft.EntityFrameworkCore.Query;

namespace api.Data.Repositories;
public interface IBaseRepository<T> where T : Entity
{
    Task<T?> GetByIdAsync(int id, Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null);
    Task<IEnumerable<T>> GetAll(Expression<Func<T, bool>> filter = null, Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null);
    Task AddAsync(T entity);
    Task AddBatchAsync(List<T> entities);
    Task UpdateAsync(T entity);
    void Delete(T entity);
}
