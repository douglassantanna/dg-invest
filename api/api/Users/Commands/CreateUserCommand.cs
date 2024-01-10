using FluentValidation;
using FluentValidation.Results;
using api.Data;
using api.Interfaces;
using api.Shared;
using api.Users.Models;
using MediatR;
using api.Users.Events;

namespace api.Users.Commands;
public record CreateUserCommand(string FullName,
                                string Email,
                                Role Role) : IRequest<Response>;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    private readonly DataContext _context;
    public CreateUserCommandValidator(DataContext context)
    {
        _context = context;

        RuleFor(x => x.FullName)
            .NotNull().WithMessage("FullName can't be null.")
            .NotEmpty().WithMessage("FullName can't be empty.")
            .Length(0, 255).WithMessage("Name must contain 0 to 255 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("E-mail can't be empty.")
            .Length(0, 255).WithMessage("E-mail must contain 0 to 255 characters.")
            .Must(BeUniqueEmail).WithMessage("Email already exists.");

        RuleFor(x => x.Role)
        .IsInEnum().WithMessage("Role can't be null.");
    }

    private bool BeUniqueEmail(string email)
    {
        return !_context.Users.Any(x => x.Email == email);
    }
}

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Response>
{
    private readonly DataContext _context;
    private readonly IPasswordHelper _passwordHelper;
    private readonly IPublisher _publisher;


    public CreateUserCommandHandler(
        DataContext context,
        IPasswordHelper passwordHelper,
        IPublisher publisher)
    {
        _context = context;
        _passwordHelper = passwordHelper;
        _publisher = publisher;
    }

    public async Task<Response> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await ValidateRequestAsync(request, _context);
        if (!validationResult.IsValid)
            return new Response("Validation failed", false, validationResult.Errors.Select(x => x.ErrorMessage).ToList());

        var randomPassword = _passwordHelper.RandomPassword();
        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            Password = _passwordHelper.EncryptPassword(randomPassword),
            Role = request.Role
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        await _publisher.Publish(new NewUserCreatedCommand(user, randomPassword), cancellationToken);

        return new Response("User created successfully", true);
    }

    private async Task<ValidationResult> ValidateRequestAsync(CreateUserCommand request, DataContext dataContext)
    {
        var validation = new CreateUserCommandValidator(dataContext);
        return await validation.ValidateAsync(request);
    }
}

