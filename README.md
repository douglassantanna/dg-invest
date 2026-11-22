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

DG Invest reads your Bybit **spot** activity and balances using **read-only** API keys. It never places orders or moves funds. Sync runs in the background every 30 seconds and on demand.

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

The timer `SyncBybitOrders` (every 30 seconds, when `BybitSync:Enabled`) and the manual **Sync now** button (`POST /api/Exchange/bybit/sync/{accountId}`) both run `BybitAccountSyncService.SyncAsync`. For each enabled account, one run does the following:

1. **Checks** that the account has a sync status, sync is enabled, the integration is connected (not disconnected or auto-paused) and credentials exist.
2. **Picks the start of the range:**
   - `LastSyncAt − 5 min` (the 5-minute overlap catches late arrivals);
   - if the account never synced, `BybitCredentialsSetAt − 5 min`, so manual entries made before connecting are not imported again;
   - with neither, the last 30 days.
3. **Fetches from that point to now in windows of at most 7 days**, always sending both `startTime` and `endTime`.
   - Bybit allows 7 days per request for orders and transfers, and less than 30 days for deposits and withdrawals. If you send only a start time, Bybit returns just the next 7 days, or rejects deposits with `131002`.
   - One run covers at most **8 windows (~56 days)**. An account further behind catches up over the following runs.
   - Each window fetches spot order history (paginated), deposits, withdrawals, internal transfers and universal transfers.
4. **Records the events in time order**, so a buy is recorded after the deposit, transfer or sell that funded it. Every event is idempotent:

   | Event | Deduplication key |
   |---|---|
   | Order | Bybit `orderId`, per crypto asset (unique index) |
   | Deposit / withdrawal | Bybit `txID`, or Bybit's record id when `txID` is empty (`bybit-deposit-{id}`, `bybit-withdraw-{withdrawId}`), **per account** (two accounts can share a `txID`: one withdraws, the other receives) |
   | Transfer | transfer ID + direction + account |

   Orders imported:
   - `Filled`;
   - `PartiallyFilledCanceled` with an executed quantity above zero (only the executed quantity and average price are used).
   - Only pairs quoted in **USDT, USDC or USD**. A pair like `BTCBRL` or `ETHBTC` is priced in another currency, so it is not imported. It is logged and counted as a skipped item.

   Fees come from Bybit's execution history when available.
5. **Reconciles balances with Bybit** (see [where each number comes from](#where-each-number-comes-from)).
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
| **Transfer in** (crypto deposit) | Adds coins at **zero cost**, ignoring the recorded price | Adds cost at the **recorded price** (market price when the deposit was recorded) |
| **Transfer out** (crypto withdrawal) | Keeps the **full cost** on the remaining coins, so the average price jumps | Removes cost at the **average price**, like a sale |
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
4. **Manual accounts** keep their stored figures until recalculated. An **admin** runs `POST /api/Recalculate/recalculate` once, with `RecalculationSettings__EnableRecalculation = true` set temporarily in the API app settings, then removes it. The next infra deploy also removes it.

`POST /api/Recalculate/recalculate` recalculates **every user's** accounts. It requires the Admin role, as well as the flag.

#### Transfers

| Transfer | What DG Invest records |
|---|---|
| Within the same UID (Funding ↔ Unified) | Nothing. It nets to zero. |
| Between UIDs (master ↔ sub, sub ↔ sub) in USDT/USDC | `TransferOut` on the source account and `TransferIn` on the destination, each side recorded by that account's own sync. They count toward each account's total deposited and net to zero across your accounts. |
| Between UIDs in other coins (e.g. BTC) | Not recorded in history yet. Quantities move only through wallet reconciliation (see Known limitations). |

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
| 10 consecutive transport failures (network, 5xx, 429) | Integration auto-paused |
| Any other error | The account's sync status becomes `Error`, and the next run retries |

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
4. If it is still missing, check its type and status in Bybit. Only spot orders (`Filled` / `PartiallyFilledCanceled`) and successful deposits and withdrawals are imported.

**Common errors**

| Message | Meaning and fix |
|---|---|
| `Bybit rejected sync request: 10003 …` / `10004 …` | The key is invalid, revoked, or the region is wrong. Save a new read-only key with the right region. |
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
  - Until `TransferCostPolicy` is switched to `Proportional`, transfers and balance without history use the Legacy rules described in [Cost basis](#cost-basis).
  - Crypto transfers between your own accounts are not recorded, so cost is lost at the destination.
  - Deposits record the price at processing time, not at deposit time, which is wrong for backfilled deposits.
- **Duplicate coins in one account** can still be created when two syncs of the same account run at the same time. There is no unique index yet.
- **A key saved on the wrong account** (for example, another sub-account's key) is accepted. The account then mirrors that other wallet.
- **Cost and resilience:** the timer runs every 30 s with sequential Bybit calls, ~15 per account; there is no short timeout or rate-limit handling.

---

### Account transactions

The **Accounts** page groups Manual portfolios and Exchange accounts, then shows the selected account's balance and transaction history.

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
