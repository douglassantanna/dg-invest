# Repository Map

Use this page as the starting point for code navigation. The root README is for local setup and product behavior; session notes are historical context, not current architecture.

## Start Here

| Need | Read or inspect |
| --- | --- |
| Product boundaries and non-negotiable rules | [`prompt.md`](../prompt.md) |
| Exchange phase scope and delivery policy | [`exchange-rollout-roadmap.md`](exchange-rollout-roadmap.md) |
| API composition, DI, auth, and configuration | [`api/api/Program.cs`](../api/api/Program.cs) |
| API request examples | [`api/api/Requests/`](../api/api/Requests/) |
| Deployment and CI behavior | [`.github/workflows/`](../.github/workflows/) |
| Database model and migrations | [`api/api/Data/`](../api/api/Data/) |

## Application Areas

| Area | Primary location | Tests |
| --- | --- | --- |
| Exchange HTTP surface | [`api/api/Controllers/ExchangeController.cs`](../api/api/Controllers/ExchangeController.cs) | [`api/unit-tests/IntegrationTests/ExchangeControllerIntegrationTests.cs`](../api/unit-tests/IntegrationTests/ExchangeControllerIntegrationTests.cs) |
| Bybit commands, queries, services, and models | [`api/api/Exchanges/`](../api/api/Exchanges/) | [`api/unit-tests/ExchangesTests/`](../api/unit-tests/ExchangesTests/) |
| Scheduled Bybit sync | [`api/functions/SyncBybitOrders.cs`](../api/functions/SyncBybitOrders.cs) and [`api/api/Exchanges/Services/BybitAccountSyncService.cs`](../api/api/Exchanges/Services/BybitAccountSyncService.cs) | [`api/unit-tests/FunctionsTests/SyncBybitOrdersTests.cs`](../api/unit-tests/FunctionsTests/SyncBybitOrdersTests.cs), [`api/unit-tests/ExchangesTests/Services/`](../api/unit-tests/ExchangesTests/Services/) |
| Other Functions | [`api/functions/`](../api/functions/) | [`api/unit-tests/FunctionsTests/`](../api/unit-tests/FunctionsTests/) |
| Accounts page and persisted account selector | [`web-app/src/app/pages/cryptos/containers/account/`](../web-app/src/app/pages/cryptos/containers/account/), [`web-app/src/app/core/services/account-selector-state.service.ts`](../web-app/src/app/core/services/account-selector-state.service.ts) | [`web-app/cypress/e2e/accounts-page.cy.ts`](../web-app/cypress/e2e/accounts-page.cy.ts), selector API test in `ExchangeControllerIntegrationTests` |
| Angular exchange pages | [`web-app/src/app/pages/exchanges/`](../web-app/src/app/pages/exchanges/) | [`web-app/cypress/e2e/exchanges/exchange-management.cy.ts`](../web-app/cypress/e2e/exchanges/exchange-management.cy.ts) |
| Angular API client and DTOs | [`web-app/src/app/core/services/exchange.service.ts`](../web-app/src/app/core/services/exchange.service.ts), [`web-app/src/app/core/models/`](../web-app/src/app/core/models/) | Cypress exchange spec |

## Current Bybit Flow

- `POST /api/Exchange/bybit/integration-credentials` saves integration credentials and region.
- `POST /api/Exchange/bybit/sync-accounts` discovers or reconciles exchange accounts.
- `POST /api/Exchange/bybit/sync/{accountId}` runs an account-level manual sync.
- `POST /api/Exchange/bybit/backfill/{accountId}` imports historical data from a requested date.
- `SyncBybitOrders` runs every 30 seconds when `BybitSync:Enabled` is enabled, then delegates account work to `IBybitAccountSyncService`.
- Webhook processing is handled by `ProcessBybitWebhookCommand`; do not assume the timer is the only sync path.

For a route list, use `ExchangeController.cs` as the source of truth. For sync semantics, use `BybitAccountSyncService.cs` and `BybitOrderSyncService.cs`, not older README examples.

## Accounts Page and Selector

- `GET /api/Account/selector-options` returns Manual and currently active Exchange contexts with selected state, balance, source, and sync health.
- The selector groups by `AccountType` and `Exchange`, excludes disconnected/stale Exchange accounts, and keeps paused or unhealthy active Exchange accounts visible with their status.
- `POST /api/Account/select-account` persists the active account. `POST /api/Account/create` adds a Manual portfolio.
- The Accounts page preserves transaction search, date/status filters, and 20-row server pagination; account switches retain filters and restart at page 1.
- The header is a passive display of the selected account. `Exchange Hub` is the onboarding and integration-management entry point.
- Use `GetAccountSelectorOptionsQuery` and `accounts-page.cy.ts` for the selector API and browser flow.

## Test Entry Points

Run backend commands from `api/`:

```bash
dotnet test unit-tests/unit-tests.csproj
dotnet test unit-tests/unit-tests.csproj --filter FullyQualifiedName~ExchangeControllerIntegrationTests
dotnet test unit-tests/unit-tests.csproj --filter FullyQualifiedName~AccountSelectorOptions
dotnet test unit-tests/unit-tests.csproj --filter FullyQualifiedName~SyncBybitOrdersTests
```

Run frontend commands from `web-app/`:

```bash
npm run build
npm run cypress:run -- --spec cypress/e2e/accounts-page.cy.ts
npm run cypress:run -- --spec cypress/e2e/exchanges/exchange-management.cy.ts
```

The repository pins .NET through [`global.json`](../global.json) and requires Node 20 or newer through [`web-app/package.json`](../web-app/package.json).

## Documentation Rules

- Update this map when a major entry point moves.
- Keep current behavior in repository docs close to the code; keep session notes for decisions, investigation history, and handoff context.
- Treat API routes, timer expressions, package versions, and migration names in code as authoritative over copied examples in historical notes.
- Before changing a phase, check the current branch and `docs/exchange-rollout-roadmap.md`; do not infer phase status from an old session note.
