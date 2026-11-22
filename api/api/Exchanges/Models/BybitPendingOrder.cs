using api.Shared;

namespace api.Exchanges.Models;

// A spot order seen partly filled. Order history is filtered by creation time and a cancel is not a fill, so a limit
// order that fills partly and is cancelled long afterwards would never reach the sync. It is remembered here and its
// status is checked on each sync until it is filled, cancelled or given up on.
public class BybitPendingOrder : Entity
{
    public static readonly TimeSpan GiveUpAfter = TimeSpan.FromDays(30);

    public int AccountId { get; private set; }
    public string OrderId { get; private set; } = string.Empty;
    public DateTime FirstSeenAt { get; private set; }

    private BybitPendingOrder() { }

    public BybitPendingOrder(int accountId, string orderId, DateTime firstSeenAt)
    {
        AccountId = accountId;
        OrderId = orderId;
        FirstSeenAt = firstSeenAt;
    }

    public bool IsExpired(DateTime now) => now - FirstSeenAt > GiveUpAfter;
}
