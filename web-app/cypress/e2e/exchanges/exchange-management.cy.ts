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

  it('should navigate to account detail page', () => {
    const mockDetail = {
      accountId: 1,
      accountTag: 'main',
      connections: [{
        exchangeName: 'Bybit',
        status: 'Connected',
        lastSyncAt: '2026-06-30T00:00:00Z',
        errorCount: 0,
        lastErrorMessage: null,
        hasApiKey: true,
        hasApiSecret: true,
        hasWebhookSecret: false,
      }],
    };

    cy.intercept('GET', '**/Exchange/1', { statusCode: 200, body: { isSuccess: true, data: mockDetail } }).as('detail');
    cy.intercept('GET', '**/Exchange/1/transactions*', { statusCode: 200, body: { isSuccess: true, data: [] } }).as('transactions');

    cy.visit('/#/exchanges');
    cy.get('app-exchange-list', { timeout: 10000 }).should('exist');

    cy.contains('Manage').first().click();
    cy.url().should('include', '/exchanges/1');
    cy.get('app-exchange-detail', { timeout: 10000 }).should('exist');
    cy.contains('main — Exchange Settings').should('exist');
  });

  it('should save credentials from detail page', () => {
    const mockDetail = {
      accountId: 1,
      accountTag: 'main',
      connections: [{
        exchangeName: 'Bybit',
        status: 'NotConfigured',
        lastSyncAt: null,
        errorCount: 0,
        lastErrorMessage: null,
        hasApiKey: false,
        hasApiSecret: false,
        hasWebhookSecret: false,
      }],
    };

    cy.intercept('GET', '**/Exchange/1', { statusCode: 200, body: { isSuccess: true, data: mockDetail } }).as('detail');
    cy.intercept('GET', '**/Exchange/1/transactions*', { statusCode: 200, body: { isSuccess: true, data: [] } }).as('transactions');
    cy.intercept('POST', '**/bybit/credentials', { statusCode: 200, body: { isSuccess: true, message: 'Credentials saved' } }).as('saveCredentials');
    cy.intercept('GET', '**/sub-members', { statusCode: 200, body: { isSuccess: true, data: [] } }).as('subMembers');

    cy.visit('/#/exchanges/1');
    cy.get('app-exchange-detail', { timeout: 10000 }).should('exist');

    cy.contains('Save Credentials').should('exist');
  });

  it('should show sub-accounts and Map button on detail page', () => {
    const mockDetail = {
      accountId: 1,
      accountTag: 'main',
      connections: [{
        exchangeName: 'Bybit',
        status: 'Connected',
        lastSyncAt: '2026-06-30T00:00:00Z',
        errorCount: 0,
        lastErrorMessage: null,
        hasApiKey: true,
        hasApiSecret: true,
        hasWebhookSecret: false,
      }],
    };

    const mockSubMembers = [
      { uid: '10001', username: 'rentAccount', remark: '', mappedAccountTag: null, accountId: null },
    ];

    cy.intercept('GET', '**/Exchange/1', { statusCode: 200, body: { isSuccess: true, data: mockDetail } }).as('detail');
    cy.intercept('GET', '**/Exchange/1/transactions*', { statusCode: 200, body: { isSuccess: true, data: [] } }).as('transactions');
    cy.intercept('GET', '**/sub-members', { statusCode: 200, body: { isSuccess: true, data: mockSubMembers } }).as('subMembers');

    cy.visit('/#/exchanges/1');
    cy.get('app-exchange-detail', { timeout: 10000 }).should('exist');

    cy.contains('Refresh').click();
    cy.wait('@subMembers');

    cy.contains('rentAccount').should('exist');
    cy.contains('Map to main').should('exist');
  });

  it('should map sub-account from detail page', () => {
    const mockDetail = {
      accountId: 1,
      accountTag: 'main',
      connections: [{
        exchangeName: 'Bybit',
        status: 'Connected',
        lastSyncAt: '2026-06-30T00:00:00Z',
        errorCount: 0,
        lastErrorMessage: null,
        hasApiKey: true,
        hasApiSecret: true,
        hasWebhookSecret: false,
      }],
    };

    cy.intercept('GET', '**/Exchange/1', { statusCode: 200, body: { isSuccess: true, data: mockDetail } }).as('detail');
    cy.intercept('GET', '**/Exchange/1/transactions*', { statusCode: 200, body: { isSuccess: true, data: [] } }).as('transactions');
    cy.intercept('GET', '**/sub-members', {
      statusCode: 200,
      body: { isSuccess: true, data: [{ uid: '10001', username: 'testAccount', remark: '', mappedAccountTag: null, accountId: null }] },
    }).as('subMembers');

    cy.intercept('POST', '**/map-account', {
      statusCode: 200,
      body: { isSuccess: true, message: "Account 'main' linked to Bybit UID 10001" },
    }).as('mapAccount');

    cy.visit('/#/exchanges/1');
    cy.get('app-exchange-detail', { timeout: 10000 }).should('exist');

    cy.contains('Refresh').click();
    cy.wait('@subMembers');

    cy.contains('Map to main').click();
    cy.wait('@mapAccount').its('request.body').should('deep.equal', {
      accountId: 1,
      bybitUid: '10001',
    });
  });
});
