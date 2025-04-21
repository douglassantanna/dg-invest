using api.Cryptos.Models;

namespace api.Services.Contracts;
public interface ITimeframeCalculator
{
    long CalculateStartTime(ETimeframe timeframe);
    long CalculateGroupingInterval(ETimeframe timeframe);
}
