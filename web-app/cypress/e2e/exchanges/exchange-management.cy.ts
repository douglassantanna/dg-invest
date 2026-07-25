describe('Exchange Management', () => {
  const localStorageTokenKey = 'dg-invest-token';
  const fakeValidJwt = 'fake.valid.jwt';

  const setAuthToken = (jwt: string) => {
    cy.window().then((win) => {
      win.localStorage.setItem(localStorageTokenKey, JSON.stringify({ jwtToken: jwt }));
    });
  };

  const mockAccounts = [
    { accountId: 1, accountTag: 'main', exchangeName: 'Bybit', status: 'Connected', lastSyncAt: '2026-06-30T00:00:00Z', errorCount: 0, lastErrorMessage: null },
    { accountId: 2, accountTag: 'rent', exchangeName: '', status: 'NotConfigured', lastSyncAt: null, errorCount: 0, lastErrorMessage: null },
  ];

  const interceptBase = () => {
    cy.intercept('GET', '**/Account', { statusCode: 200, body: [{ id: 1, subaccountTag: 'main' }] }).as('accounts');
    cy.intercept('GET', '**/Exchange/accounts', { statusCode: 200, body: { isSuccess: true, data: mockAccounts } }).as('listAccounts');
  };

  beforeEach(() => {
    cy.clearLocalStorage();
    setAuthToken(fakeValidJwt);
    interceptBase();
  });

  it('should show exchange account list', () => {
    cy.visit('/#/exchanges');
    cy.get('app-exchange-list', { timeout: 10000 }).should('exist');
    cy.contains('main').should('exist');
    cy.contains('rent').should('exist');
    cy.contains('Connected').should('exist');
    cy.contains('NotConfigured').should('exist');
  });

  it('should sync sub-accounts from detail page', () => {
    const mockDetail = {
      accountId: 1, accountTag: 'main',
      connections: [{ exchangeName: 'Bybit', status: 'Connected', lastSyncAt: '2026-06-30T00:00:00Z', errorCount: 0, lastErrorMessage: null, hasApiKey: true, hasApiSecret: true, hasWebhookSecret: false }],
    };

    cy.intercept('GET', '**/Account', { statusCode: 200, body: [{ id: 1, subaccountTag: 'main' }, { id: 2, subaccountTag: 'rent' }] }).as('accounts');
    cy.intercept('GET', '**/Exchange/1', { statusCode: 200, body: { isSuccess: true, data: mockDetail } }).as('detail');
    cy.intercept('GET', '**/Exchange/1/transactions*', { statusCode: 200, body: { isSuccess: true, data: [] } }).as('transactions');
    cy.intercept('POST', '**/sync-accounts', { statusCode: 200, body: { isSuccess: true, message: 'Sync complete.' } }).as('syncAccounts');
    cy.intercept('GET', '**/sub-members', {
      statusCode: 200,
      body: { isSuccess: true, data: [{ uid: '10001', username: 'rentAccount', remark: '', mappedAccountTag: null, accountId: null }] },
    }).as('subMembers');

    cy.visit('/#/exchanges/1');
    cy.get('app-exchange-detail', { timeout: 10000 }).should('exist');

    cy.contains('Sync from Bybit').click({ force: true });
    cy.wait('@syncAccounts');
    cy.wait('@subMembers');

    cy.contains('rentAccount').should('exist');
  });

  it('should navigate to account detail page', () => {
    const mockDetail = {
      accountId: 1, accountTag: 'main',
      connections: [{ exchangeName: 'Bybit', status: 'Connected', lastSyncAt: '2026-06-30T00:00:00Z', errorCount: 0, lastErrorMessage: null, hasApiKey: true, hasApiSecret: true, hasWebhookSecret: false }],
    };

    cy.intercept('GET', '**/Exchange/1', { statusCode: 200, body: { isSuccess: true, data: mockDetail } }).as('detail');
    cy.intercept('GET', '**/Exchange/1/transactions*', { statusCode: 200, body: { isSuccess: true, data: [] } }).as('transactions');

    cy.visit('/#/exchanges');
    cy.get('app-exchange-list', { timeout: 10000 }).should('exist');
    cy.contains('Manage').first().click({ force: true });
    cy.url().should('include', '/exchanges/1');
    cy.get('app-exchange-detail', { timeout: 10000 }).should('exist');
    cy.contains('main — Exchange Settings').should('exist');
  });

  it('should show recent transactions on detail page', () => {
    const mockDetail = {
      accountId: 1, accountTag: 'main',
      connections: [{ exchangeName: 'Bybit', status: 'Connected', lastSyncAt: '2026-06-30T00:00:00Z', errorCount: 0, lastErrorMessage: null, hasApiKey: true, hasApiSecret: true, hasWebhookSecret: false }],
    };
    const mockTxs = [
      { id: 1, date: '2026-06-30T00:00:00Z', type: 'Buy', asset: 'BTC', amount: 0.01, price: 50000, fee: 5, exchangeName: 'Bybit', exchangeStatus: '3', notes: 'Auto-synced' },
    ];

    cy.intercept('GET', '**/Exchange/1', { statusCode: 200, body: { isSuccess: true, data: mockDetail } }).as('detail');
    cy.intercept('GET', '**/Exchange/1/transactions*', { statusCode: 200, body: { isSuccess: true, data: mockTxs } }).as('transactions');

    cy.visit('/#/exchanges/1');
    cy.get('app-exchange-detail', { timeout: 10000 }).should('exist');
    cy.contains('Recent Transactions').should('exist');
    cy.contains('BTC').should('exist');
    cy.contains('Buy').should('exist');
  });
});
