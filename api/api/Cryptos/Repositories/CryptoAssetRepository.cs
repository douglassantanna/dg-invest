using api.Data;
using api.Data.Repositories;
using api.Models.Cryptos;

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
        throw new NotImplementedException();
    }

    public void Delete(int id)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<CryptoAsset> GetAll()
    {
        throw new NotImplementedException();
    }

    public CryptoAsset? GetById(int id) => _context.CryptoAssets.Where(x => x.Id == id).FirstOrDefault();

    public async Task UpdateAsync(CryptoAsset entity)
    {
        _context.CryptoAssets.Update(entity);
        await _context.SaveChangesAsync();
    }
}
