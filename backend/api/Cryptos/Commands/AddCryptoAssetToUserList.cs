using System.Text;
using api.Models.Cryptos;
using api.Shared;
using api.Users.Repositories;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace api.Cryptos.Commands;
public record AddCryptoAssetToUserListCommand(string Crypto,
                                              string Currency,
                                              int CoinMarketCapId,
                                              int UserId) : IRequest<Response>;

public class AddCryptoAssetToUserListCommandValidator : AbstractValidator<AddCryptoAssetToUserListCommand>
{
    public AddCryptoAssetToUserListCommandValidator()
    {
        RuleFor(x => x.Crypto).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty();
        RuleFor(x => x.CoinMarketCapId).NotNull();
        RuleFor(x => x.UserId).NotNull();
    }
}
public class AddCryptoAssetToUserListCommandHandler : IRequestHandler<AddCryptoAssetToUserListCommand, Response>
{
    private readonly ILogger<AddCryptoAssetToUserListCommandHandler> _logger;
    private readonly IUserRepository _userRepository;
    public AddCryptoAssetToUserListCommandHandler(
        ILogger<AddCryptoAssetToUserListCommandHandler> logger,
        IUserRepository userRepository)
    {
        _logger = logger;
        _userRepository = userRepository;
    }

    public async Task<Response> Handle(AddCryptoAssetToUserListCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("AddCryptoAssetToUserListCommandHandler");
        var validationResult = await ValidateRequestAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
            _logger.LogError("AddCryptoAssetToUserListCommandHandler. Validation failed: {0}", errors);
            return new Response("Validation failed!", false, errors);
        }

        var userResult = await _userRepository.GetByIdAsync(request.UserId);
        if (!userResult.IsSuccess)
        {
            _logger.LogError("AddCryptoAssetToUserListCommandHandler. User not found: {0}", request.UserId);
            return new Response("User not found!", false);
        }

        var stringBuilder = new StringBuilder();
        stringBuilder.Append(request.Crypto);
        stringBuilder.Append(request.Currency);
        var cryptoAssetName = stringBuilder.ToString();

        var cryptoAsset = new CryptoAsset(request.Crypto,
                                          request.Currency,
                                          cryptoAssetName,
                                          request.CoinMarketCapId);
        try
        {
            // user.AddCryptoAsset(cryptoAsset);
        }
        catch (System.Exception)
        {
            _logger.LogError("AddCryptoAssetToUserListCommandHandler. Asset already exists: {0}", request.CoinMarketCapId);
            return new Response("Asset already exists on your lis!", false);
        }
        await _userRepository.UpdateAsync(userResult.Value!);

        return new Response("ok", true, cryptoAsset.Id);
    }
    private async Task<ValidationResult> ValidateRequestAsync(AddCryptoAssetToUserListCommand request)
    {
        var validation = new AddCryptoAssetToUserListCommandValidator();
        return await validation.ValidateAsync(request);
    }
}