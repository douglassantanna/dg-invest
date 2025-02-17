using api.Cache;
using api.Cryptos.Exceptions;
using api.Cryptos.Models;
using api.Cryptos.TransactionStrategies.Contracts;
using api.Data;
using api.Models.Cryptos;
using api.Shared;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Cryptos.Commands;
public record AddTransactionCommand(decimal Amount,
                                    decimal Price,
                                    DateTimeOffset PurchaseDate,
                                    string ExchangeName,
                                    ETransactionType TransactionType,
                                    int CryptoAssetId,
                                    int UserId,
                                    decimal Fee) : IRequest<Response>;

public class AddTransactionCommandValidator : AbstractValidator<AddTransactionCommand>
{
    public AddTransactionCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.CryptoAssetId).GreaterThan(0);
        RuleFor(x => x.ExchangeName).NotEmpty();
        RuleFor(x => x.TransactionType).IsInEnum();
        RuleFor(x => x.PurchaseDate)
            .NotEmpty().WithMessage("Purchase date can't be empty")
            .Must(BeInPastOrPresent)
            .WithMessage("Purchase date must be in the present or in the past");

        //add logic to check if the balance if zero before
        //adding a sell transaction. if balance is zero, return error
    }
    private bool BeInPastOrPresent(DateTimeOffset purchaseDate)
    {
        return purchaseDate <= DateTime.Now;
    }
}
public class AddTransactionCommandHandler : IRequestHandler<AddTransactionCommand, Response>
{
    private readonly ILogger<AddTransactionCommandHandler> _logger;
    private readonly ITransactionService _transactionService;
    private readonly DataContext _context;
    private readonly ICacheService _cacheService;

    public AddTransactionCommandHandler(ILogger<AddTransactionCommandHandler> logger,
                                        ITransactionService transactionService,
                                        DataContext context,
                                        ICacheService cacheService)
    {
        _logger = logger;
        _transactionService = transactionService;
        _context = context;
        _cacheService = cacheService;
    }

    public async Task<Response> Handle(AddTransactionCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await ValidateRequestAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
            _logger.LogError("AddTransactionCommandHandler. Validation failed: {0}", errors);
            return new Response("Validation failed", false, errors);
        }

        var account = await _context.Accounts
                                    .Include(x => x.CryptoAssets)
                                    .Where(x => x.UserId == request.UserId)
                                    .Where(x => x.IsSelected == true)
                                    .FirstOrDefaultAsync(cancellationToken);
        if (account == null)
        {
            _logger.LogError("AddTransactionCommandHandler. Account for UserId {0} not found.", request.UserId);
            return new Response("Account not found", false, 404);
        }

        var cryptoAsset = account.CryptoAssets.Where(x => x.Id == request.CryptoAssetId).FirstOrDefault();
        if (cryptoAsset == null)
        {
            _logger.LogError("AddTransactionCommandHandler. Crypto asset {0} not found.", request.CryptoAssetId);
            return new Response("Crypto asset not found", false, 404);
        }

        var transaction = new CryptoTransaction(request.Amount,
                                                request.Price,
                                                request.PurchaseDate,
                                                request.ExchangeName,
                                                request.TransactionType,
                                                request.Fee);
        try
        {
            var accountTransactionType = GetAccountTransactionType(request.TransactionType);
            var date = new DateTime(request.PurchaseDate.Year, request.PurchaseDate.Month, request.PurchaseDate.Day);
            var response = _transactionService.ExecuteTransaction(account,
                                                                  new AccountTransaction(date: date,
                                                                                         transactionType: accountTransactionType,
                                                                                         amount: request.Amount,
                                                                                         cryptoCurrentPrice: request.Price,
                                                                                         exchangeName: request.ExchangeName,
                                                                                         notes: string.Empty,
                                                                                         cryptoAssetId: cryptoAsset.Id,
                                                                                         cryptoAsset: cryptoAsset,
                                                                                         fee: request.Fee));
            cryptoAsset.AddTransaction(transaction);

            if (!response.IsSuccess)
            {
                _logger.LogError("AddTransactionCommandHandler. Error adding transaction: {0}", response.Message);
                return response;
            }

            _context.Accounts.Update(account);
            await _context.SaveChangesAsync(cancellationToken);

            InvalidateCache(request.UserId.ToString(), cryptoAsset.Id.ToString());

            return new Response("ok", true);
        }
        catch (CryptoAssetException ex)
        {
            _logger.LogError("AddTransactionCommandHandler. Error adding transaction: {0}", ex.Message);
            return new Response(ex.Message, false, 500);
        }
    }

    private void InvalidateCache(string userId, string cryptoAssetId)
    {
        var cachedAccountKey = $"{CacheKeyConstants.UserAccountDetails}{userId}";
        var cachedCryptoAssetKey = $"{CacheKeyConstants.UserCryptoAsset}{cryptoAssetId}";
        var cachedCryptoAssetsKey = CacheKeyConstants.GetLastCryptoAssetsCacheKeyForUser(userId);

        _cacheService.Remove(cachedAccountKey);
        _cacheService.Remove(cachedCryptoAssetKey);
        _cacheService.Remove(cachedCryptoAssetsKey);

    }
    private EAccountTransactionType GetAccountTransactionType(ETransactionType transactionType)
    {
        return transactionType == ETransactionType.Buy ? EAccountTransactionType.Out : EAccountTransactionType.In;
    }
    private async Task<ValidationResult> ValidateRequestAsync(AddTransactionCommand request)
    {
        var validation = new AddTransactionCommandValidator();
        return await validation.ValidateAsync(request);
    }
}