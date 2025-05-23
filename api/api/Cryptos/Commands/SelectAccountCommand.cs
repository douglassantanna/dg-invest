using api.Cache;
using api.Data;
using api.Shared;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Cryptos.Commands;
public record SelectAccountCommand(int UserId, int AccountId) : IRequest<Response>;
public record SelectAccountRequest(int AccountId);
public class SelectAccountCommandValidator : AbstractValidator<SelectAccountCommand>
{
    public SelectAccountCommandValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.AccountId).GreaterThan(0);
    }
}

public class SelectAccountCommandHandler : IRequestHandler<SelectAccountCommand, Response>
{
    private readonly DataContext _context;
    private readonly ILogger<SelectAccountCommandHandler> _logger;
    private readonly ICacheService _cacheService;

    public SelectAccountCommandHandler(DataContext context, ILogger<SelectAccountCommandHandler> logger, ICacheService cacheService)
    {
        _context = context;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<Response> Handle(SelectAccountCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await ValidateRequestAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
            _logger.LogError("SelectAccountCommandHandler. Validation failed: {0}", errors);
            return new Response("Validation failed", false, errors);
        }

        var user = await _context.Users
                                 .Include(x => x.Accounts)
                                 .FirstOrDefaultAsync(a => a.Id == request.UserId, cancellationToken);
        if (user == null)
        {
            _logger.LogError("SelectAccountCommandHandler. User not found: {0}", request.UserId);
            return new Response("User not found", false, 404);
        }

        user.SelectAccount(request.AccountId);

        try
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync(cancellationToken);
            InvalidateCache(request);

            return new Response("Account selected successfully", true);
        }
        catch (Exception ex)
        {
            _logger.LogError("SelectAccountCommandHandler. Error selecting account: {0}", ex.Message);
            return new Response("Error selecting account", false);
        }
    }

    private void InvalidateCache(SelectAccountCommand request)
    {
        var keysToRemove = new List<string>
        {
            $"{CacheKeyConstants.UserAccounts}{request.UserId}",
            $"{CacheKeyConstants.UserAccountDetails}{request.UserId}",
            CacheKeyConstants.GetLastCryptoAssetsCacheKeyForUser(request.UserId.ToString())
        };

        keysToRemove.AddRange(CacheKeyConstants.GetAllUserMarketDataCacheKeys(request.UserId));

        foreach (var key in keysToRemove.Where(k => !string.IsNullOrEmpty(k)))
        {
            _cacheService.Remove(key);
        }
    }


    private async Task<ValidationResult> ValidateRequestAsync(SelectAccountCommand request)
    {
        var validation = new SelectAccountCommandValidator();
        return await validation.ValidateAsync(request);
    }
}