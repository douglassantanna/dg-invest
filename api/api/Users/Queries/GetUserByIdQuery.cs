using api.Data;
using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Cryptos.Queries;
public record GetUserByIdQuery(int UserId) : IRequest<Response>;
public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Response>
{
    private readonly DataContext _context;

    public GetUserByIdQueryHandler(DataContext context)
    {
        _context = context;
    }

    public async Task<Response> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);
        if (user is null)
            return new Response("User not found", false, 404);
        return new Response("", true, user);
    }
}
