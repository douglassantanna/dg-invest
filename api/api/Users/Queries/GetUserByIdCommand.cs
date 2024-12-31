using System.Xml.Serialization;
using api.Data;
using api.Shared;
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
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);
        if (user is null)
            return new Response("User not found", false, 404);
        return new Response("", true, user);
    }
}
