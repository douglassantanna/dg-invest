using api.Cryptos.Models;
using api.Cryptos.TransactionStrategies.Contracts;
using api.Models.Cryptos;
using api.Shared;
using api.Users.Repositories;
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
    private readonly IUserRepository _userRepository;
    private readonly ILogger<DepositFundCommandHandler> _logger;
    private readonly ITransactionService _transactionService;
    public DepositFundCommandHandler(
        IUserRepository userRepository,
        ILogger<DepositFundCommandHandler> logger,
        ITransactionService transactionService)
    {
        _userRepository = userRepository;
        _logger = logger;
        _transactionService = transactionService;
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

        var user = await _userRepository.GetByIdAsync(request.UserId,
                                                    x => x.Include(q => q.Account).ThenInclude(x => x.AccountTransactions)
                                                          .Include(x => x.CryptoAssets).ThenInclude(x => x.Transactions));
        if (user == null)
        {
            _logger.LogError("DepositFundCommandHandler. User {0} not found.", request.UserId);
            return new Response("User not found", false);
        }

        try
        {
            var currentServerTime = DateTime.Now;
            var date = new DateTime(request.Date.Year, request.Date.Month, request.Date.Day, currentServerTime.Hour, currentServerTime.Minute, currentServerTime.Second);
            var accountTransactionType = GetAccountTransactionType(request.AccountTransactionType);
            var newAccountTransaction = CreateAccountTransaction(request, date, accountTransactionType, user.CryptoAssets);
            if (newAccountTransaction == null)
            {
                _logger.LogError("DepositFundCommandHandler. Crypto asset {0} not found.", request.CryptoAssetId);
                return new Response("Crypto asset not found", false);
            }

            var response = _transactionService.ExecuteTransaction(user.Account!, newAccountTransaction);
            if (!response.IsSuccess)
            {
                _logger.LogError("DepositFundCommandHandler. Error adding transaction: {0}", response.Message);
                return response;
            }

            await _userRepository.UpdateAsync(user);

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
                ETransactionType.Buy
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
                cryptoAsset: cryptoAsset
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