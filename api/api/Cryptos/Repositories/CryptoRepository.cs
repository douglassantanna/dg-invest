using System.Linq.Expressions;
using api.Cryptos.Models;
using api.Data;
using api.Data.Repositories;
using Microsoft.EntityFrameworkCore.Query;

namespace api.Cryptos.Repositories;
public interface ICryptoRepository
{
    Task AddAsync(Crypto entity);
    Task UpdateAsync(Crypto entity);
    IEnumerable<Crypto> GetAll(
        Expression<Func<Crypto, bool>> filter = null,
        Func<IQueryable<Crypto>, IIncludableQueryable<Crypto, object>> include = null);
    Task<Crypto?> GetByIdAsync(
        int id,
        Func<IQueryable<Crypto>, IIncludableQueryable<Crypto, object>> include = null);
}
public class CryptoRepository : ICryptoRepository
{
    private readonly DataContext _dataContext;
    private readonly IBaseRepository<Crypto> _baseRepository;
    public CryptoRepository(IBaseRepository<Crypto> baseRepository, DataContext dataContext)
    {
        _baseRepository = baseRepository;
        _dataContext = dataContext;
    }

    public async Task AddAsync(Crypto entity) => await _baseRepository.AddAsync(entity);
    public IEnumerable<Crypto> GetAll(Expression<Func<Crypto, bool>> filter = null, Func<IQueryable<Crypto>, IIncludableQueryable<Crypto, object>> include = null)
    {
        return _dataContext.Cryptos.OrderBy(x => x.Symbol);
    }
    public async Task<Crypto?> GetByIdAsync(int id, Func<IQueryable<Crypto>, IIncludableQueryable<Crypto, object>> include = null) => await _baseRepository.GetByIdAsync(id, include);
    public async Task UpdateAsync(Crypto entity) => await _baseRepository.UpdateAsync(entity);
}
