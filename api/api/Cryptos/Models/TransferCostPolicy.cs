namespace api.Models.Cryptos;

// How transfers affect cost basis (average-cost method).
// Legacy: TransferIn adds coins at zero cost and TransferOut keeps the full cost on the remaining coins, which
//   distorts the average price. Kept only so the fix can be compared and switched on deliberately
//   (CostBasisSettings:TransferCostPolicy); remove after the rollout.
// Proportional: TransferIn adds cost at the recorded price, and TransferOut removes cost at the average price,
//   like a sale. Coins Bybit holds beyond what history explains are valued at the current price (no gain or loss).
public enum TransferCostPolicy
{
    Legacy,
    Proportional
}

// Cost figures for display. UnexplainedBalance is the quantity Bybit holds that history does not explain.
public sealed record CostBasisView(
    decimal Invested,
    decimal AveragePrice,
    decimal GainLoss,
    decimal GainLossPercentage,
    decimal UnexplainedBalance);

public class CostBasisSettings
{
    public TransferCostPolicy TransferCostPolicy { get; set; } = TransferCostPolicy.Legacy;
}
