using api.Cryptos.Models;
using api.Cryptos.TransactionStrategies.Contracts;
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
                                 decimal? CurrentPrice = null,
                                 string? ExchangeName = null) : IRequest<Response>;
public class DepositFundCommandValidator : AbstractValidator<DepositFundCommand>
{
    public DepositFundCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Deposit amount must be greater than zero");

        When(x => x.AccountTransactionType == EAccountTransactionType.DepositCrypto, () =>
        {
            RuleFor(x => x.CurrentPrice)
                .NotNull()
                .GreaterThan(0)
                .WithMessage("Crypto Current Price must be greater than zero");

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
            _logger.LogInformation("DepositFundCommandHandler. Validation failed: {0}", errors);
            return new Response("Validation failed", false, errors);
        }

        var user = await _userRepository.GetByIdAsync(request.UserId,
                                                    x => x.Include(q => q.Account).ThenInclude(x => x.AccountTransactions));
        if (user == null)
        {
            _logger.LogInformation("DepositFundCommandHandler. User {0} not found.", request.UserId);
            return new Response("User not found", false);
        }

        try
        {
            var accountTransactionType = GetAccountTransactionType(request.AccountTransactionType);
            var date = new DateTime(request.Date.Year, request.Date.Month, request.Date.Day);
            var response = _transactionService.ExecuteTransaction(
               user.Account,
               new AccountTransaction(date: date,
                                      transactionType: accountTransactionType,
                                      amount: request.Amount,
                                      cryptoCurrentPrice: request.CurrentPrice ?? 0,
                                      exchangeName: request.ExchangeName ?? string.Empty,
                                      currency: string.Empty,
                                      destination: string.Empty,
                                      notes: string.Empty));

            if (!response.IsSuccess)
            {
                _logger.LogError("DepositFundCommandHandler. Error adding transaction: {0}", response.Message);
                return response;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("DepositFundCommandHandler. Error adding transaction: {0}", ex.Message);
            return new Response(ex.Message, false);
        }

        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("DepositFundCommandHandler. Deposit added for UserId: {0}", request.UserId);
        return new Response("ok", true);
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
}