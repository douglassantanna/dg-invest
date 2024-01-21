using System.Linq.Expressions;
using api.Data.Repositories;
using api.Models.Cryptos;
using Microsoft.EntityFrameworkCore.Query;

namespace api.Cryptos.Repositories;
public interface ICryptoAssetRepository
{
    Task AddAsync(CryptoAsset entity);
    Task UpdateAsync(CryptoAsset entity);
    Task<IEnumerable<CryptoAsset>> GetAll(
        Expression<Func<CryptoAsset, bool>> filter = null,
        Func<IQueryable<CryptoAsset>, IIncludableQueryable<CryptoAsset, object>> include = null);
    Task<CryptoAsset?> GetByIdAsync(
        int id,
        Func<IQueryable<CryptoAsset>, IIncludableQueryable<CryptoAsset, object>> include = null);
}

public class CryptoAssetRepository : ICryptoAssetRepository
{
    private readonly IBaseRepository<CryptoAsset> _baseRepository;
    public CryptoAssetRepository(IBaseRepository<CryptoAsset> baseRepository) => _baseRepository = baseRepository;
    public async Task AddAsync(CryptoAsset entity) => await _baseRepository.AddAsync(entity);
    public async Task<IEnumerable<CryptoAsset>> GetAll(Expression<Func<CryptoAsset, bool>> filter = null, Func<IQueryable<CryptoAsset>, IIncludableQueryable<CryptoAsset, object>> include = null) => await _baseRepository.GetAll(filter);
    public async Task<CryptoAsset?> GetByIdAsync(int id, Func<IQueryable<CryptoAsset>, IIncludableQueryable<CryptoAsset, object>> include = null) => await _baseRepository.GetByIdAsync(id, include);
    public async Task UpdateAsync(CryptoAsset entity) => await _baseRepository.UpdateAsync(entity);
}
