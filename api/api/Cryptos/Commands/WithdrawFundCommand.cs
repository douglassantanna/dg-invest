using api.Cryptos.Models;
using api.Cryptos.TransactionStrategies.Contracts;
using api.Data;
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
                                 string? SubAccountTag) : IRequest<Response>;


public class WithdrawFundCommandValidator : AbstractValidator<WithdrawFundCommand>
{
    public WithdrawFundCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Deposit amount must be greater than zero");
        RuleFor(x => x.SubAccountTag).NotEmpty().WithMessage("SubAccountTag can't be empty");
        RuleFor(x => x.Notes)
            .MaximumLength(255)
            .WithMessage("Notes must be between 1 and 255 characters");
    }
}

public class WithdrawFundCommandHandler : IRequestHandler<WithdrawFundCommand, Response>
{
    private readonly ITransactionService _transactionService;
    private readonly ILogger<WithdrawFundCommandHandler> _logger;
    private readonly DataContext _context;

    public WithdrawFundCommandHandler(
        ILogger<WithdrawFundCommandHandler> logger,
        ITransactionService transactionService,
        DataContext context)
    {
        _logger = logger;
        _transactionService = transactionService;
        _context = context;
    }

    public async Task<Response> Handle(WithdrawFundCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("WithdrawFundCommandHandler for UserId: {0}", request.UserId);
        var validationResult = await ValidateRequestAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
            _logger.LogError("WithdrawFundCommandHandler. Validation failed: {0}", errors);
            return new Response("Validation failed", false, errors);
        }

        var account = await _context.Accounts.Include(x => x.CryptoAssets)
                                            .Where(x => x.UserId == request.UserId)
                                            .Where(x => x.SubaccountTag == request.SubAccountTag)
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
            var accountTransaction = new AccountTransaction(date: date,
                                                            transactionType: EAccountTransactionType.WithdrawToBank,
                                                            amount: request.Amount,
                                                            notes: request.Notes);
            // still need to implement account transaction type withdraw for crypto

            var response = _transactionService.ExecuteTransaction(account, accountTransaction);
            if (!response.IsSuccess)
            {
                _logger.LogError("WithdrawFundCommandHandler. Error adding transaction: {0}", response.Message);
                return response;
            }

            _context.Accounts.Update(account);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("WithdrawFundCommandHandler. Withdraw for UserId: {0}", request.UserId);
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