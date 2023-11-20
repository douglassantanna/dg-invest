using api.Data;
using api.Data.Repositories;
using api.Models.Cryptos;
using Microsoft.EntityFrameworkCore;

namespace api.Cryptos.Repositories;
public class CryptoAssetRepository : IBaseRepository<CryptoAsset>
{
    private readonly DataContext _context;

    public CryptoAssetRepository(DataContext context)
    {
        _context = context;
    }

    public void Add(CryptoAsset entity)
    {
        _context.CryptoAssets.Add(entity);
    }

    public void Delete(int id)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<CryptoAsset> GetAll()
    {
        return _context.CryptoAssets;
    }

    public CryptoAsset? GetById(int id) => _context.CryptoAssets
                                                    .Include(x => x.Transactions)
                                                    .Where(x => x.Id == id)
                                                    .FirstOrDefault();

    public Task<CryptoAsset?> GetByIdAsync(int cryptoAssetId, CancellationToken cancellationToken)
    {
        return _context.CryptoAssets
                       .Include(x => x.Transactions)
                       .Include(x => x.Addresses)
                       .Where(x => x.Id == cryptoAssetId && !x.Deleted)
                       .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpdateAsync(CryptoAsset entity)
    {
        _context.CryptoAssets.Update(entity);
        await _context.SaveChangesAsync();
    }
}
