using api.Cryptos.Models;
using api.Cryptos.Queries;

namespace api.unit_tests.Helpers;

public static class MarketDataHelper
{
  private const long SecondsPerDay = 86400;
  private const long SecondsPerWeek = 604800; // 7 days
  private const long SecondsPerMonth = 2592000; // Approx 30 days
  private const long SecondsPerYear = 31536000; // Approx 365 days
  public static List<UserPortfolioSnapshot> GenerateDailySnapshots()
  {
    var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    var startTime = now - SecondsPerDay * 30; // 30 days ago
    var snapshots = new List<UserPortfolioSnapshot>();

    for (var i = 0; i < 30; i++)
    {
      var time = startTime + i * SecondsPerDay;
      var value = new Random().Next(1, 1000);
      snapshots.Add(new UserPortfolioSnapshot
      {
        UserId = 1,
        AccountId = 10,
        Time = time,
        Value = value
      });
    }

    return snapshots;
  }

  public static List<UserPortfolioSnapshot> GenerateWeeklySnapshots()
  {
    var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    var startTime = now - SecondsPerWeek * 7; // 7 weeks ago
    var snapshots = new List<UserPortfolioSnapshot>();

    for (var i = 0; i < 7; i++)
    {
      var time = startTime + i * SecondsPerWeek;
      var value = new Random().Next(1, 1000);
      snapshots.Add(new UserPortfolioSnapshot
      {
        UserId = 1,
        AccountId = 10,
        Time = time,
        Value = value
      });
    }

    return snapshots;
  }

  public static List<UserPortfolioSnapshot> GenerateMonthlySnapshots()
  {
    var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    var startTime = now - SecondsPerMonth * 12; // 12 months ago
    var snapshots = new List<UserPortfolioSnapshot>();

    for (var i = 0; i < 12; i++)
    {
      var time = startTime + i * SecondsPerMonth;
      var value = new Random().Next(1, 1000);
      snapshots.Add(new UserPortfolioSnapshot
      {
        UserId = 1,
        AccountId = 10,
        Time = time,
        Value = value
      });
    }

    return snapshots;
  }

  public static List<UserPortfolioSnapshot> GenerateYearlySnapshots()
  {
    var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    var startTime = now - SecondsPerYear * 5; // 5 years ago
    var snapshots = new List<UserPortfolioSnapshot>();

    for (var i = 0; i < 5; i++)
    {
      var time = startTime + i * SecondsPerYear;
      var value = new Random().Next(1, 1000);
      snapshots.Add(new UserPortfolioSnapshot
      {
        UserId = 1,
        AccountId = 10,
        Time = time,
        Value = value
      });
    }

    return snapshots;
  }
}