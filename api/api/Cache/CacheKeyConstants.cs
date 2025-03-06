using System.Collections.Concurrent;
using api.Cryptos.Queries;

namespace api.Cache;

public static class CacheKeyConstants
{
  // Cache keys. DO NOT CHANGE THESE VALUES
  private const string MarketData = "market_data_";
  public const string UserAccounts = "user_accounts_";
  public const string UserAccountDetails = "user_account_details_";
  private const string UserAllCryptoAssets = "user_all_crypto_assets_";
  public const string UserCryptoAsset = "user_crypto_asset_";
  public const string AllCryptos = "all_cryptos";
  public const string AllUsers = "all_users";
  private static readonly ConcurrentDictionary<string, string> _cacheKeyHistory = new();
  public static string GenerateCryptoAssetsCacheKey(string userId, string assetName, string sortBy, string sortOrder, bool hideZeroBalance)
  {
    var parts = new List<string> { UserAllCryptoAssets, userId };

    if (!string.IsNullOrEmpty(assetName)) parts.Add(assetName);
    if (!string.IsNullOrEmpty(sortBy)) parts.Add(sortBy);
    if (!string.IsNullOrEmpty(sortOrder)) parts.Add(sortOrder);

    parts.Add(hideZeroBalance.ToString());

    var cacheKey = string.Join("_", parts);
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
    _cacheKeyHistory[$"{AllUsers}_{request.UserId}"] = cacheKey;
    return cacheKey;
  }

  public static string GenerateMarketDataCacheKey(GetMarketDataByTimeframeQuery request)
  {
    var parts = new List<string> { MarketData, request.UserId.ToString() };

    if (!string.IsNullOrEmpty(request.Timeframe.ToString()))
      parts.Add(request.Timeframe.ToString());

    var cacheKey = string.Join("_", parts);
    _cacheKeyHistory[$"{MarketData}_{request.UserId}_{request.Timeframe}"] = cacheKey;
    return cacheKey;
  }
  public static string GetLastUsersCacheKey(string userId)
  {
    return _cacheKeyHistory.TryGetValue($"{AllUsers}_{userId}", out var cacheKey) ? cacheKey : "";
  }

  public static List<string> GetAllUserMarketDataCacheKeys(int userId)
  {
    var keys = new List<string>();
    var userIdPrefix = $"{userId}";

    foreach (var key in _cacheKeyHistory.Keys)
    {
      if (key.Contains(userIdPrefix))
      {
        if (_cacheKeyHistory.TryGetValue(key, out var cacheKey))
        {
          keys.Add(cacheKey);
        }
      }
    }

    return keys;
  }
}
