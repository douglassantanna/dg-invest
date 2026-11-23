using System.Collections.Concurrent;
using api.Cryptos.Queries;

namespace api.Cache;

// Response caches kept only for data that changes rarely: the coin list and the admin users list. Account, asset and
// portfolio queries are not cached; current prices are shared through IPriceService instead.
public static class CacheKeyConstants
{
  // Cache keys. DO NOT CHANGE THESE VALUES
  public const string AllCryptos = "all_cryptos";
  public const string AllUsers = "all_users";
  private static readonly ConcurrentDictionary<string, string> _cacheKeyHistory = new();

  public static string GenerateUsersCacheKey(GetUsersQuery request)
  {
    var parts = new List<string> { AllUsers, request.UserId.ToString() };

    if (!string.IsNullOrEmpty(request.FullName)) parts.Add(request.FullName);
    if (!string.IsNullOrEmpty(request.Email)) parts.Add(request.Email);
    if (!string.IsNullOrEmpty(request.SortColumn)) parts.Add(request.SortColumn);
    if (!string.IsNullOrEmpty(request.SortOrder)) parts.Add(request.SortOrder);

    var cacheKey = string.Join("_", parts);
    _cacheKeyHistory[$"{AllUsers}_{request.UserId}"] = cacheKey;
    return cacheKey;
  }

  public static string GetLastUsersCacheKey(string userId)
  {
    return _cacheKeyHistory.TryGetValue($"{AllUsers}_{userId}", out var cacheKey) ? cacheKey : "";
  }
}
