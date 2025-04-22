using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Services.Contracts;
using api.Shared;
using Microsoft.EntityFrameworkCore;

namespace api.Services
{
    public class HealthCheckService : IHealthCheckService
    {
        private readonly DataContext _context;

        public HealthCheckService(DataContext context)
        {
            _context = context;
        }

        public async Task<Result<bool>> IsDatabaseHealthyAsync(CancellationToken cancellationToken)
        {
            try
            {
                var result = await _context.Database
                    .SqlQueryRaw<int>("SELECT 1")
                    .SingleOrDefaultAsync(cancellationToken);
                return result == 1
                    ? Result<bool>.Success(true)
                    : Result<bool>.Failure("Database is not healthy");
            }
            catch (Exception)
            {
                return Result<bool>.Failure("Database is not healthy");
            }
        }
    }
}