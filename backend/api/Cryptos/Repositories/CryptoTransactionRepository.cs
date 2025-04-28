// using api.Data;
// using api.Data.Repositories;
// using api.Models.Cryptos;

// namespace api.Cryptos.Repositories;
// public interface ICryptoTransactionRepository { }
// public class CryptoTransactionRepository : ICryptoTransactionRepository
// {
//     private readonly DataContext _context;

//     public CryptoTransactionRepository(DataContext context)
//     {
//         _context = context;
//     }

//     public void AddAsync(CryptoTransaction entity)
//     {
//         throw new NotImplementedException();
//     }

//     public void Delete(int id)
//     {
//         throw new NotImplementedException();
//     }

//     public IEnumerable<CryptoTransaction> GetAll()
//     {
//         throw new NotImplementedException();
//     }

//     public Task<bool> IsCryptoAssetInUserListAsync(int coinMarketCapId, CancellationToken cancellationToken)
//     {
//         throw new NotImplementedException();
//     }

//     public CryptoTransaction? GetByIdAsync(int id)
//     {
//         throw new NotImplementedException();
//     }

//     public Task<CryptoAsset?> GetByIdAsync(int cryptoAssetId, CancellationToken cancellationToken)
//     {
//         throw new NotImplementedException();
//     }

//     public bool IsUnique(string data)
//     {
//         throw new NotImplementedException();
//     }

//     public async Task UpdateAsync(CryptoTransaction entity)
//     {
//         _context.CryptoTransactions.Update(entity);
//         await _context.SaveChangesAsync();
//     }
// }
