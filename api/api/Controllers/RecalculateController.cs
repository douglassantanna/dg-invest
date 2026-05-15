using api.Data;
using api.Users.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers;

[ApiController]
[Authorize(Roles = nameof(Role.Admin))]
[Route("api/[controller]")]
public class RecalculateController : ControllerBase
{
    private readonly DataContext _context;

    public RecalculateController(DataContext context)
    {
        _context = context;
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
            }
        }

        await _context.SaveChangesAsync();
        return Ok("Recalculated successfully");
    }
}
