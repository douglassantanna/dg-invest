using api.Data;
using api.Shared;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Cryptos.Commands;
public record AddCryptoAssetToAccountListCommand(int UserId, string SubAccountTag, int CryptoId)
    : IRequest<Response>;
public record AddCryptoAssetToAccountListRequest(string SubAccountTag, int CryptoId);
public class AddCryptoAssetToAccountListCommandValidator : AbstractValidator<AddCryptoAssetToAccountListCommand>
{
    public AddCryptoAssetToAccountListCommandValidator()
    {
        RuleFor(x => x.UserId).NotNull();
        RuleFor(x => x.SubAccountTag).NotEmpty();
        RuleFor(x => x.CryptoId).NotEmpty();
    }
}

public class AddCryptoAssetToAccountListCommandHandler : IRequestHandler<AddCryptoAssetToAccountListCommand, Response>
{
    private readonly DataContext _context;
    private readonly ILogger<AddCryptoAssetToAccountListCommandHandler> _logger;

    public AddCryptoAssetToAccountListCommandHandler(
        DataContext context,
        ILogger<AddCryptoAssetToAccountListCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
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

            var account = await _context.Accounts
                                        .Include(x => x.CryptoAssets)
                                        .Where(x => x.UserId == request.UserId)
                                        .Where(x => x.SubaccountTag == request.SubAccountTag)
                                        .FirstOrDefaultAsync(cancellationToken);

            if (account == null)
            {
                _logger.LogError("AddCryptoAssetToAccountListCommandHandler. Account not found: {0}", request.UserId);
                return new Response("Account not found!", false, 404);
            }

            var cryptoAsset = await _context.CryptoAssets
                                        .Where(x => x.Id == request.CryptoId)
                                        .FirstOrDefaultAsync(cancellationToken);
            if (cryptoAsset == null)
            {
                _logger.LogError("AddCryptoAssetToAccountListCommandHandler. Crypto asset not found: {0}", request.CryptoId);
                return new Response("Crypto asset not found!", false, 404);
            }

            var cryptoAssetResult = account.AddCryptoAsset(cryptoAsset);
            if (!cryptoAssetResult.IsSuccess)
            {
                _logger.LogError("AddCryptoAssetToAccountListCommandHandler. AddCryptoAsset failed: {0}", cryptoAssetResult.Message);
                return cryptoAssetResult;
            }

            _context.Accounts.Update(account);
            await _context.SaveChangesAsync(cancellationToken);
            return new Response("", true, cryptoAsset.Id);
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
