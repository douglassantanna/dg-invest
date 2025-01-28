using api.Cache;
using api.Cryptos.Models;
using api.Data;
using api.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Cryptos.Commands;
public record CreateCryptoCommand(string Name, string Symbol, string Image, int CoinMarketCapId) : IRequest<Response>;
public class CreateCryptoCommandValidator : AbstractValidator<CreateCryptoCommand>
{
    public CreateCryptoCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("The Name field is required.")
            .MaximumLength(255)
            .WithMessage("The Name field cannot exceed 255 characters.");

        RuleFor(x => x.Symbol)
            .NotEmpty()
            .WithMessage("The Symbol field is required.")
            .MaximumLength(255)
            .WithMessage("The Symbol field cannot exceed 255 characters.");

        RuleFor(x => x.Image)
            .NotEmpty()
            .WithMessage("The Image URL field is required.");

        RuleFor(x => x.CoinMarketCapId)
            .NotEmpty()
            .WithMessage("The CoinMarketCap ID field is required.");
    }
}
public class CreateCryptoCommandHandler : IRequestHandler<CreateCryptoCommand, Response>
{
    private readonly DataContext _context;
    private readonly ILogger<CreateCryptoCommandHandler> _logger;
    private readonly ICacheService _cacheService;

    public CreateCryptoCommandHandler(
        DataContext context,
        ILogger<CreateCryptoCommandHandler> logger,
        ICacheService cacheService)
    {
        _context = context;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<Response> Handle(CreateCryptoCommand request, CancellationToken cancellationToken)
    {
        var validation = new CreateCryptoCommandValidator();
        var validationResult = await validation.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogError("Validation errors occurred while creating a crypto");
            var errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
            return new Response("Validation errors", false, errors);
        }

        var existingCrypto = await _context.Cryptos.FirstOrDefaultAsync(x => x.CoinMarketCapId == request.CoinMarketCapId, cancellationToken);
        if (existingCrypto != null)
        {
            _logger.LogError("A crypto with the same CoinMarketCap ID already exists");
            return new Response("A crypto with the same CoinMarketCap ID already exists", false);
        }

        var crypto = new Crypto(request.Name, request.Symbol, request.Image, request.CoinMarketCapId);
        try
        {
            await _context.Cryptos.AddAsync(crypto, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            _cacheService.Remove(CacheKeyConstants.AllCryptos);
            return new Response("Crypto created successfully", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the crypto");
            return new Response("An error occurred while creating the crypto", false);
        }
    }
}