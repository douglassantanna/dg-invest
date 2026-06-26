### Overview
This project is a full cryptocurrency investment manager and analytics suite that I’ve been sculpting over the last two years. It brings together real-time price tracking, detailed transaction logging, and rich statistical insight to help you keep a steady hand in the stormy seas of digital assets.

---
### Getting started
#### Prerequisites
You can run the project either directly on your machine or inside Docker.
#### Option 1: Local Environment
- .NET 8
- Node.js v18+
- Angular v18
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
  "RateLimiterSettings": {
    "RequestsPermitLimit": 320,
    "WindowLimitInMinutes": 10
  },
  "RunMigrations": false,
  "Serilog": {
    "Enrich": [
      "FromLogContext",
      "WithMachineName",
      "WithThreadId"
    ],
    "MinimumLevel": {
      "Default": "Error",
      "Override": {
        "Microsoft": "Error",
        "System": "Error"
      }
    },
    "Using": [
      "Serilog.Sinks.AzureBlobStorage"
    ],
    "WriteTo": [
      {
        "Name": "AzureBlobStorage",
        "Args": {
          "connectionString": "YourSerilogConnectionStringHere",
          "restrictedToMinimumLevel": "Information",
          "storageContainerName": "logs",
          "storageFileName": "log-{yyyy}-{MM}-{dd}.json"
        }
      }
    ]
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
#### Running everything locally (standalone)
#### 1. Installation steps
Install SQL Server Express, Azure Functions Core Tools, Azurite (VS Code extension or npm), .NET 8, EF Core tools, Node.js V18+, NPM and Angular CLI V18
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
### Bybit auto-trade sync

Orders, deposits, and withdrawals from your Bybit account are automatically synced to the app — no manual entry required. This works through two complementary mechanisms:

| Method | When | What it syncs |
|--------|------|---------------|
| **Webhook** (real-time) | An order is filled | Orders only |
| **REST polling** (every 30s) | Runs continuously in the background | Orders, deposits, and withdrawals |

The webhook catches trades instantly. The REST poll catches anything the webhook missed, including deposits and withdrawals Bybit doesn't send webhooks for. Both paths pass through the same dedup logic, so nothing is ever double-counted.

#### What gets synced

| Data | How it's recorded |
|------|------------------|
| **Buy** | Deducts cost (qty × price + fee) from your account balance. Adds coins to the crypto asset. |
| **Sell** | Adds proceeds (qty × price − fee) to your account balance. Removes coins from the crypto asset. |
| **Deposit** (crypto) | Adds coins to the crypto asset balance. Updates account balance by deposit value (qty × price). |
| **Withdrawal** (crypto) | Removes coins from the crypto asset balance. Deducts withdrawal value from account balance. |

If a coin doesn't exist in your portfolio yet, the sync automatically creates it by looking up the symbol on CoinMarketCap. Unknown coins will appear in the sync logs as failed — you can create them manually from the app.

Deposits and withdrawals go through multiple statuses (Pending → Processing → Success / Failed). The sync tracks each status change and only affects your balances when the status reaches **Success**. Pending/failed transactions appear in your account history with a status badge so you always know what's happening.

#### Data flow

```
Bybit (order filled)
  │
  ├─ Webhook POST → /api/tradewebhook/bybit/{userId}/{accountId}
  │    ├─ HMAC-SHA256 signature validated against your Key Vault secret
  │    ├─ Dedup check via ExchangeOrderId (idempotent)
  │    ├─ Coin auto-created via CoinMarketCap if new
  │    └─ Order saved + balance updated
  │
  └─ (missed webhook / deposit / withdrawal)
       │
       └─ Timer function (every 30s)
            ├─ Fetch recent orders from Bybit REST API
            ├─ Fetch recent deposits & withdrawals from Bybit REST API
            ├─ Dedup check via ExchangeOrderId / ExchangeTransactionId
            ├─ Status tracked (Pending / Success / Failed)
            └─ Saved to database + balance updated

     Sync logs are written to Azure Blob Storage (JSONL format)
     and can be viewed in the Exchange Management UI.
```

#### Setup

**1. Save your Bybit API credentials**

```http
POST /api/exchange/bybit/credentials
Authorization: Bearer <jwt>

{
  "accountId": 1,
  "apiKey": "your-bybit-api-key",
  "apiSecret": "your-bybit-api-secret",
  "webhookSecret": "your-bybit-webhook-signing-secret"
}
```

Credentials are stored in **Azure Key Vault** — never in the database.  
Key naming: `bybit-{userId}-{accountId}-{api-key|api-secret|webhook-secret}`.

**2. Sync your sub-accounts**

```http
POST /api/exchange/bybit/sync-accounts
Authorization: Bearer <jwt>
```

This reads all sub-accounts from Bybit, matches them to your internal portfolio accounts by their `SubaccountTag`, and links them via the Bybit UID. Any unmatched sub-accounts are created as new portfolio accounts automatically.

**3. Set up the webhook (optional — for instant order sync)**

In Bybit → Account → API Management → Webhooks, set the endpoint URL to:

```
https://your-domain.com/api/tradewebhook/bybit/{userId}/{accountId}
```

Use a different URL for each sub-account so trades are routed to the correct portfolio account. If you skip this step, orders are still picked up by the REST poller within 30 seconds.

#### Feature flag

The background sync can be turned off without deploying code. Set the following in your Azure Function app settings:

```
BybitSync:Enabled = false
```

It defaults to `true` if not set.

#### Sync cutoff (preventing manual-entry duplicates)

When you enable Bybit sync on an account that already has manual entries, the sync must avoid importing the same trades again and doubling your balances. The system uses a sliding time-window cutoff:

| Scenario | `startTime` sent to Bybit | Result |
|----------|--------------------------|--------|
| First sync after credentials saved | `BybitCredentialsSetAt` (time credentials were saved) | Manual entries before this timestamp are untouched |
| Every subsequent sync | `LastSyncAt` (time of the last successful sync) | Only new Bybit orders are fetched; the window slides forward naturally |

`BybitCredentialsSetAt` is stamped exactly once (never overwritten) when you save your credentials in the UI. `LastSyncAt` updates after every successful sync run, keeping the window within Bybit's 7-day order-history limit. Deposits and withdrawals use the same sliding window.

#### Migrating existing accounts

If you saved Bybit credentials **before** this cutoff mechanism was introduced, your account lacks a `SyncStatus` row in the database. The sync **skips** these accounts to prevent accidental full-history imports.

To enable sync on these accounts, **re-save your credentials** in the Exchange Management UI. This creates the `SyncStatus` row with a fresh `BybitCredentialsSetAt` timestamp, and the next sync run will only import orders after that moment — leaving your manual entries intact.

#### Azure Key Vault configuration

Add the following to `appsettings.json`:

```json
"KeyVaultSettings": {
  "VaultUri": "https://your-keyvault-name.vault.azure.net/"
}
```

The API uses `DefaultAzureCredential` to authenticate, which works with:
- Managed Identity (Azure App Service / Azure Container Apps)
- Azure CLI (`az login`) for local development

#### Security

- Webhook endpoint does **not** require JWT — it is protected exclusively by HMAC-SHA256 signature validation.
- Invalid signatures return `401`. Processing errors return `200` to prevent Bybit retry storms.
- `ExchangeOrderId` and `ExchangeTransactionId` provide idempotency — if Bybit delivers the same event more than once, it's silently skipped.
- API credentials are stored in Azure Key Vault, never in the database or browser.

#### Exchange Management UI

The frontend provides a dedicated **Exchange Connections** page (`/exchanges`) for managing your Bybit integration without touching the API. The page is organized into four sections:

1. **Credentials form** — enter API key, secret, and webhook signing secret per internal account.
2. **Sub-account sync** — pull sub-accounts from Bybit, see them listed with their UID, username, and remark. Unmapped sub-accounts can be linked to an internal portfolio account with one click.
3. **Sync status table** — shows the health of each exchange connection: status badge (Connected / Error / Disconnected), last sync timestamp, last processed order/transaction ID, and error count.
4. **Sync log viewer** — expandable per account, displays every processed event (order, deposit, withdrawal) with its status (Success / Duplicate / Failed), symbol, type (Buy / Sell / Deposit / Withdraw), quantity, price, and error message. Supports date filtering.

#### Monitoring

Every synced event is recorded in two places:

| Where | What's stored |
|-------|--------------|
| **Sync logs** (Azure Blob Storage) | JSONL log per day per account with full event details |
| **Sync status** (database) | Current health of each exchange connection (last sync time, error state, last processed ID) |
| **Account transactions** (database) | Your account history with status badges (Pending / Completed / Failed) |
| **Crypto transactions** (database) | Per-coin trade history with type badges (Buy / Sell / Deposit / Withdraw) |

#### Troubleshooting

**Transactions not appearing in the app**
1. Check the Exchange Management UI → Sync Logs — every event is logged with its status
2. Verify `BybitSync:Enabled` is not set to `false` in your function app settings
3. Confirm your API key has the required permissions (account transfer + spot trade history)
4. For deposits/withdrawals, only `Success` status affects your balance; pending ones display with a yellow badge

**Sync log errors**
- `Could not resolve asset for symbol` — the coin is not in your portfolio and CoinMarketCap returned no data. Create it manually from the app.
- `Transaction strategy failed` — check the error message in the log detail. Usually indicates an inconsistent balance (e.g., trying to withdraw more than you hold).
- `Failed to fetch` — your Bybit API credentials may be invalid or expired. Re-save them from the Credentials form.
- `no sync status for account` — your credentials were saved before the sync cutoff mechanism was introduced. Re-save them in the Exchange Management UI to enable sync.

**Duplicate transactions**
The sync is idempotent — running it multiple times will never create duplicates. If you see duplicates, check the `ExchangeOrderId` or `ExchangeTransactionId` values in the database for collisions.

---
### Project structure
```bash
dg-invest/
│
├── .github/
│   └── workflows/               # CI/CD pipelines
│
├── api/
│   ├── api/                     # .NET 8 Web API
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
