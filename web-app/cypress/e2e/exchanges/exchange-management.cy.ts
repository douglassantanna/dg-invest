describe('Exchange Management', () => {
  const tokenKey = 'dg-invest-token';
  const api = '**/api/Exchange/bybit';

  const response = (data: unknown, message = 'ok') => ({
    statusCode: 200,
    body: { message, isSuccess: true, data },
  });

  const groups = (accounts = [connectedAccount()]) => response([
    {
      id: 'bybit-main',
      name: 'Main account (Bybit login)',
      subaccountCount: accounts.length,
      maxSubaccounts: 10,
      subaccounts: accounts,
    },
  ]);

  const statuses = (accounts = [connectedAccount()]) => response(accounts.map(account => ({
    accountId: account.accountId,
    accountName: account.name,
    exchangeName: 'Bybit',
    status: 'Connected',
    lastSyncAt: '2026-08-19T12:00:00Z',
    lastOrderId: null,
    errorCount: 0,
    lastErrorMessage: null,
  })));

  function connectedAccount() {
    return {
      accountId: 101,
      name: 'Trading account',
      externalId: '123456',
      status: 'ok',
      hasApiKey: true,
      hasApiSecret: true,
      hasWebhookSecret: true,
      maskedApiKey: '....1234',
      webhookUrl: '/api/tradewebhook/bybit/1/101',
      lastVerifiedAt: 'Just now',
      isEnabled: true,
    };
  }

  const authenticateAndVisit = (groupResponse = groups(), statusResponse = statuses()) => {
    cy.intercept('GET', `${api}/connection-groups`, groupResponse).as('connectionGroups');
    cy.intercept('GET', `${api}/sync-status`, statusResponse).as('syncStatus');
    cy.visit('/');
    cy.window().then(win => {
      win.localStorage.setItem(tokenKey, JSON.stringify({ jwtToken: 'auth-token' }));
    });
    cy.visit('/#/exchanges');
    cy.wait('@connectionGroups');
    cy.wait('@syncStatus');
  };

  beforeEach(() => cy.clearLocalStorage());

  it('renders synchronized Bybit account state and available actions', () => {
    authenticateAndVisit();

    cy.contains('Bybit integration').should('be.visible');
    cy.contains('1 configured').should('be.visible');
    cy.contains('1 enabled').should('be.visible');
    cy.contains('Trading account').should('be.visible');
    cy.contains('UID 123456').should('be.visible');
    cy.contains('Connected').should('be.visible');
    cy.contains('Configured').should('be.visible');
    cy.contains('Test').should('be.enabled');
    cy.contains('Pause').should('be.enabled');
  });

  it('shows setup and disabled-test states for missing Key Vault credentials', () => {
    const missingCredentials = {
      ...connectedAccount(),
      status: 'pending',
      hasApiKey: false,
      hasApiSecret: false,
      hasWebhookSecret: false,
      isEnabled: false,
    };
    authenticateAndVisit(groups([missingCredentials]), statuses([missingCredentials]));

    cy.contains('Needs setup').should('be.visible');
    cy.contains('Missing').should('be.visible');
    cy.contains('Test').should('be.disabled');
    cy.contains('Enable').should('be.enabled');
  });

  it('runs discovery, connection test, and sync toggle through the API', () => {
    cy.intercept('POST', `${api}/sync-accounts`, request => {
      expect(request.body).to.deep.equal({});
      request.reply(response(null, 'Accounts synchronized'));
    }).as('syncAccounts');
    cy.intercept('POST', `${api}/test-connection/101`, request => {
      expect(request.body).to.deep.equal({});
      request.reply(response(null, 'Connection successful'));
    }).as('testConnection');
    cy.intercept('POST', `${api}/toggle/101`, request => {
      expect(request.body).to.deep.equal({});
      request.reply(response(null, 'Sync paused'));
    }).as('toggleAccount');
    authenticateAndVisit();

    cy.contains('Sync accounts').click();
    cy.wait('@syncAccounts');
    cy.contains('Accounts synchronized').should('be.visible');

    cy.contains('Test').click();
    cy.wait('@testConnection');
    cy.contains('Connection successful').should('be.visible');

    cy.contains('Pause').click();
    cy.wait('@toggleAccount');
    cy.contains('Sync paused').should('be.visible');
  });

  it('surfaces missing Key Vault credentials returned by account discovery', () => {
    cy.intercept('GET', `${api}/connection-groups`, {
      statusCode: 400,
      body: { message: 'Bybit credentials not found in Key Vault', isSuccess: false, data: null },
    }).as('connectionGroups');
    cy.intercept('GET', `${api}/sync-status`, response([])).as('syncStatus');
    cy.visit('/');
    cy.window().then(win => {
      win.localStorage.setItem(tokenKey, JSON.stringify({ jwtToken: 'auth-token' }));
    });
    cy.visit('/#/exchanges');
    cy.wait('@connectionGroups');

    cy.contains('Failed to load Bybit accounts').should('be.visible');
    cy.contains('No Bybit accounts found').should('be.visible');
  });

  it('shows the API error when syncing without Key Vault credentials', () => {
    cy.intercept('POST', `${api}/sync-accounts`, {
      statusCode: 400,
      body: { message: 'Bybit credentials not found. Please save your API key and secret first.', isSuccess: false, data: null },
    }).as('syncAccounts');
    authenticateAndVisit();

    cy.contains('Sync accounts').click();
    cy.wait('@syncAccounts');
    cy.contains('Bybit credentials not found. Please save your API key and secret first.').should('be.visible');
  });
});
