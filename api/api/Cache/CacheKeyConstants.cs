namespace api.Cache;

public static class CacheKeyConstants
{
  // Cache keys. DO NOT CHANGE THESE VALUES
  public const string UserAccounts = "user_accounts_";
  public const string AllCryptos = "all_cryptos";
  public const string CryptoAsset = "crypto_asset_";
  private const string CryptoAssets = "user_crypto_assets_";
  private static string _lastGeneratedCacheKey = "";
  public const string UserCryptoAsset = "user_crypto_asset_";
  public const string UserAccountDetails = "account_details_user_id_";
  public static string GenerateCryptoAssetsCacheKey(string userId, string assetName, string sortBy, string sortOrder, bool hideZeroBalance)
  {
    _lastGeneratedCacheKey = $"{CryptoAssets}{userId}_{assetName}_{sortBy}_{sortOrder}_{hideZeroBalance}";
    return _lastGeneratedCacheKey;
  }
  public static string GetLastGeneratedCacheKey() => _lastGeneratedCacheKey;

}
