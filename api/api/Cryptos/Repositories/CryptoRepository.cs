using System.Linq.Expressions;
using api.Cryptos.Models;
using api.Data.Repositories;
using Microsoft.EntityFrameworkCore.Query;

namespace api.Cryptos.Repositories;
public interface ICryptoRepository
{
    Task AddAsync(Crypto entity);
    Task UpdateAsync(Crypto entity);
    Task<IEnumerable<Crypto>> GetAll(
        Expression<Func<Crypto, bool>> filter = null,
        Func<IQueryable<Crypto>, IIncludableQueryable<Crypto, object>> include = null);
    Task<Crypto?> GetByIdAsync(
        int id,
        Func<IQueryable<Crypto>, IIncludableQueryable<Crypto, object>> include = null);
}
public class CryptoRepository : ICryptoRepository
{
    private readonly IBaseRepository<Crypto> _baseRepository;
    public CryptoRepository(IBaseRepository<Crypto> baseRepository) => _baseRepository = baseRepository;
    public async Task AddAsync(Crypto entity) => await _baseRepository.AddAsync(entity);
    public async Task<IEnumerable<Crypto>> GetAll(Expression<Func<Crypto, bool>> filter = null, Func<IQueryable<Crypto>, IIncludableQueryable<Crypto, object>> include = null) => await _baseRepository.GetAll(filter, include);
    public async Task<Crypto?> GetByIdAsync(int id, Func<IQueryable<Crypto>, IIncludableQueryable<Crypto, object>> include = null) => await _baseRepository.GetByIdAsync(id, include);
    public async Task UpdateAsync(Crypto entity) => await _baseRepository.UpdateAsync(entity);
}
