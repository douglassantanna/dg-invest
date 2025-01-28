using api.Cache;
using api.Data;
using api.Models.Cryptos;
using api.Shared;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Cryptos.Commands;
public record AddCryptoAssetToAccountListCommand(int UserId, int CoinMarketCapId, string Symbol)
    : IRequest<Response>;
public record AddCryptoAssetToAccountListRequest(int CoinMarketCapId, string Symbol);
public class AddCryptoAssetToAccountListCommandValidator : AbstractValidator<AddCryptoAssetToAccountListCommand>
{
    public AddCryptoAssetToAccountListCommandValidator()
    {
        RuleFor(x => x.UserId).NotNull().WithMessage("UserId can't be null");
        RuleFor(x => x.CoinMarketCapId).NotEmpty().WithMessage("CoinMarketCapId can't be empty");
        RuleFor(x => x.Symbol)
            .NotEmpty().WithMessage("Symbol can't be empty")
            .Length(1, 255).WithMessage("Symbol must be between 1 and 255 characters");
    }
}

public class AddCryptoAssetToAccountListCommandHandler : IRequestHandler<AddCryptoAssetToAccountListCommand, Response>
{
    private readonly DataContext _context;
    private readonly ILogger<AddCryptoAssetToAccountListCommandHandler> _logger;
    private readonly ICacheService _cacheService;

    public AddCryptoAssetToAccountListCommandHandler(
        DataContext context,
        ILogger<AddCryptoAssetToAccountListCommandHandler> logger,
        ICacheService cacheService)
    {
        _context = context;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<Response> Handle(AddCryptoAssetToAccountListCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("AddCryptoAssetToAccountListCommandHandler");

            var validationResult = await ValidateRequestAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                _logger.LogError("AddCryptoAssetToAccountListCommandHandler. Validation failed: {0}", errors);
                return new Response("Validation failed!", false, errors);
            }

            var account = await _context.Accounts.Include(x => x.CryptoAssets)
                                                .Where(x => x.UserId == request.UserId)
                                                .Where(x => x.IsSelected == true)
                                                .FirstOrDefaultAsync(cancellationToken);
            if (account == null)
            {
                _logger.LogError("AddCryptoAssetToAccountListCommandHandler. Account not found: {0}", request.UserId);
                return new Response("Account not found!", false, 404);
            }

            var cryptoAssetResult = account.AddCryptoAsset(new CryptoAsset("USD", request.Symbol, request.Symbol, request.CoinMarketCapId));
            if (!cryptoAssetResult.IsSuccess)
            {
                _logger.LogError("AddCryptoAssetToAccountListCommandHandler. AddCryptoAsset failed: {0}", cryptoAssetResult.Message);
                return cryptoAssetResult;
            }

            _context.Accounts.Update(account);
            await _context.SaveChangesAsync(cancellationToken);
            var cachedCryptoAssets = CacheKeyConstants.GetLastCryptoAssetsCacheKeyForUser(request.UserId.ToString());
            _cacheService.Remove(cachedCryptoAssets);
            return new Response("", true);
        }
        catch (Exception ex)
        {
            _logger.LogError("AddCryptoAssetToAccountListCommandHandler. Error: {0}", ex.Message);
            return new Response("Error adding crypto asset. Try again later!", false, 500);
        }
    }
    private async Task<ValidationResult> ValidateRequestAsync(AddCryptoAssetToAccountListCommand request)
    {
        var validation = new AddCryptoAssetToAccountListCommandValidator();
        return await validation.ValidateAsync(request);
    }
}
