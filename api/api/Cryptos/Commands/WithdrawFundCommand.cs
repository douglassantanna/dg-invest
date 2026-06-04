using api.Cache;
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
public record WithdrawFundCommand(decimal Amount,
                                 DateTime Date,
                                 int UserId,
                                 string Notes,
                                 EAccountTransactionType TransactionType = EAccountTransactionType.WithdrawToBank,
                                 decimal? CurrentPrice = null,
                                 string? CryptoAssetId = null,
                                 string? ExchangeName = null) : IRequest<Response>;


public class WithdrawFundCommandValidator : AbstractValidator<WithdrawFundCommand>
{
    public WithdrawFundCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Withdrawal amount must be greater than zero");
        RuleFor(x => x.Notes)
            .MaximumLength(255)
            .WithMessage("Notes must be between 1 and 255 characters");

        When(x => x.TransactionType == EAccountTransactionType.WithdrawCrypto, () =>
        {
            RuleFor(x => x.CurrentPrice)
                .NotNull()
                .GreaterThan(0)
                .WithMessage("Crypto Current Price must be greater than zero");

            RuleFor(x => x.CryptoAssetId)
                .NotNull()
                .WithMessage("Crypto Asset Id must be provided");

            RuleFor(x => x.ExchangeName)
                .Length(1, 255)
                .NotEmpty()
                .WithMessage("Please provide an Exchange Name");
        });
    }
}

public class WithdrawFundCommandHandler : IRequestHandler<WithdrawFundCommand, Response>
{
    private readonly ITransactionService _transactionService;
    private readonly ILogger<WithdrawFundCommandHandler> _logger;
    private readonly DataContext _context;
    private readonly ICacheService _cacheService;

    public WithdrawFundCommandHandler(
        ILogger<WithdrawFundCommandHandler> logger,
        ITransactionService transactionService,
        DataContext context,
        ICacheService cacheService)
    {
        _logger = logger;
        _transactionService = transactionService;
        _context = context;
        _cacheService = cacheService;
    }

    public async Task<Response> Handle(WithdrawFundCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await ValidateRequestAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
            _logger.LogError("WithdrawFundCommandHandler. Validation failed: {0}", errors);
            return new Response("Validation failed", false, errors);
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

        try
        {
            var currentServerTime = DateTime.Now;
            var date = new DateTime(request.Date.Year, request.Date.Month, request.Date.Day, currentServerTime.Hour, currentServerTime.Minute, currentServerTime.Second);

            if (request.TransactionType == EAccountTransactionType.WithdrawCrypto)
            {
                _ = int.TryParse(request.CryptoAssetId, out var cryptoId);
                var cryptoAsset = account.CryptoAssets.FirstOrDefault(c => c.Id == cryptoId);
                if (cryptoAsset == null)
                {
                    _logger.LogError("WithdrawFundCommandHandler. Crypto asset {CryptoAssetId} not found.", request.CryptoAssetId);
                    return new Response("Crypto asset not found", false, 404);
                }

                var sellTransaction = new CryptoTransaction(
                    request.Amount,
                    request.CurrentPrice ?? 0,
                    date,
                    request.ExchangeName ?? string.Empty,
                    ETransactionType.Sell,
                    0
                );

                cryptoAsset.AddTransaction(sellTransaction);

                var accountTransaction = new AccountTransaction(
                    date: date,
                    transactionType: EAccountTransactionType.WithdrawCrypto,
                    amount: request.Amount,
                    cryptoCurrentPrice: request.CurrentPrice ?? 0,
                    exchangeName: request.ExchangeName ?? string.Empty,
                    notes: request.Notes,
                    cryptoAssetId: cryptoAsset.Id,
                    cryptoAsset: cryptoAsset,
                    fee: 0
                );

                var response = _transactionService.ExecuteTransaction(account, accountTransaction);
                if (!response.IsSuccess)
                {
                    _logger.LogError("WithdrawFundCommandHandler. Error adding transaction: {0}", response.Message);
                    return response;
                }
            }
            else
            {
                var accountTransaction = new AccountTransaction(date: date,
                                                                transactionType: EAccountTransactionType.WithdrawToBank,
                                                                amount: request.Amount,
                                                                notes: request.Notes);

                var response = _transactionService.ExecuteTransaction(account, accountTransaction);
                if (!response.IsSuccess)
                {
                    _logger.LogError("WithdrawFundCommandHandler. Error adding transaction: {0}", response.Message);
                    return response;
                }
            }

            _context.Accounts.Update(account);
            await _context.SaveChangesAsync(cancellationToken);

            var cachedAccount = $"{CacheKeyConstants.UserAccountDetails}{request.UserId}";
            _cacheService.Remove(cachedAccount);

            return new Response("Withdraw succesfully", true);
        }
        catch (Exception ex)
        {
            _logger.LogError("WithdrawFundCommandHandler. Error adding transaction: {0}", ex.Message);
            return new Response(ex.Message, false, 500);
        }
    }
    private async Task<ValidationResult> ValidateRequestAsync(WithdrawFundCommand request)
    {
        var validation = new WithdrawFundCommandValidator();
        return await validation.ValidateAsync(request);
    }
}