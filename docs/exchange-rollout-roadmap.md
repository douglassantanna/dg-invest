# Exchange Integration Rollout

The exchange integration is intentionally delivered in five pull-request phases. GitHub milestones are the operational backlog; this document preserves the scope and ordering.

## PR1: Data-model foundation

Current implementation: PR #232.

- Evolve `Account` with explicit manual/exchange origin, exchange, external ID, enabled state, and soft deletion.
- Add `ExchangeIntegration` as the per-user/per-exchange connection foundation.
- Preserve Key Vault credentials, per-account `SyncStatus`, and Blob sync logs.
- Apply safe migrations and retain compatibility with legacy account contracts.

## PR2: Exchange-agnostic API

- `GET /Exchange` integration list.
- `GET /Exchange/{exchange}` account list.
- `GET /Exchange/{exchange}/{account}` account detail.
- Generalize credential, test, enable/disable, sync-now, disconnect, and log operations.
- Ensure manual accounts and exchange accounts remain separate.

Tracked by milestone `Exchange PR2: Exchange-agnostic API`.

## PR3: Real exchange pages

- Replace mock exchange data with PR2 API contracts.
- Build `/exchanges`, `/exchanges/bybit`, and `/exchanges/bybit/{account}`.
- Implement real onboarding, credential management, account mapping, sync health, logs, and Cypress coverage.

Tracked by milestone `Exchange PR3: Real exchange pages`.

## PR4: Account selector

- Group manual portfolios, Bybit accounts, and future exchange accounts by origin.
- Show source icons and sync-health indicators without changing portfolio history behavior.

Tracked by milestone `Exchange PR4: Account selector`.

## PR5: Sync-engine refinement

- Drive scheduled synchronization through integrations.
- Honor account enablement for every sync path.
- Add manual sync-now and roll up integration health.
- Make polling safe through typed exchange errors, pagination, checkpoint safety, replay protection, and SQL-backed financial sync tests.

Tracked by milestone `Exchange PR5: Sync-engine refinement`.

## Test Policy

- Domain models, handlers, and error branches require unit tests.
- Migrations and API persistence/authorization flows require Testcontainers SQL Server integration tests.
- User-facing workflows require Cypress coverage with production API contracts mocked at the browser boundary.
- A phase is not complete until its relevant test layers pass in CI.

## Target Account Context Architecture

The completed integration will separate three concepts:

- A manual `Account` is a user-owned portfolio context. It never receives exchange credentials or is converted by discovery.
- An `ExchangeIntegration` is the per-user connection to an exchange. It holds integration-level discovery state and Key Vault credentials.
- An exchange `Account` is a portfolio context with `AccountType = Exchange`, an exchange name, and an external account ID such as a Bybit UID.

PR2 makes discovery create or update only exchange accounts and match them by `(UserId, Exchange, ExternalId)`. It must not match or mutate manual accounts based on a display name. PR4 adds an account selector that groups manual and exchange contexts by origin.

Disabling an integration or account stops every sync path, including polling and webhooks. Disconnecting removes stored credentials while preserving historical portfolio data.

## Delivery Flow

Each phase is released independently to `stage`; do not maintain one long-lived PR1-PR5 branch.

1. Create the phase branch from the current `stage` branch, for example `feat/exchange-pr2-api`.
2. Create small issue branches from that phase branch. Their pull requests target the phase branch, not `stage`.
3. Require unit tests for domain and handler behavior, Testcontainers integration tests for migrations/API persistence and authorization, and Cypress tests for user-facing workflows.
4. Open one phase pull request from the phase branch to `stage` after all milestone issues and CI checks are complete.
5. Deploy to stage in a feature-disabled state when the phase is foundation-only. Run smoke tests using isolated Bybit test accounts before enabling a user-facing beta.
6. Merge the phase pull request into `stage`, then create the next phase branch from the updated `stage` head.

For PR1, apply migrations and deploy the API/Function while keeping user self-service disabled. PR2 supplies the stable API boundary, PR3 exposes it to users, PR4 improves account navigation, and PR5 enables safe, observable automatic synchronization.
