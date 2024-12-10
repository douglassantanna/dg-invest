using api.Data;
using api.Shared;
using api.Users.Dtos;
using api.Users.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Cryptos.Queries;
public record GetUserByIdCommand(int UserId) : IRequest<Response>;
public class GetUserByIdCommandHandler : IRequestHandler<GetUserByIdCommand, Response>
{
    private readonly DataContext _context;
    private readonly IUserRepository _userRepository;

    public GetUserByIdCommandHandler(DataContext context, IUserRepository userRepository)
    {
        _context = context;
        _userRepository = userRepository;
    }

    public async Task<Response> Handle(GetUserByIdCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId,
                                                      x => x.Include(q => q.Account).ThenInclude(x => x.AccountTransactions)
                                                      .Include(x => x.CryptoAssets));
        if (user is null)
            return new Response("User not found", false);


        var groupedTransactions = user.Account.AccountTransactions
            .GroupBy(at => at.Date.Date)
            .Select(g => new GroupedAccountTransactionsDto(
                g.Key,
                g.Select(at => new AccountTransactionDto(
                    at.Date,
                    at.TransactionType,
                    at.Amount,
                    at.ExchangeName,
                    at.Notes,
                    at.CryptoCurrentPrice,
                    at.CryptoAsset?.Symbol ?? "",
                    at.Fee
                )).ToList()
            ))
            .OrderByDescending(g => g.Date)
            .ToList();

        var userDto = new UserDto(
            user.Id,
            user.FullName,
            user.Email,
            user.Role,
            new AccountDto(
                user.Account.Id,
                user.Account.Balance,
                groupedTransactions
            )
        );
        return new Response("", true, userDto);
    }
}
