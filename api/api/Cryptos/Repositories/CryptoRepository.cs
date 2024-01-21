using api.Cryptos.Models;
using api.Data;
using api.Data.Repositories;
using api.Models.Cryptos;

namespace api.Cryptos.Repositories;
public class CryptoRepository : IBaseRepository<Crypto>
{
    private readonly DataContext _context;

    public CryptoRepository(DataContext context)
    {
        _context = context;
    }

    public void AddAsync(Crypto entity)
    {
        _context.Cryptos.Add(entity);
    }

    public void Delete(int id)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<Crypto> GetAll()
    {
        return _context.Cryptos.OrderByDescending(c => c.Name);
    }

    public Task<bool> IsCryptoAssetInUserListAsync(int coinMarketCapId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Crypto? GetByIdAsync(int id) => _context.Cryptos
                                              .Where(x => x.Id == id)
                                              .FirstOrDefault();

    public Task<CryptoAsset?> GetByIdAsync(int cryptoAssetId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public bool IsUnique(string data)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateAsync(Crypto entity)
    {
        _context.Cryptos.Update(entity);
        await _context.SaveChangesAsync();
    }
}
