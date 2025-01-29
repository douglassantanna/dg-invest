using System.Collections.Concurrent;
using api.Cryptos.Queries;

namespace api.Cache;

public static class CacheKeyConstants
{
  // Cache keys. DO NOT CHANGE THESE VALUES
  public const string UserAccounts = "user_accounts_";
  public const string UserAccountDetails = "user_account_details_";
  private const string UserAllCryptoAssets = "user_all_crypto_assets_";
  public const string UserCryptoAsset = "user_crypto_asset_";
  public const string AllCryptos = "all_cryptos";
  public const string AllUsers = "all_users";
  private static readonly ConcurrentDictionary<string, string> _cacheKeyHistory = new();
  public static string GenerateCryptoAssetsCacheKey(string userId, string assetName, string sortBy, string sortOrder, bool hideZeroBalance)
  {
    var cacheKey = $"{UserAllCryptoAssets}{userId}_{assetName}_{sortBy}_{sortOrder}_{hideZeroBalance}";
    _cacheKeyHistory[userId] = cacheKey;
    return cacheKey;
  }
  public static string GetLastCryptoAssetsCacheKeyForUser(string userId)
  {
    return _cacheKeyHistory.TryGetValue(userId, out var cacheKey) ? cacheKey : "";
  }
  public static string GenerateUsersCacheKey(GetUsersQuery request)
  {
    var parts = new List<string> { AllUsers, request.UserId.ToString() };

    if (!string.IsNullOrEmpty(request.FullName)) parts.Add(request.FullName);
    if (!string.IsNullOrEmpty(request.Email)) parts.Add(request.Email);
    if (!string.IsNullOrEmpty(request.SortColumn)) parts.Add(request.SortColumn);
    if (!string.IsNullOrEmpty(request.SortOrder)) parts.Add(request.SortOrder);

    var cacheKey = string.Join("_", parts);
    var key = $"{AllUsers}_{request.UserId}";
    _cacheKeyHistory[key] = cacheKey;
    return cacheKey;
  }
  public static string GetLastUsersCacheKey(string userId)
  {
    var cachedKey = _cacheKeyHistory.TryGetValue($"{AllUsers}_{userId}", out var cacheKey) ? cacheKey : "";
    return cachedKey;
  }
}
