# dg-invest

See [`docs/README.md`](docs/README.md) for the code navigation map and current Bybit entry points.

### Overview
This project is a full cryptocurrency investment manager and analytics suite that I’ve been sculpting over the last two years. It brings together real-time price tracking, detailed transaction logging, and rich statistical insight to help you keep a steady hand in the stormy seas of digital assets.

---
### Getting started
#### Prerequisites
You can run the project either directly on your machine or inside Docker.
#### Option 1: Local Environment
- .NET 10
- Node.js v20+
- Angular 17
- SQL Server
- Azure Functions Core Tools
- EF Core tools
- **Azurite** (for local Azure Blob + Queue storage emulation)
Azurite can be installed as a VS Code extension or via npm.
#### Option 2: Docker Environment
- Docker Desktop

---
### Configuration
Before running the API you need to configure the settings file:

`dg-invest/api/api/appsettings.json`

```json
{
  "AzureStorageSettings": {
    "ConnectionString": "YourConnectionStringHere",
    "ContainerName": "YourContainerNameHere",
    "WelcomeEmailQueue": "welcomeemail"
  },
  "CoinMarketCapSettings": {
    "ApiKey": "YourCoinMarketCapApiKeyHere",
    "BaseUrl": "https://pro-api.coinmarketcap.com",
    "Header": "X-CMC_PRO_API_KEY"
  },
  "JWTSettings": {
    "ExpiryMinutes": 120,
    "Issuer": "http://my-local-host.com",
    "Secret": "YourJwtSecretHere"
  },
  "KeyVaultSettings": {
    "VaultUri": "https://your-keyvault-name.vault.azure.net/"
  },
  "AllowedOrigins": [
    "http://localhost:4200"
  ],
  "RateLimiterSettings": {
    "RequestsPermitLimit": 320,
    "WindowLimitInMinutes": 10
  },
  "CostBasisSettings": {
    "TransferCostPolicy": "Legacy"
  },
  "RunMigrations": false,
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "YourSqlAzureConnectionStringHere"
  }
}
```

---
#### Running with Docker
1. Clone the repository
```bash
git clone https://github.com/douglassantanna/dg-invest.git
cd dg-invest
```
2. Run the application
 ```bash
docker compose up
```

Docker will launch:
- SQL Server
- API Service
- Angular frontend

Once everything settles, open:
https://localhost:4200

Use these credentials:
- email: admin@admin.com
- password: admin123

---
### Testing

Run the backend test suite from the `api` directory:

```bash
dotnet test unit-tests/unit-tests.csproj
```

Docker Desktop must be running. The suite includes API integration tests that start a disposable SQL Server instance with Testcontainers, apply the production EF Core migrations, and call the authenticated Account and Exchange endpoints through the full ASP.NET Core pipeline. Key Vault, Bybit, and Blob Storage are replaced with in-memory test doubles so no external credentials or network calls are required.

To run only the Exchange API integration flow:

```bash
dotnet test unit-tests/unit-tests.csproj --filter FullyQualifiedName~ExchangeControllerIntegrationTests
```

### Full-stack deployment database migrations

The `full-stack-deployment.yml` workflow deploys the API and then calls the existing migration endpoint on that deployed API. The API applies EF Core migrations using its existing Azure SQL network access, so the GitHub-hosted runner does not need direct database access or Azure SQL firewall permissions.

Configure these values in each GitHub Environment used by the workflow:

- Secret `MIGRATION_TOKEN`: a long random token. Store the same value in the environment Key Vault as secret `MigrationToken`; Bicep references it in the API App Service setting.
- Variable `AZURE_API_BASE_URL`: the deployed API base URL, including `https://` and without a trailing slash.

Bicep sets `Migrations__RemoteTriggerEnabled=true` and configures `Migrations__RemoteTriggerToken` as a versionless Key Vault reference. Do not add the token value directly to the App Service settings or repository.

The endpoint also remains available to authenticated administrators and never logs the token. The API's normal database connection string remains an App Service setting and is not copied into GitHub Actions.

### Blob logging

Each environment uses its own Blob container. The API, Azure Functions, and Bybit sync records use the configured `AzureStorageSettings:LogContainer` container with UTC date-partitioned JSONL blobs:

```text
2026/09/15/api.jsonl
2026/09/15/functions.jsonl
2026/09/15/sync.jsonl
```

Configure `AzureStorageSettings__ConnectionString` and `AzureStorageSettings__LogContainer` in both the API App Service and Function App. User and account identifiers are stored in sync records, not in blob paths.

---
#### Running everything locally (standalone)
#### 1. Installation steps
Install SQL Server Express, Azure Functions Core Tools, Azurite (VS Code extension or npm), .NET 10, EF Core tools, Node.js V18+, NPM and Angular CLI V18
#### 2. Set up the database
1. Create a database named `dg-invest` in SQL Server
2. Copy its connection string into `appsettings.json` under `ConnectionString:Default`
#### 3. Apply EF migrations
```bash
cd dg-invest/api/api
dotnet ef database update
``` 
#### 4. Start Azurite (local Azure storage emulator)
1. In VS Code, open the command palette and type `Azurite: Start`
2. Azurite will print its local connections string in the output panel. Copy that string. For reference, check [Documentation](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite)
3. Add the Azurite connection string into the `appsettings.json` under `AzureStorageSettings:ConnectionString`
#### 5. Run the API
```bash
cd dg-invest/api/api
dotnet run
```
#### 6. Start the Azure Functions
```bash
cd dg-invest/api/functions
func start
```
#### 7. Start the Angular frontend
```bash
cd dg-invest/web-app
ng s -o
```

The frontend launches automatically. Access the application using the credentials
- email: admin@admin.com
- password: admin123

---
### Bybit integration

DG Invest reads your Bybit **spot** activity and balances using **read-only** API keys. It never places orders or moves funds. Sync runs in the background every minute and on demand.

This section describes how the integration works today. Keep it current: every change to the integration updates this section in the same PR.

#### Concepts

- **Manual account**: a user-owned portfolio such as Main or Savings. Discovery never turns it into an exchange account.
- **Exchange integration**: the per-user connection to Bybit. It holds the integration-level (master) credentials, the region, and the connection state (connected, disconnected, auto-paused).
- **Exchange account**: one portfolio per Bybit UID (`AccountType = Exchange`, `Exchange = "Bybit"`, `ExternalId = <UID>`). The master account and each sub-account are separate exchange accounts, matched by user, exchange and UID.

**Account selector:**
- `GET /api/Account/selector-options` returns every non-deleted Manual and Exchange account of the user, with balance, selection and sync health (connected, disconnected, paused, setup-needed, attention-required).
- `POST /api/Account/select-account` persists the selection.
- **Add portfolio** creates a Manual account (`/api/Account/create`).
- Deposits and withdrawals are entered manually only for Manual portfolios; exchange accounts are managed from the Exchange Hub.

See [Exchange Integration Rollout](docs/exchange-rollout-roadmap.md) and the [Repository Map](docs/README.md) for code entry points.

#### Setup

**1. Create read-only API keys in Bybit** (API Management → *Read-Only*):

- **Master key:** enable *Wallet → Account Transfer / Subaccount Transfer*. Bybit requires this permission to list sub-accounts (`/v5/user/submembers`), and it is allowed on read-only keys.
- **Each sub-account** that should sync needs its own read-only key with *Wallet → SubMemberTransferList* enabled. Bybit requires this permission for a sub-account key to list universal transfers.
- When you save a new key, DG Invest checks it with Bybit (`/v5/user/query-api`) **before** storing it:
  - keys that can trade or withdraw (`readOnly != 1`) are rejected;
  - keys that belong to a **different UID** than the account (or than the master UID, for the integration key; when the master UID is left empty on a re-key, the already linked master UID is used) are rejected. A key from another sub-account would make the account mirror that other wallet;
  - keys Bybit rejects are refused;
  - if Bybit is unreachable, the save fails with 503 and nothing is stored.

**2. Connect the integration.** Use the Exchange Hub (`/exchanges/bybit`) to enter the master key, secret, master UID and region. The API endpoint is:

```http
POST /api/exchange/bybit/integration-credentials
Authorization: Bearer <jwt>

{ "apiKey": "...", "apiSecret": "...", "region": "Global", "masterUid": "optional-bybit-master-uid" }
```

**Region:** `Global` (api.bybit.com) or `Eu` (api.bybit.eu, for accounts under the EU entity).

**3. Discover sub-accounts** (`POST /api/exchange/bybit/sync-accounts`):
- Bybit UIDs are matched by user, exchange and UID.
- New UIDs become new exchange accounts, and returned UIDs are re-enabled.
- UIDs no longer returned are disabled, but their history is kept.
- The linked master account is never disabled by discovery.

**4. Save a read-only key for each sub-account** on its account page (`POST /api/exchange/bybit/credentials`).

**5. Optional:** run the [historical import](#historical-import-backfill) to bring in activity from before the connection.

#### How sync works

The timer `SyncBybitOrders` (every minute, `0 */1 * * * *`, when `BybitSync:Enabled`) and the manual **Sync now** button (`POST /api/Exchange/bybit/sync/{accountId}`) both run `BybitAccountSyncService.SyncAsync`. For each enabled account, one run does the following:

1. **Checks** that the account has a sync status, sync is enabled, the integration is connected (not disconnected or auto-paused) and credentials exist.
2. **Picks the start of the range:**
   - `LastSyncAt − 5 min` (the 5-minute overlap catches late arrivals);
   - if the account never synced, `BybitCredentialsSetAt − 5 min`, so manual entries made before connecting are not imported again;
   - with neither, the last 30 days.
3. **Fetches from that point to now in windows of at most 7 days**, always sending both `startTime` and `endTime`.
   - Bybit allows 7 days per request for orders and transfers, and less than 30 days for deposits and withdrawals. If you send only a start time, Bybit returns just the next 7 days, or rejects deposits with `131002`.
   - One run covers at most **8 windows (~56 days)**. An account further behind catches up over the following runs.
   - Each window fetches spot order history (paginated), spot fills (`/v5/execution/list`, paginated), deposits, withdrawals, internal transfers and universal transfers.
4. **Records the events in time order**, so a buy is recorded after the deposit, transfer or sell that funded it. Every event is idempotent:

   | Event | Deduplication key |
   |---|---|
   | Order | Bybit `orderId`, per crypto asset (unique index) |
   | Deposit / withdrawal | Bybit `txID`, or Bybit's record id when `txID` is empty (`bybit-deposit-{id}`, `bybit-withdraw-{withdrawId}`), **per account** (two accounts can share a `txID`: one withdraws, the other receives) |
   | Transfer | transfer ID + direction + account |

   The database enforces both per-account rules with unique indexes: `IX_AccountTransactions_AccountId_ExchangeTransactionId` (non-empty ids) and `IX_CryptoAssets_AccountId_CoinMarketCapId` (one asset per coin per account). If two syncs of the same account overlap, the second one fails on the index and the next run finds the existing record. Like the order index, these indexes are created by SQL migrations and are not part of the EF model.

   **Limit orders that fill late.** Order history is filtered by **creation** time, so a limit order that fills after its window has passed (more than ~5 minutes after it was placed) is never returned again. Fills are listed by **execution** time, so each window also checks them:
   - an order with a fill in the window that order history did not return, and that is not imported yet, is **looked up by order id** (`/v5/order/realtime`, which also returns recently closed orders);
   - if it closed with fills (`Filled` / `PartiallyFilledCanceled`), Bybit's own totals are used for quantity and average price. Its fills are fetched for fees and the date; they are **not** summed into the order, because Bybit may only return the last 7 days of fills for an order;
   - **dated by when it completed:** the last fill when the fills add up to the order's filled quantity, otherwise the order's last update (and fees then come from the order's own fee details);
   - still open, or not returned by id: remembered as pending (below);
   - it then goes through the normal order import (same deduplication and rules).

   **Partly filled orders.** A cancel is not a fill, so an order that fills partly and is cancelled long after its window would still be missed. The sync **remembers** such orders in `BybitPendingOrders` (one row per account and order):
   - a row is added when a fill leaves part of the order open (`leavesQty > 0`), or when order history returns it as `PartiallyFilled`;
   - on each sync, only when the account has pending rows, one call lists the open orders (`/v5/order/realtime`). Rows still open are kept; each row that left the list is looked up once by order id (Bybit returns the last 500 closed orders);
   - closed **with** fills (`PartiallyFilledCanceled`, or `Filled`): imported through the normal order path, then the row is removed;
   - already imported, or closed **without** fills (`Cancelled`, `Rejected`): the row is removed;
   - not found for **30 days**: the row is removed with a warning in the Function logs; run the historical import for that period.

   Orders imported:
   - `Filled`;
   - `PartiallyFilledCanceled` with an executed quantity above zero (only the executed quantity and average price are used).
   - Only pairs quoted in **USDT, USDC or USD**. A pair like `BTCBRL` or `ETHBTC` is priced in another currency, so it is not imported. It is logged and counted as a skipped item.

   Fees come from Bybit's execution history when available. Executions are fetched only for orders not yet imported with one; an order already stored with its execution keeps its fee, so a re-fetched window costs no extra calls.
5. **Reconciles balances with Bybit** (see [where each number comes from](#where-each-number-comes-from)). The cash wallet is read **once per run**, after the events: four calls (Funding and Unified × USDT and USDC). The same read sets the opening balance on the first sync.
6. **Advances the checkpoint** to the end of the last fetched window. Failures are handled in two ways:
   - **An item that can never be imported** (a coin CoinMarketCap does not know, unparseable data, a non-USD pair, a record without an id) is logged as Failed and **skipped**. The account status stays `Connected` with a warning: *"N Bybit item(s) could not be imported and were skipped…"*. After fixing the cause, run the historical import to bring them in.
   - **A temporary failure** (network, database, CoinMarketCap outage) fails the run. The checkpoint stays, and the next run retries the same range.

   An account with **no stored key** is skipped and its status set to `NotConfigured` (shown as "setup needed"), replacing any old error.

Synced events are recorded **without** the local "insufficient funds" check: Bybit already executed them, and cash is taken from the Bybit wallet afterwards.

#### Where each number comes from

| Value | Source |
|---|---|
| Quantity of each coin | **Bybit wallet** (Funding + Unified). It replaces the history-derived quantity on every sync. |
| Cash (USDT + USDC) | **Bybit wallet** (Funding + Unified): the wallet balance, **including funds locked in open orders**. A failed or incomplete read leaves cash unchanged. |
| Cost, average price, gain/loss | DG Invest transaction history |
| Total deposited | History: deposits + transfers in − withdrawals − transfers out (the opening balance is excluded) |

On every sync, each asset is first recalculated from its history (see [Cost basis](#cost-basis)), then its quantity is set to the Bybit wallet:
- If Bybit holds **less** than history explains, the cost is reduced proportionally, like a sale at the average price.
- If Bybit holds **more**, the difference is the asset's **balance without history**. How it is valued depends on the transfer cost policy (below).

Calculating everything from history alone is not viable: Bybit has many activity types (Earn, convert, P2P, airdrops, fees in the traded coin), and any missed event would leave balances wrong forever. Bybit, however, does not know your cost basis for coins that came from elsewhere, so cost comes from DG Invest's history.

#### Cost basis

DG Invest uses the **average-cost method**:
- A **buy** adds `quantity × price + fee` to the invested amount.
- A **sell** removes cost at the current average price, so the average does not change.

How transfers and balance without history are treated depends on `CostBasisSettings:TransferCostPolicy` (API and Function App settings). It applies to Bybit and manual accounts alike:

| | `Legacy` (default, being phased out) | `Proportional` |
|---|---|---|
| **Transfer in** (crypto deposit) | Adds coins at **zero cost**, ignoring the recorded price | Adds cost at the **recorded price**: the market price at the deposit time, or the source's average cost for a transfer between your own accounts (see [Transfers](#transfers)) |
| **Transfer out** (crypto withdrawal) | Keeps the **full cost** on the remaining coins, so the average price jumps. A transfer larger than the history explains is ignored. | Removes cost at the **average price**, like a sale. If more leaves than the history explains, the known coins leave first with their cost; what remains counts as balance without history. |
| **Balance without history** (Bybit only) | Counts as free coins, which **inflates** the gain | Valued at the **current price**, so it adds no gain or loss. The asset page shows a "Without history" card and a note suggesting the historical import. |

**Current setting:** STAGE uses `Proportional` (since 2026-11-10); PROD uses `Legacy`.

Example: buy 1 BTC at 60k, then withdraw 0.5 BTC. With `Legacy` the remaining 0.5 BTC shows an average of 120k. With `Proportional` it shows 60k.

**Preview before switching:**
- `GET /api/Recalculate/preview` (any signed-in user) returns, for the caller's **own** assets, the figures shown today (`before`, Legacy) and the figures after switching (`after`, Proportional). Nothing is saved. Rows are ordered by the largest change in gain percentage.
- `before`/`after` contain `invested`, `averagePrice`, `gainLoss`, `gainLossPercentage` and `unexplainedBalance`.

**Switching to `Proportional`:**
1. Check `GET /api/Recalculate/preview` for the accounts you care about.
2. Set `param transferCostPolicy = 'Proportional'` in `infra/environments/<env>.bicepparam` and run **Deploy Azure Infrastructure**. This sets `CostBasisSettings__TransferCostPolicy` on both the API and the Function App. Do not set it by hand in the portal: the next infra deploy rewrites all app settings and would revert it.
3. Bybit accounts update on their next sync, because sync recalculates from history.
4. **Manual accounts** keep their stored figures until recalculated:
   - set `param enableRecalculation = true` in the same `.bicepparam` and deploy;
   - an **admin** runs `POST /api/Recalculate/recalculate` once;
   - set it back to `false` in a follow-up PR and deploy again.
   It is a Bicep parameter (default `false`, API only) so that it never drifts from what Bicep deploys.

`POST /api/Recalculate/recalculate` recalculates **every user's** accounts. It requires the Admin role, as well as the flag.

#### Transfers

| Transfer | What DG Invest records |
|---|---|
| Within the same UID (Funding ↔ Unified) | Nothing. It nets to zero. |
| Between UIDs (master ↔ sub, sub ↔ sub) in USDT/USDC | `TransferOut` on the source account and `TransferIn` on the destination, each side recorded by that account's own sync. They count toward each account's total deposited and net to zero across your accounts. |
| Between UIDs in other coins (e.g. BTC) | `TransferOut` on the source coin and `TransferIn` on the destination coin, each side recorded by that account's own sync. The account history shows them as a crypto withdrawal and a crypto deposit, so cash and total deposited do not change. |

**Cost of crypto moved between your own accounts.** The destination's `TransferIn` is priced at the **source's average cost just before the transfer**, replayed from the source's history. Under `Proportional`, the source loses that cost and the destination gains it, so the average price is the same on both sides. The **market price at the transfer time** is used instead when:
- the source UID is not one of your accounts in DG Invest;
- the source's history before the transfer holds fewer coins than were sent (missing history, or the source has not imported its earlier buys yet);
- the source's coins have no known cost.

Under `Legacy`, a `TransferIn` ignores the price, so the coins still arrive at zero cost.

**Market price at a past time.** Crypto deposits, crypto withdrawals and the transfer fallback use the close of Bybit's public 1-minute `{COIN}USDT` spot candle at the event time (`/v5/market/kline`, no key needed). If Bybit does not list that pair, today's CoinMarketCap price is used and a warning is logged. If the candle request fails, the event is retried on the next sync instead of being recorded at a fallback price.

Rows recorded before this change keep their price. Crypto transfers that happened before it are only recorded by a historical import, because the checkpoint has already passed them.

If a key lacks permission to list transfers (Bybit error `10005`), the sync **skips transfers** for that account and logs a warning, but still imports orders, deposits and withdrawals. Cash is still reconciled from the wallet. Only the `TransferIn`/`TransferOut` history entries, and therefore that account's total deposited, are missing. After adding the permission, run the historical import to recover them: the checkpoint has already moved past the skipped range.

#### Historical import (backfill)

`POST /api/Exchange/bybit/backfill/{accountId}` (the **Historical import** card on the account page) imports Bybit history from a start date, up to two years back, through now:
- It uses 7-day windows and drops events whose timestamp falls outside each window.
- It records history **without changing cash**, reconciles coin quantities, and sets the checkpoint to the end of the import.
- It is safe to run again: events already stored are skipped.

Use it to recover trades that are missing from history. It runs as a single synchronous request.

To force the regular sync to look further back instead, rewind the checkpoint for one account (advanced; affects only the next runs):

```sql
BEGIN TRANSACTION;
UPDATE SyncStatuses
SET LastSyncAt = DATEADD(minute, -15, SYSUTCDATETIME()), LastOrderId = NULL
WHERE UserId = 6 AND AccountId = 33 AND ExchangeName = 'Bybit';
SELECT Id, UserId, AccountId, LastSyncAt, Status FROM SyncStatuses
WHERE UserId = 6 AND AccountId = 33 AND ExchangeName = 'Bybit';
COMMIT TRANSACTION;
```

#### Credentials and security

- **Read-only keys only** (see Setup).
- **Storage:** credentials live in Azure Key Vault, never in the database or browser state.
  - Each environment has a dedicated credential vault, `dginvest-<env>-cred-weu1`, separate from the app-secrets vault (see [infra/README.md](infra/README.md#key-vaults)).
  - Secret names: `bybit-{userId}-{accountId}-api-key|api-secret` (per account) and `bybit-integration-{userId}-api-key|api-secret` (integration).
- **The browser only sees** a masked key (last 4 characters) and has/has-not flags.
- **Saving only writes the values provided.** A save without a new key never blanks the stored secrets.
- **Caching:** each process caches credentials for 24 hours. When Bybit rejects a key (10003/10004), the sync re-reads it from Key Vault and retries **once** before pausing, so a key rotated through the API is picked up by the Functions host.
- **Disconnect** stops discovery and sync, disables the accounts' sync statuses and blanks the stored keys; history is kept. **Also delete the key in Bybit** (the confirmation dialog says so). Deleting the secrets instead would block reconnecting for 7 days, because the credential vault has soft delete with purge protection.

#### Automatic pause

| Trigger | Result |
|---|---|
| Bybit rejects the credentials again after the fresh-key retry | Integration auto-paused immediately, and the user is emailed. Disconnect and reconnect from the Exchange Hub. |
| 10 consecutive transport failures (network, timeout, 408, 5xx) | Integration auto-paused |
| Rate limit (HTTP 403 or 429, codes `10006` / `10018`) | **Not counted.** The run stops for every account, because Bybit limits per IP, and the next run retries. The account status is not changed. |
| Any other error | The account's sync status becomes `Error`, and the next run retries |

Every Bybit request times out after **20 seconds**, so a slow Bybit fails fast instead of overlapping the next run.

#### Bybit status reference

Non-success deposits and withdrawals are **skipped entirely** and recorded once they succeed. Storing a pending row first would make the later success look like a duplicate.

| Operation | Imported when | Bybit docs |
|---|---|---|
| **Deposit** | `"3"` (success) | [depositStatus](https://bybit-exchange.github.io/docs/v5/enum#depositstatus) |
| **Withdrawal** | `"success"` | [withdrawStatus](https://bybit-exchange.github.io/docs/v5/enum#withdrawstatus) |
| **Order** | `Filled`, or `PartiallyFilledCanceled` with executed quantity > 0 | [orderStatus](https://bybit-exchange.github.io/docs/v5/enum#orderstatus) |

#### Exchange Hub

The **Exchange Hub** (`/exchanges`) manages exchange integrations from the app. The Accounts page's **Connect an exchange** link opens it. The Bybit area provides:

1. **Integration credentials:** master key, secret, master UID and region, plus connect and disconnect.
2. **Sub-account discovery:** lists Bybit sub-accounts by UID and maps them to exchange accounts.
3. **Account pages:** a per-account read-only key and region, test connection, sync now, pause and resume, historical import, and sync logs.
4. **Sync health:** status badge, last sync time, error count and the last error.

#### Monitoring

| Where | What's stored |
|-------|--------------|
| **Sync logs** (Azure Blob Storage) | A JSONL log per day with the events that **failed** to import and why. Successful and duplicate events are not logged; they are visible as transactions. |
| **Sync status** (database) | Per-account health: last sync (checkpoint), status, error count, last error |
| **Account transactions** (database) | Account history with status badges (Pending / Completed / Failed) |
| **Crypto transactions** (database) | Per-coin history (Buy / Sell / Transfer In / Transfer Out) |

#### Feature flag

Set `BybitSync:Enabled = false` in the Function App settings to stop background sync without deploying code. It defaults to `true`.

#### Troubleshooting

**A transaction is missing**
1. Check the account's sync logs, which list items that failed to import.
2. Check that `BybitSync:Enabled` is not `false` and the integration is not paused.
3. Run the **historical import** from a date before the missing trade.
4. If it is still missing, check its type and status in Bybit. Only spot orders (`Filled` / `PartiallyFilledCanceled`) and successful deposits and withdrawals are imported. In Bybit, **Order History shows when an order was placed; Trade History shows when it filled.**
5. Before late-filled limit orders were handled (2026-11), the regular sync skipped them. Run the historical import from before the order to recover them.

**Common errors**

| Message | Meaning and fix |
|---|---|
| `Bybit rejected the API key … [Bybit 10003]` / `… the API secret does not match the key … [Bybit 10004]` | The key is invalid, revoked, or the region is wrong. Save a new read-only key with the right region. |
| `… only accepts requests from specific IP addresses … [Bybit 10010]` | The key has an IP whitelist. Remove it in Bybit, or create a key without one. |
| `… missing a permission … [Bybit 10005]` | The key lacks a read permission for that request. Check the key's permissions in Bybit. |
| `Bybit rejected the requested date range … [Bybit 131002]` | A time window Bybit does not accept. The next sync uses 7-day windows; if it keeps failing, run a historical import. |
| `Bybit is limiting requests right now…` | Rate limit (HTTP 429 from the API, also when saving or testing a key or listing sub-accounts). Nothing to fix: wait a minute and try again. The sync retries on its own, and it does not count toward the automatic pause. If it lasts for hours, check whether Bybit is blocking the Function's region (Bybit also answers 403 to blocked regions). |
| `Could not reach Bybit (…)` | Network error, timeout or Bybit outage. The next run retries; 10 in a row pause the integration. |
| `Bybit rejected sync request: <code> - <text>` | Any other Bybit error, with Bybit's own text. |
| `This Bybit API key belongs to UID …, but this account is UID …` | The key was created in another Bybit account (for example, another sub-account). Create a read-only key inside the account shown on the page. |
| `This Bybit API key can trade or withdraw…` | Create a **read-only** key. For the master key, also enable Wallet → Account Transfer / Subaccount Transfer. |
| `N Bybit item(s) could not be imported and were skipped; see the sync logs…` | Those items were logged as Failed and skipped; the account keeps syncing. Fix the cause (for example, create a coin CoinMarketCap does not know), then run the historical import from before the skipped items. |
| `Not imported: … is not quoted in USDT, USDC or USD` (sync log) | Only USD-quoted pairs are supported. The order is not imported. |
| Account shows "setup needed" / status `NotConfigured` | No API key is stored for that account. Save a read-only key on its account page, or pause it if it should not sync. |
| `Could not resolve asset for symbol` | CoinMarketCap has no data for the coin. Create it manually in the app. |
| `… cannot list universal transfers (10005); transfers are skipped` (warning in the Function logs) | The key lacks the transfer permission. Enable *Wallet → SubMemberTransferList* (sub-account key) or *Subaccount Transfer* (master key) in Bybit. Then run the **historical import** from the date transfers started being skipped, because the regular sync does not go back for them. |
| `Credential storage is temporarily unavailable` | Key Vault is unreachable. The next run retries automatically. |
| `no sync status for account` (log) | The credentials predate the sync-status mechanism. Re-save them to enable sync. |

**Duplicates:** sync is idempotent, so running it again never duplicates. If you see duplicates, check `ExchangeOrderId` / `ExchangeTransactionId` for collisions.

#### Known limitations

These are tracked in the integration review and planned for upcoming PRs:

- **Cost basis:**
  - Environments still on `TransferCostPolicy = Legacy` (PROD) use the Legacy rules described in [Cost basis](#cost-basis).
  - Deposits, withdrawals and transfers recorded before 2026-11 keep the price from processing time (today's CoinMarketCap price), not the event time.
- **Sequential calls:** accounts are synced one after another, ~13 calls per account per run, plus one per new or late-filled order.
- **Pending orders not found:** a partly filled order Bybit no longer returns by id (beyond its last 500 closed orders, or after a Bybit restart) is dropped after 30 days and needs the historical import.

---

### Account transactions

The **Accounts** page groups Manual portfolios and Exchange accounts, then shows the selected account's balance and transaction history.

**How each transaction moves cash** ("Available Funds", `Account.Balance`) in a manual account:

| Transaction | Cash | Coins |
|---|---|---|
| Fiat deposit / transfer in (USD) | + amount | — |
| Withdraw to bank / transfer out (USD) | − amount | — |
| Buy | − (quantity × price + fee) | + quantity |
| Sell | + (quantity × price − fee) | − quantity |
| **Crypto deposit** | **unchanged** | + quantity, cost per [Cost basis](#cost-basis) |
| **Crypto withdrawal** | **unchanged** | − quantity, cost per [Cost basis](#cost-basis) |

A crypto deposit or withdrawal moves the coin itself, so its value is counted once, in the coin's market value. Until 2026-11, they also added or removed quantity × price from cash, which counted the value twice. Exchange accounts take their cash from the Bybit wallet, so they were not affected.

**Landing page:** Shows the 20 most recent transactions across all categories (deposits, withdrawals, buys, sells), grouped by date with the latest date group first. Each transaction displays its time.

**Filters** (server-side, combined with AND logic):

| Filter | Description |
|--------|-------------|
| **Text search** | Searches across exchange name, notes, crypto symbol, and transaction type label (e.g., typing "bybit" finds all Bybit-synced trades) |
| **From date** | Show transactions on or after this date |
| **To date** | Show transactions on or before this date |
| **Status** | Filter by exchange status: All / Completed / Failed / Pending |

**Examples:**
- Filter by month: set From `2026-07-01`, To `2026-07-31`
- See only failed syncs: set Status to `Failed`
- Find all Bybit deposits from June: text `bybit` + From `2026-06-01` + To `2026-06-30`

All filters reset to page 1 when changed. When switching accounts, the current filters are retained and results restart on page 1. The date inputs and status dropdown can be used independently or together. A "Clear dates" button appears when dates are set.

**Pagination:** 20 transactions per page. Previous/Next buttons appear when there are more than 20 results, showing the current page and total transaction count. Changing accounts retains the current filters and returns to page 1.

**Persistence:** Account selection and Manual portfolio creation use the Account API. Account activity and filters are loaded from the selected portfolio; exchange sync events continue to use the existing persisted transaction and sync-status records.

---

### Project structure
```bash
dg-invest/
│
├── .github/
│   └── workflows/               # CI/CD pipelines
│
├── api/
│   ├── api/                     # .NET 10 Web API
│   ├── functions/               # Azure Functions (background workers)
│   ├── unit-tests/              # Automated tests
│   ├── dg-invest.api.sln        # Solution file
│   └── Dockerfile               # API + Functions Docker image
│
├── web-app/
│   ├── cypress/                 # E2E tests
│   ├── src/                     # Angular 18 application
│   ├── Dockerfile               # Frontend Docker image
│   └── package.json
│
├── docker-compose.yml           # Full stack environment
└── README.md
```

## Roadmap / Future Work

- **Notification System** — in-app badge notifications + email alerts for actionable events (e.g., CMC coin lookup failure during Bybit sync, sync errors, unsupported deposit/withdrawal coins). Users can manually create missing assets when notified.
```
