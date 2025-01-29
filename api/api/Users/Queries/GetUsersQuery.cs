using System.Linq.Expressions;
using api.Cache;
using api.Data;
using api.Shared;
using api.Users.Dtos;
using api.Users.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Cryptos.Queries;
public record GetUsersQuery : IRequest<PageList<UserDto>>
{
    public string? FullName { get; set; } = string.Empty;
    public string? Email { get; set; } = string.Empty;
    public string? SortColumn { get; set; } = string.Empty;
    public string? SortOrder { get; set; } = "ASC";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; }
    public int UserId { get; set; }
}
public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PageList<UserDto>>
{
    private readonly DataContext _context;
    private readonly ICacheService _cacheService;

    public GetUsersQueryHandler(DataContext context, ICacheService cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }

    public async Task<PageList<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        string cacheKey = CacheKeyConstants.GenerateUsersCacheKey(request);
        var absoluteExpiration = TimeSpan.FromMinutes(5);
        var cachesResults = await _cacheService.GetOrCreateAsync(cacheKey, async (ct) =>
        {
            IQueryable<User> userQuery = _context.Users.AsNoTracking();

            int maxPageSize = 20;

            if (request.PageSize > maxPageSize)
            {
                request.PageSize = maxPageSize;
            }

            if (!string.IsNullOrEmpty(request.FullName))
            {
                request.FullName = request.FullName?.ToLower().Trim();
                userQuery = userQuery.Where(x => x.FullName.ToLower().Contains(request.FullName));
            }

            if (!string.IsNullOrEmpty(request.Email))
            {
                request.Email = request.Email?.ToLower().Trim();
                userQuery = userQuery.Where(x => x.Email.ToLower().Contains(request.Email));
            }

            if (request.SortOrder?.ToUpper() == "DESC")
            {
                userQuery = userQuery.OrderByDescending(GetSortProperty(request));
            }
            else
            {
                userQuery = userQuery.OrderBy(GetSortProperty(request));
            }

            var collection = userQuery.Select(x => new UserDto(x.Id,
                                                            x.FullName,
                                                            x.Email,
                                                            x.Role,
                                                            null));

            var pagedCollection = await PageList<UserDto>.CreateAsync(collection,
                                                                    request.Page,
                                                                    request.PageSize);
            return pagedCollection;
        },
        absoluteExpiration,
        cancellationToken);
        return cachesResults;
    }

    private static Expression<Func<User, object>> GetSortProperty(GetUsersQuery request)
    {
        return request.SortColumn?.ToLower() switch
        {
            "email" => user => user.Email,
            "first_name" => user => user.FullName,
            "role" => user => user.Role,
            _ => user => user.Id
        };
    }
}
