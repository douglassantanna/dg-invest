using api.Data;
using api.Interfaces;
using api.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Authentication.Commands;
public record AuthenticateCommand(string Email, string Password) : IRequest<Response>;

public class AuthenticateCommandValidator : AbstractValidator<AuthenticateCommand>
{
    public AuthenticateCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}
public class AuthenticateHandler : IRequestHandler<AuthenticateCommand, Response>
{
    private readonly DataContext _context;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHelper _passwordHelper;
    private readonly ILogger<AuthenticateHandler> _logger;

    public AuthenticateHandler(
        DataContext context,
        ITokenService tokenService,
        IPasswordHelper passwordHelper,
        ILogger<AuthenticateHandler> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _passwordHelper = passwordHelper;
        _logger = logger;
    }
    public async Task<Response> Handle(AuthenticateCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Authenticating user {0}", request.Email);

        var user = await _context.Users.Where(u => u.Email == request.Email).FirstOrDefaultAsync(cancellationToken);
        if (user is null)
        {
            _logger.LogInformation("User {0} not found", request.Email);
            return new Response("User not found", false);
        }

        if (!_passwordHelper.VerifyPassword(request.Password, user.Password ?? string.Empty))
        {
            _logger.LogInformation("Password for user {0} is incorrect", request.Email);
            return new Response("Password is incorrect", false);
        }

        var token = _tokenService.GenerateToken(user);
        _logger.LogInformation("User {0} authenticated", request.Email);
        return new Response("", true, new { token });
    }
}