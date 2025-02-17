using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Data.Commands;
public record RunMigrationsCommand : IRequest<Response>;
public class RunMigrationsCommandHandler : IRequestHandler<RunMigrationsCommand, Response>
{
    private readonly DataContext _context;
    private readonly ILogger<RunMigrationsCommandHandler> _logger;

    public RunMigrationsCommandHandler(DataContext context, ILogger<RunMigrationsCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Response> Handle(RunMigrationsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _context.Database.MigrateAsync(cancellationToken);
            return new Response("success", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while applying database migrations.");
            return new Response("Database migration failed. Check logs for details.", false);
        }
    }
}
