using api.Cache;
using api.Data;
using api.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecalculateController : ControllerBase
{
    private readonly DataContext _context;
    private readonly ICacheService _cacheService;
    private readonly RecalculationSettings _settings;

    public RecalculateController(DataContext context, ICacheService cacheService, IOptions<RecalculationSettings> settings)
    {
        _context = context;
        _cacheService = cacheService;
        _settings = settings.Value;
    }

    [HttpPost("recalculate")]
    public async Task<IActionResult> Recalculate()
    {
        var isRecalculationEnabled = _settings.EnableRecalculation;
        if (!isRecalculationEnabled)
        {
            return BadRequest("Recalculation is not enabled");
        }

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
