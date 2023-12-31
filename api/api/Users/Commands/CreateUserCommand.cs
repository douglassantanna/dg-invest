using FluentValidation;
using FluentValidation.Results;
using api.Data;
using api.Interfaces;
using api.Shared;
using api.Users.Models;
using MediatR;

namespace api.Users.Commands;
public record CreateUserCommand(string FirstName,
                                string LastName,
                                string Email,
                                string Password,
                                string ConfirmPassword,
                                Role Role) : IRequest<Response>;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    private readonly DataContext _context;
    public CreateUserCommandValidator(DataContext context)
    {
        _context = context;

        RuleFor(x => x.FirstName)
            .NotNull().WithMessage("FirstName can't be null.")
            .NotEmpty().WithMessage("FirstName can't be empty.")
            .Length(0, 255).WithMessage("Name must contain 0 to 255 characters.");

        RuleFor(x => x.LastName)
            .NotNull().WithMessage("LastName can't be null.")
            .NotEmpty().WithMessage("LastName can't be empty.")
            .Length(0, 255).WithMessage("Name must contain 0 to 255 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("E-mail can't be empty.")
            .Length(0, 255).WithMessage("E-mail must contain 0 to 255 characters.")
            .Must(BeUniqueEmail).WithMessage("Email already exists.");

        RuleFor(x => x.Password)
            .NotNull().WithMessage("Password can't be null.")
            .NotEmpty().WithMessage("Password can't be empty.")
            .Length(4, 10).WithMessage("Password must contain 4 to 8 characters.");

        RuleFor(x => x.ConfirmPassword)
            .NotNull().WithMessage("ConfirmPassword can't be null.")
            .NotEmpty().Length(4, 10).WithMessage("Confirm Password can't be empty.")
            .Equal(x => x.Password).WithMessage("Passwords don't match.");

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

    public CreateUserCommandHandler(
        DataContext context,
        IPasswordHelper passwordHelper)
    {
        _context = context;
        _passwordHelper = passwordHelper;
    }

    public async Task<Response> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await ValidateRequestAsync(request, _context);
        if (!validationResult.IsValid)
            return new Response("Validation failed", false, validationResult.Errors.Select(x => x.ErrorMessage).ToList());

        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Password = _passwordHelper.EncryptPassword(request.Password),
            Role = request.Role
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return new Response("User created successfully", true);
    }

    private async Task<ValidationResult> ValidateRequestAsync(CreateUserCommand request, DataContext dataContext)
    {
        var validation = new CreateUserCommandValidator(dataContext);
        return await validation.ValidateAsync(request);
    }
}

