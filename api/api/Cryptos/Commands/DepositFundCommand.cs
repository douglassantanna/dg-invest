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
public record DepositFundCommand(EAccountTransactionType AccountTransactionType,
                                 decimal Amount,
                                 DateTime Date,
                                 int UserId,
                                 string Notes,
                                 decimal? CurrentPrice = null,
                                 string? CryptoAssetId = null,
                                 string? ExchangeName = null) : IRequest<Response>;
public class DepositFundCommandValidator : AbstractValidator<DepositFundCommand>
{
    public DepositFundCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Deposit amount must be greater than zero");

        RuleFor(x => x.Notes)
            .MaximumLength(255)
            .WithMessage("Notes must be between 1 and 255 characters");

        When(x => x.AccountTransactionType == EAccountTransactionType.DepositCrypto, () =>
        {
            RuleFor(x => x.CurrentPrice)
                .NotNull()
                .GreaterThan(0)
                .WithMessage("Crypto Current Price must be greater than zero");

            RuleFor(x => x.CryptoAssetId)
                .NotNull()
                .WithMessage("Crypto Asset Id must be greater than zero");

            RuleFor(x => x.ExchangeName)
                .Length(1, 255)
                .WithMessage("Exchange Name must be between 1 and 255 characters")
                .NotEmpty()
                .WithMessage("Please provide an Exchange Name");
        });
    }
}

public class DepositFundCommandHandler : IRequestHandler<DepositFundCommand, Response>
{
    private readonly DataContext _context;
    private readonly ILogger<DepositFundCommandHandler> _logger;
    private readonly ITransactionService _transactionService;
    public DepositFundCommandHandler(
        ILogger<DepositFundCommandHandler> logger,
        ITransactionService transactionService,
        DataContext context)
    {
        _logger = logger;
        _transactionService = transactionService;
        _context = context;
    }

    public async Task<Response> Handle(DepositFundCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DepositFundCommandHandler for UserId: {0}", request.UserId);
        var validationResult = await ValidateRequestAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
            _logger.LogError("DepositFundCommandHandler. Validation failed: {0}", errors);
            return new Response("Validation failed", false, errors);
        }

        var account = await _context.Accounts.Include(a => a.CryptoAssets)
                                            .Where(ac => ac.IsSelected == true)
                                            .Where(ac => ac.UserId == request.UserId)
                                            .FirstOrDefaultAsync(cancellationToken);
        if (account == null)
        {
            _logger.LogError("DepositFundCommandHandler. Account from UserId {0} not found.", request.UserId);
            return new Response("Account not found", false, 404);
        }

        try
        {
            var currentServerTime = DateTime.Now;
            var date = new DateTime(request.Date.Year, request.Date.Month, request.Date.Day, currentServerTime.Hour, currentServerTime.Minute, currentServerTime.Second);
            var accountTransactionType = GetAccountTransactionType(request.AccountTransactionType);
            var newAccountTransaction = CreateAccountTransaction(request, date, accountTransactionType, []);
            if (newAccountTransaction == null)
            {
                _logger.LogError("DepositFundCommandHandler. Crypto asset {0} not found.", request.CryptoAssetId);
                return new Response("Crypto asset not found", false, 404);
            }

            var response = _transactionService.ExecuteTransaction(account, newAccountTransaction);
            if (!response.IsSuccess)
            {
                _logger.LogError("DepositFundCommandHandler. Error adding transaction: {0}", response.Message);
                return response;
            }

            _context.Accounts.Update(account);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("DepositFundCommandHandler. Deposit added for UserId: {0}", request.UserId);
            return new Response("Deposit added succesfully", true);
        }
        catch (Exception ex)
        {
            _logger.LogError("DepositFundCommandHandler. Error adding transaction: {0}", ex.Message);
            return new Response(ex.Message, false);
        }
    }
    private EAccountTransactionType GetAccountTransactionType(EAccountTransactionType accountTransactionType)
    {
        return accountTransactionType == EAccountTransactionType.DepositFiat ? EAccountTransactionType.DepositFiat : EAccountTransactionType.DepositCrypto;
    }
    private async Task<ValidationResult> ValidateRequestAsync(DepositFundCommand request)
    {
        var validation = new DepositFundCommandValidator();
        return await validation.ValidateAsync(request);
    }
    private AccountTransaction? CreateAccountTransaction(DepositFundCommand request,
                                                                     DateTime date,
                                                                     EAccountTransactionType accountTransactionType,
                                                                     IReadOnlyCollection<CryptoAsset> cryptoAssets)
    {
        if (accountTransactionType == EAccountTransactionType.DepositCrypto)
        {
            _ = int.TryParse(request.CryptoAssetId, out var cryptoId);
            var cryptoAsset = cryptoAssets.FirstOrDefault(c => c.Id == cryptoId);
            if (cryptoAsset == null)
                return null;


            var buyTransaction = new CryptoTransaction(
                request.Amount,
                request.CurrentPrice ?? 0,
                request.Date,
                request.ExchangeName ?? string.Empty,
                ETransactionType.Buy,
                0
            );

            cryptoAsset.AddTransaction(buyTransaction);

            return new AccountTransaction(
                date: date,
                transactionType: accountTransactionType,
                amount: request.Amount,
                cryptoCurrentPrice: request.CurrentPrice ?? 0,
                exchangeName: request.ExchangeName ?? string.Empty,
                notes: request.Notes,
                cryptoAssetId: cryptoAsset.Id,
                cryptoAsset: cryptoAsset,
                fee: 0
            );
        }
        else
        {
            return new AccountTransaction(
                date: date,
                transactionType: accountTransactionType,
                amount: request.Amount,
                notes: string.Empty
            );
        }
    }
}