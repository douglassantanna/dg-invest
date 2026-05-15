using api.Cache;
using api.Data;
using api.Users.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers;

[ApiController]
// [Authorize(Roles = nameof(Role.Admin))]
[Route("api/[controller]")]
public class RecalculateController : ControllerBase
{
    private readonly DataContext _context;
    private readonly ICacheService _cacheService;

    public RecalculateController(DataContext context, ICacheService cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }

    [HttpPost("recalculate")]
    public async Task<IActionResult> Recalculate()
    {
        var accounts = await _context.Accounts
            .Include(a => a.CryptoAssets)
                .ThenInclude(ca => ca.Transactions)
            .ToListAsync();

        foreach (var account in accounts)
        {
            foreach (var asset in account.CryptoAssets)
            {
                asset.RecalculateFromTransactions();

                // evict per-asset cache
                _cacheService.Remove($"{CacheKeyConstants.UserCryptoAsset}{asset.Id}");
            }

            // evict per-user caches
            _cacheService.Remove($"{CacheKeyConstants.UserAccountDetails}{account.UserId}");
            _cacheService.Remove(CacheKeyConstants.GetLastCryptoAssetsCacheKeyForUser(account.UserId.ToString()));
        }

        await _context.SaveChangesAsync();
        return Ok("Recalculated successfully");
    }
}
