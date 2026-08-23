# dg-invest Product Context

## Product

dg-invest is a cryptocurrency portfolio manager. Users track manual portfolios, assets, transactions, balances, and analytics. The current major initiative is safe exchange integration, beginning with Bybit and designed to support future exchanges.

## Account Architecture

Keep these concepts separate:

- A manual account is a user-owned portfolio context. Discovery must never convert it into an exchange account by matching a name.
- An exchange integration is the per-user connection to an exchange. It owns integration-level status and discovery credentials.
- An exchange account is a separately selectable portfolio context identified by `AccountType = Exchange`, its exchange name, and an external account ID such as a Bybit UID.

Discovery matches exchange accounts only by `(UserId, Exchange, ExternalId)`. Manual accounts remain outside exchange-management views and exchange account limits.

## Credential Rules

- API keys, API secrets, and webhook secrets belong in Azure Key Vault, never in database rows, logs, responses, or browser state after save.
- Immutable credential sets are activated through durable operations. Do not overwrite an active set directly.
- Credential operations must be recoverable, concurrency-safe, and observable.
- Disabling or disconnecting an integration or account must stop polling and webhook processing while preserving portfolio history.

## Exchange Delivery Roadmap

1. PR1: account and integration data-model foundation.
2. PR2: exchange-agnostic API, credential safety, migration, and disconnect operations.
3. PR3: real exchange pages, onboarding, credential management, and Cypress workflows.
4. PR4: grouped manual/exchange account selector with source and health indicators.
5. PR5: reliable sync engine, pagination, checkpoints, replay protection, and financial import coverage.

## Delivery Rules

- Each phase branch starts from the current `stage` branch.
- Small issue branches target the current phase branch.
- A completed phase opens one PR into `stage`.
- Use unit tests for domain and handler behavior.
- Use Testcontainers SQL Server tests for migrations, persistence, API contracts, authorization, concurrency, and recovery.
- Use Cypress for user-facing workflows.
- Keep foundation-only features disabled in stage until their user-facing and sync-safety phases are ready.

## Current Focus

Finish PR2 safely: legacy credential promotion, safe disconnect, and complete API integration coverage. Do not expose self-service Bybit onboarding until PR3.
