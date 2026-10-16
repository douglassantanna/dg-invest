# WebApp

This is the Angular 17 frontend. See the repository [navigation map](../docs/README.md) for the exchange page and API client entry points.

## Development server

Run `npm start` for a development server at `http://localhost:4200/`. The application automatically reloads when source files change.

## Build

Run `npm run build` to build the project. The build artifacts are written to `dist/`.

## Unit tests

Run `npm test` to execute unit tests via [Karma](https://karma-runner.github.io).

## End-to-end tests

Run `npm run cypress:run` to execute the Cypress end-to-end tests. To run only exchange management tests:

```bash
npm run cypress:run -- --spec cypress/e2e/exchanges/exchange-management.cy.ts
```

## Source layout

- `src/app/pages/` contains route-level pages and containers.
- `src/app/core/services/exchange.service.ts` contains the exchange API client.
- `src/app/core/models/` contains API-facing DTOs.
- `cypress/e2e/` contains browser contract tests.
