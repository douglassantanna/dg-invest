using api.Data;
using api.Data.Repositories;
using api.Models.Cryptos;
using api.Users.Models;

namespace api.Users.Repositories;
public class UserRepository : IBaseRepository<User>
{
    private readonly DataContext _context;

    public UserRepository(DataContext context)
    {
        _context = context;
    }

    public void Add(User entity)
    {
        _context.Users.Add(entity);
        _context.SaveChanges();
    }

    public void Delete(int id)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<User> GetAll()
    {
        throw new NotImplementedException();
    }

    public Task<bool> GetByCoinMarketCapIdAsync(int coinMarketCapId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public User? GetById(int id)
    {
        throw new NotImplementedException();
    }

    public Task<CryptoAsset?> GetByIdAsync(int cryptoAssetId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public bool IsUnique(string data)
    {
        return _context.Users.Any(x => x.Email == data);
    }

    public Task UpdateAsync(User entity)
    {
        throw new NotImplementedException();
    }
}
