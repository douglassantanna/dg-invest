// using api.Shared;
// using api.Users.Dtos;
// using api.Users.Repositories;
// using MediatR;
// using Microsoft.EntityFrameworkCore;

// namespace api.Users.Queries;
// public record GetUserAccountsQueryCommand(int UserId) : IRequest<Response>;
// public class GetUserAccountsQueryCommandHandler : IRequestHandler<GetUserAccountsQueryCommand, Response>
// {
//     private readonly IUserRepository _userRepository;
//     private readonly ILogger<GetUserAccountsQueryCommandHandler> _logger;

//     public GetUserAccountsQueryCommandHandler(
//         IUserRepository userRepository,
//         ILogger<GetUserAccountsQueryCommandHandler> logger)
//     {
//         _userRepository = userRepository;
//         _logger = logger;
//     }

//     public async Task<Response> Handle(GetUserAccountsQueryCommand request, CancellationToken cancellationToken)
//     {
//         _logger.LogInformation("GetUserAccountsQueryCommandHandler for UserId: {0}", request.UserId);
//         var user = await _userRepository.GetByIdAsync(request.UserId, x => x.Include(x => x.Accounts));
//         if (user is null)
//         {
//             _logger.LogError("GetUserAccountsQueryCommandHandler. User {0} not found.", request.UserId);
//             return new Response("User not found", false);
//         }

//         var accounts = user.Accounts.Select(x => new SimpleAccountDto(x.Id, x.)).OrderBy(x => x.Name).ToList();
//     }
// }