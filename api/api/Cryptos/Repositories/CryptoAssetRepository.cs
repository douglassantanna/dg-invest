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

    public void AddAsync(CryptoAsset entity)
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

    public async Task<bool> IsCryptoAssetInUserListAsync(int userId,
                                                      CancellationToken cancellationToken)
    => await _context.CryptoAssets.AnyAsync(x => x.UserId == userId, cancellationToken: cancellationToken);

    public CryptoAsset? GetByIdAsync(int id) => _context.CryptoAssets
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

    public bool IsUnique(string data)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateAsync(CryptoAsset entity)
    {
        _context.CryptoAssets.Update(entity);
        await _context.SaveChangesAsync();
    }
}
