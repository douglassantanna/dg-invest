using FluentValidation;
using FluentValidation.Results;
using api.Shared.Interfaces;
using api.Shared;
using api.Users.Models;
using MediatR;
using api.Users.Events;
using api.Users.Repositories;
using api.Cache;

namespace api.Users.Commands;
public record CreateUserCommand(string FullName,
                                string Email,
                                Role Role,
                                int UserCreatorId) : IRequest<Response>;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotNull().WithMessage("FullName can't be null.")
            .NotEmpty().WithMessage("FullName can't be empty.")
            .Length(0, 255).WithMessage("Name must contain 0 to 255 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("E-mail can't be empty.")
            .Length(0, 255).WithMessage("E-mail must contain 0 to 255 characters.");
        RuleFor(x => x.Role)
        .IsInEnum().WithMessage("Role can't be null.");
    }
}

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Response>
{
    private readonly IPasswordHelper _passwordHelper;
    private readonly IPublisher _publisher;
    private readonly IUserRepository _userRepository;
    private readonly ICacheService _cacheService;

    public CreateUserCommandHandler(
        IPasswordHelper passwordHelper,
        IPublisher publisher,
        IUserRepository userRepository,
        ICacheService cacheService)
    {
        _passwordHelper = passwordHelper;
        _publisher = publisher;
        _userRepository = userRepository;
        _cacheService = cacheService;
    }

    public async Task<Response> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await ValidateRequestAsync(request);
        if (!validationResult.IsValid)
            return new Response("Validation failed", false, validationResult.Errors.Select(x => x.ErrorMessage).ToList());

        if (await _userRepository.IsUnique(request.Email))
            return new Response("Email already exists", false);

        var randomPassword = "111111"; //_passwordHelper.RandomPassword();

        var user = new User(request.FullName,
                            request.Email,
                            _passwordHelper.EncryptPassword(randomPassword),
                            request.Role);

        await _userRepository.AddAsync(user);

        await _publisher.Publish(new NewUserCreatedCommand(user, randomPassword), cancellationToken);

        var cachedKey = CacheKeyConstants.GetLastUsersCacheKey(request.UserCreatorId.ToString());
        _cacheService.Remove(cachedKey);

        return new Response("User created successfully. An email was sent to it.", true);
    }

    private async Task<ValidationResult> ValidateRequestAsync(CreateUserCommand request)
    {
        var validation = new CreateUserCommandValidator();
        return await validation.ValidateAsync(request);
    }
}

