using api.Cryptos.Models;
using api.Services.Contracts;

namespace api.Services;
public class TimeframeCalculator : ITimeframeCalculator
{
    private const long SecondsPerHour = 3600;
    private const long SecondsPerDay = 86400;
    private const long SecondsPerMonth = 2592000; // Approx 30 days
    private const long SecondsPerYear = 31536000; // Approx 365 days

    public long CalculateStartTime(ETimeframe timeframe)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return timeframe switch
        {
            ETimeframe._24h => now - (24 * SecondsPerHour),
            ETimeframe._7d => now - (7 * SecondsPerDay),
            ETimeframe._1m => now - (30 * SecondsPerDay),
            ETimeframe._1y => now - (365 * SecondsPerDay),
            ETimeframe.All => 0,
            _ => throw new ArgumentException("Invalid timeframe", nameof(timeframe))
        };
    }

    public long CalculateGroupingInterval(ETimeframe timeframe)
    {
        return timeframe switch
        {
            ETimeframe._24h => SecondsPerHour,    // Group by hour
            ETimeframe._7d => SecondsPerDay,      // Group by day
            ETimeframe._1m => SecondsPerDay,      // Group by day
            ETimeframe._1y => SecondsPerMonth,    // Group by month
            ETimeframe.All => SecondsPerYear,     // Group by year
            _ => throw new ArgumentException("Invalid timeframe", nameof(timeframe))
        };
    }
}
