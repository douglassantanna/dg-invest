using System.Linq.Expressions;
using api.Data;
using api.Data.Repositories;
using api.Shared;
using api.Users.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace api.Users.Repositories;

public interface IUserRepository
{
    Task AddAsync(User entity);
    Task UpdateAsync(User entity);
    Task<IEnumerable<User>> GetAll(Expression<Func<User, bool>> filter = null,
                                   Func<IQueryable<User>, IIncludableQueryable<User, object>> include = null);
    Task<bool> IsCryptoAssetInUserListAsync(int userId);
    Task<Result<User?>> GetByIdAsync(int id, Func<IQueryable<User>, IIncludableQueryable<User, object>> include = null);
    Task<bool> IsUnique(string email);
}
public class UserRepository : IUserRepository
{
    private readonly IBaseRepository<User> _baseRepository;
    private readonly DataContext _dataContext;

    public UserRepository(
        IBaseRepository<User> baseRepository,
        DataContext dataContext)
    {
        _baseRepository = baseRepository;
        _dataContext = dataContext;
    }

    public async Task AddAsync(User entity) => await _baseRepository.AddAsync(entity);

    public async Task<IEnumerable<User>> GetAll(
        Expression<Func<User, bool>> filter = null,
        Func<IQueryable<User>, IIncludableQueryable<User, object>> include = null)
        => await _baseRepository.GetAll(filter);

    public async Task UpdateAsync(User entity) => await _baseRepository.UpdateAsync(entity);

    public async Task<bool> IsCryptoAssetInUserListAsync(int userId)
       => await _dataContext.CryptoAssets.AnyAsync(x => x.Id == userId);

    public async Task<Result<User?>> GetByIdAsync(
        int id,
        Func<IQueryable<User>, IIncludableQueryable<User, object>> include = null)
    {
        try
        {
            var user = await _baseRepository.GetByIdAsync(id, include);
            if (user == null)
            {
                return Result<User?>.Failure($"User with ID {id} not found.");
            }
            return Result<User?>.Success(user);
        }
        catch (Exception ex)
        {
            return Result<User?>.Failure("An error occurred while retrieving the user.");
        }
    }

    public Task<bool> IsUnique(string email) => _dataContext.Users.AnyAsync(x => x.Email == email);
}
