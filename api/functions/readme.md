# Azure Functions

The Functions project contains scheduled background work. The timer expressions below are the source of truth for the current schedules:

| Function | Schedule | Purpose |
| --- | --- | --- |
| `SyncBybitOrders` | Every 30 seconds | Sync enabled Bybit exchange accounts and universal transfers |
| `MarketData` | Hourly | Refresh market data |
| `HealthCheck` | Every 7 minutes | Check application health and send configured alerts |

`SyncBybitOrders` is feature-gated by `BybitSync:Enabled`. Its account-level work is delegated to `IBybitAccountSyncService` in the API project, so changes to sync behavior usually require inspecting both projects.

Related tests are in [`api/unit-tests/FunctionsTests/`](../unit-tests/FunctionsTests/) and [`api/unit-tests/ExchangesTests/Services/`](../unit-tests/ExchangesTests/Services/). Local Functions setup is documented in the [root README](../../README.md).
