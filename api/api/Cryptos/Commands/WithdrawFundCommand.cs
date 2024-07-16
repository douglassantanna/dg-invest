using api.Cryptos.Models;
using api.Cryptos.TransactionStrategies.Contracts;
using api.Shared;
using api.Users.Repositories;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Cryptos.Commands;
public record WithdrawFundCommand(decimal Amount,
                                 DateTime Date,
                                 int UserId,
                                 string Notes) : IRequest<Response>;


public class WithdrawFundCommandValidator : AbstractValidator<WithdrawFundCommand>
{
    public WithdrawFundCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Deposit amount must be greater than zero");

        RuleFor(x => x.Notes)
            .MaximumLength(255)
            .WithMessage("Notes must be between 1 and 255 characters");
    }
}

public class WithdrawFundCommandHandler : IRequestHandler<WithdrawFundCommand, Response>
{
    private readonly ITransactionService _transactionService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<DepositFundCommandHandler> _logger;

    public WithdrawFundCommandHandler(
        IUserRepository userRepository,
        ILogger<DepositFundCommandHandler> logger,
        ITransactionService transactionService)
    {
        _userRepository = userRepository;
        _logger = logger;
        _transactionService = transactionService;
    }

    public async Task<Response> Handle(WithdrawFundCommand request, CancellationToken cancellationToken)
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
                                                   x => x.Include(q => q.Account).ThenInclude(x => x.AccountTransactions)
                                                         .Include(x => x.CryptoAssets).ThenInclude(x => x.Transactions));
        if (user == null)
        {
            _logger.LogInformation("DepositFundCommandHandler. User {0} not found.", request.UserId);
            return new Response("User not found", false);
        }

        try
        {
            var date = new DateTime(request.Date.Year, request.Date.Month, request.Date.Day);

            var accountTransaction = new AccountTransaction(date: date,
                                                            transactionType: EAccountTransactionType.WithdrawToBank,
                                                            amount: request.Amount,
                                                            notes: request.Notes);

            var response = _transactionService.ExecuteTransaction(user.Account, accountTransaction);
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

    private async Task<ValidationResult> ValidateRequestAsync(WithdrawFundCommand request)
    {
        var validation = new WithdrawFundCommandValidator();
        return await validation.ValidateAsync(request);
    }
}