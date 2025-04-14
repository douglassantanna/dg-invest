using api.Cryptos.Models;
using api.Cryptos.Queries;

namespace api.unit_tests.Helpers;

public static class MarketDataHelper
{
  private const long SecondsPerHour = 3600;
  private const long SecondsPerDay = 86400;
  private const long SecondsPerMonth = 2592000; // Approx 30 days
  private const long SecondsPerYear = 31536000; // Approx 365 days

  public static List<MarketDataPointDto> GroupSnapshotsByTimeframe(
        List<UserPortfolioSnapshot> snapshots,
        ETimeframe timeframe,
        long startTime)
  {
    var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    return timeframe switch
    {
      ETimeframe._24h => GenerateHourlyDataPoints(snapshots, startTime, now),
      ETimeframe._7d => GenerateDailyDataPoints(snapshots, startTime, now, 7),
      ETimeframe._1m => GenerateDailyDataPoints(snapshots, startTime, now, 30),
      ETimeframe._1y => GenerateMonthlyDataPoints(snapshots, startTime, now),
      ETimeframe.All => GenerateYearlyDataPoints(snapshots, startTime, now),
      _ => throw new ArgumentException("Invalid timeframe", nameof(timeframe))
    };
  }

  private static List<MarketDataPointDto> GenerateHourlyDataPoints(
        List<UserPortfolioSnapshot> snapshots,
        long startTime,
        long now)
  {
    const long interval = SecondsPerHour;
    var grouped = snapshots
        .GroupBy(s => (s.Time / interval) * interval)
        .ToDictionary(g => g.Key, g => g.Sum(s => s.Value));

    var results = new List<MarketDataPointDto>();
    for (var time = startTime; time < now && results.Count < 24; time += interval)
    {
      var bucketTime = (time / interval) * interval;
      var value = grouped.GetValueOrDefault(bucketTime, 0m);
      results.Add(new MarketDataPointDto(bucketTime, value));
    }

    // Ensure exactly 24 records, padding with zeros if needed
    while (results.Count < 24)
    {
      var lastTime = results.Any() ? results.Last().Time + interval : startTime;
      results.Add(new MarketDataPointDto(lastTime, 0m));
    }

    return results.Take(24).ToList();
  }

  private static List<MarketDataPointDto> GenerateDailyDataPoints(
        List<UserPortfolioSnapshot> snapshots,
        long startTime,
        long now,
        int maxDays)
  {
    const long interval = SecondsPerDay;
    var grouped = snapshots
        .GroupBy(s => (s.Time / interval) * interval)
        .ToDictionary(g => g.Key, g => g.Sum(s => s.Value));

    var results = new List<MarketDataPointDto>();
    for (var time = startTime; time < now && results.Count < maxDays; time += interval)
    {
      var bucketTime = (time / interval) * interval;
      var value = grouped.GetValueOrDefault(bucketTime, 0m);
      results.Add(new MarketDataPointDto(bucketTime, value));
    }

    // Pad with zeros if needed
    while (results.Count < maxDays)
    {
      var lastTime = results.Any() ? results.Last().Time + interval : startTime;
      results.Add(new MarketDataPointDto(lastTime, 0m));
    }

    return results.Take(maxDays).ToList();
  }

  private static List<MarketDataPointDto> GenerateMonthlyDataPoints(
        List<UserPortfolioSnapshot> snapshots,
        long startTime,
        long now)
  {
    const long interval = SecondsPerMonth;
    var grouped = snapshots
        .GroupBy(s => (s.Time / interval) * interval)
        .ToDictionary(g => g.Key, g => g.Sum(s => s.Value));

    var results = new List<MarketDataPointDto>();
    for (var time = startTime; time < now && results.Count < 12; time += interval)
    {
      var bucketTime = (time / interval) * interval;
      var value = grouped.GetValueOrDefault(bucketTime, 0m);
      results.Add(new MarketDataPointDto(bucketTime, value));
    }

    // Pad with zeros if needed
    while (results.Count < 12)
    {
      var lastTime = results.Any() ? results.Last().Time + interval : startTime;
      results.Add(new MarketDataPointDto(lastTime, 0m));
    }

    return results.Take(12).ToList();
  }

  private static List<MarketDataPointDto> GenerateYearlyDataPoints(
        List<UserPortfolioSnapshot> snapshots,
        long startTime,
        long now)
  {
    const long interval = SecondsPerYear;
    var grouped = snapshots
        .GroupBy(s => (s.Time / interval) * interval)
        .ToDictionary(g => g.Key, g => g.Sum(s => s.Value));

    var results = new List<MarketDataPointDto>();
    for (var time = snapshots.Any() ? snapshots.Min(s => s.Time / interval * interval) : startTime; time < now; time += interval)
    {
      var bucketTime = (time / interval) * interval;
      var value = grouped.GetValueOrDefault(bucketTime, 0m);
      results.Add(new MarketDataPointDto(bucketTime, value));
    }

    return results.OrderBy(d => d.Time).ToList();
  }
}