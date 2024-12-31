using api.Shared;
using api.Users.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Users.Commands;
public record CreateAccountCommand(int UserId, string SubaccountTag) : IRequest<Response>;
public record CreateAccountRequest(string SubaccountTag);
public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, Response>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<CreateAccountCommandHandler> _logger;
    public CreateAccountCommandHandler(IUserRepository userRepository, ILogger<CreateAccountCommandHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }
    public async Task<Response> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("CreateAccountCommandHandler for UserId: {0}", request.UserId);
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, x => x.Include(x => x.Accounts));
            if (user == null)
            {
                _logger.LogError("CreateAccountCommandHandler. User not found for UserId: {0}", request.UserId);
                return new Response("User not found", false, 404);
            }
            var createAccountResult = user.AddAccount(request.SubaccountTag);
            if (!createAccountResult.IsSuccess)
            {
                _logger.LogError("CreateAccountCommandHandler. Error creating account for UserId: {0}; Error: {1}", request.UserId, createAccountResult.Message);
                return new Response(createAccountResult.Message, false, 409);
            }

            await _userRepository.UpdateAsync(user);
            return new Response("", true);
        }
        catch (Exception ex)
        {
            _logger.LogError("CreateAccountCommandHandler. Error creating account for UserId: {0}; Ex: {1}", request.UserId, ex.Message);
            return new Response("Error creating account", false, 500);
        }
    }
}