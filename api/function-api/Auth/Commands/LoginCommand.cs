using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using function_api.Data;
using function_api.Interfaces;
using function_api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace function_api.Auth.Commands;
public record LoginCommand(string Email, string Password) : IRequest<Response>;
public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}
public class LoginCommandHandler : IRequestHandler<LoginCommand, Response>
{
    private readonly DataContext _context;
    private readonly IPasswordHelper _passwordHelper;
    public LoginCommandHandler(
        DataContext context,
        IPasswordHelper passwordHelper)
    {
        _context = context;
        _passwordHelper = passwordHelper;
    }

    public async Task<Response> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(
                                    x => x.Email == request.Email,
                                    cancellationToken
                                    );
        if (user == null)
        {
            return new Response("User not found", false);
        }

        if (!_passwordHelper.VerifyPassword(request.Password, user.Password))
        {
            return new Response("Invalid password", false);
        }

        return new Response("Ok", true, new { user.FirstName, user.Role });
    }

}
