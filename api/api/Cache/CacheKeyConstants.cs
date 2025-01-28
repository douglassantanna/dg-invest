using System.Collections.Concurrent;

namespace api.Cache;

public static class CacheKeyConstants
{
  // Cache keys. DO NOT CHANGE THESE VALUES
  public const string UserAccounts = "user_accounts_";
  public const string UserAccountDetails = "user_account_details_";
  private const string UserAllCryptoAssets = "user_all_crypto_assets_";
  public const string UserCryptoAsset = "user_crypto_asset_";
  public const string AllCryptos = "all_cryptos";
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
}
