using api.Cache;
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
    private readonly ICacheService _cacheService;
    public CreateAccountCommandHandler(IUserRepository userRepository, ILogger<CreateAccountCommandHandler> logger, ICacheService cacheService)
    {
        _userRepository = userRepository;
        _logger = logger;
        _cacheService = cacheService;
    }
    public async Task<Response> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("CreateAccountCommandHandler for UserId: {0}", request.UserId);
        try
        {
            var userResult = await _userRepository.GetByIdAsync(request.UserId, x => x.Include(x => x.Accounts));
            if (!userResult.IsSuccess)
            {
                _logger.LogError("CreateAccountCommandHandler. User not found for UserId: {0}", request.UserId);
                return new Response("User not found", false, 404);
            }
            var createAccountResult = userResult.Value.AddAccount(request.SubaccountTag);
            if (!createAccountResult.IsSuccess)
            {
                _logger.LogError("CreateAccountCommandHandler. Error creating account for UserId: {0}; Error: {1}", request.UserId, createAccountResult.Message);
                return new Response(createAccountResult.Message, false, 409);
            }

            await _userRepository.UpdateAsync(userResult.Value);
            var cachedKey = $"{CacheKeyConstants.UserAccounts}{request.UserId}";
            _cacheService.Remove(cachedKey);
            return new Response("", true);
        }
        catch (Exception ex)
        {
            _logger.LogError("CreateAccountCommandHandler. Error creating account for UserId: {0}; Ex: {1}", request.UserId, ex.Message);
            return new Response("Error creating account", false, 500);
        }
    }
}