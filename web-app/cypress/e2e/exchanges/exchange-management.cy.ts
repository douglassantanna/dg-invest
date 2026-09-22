describe('Exchange management', () => {
  const tokenKey = 'dg-invest-token';
  const api = '**/api/Exchange';

  const response = (data: unknown, message = 'ok') => ({
    statusCode: 200,
    body: { message, isSuccess: true, data },
  });

  const account = () => ({
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
  });

  const groups = (accounts = [account()]) => response([
    {
      id: 'bybit-main',
      name: 'Main account (Bybit login)',
      subaccountCount: accounts.length,
      maxSubaccounts: 10,
      subaccounts: accounts,
    },
  ]);

  const statuses = (accounts = [account()]) => response(accounts.map(item => ({
    accountId: item.accountId,
    accountName: item.name,
    exchangeName: 'Bybit',
    status: 'Connected',
    lastSyncAt: '2026-08-19T12:00:00Z',
    lastOrderId: null,
    errorCount: 0,
    lastErrorMessage: null,
  })));

  const accountDetail = () => response({
    accountId: 101,
    accountName: 'Trading account',
    connections: [{
      exchangeName: 'Bybit',
      status: 'Connected',
      lastSyncAt: '2026-08-19T12:00:00Z',
      errorCount: 0,
      lastErrorMessage: null,
      hasApiKey: true,
      hasApiSecret: true,
      hasWebhookSecret: true,
    }],
  });

  const transactions = response([{
    id: 12,
    date: '2026-08-19T12:00:00Z',
    type: 'Buy',
    asset: 'BTC',
    amount: 0.25,
    price: 65000,
    fee: 0.001,
    exchangeName: 'Bybit',
    exchangeStatus: 'Filled',
    notes: 'Bybit order import',
  }]);

  const logs = response([{
    id: 'log-1',
    userId: 1,
    accountId: 101,
    exchangeName: 'Bybit',
    orderId: 'ORD-1',
    symbol: 'BTCUSDT',
    side: 'Buy',
    qty: 0.25,
    price: 65000,
    status: 'Imported',
    errorMessage: null,
    timestamp: '2026-08-19T12:00:00Z',
    importSource: 'Webhook',
  }]);

  const subMembers = response([{
    uid: '123456',
    username: 'trading-account',
    remark: 'Trading account',
    mappedAccountName: 'Trading account',
    accountId: 101,
  }]);

  const authenticate = () => {
    cy.visit('/');
    cy.window().then(win => {
      win.localStorage.setItem(tokenKey, JSON.stringify({ jwtToken: 'auth-token' }));
    });
  };

  const visitBybit = () => {
    cy.intercept('GET', `${api}/bybit/connection-groups`, groups()).as('connectionGroups');
    cy.intercept('GET', `${api}/bybit/sync-status`, statuses()).as('syncStatus');
    cy.intercept('GET', `${api}/bybit/sub-members`, subMembers).as('subMembers');
    authenticate();
    cy.visit('/#/exchanges/bybit');
    cy.wait('@connectionGroups');
    cy.wait('@syncStatus');
    cy.wait('@subMembers');
  };

  const visitAccount = () => {
    cy.intercept('GET', `${api}/101`, accountDetail()).as('accountDetail');
    cy.intercept('GET', `${api}/101/transactions?limit=20`, transactions).as('transactions');
    cy.intercept('GET', `${api}/bybit/sync-logs/101`, logs).as('syncLogs');
    cy.intercept('GET', `${api}/bybit/connection-groups`, groups()).as('connectionGroups');
    cy.intercept('GET', `${api}/bybit/sub-members`, subMembers).as('subMembers');
    authenticate();
    cy.visit('/#/exchanges/bybit/101');
    cy.wait('@accountDetail');
    cy.wait('@transactions');
    cy.wait('@syncLogs');
    cy.wait('@connectionGroups');
    cy.wait('@subMembers');
  };

  beforeEach(() => cy.clearLocalStorage());

  it('routes from the exchange index to the Bybit integration', () => {
    cy.intercept('GET', `${api}/accounts`, response([])).as('exchangeAccounts');
    authenticate();
    cy.visit('/#/exchanges');
    cy.wait('@exchangeAccounts');

    cy.contains('Connect exchanges').should('be.visible');
    cy.contains('Bybit').should('be.visible');
    cy.contains('Manage integration').click();

    cy.location('hash').should('eq', '#/exchanges/bybit');
  });

  it('loads exchange accounts through the exchange-agnostic account contract', () => {
    cy.intercept('GET', `${api}/accounts`, response([
      {
        accountId: 101,
        accountName: 'Trading account',
        exchangeName: 'Bybit',
        status: 'Connected',
        lastSyncAt: '2026-08-19T12:00:00Z',
        errorCount: 0,
        lastErrorMessage: null,
      },
    ])).as('exchangeAccounts');
    authenticate();
    cy.visit('/#/exchanges');
    cy.wait('@exchangeAccounts');

    cy.contains('Trading account').should('be.visible');
    cy.contains('Connected').should('be.visible');
    cy.contains('a', 'Manage account').should('have.attr', 'href', '#/exchanges/bybit/101');
  });

  it('routes unconfigured Bybit accounts to account management', () => {
    cy.intercept('GET', `${api}/accounts`, response([
      {
        accountId: 202,
        accountName: 'New trading account',
        exchangeName: 'Bybit',
        status: 'NotConfigured',
        lastSyncAt: null,
        errorCount: 0,
        lastErrorMessage: null,
      },
    ])).as('exchangeAccounts');
    authenticate();
    cy.visit('/#/exchanges');
    cy.wait('@exchangeAccounts');

    cy.contains('a', 'Manage account').should('have.attr', 'href', '#/exchanges/bybit/202');
  });

  it('runs a manual sync from the Bybit account page', () => {
    visitAccount();
    cy.intercept('POST', `${api}/bybit/sync/101`, request => {
      expect(request.body).to.deep.equal({});
      request.reply(response(null, 'Bybit account synchronized successfully'));
    }).as('manualSync');

    cy.contains('button', 'Sync now').click();
    cy.wait('@manualSync');
    cy.contains('Bybit account synchronized successfully').should('be.visible');
  });

  it('onboards Bybit, clears credential fields, discovers accounts, and routes to account management', () => {
    cy.intercept('GET', `${api}/bybit/connection-groups`, response([])).as('connectionGroups');
    cy.intercept('GET', `${api}/bybit/sync-status`, response([])).as('syncStatus');
    cy.intercept('GET', `${api}/bybit/sub-members`, response([])).as('subMembers');
    cy.intercept('POST', `${api}/bybit/integration-credentials`, request => {
      expect(request.body).to.deep.equal({ apiKey: 'api-key', apiSecret: 'api-secret', masterUid: '10001', region: 'Global' });
      request.reply(response(null, 'Integration credentials saved successfully'));
    }).as('saveIntegrationCredentials');
    cy.intercept('POST', `${api}/bybit/sync-accounts`, response(null, 'Account discovery complete. 1 found, 1 created.')).as('discoverAccounts');
    authenticate();
    cy.visit('/#/exchanges/bybit');
    cy.wait('@connectionGroups');
    cy.wait('@syncStatus');
    cy.wait('@subMembers');
    cy.intercept('GET', `${api}/bybit/connection-groups`, groups()).as('updatedGroups');
    cy.intercept('GET', `${api}/bybit/sync-status`, statuses()).as('updatedStatuses');

    cy.get('#bybit-api-key').type('api-key');
    cy.get('#bybit-api-secret').type('api-secret');
    cy.get('#bybit-master-uid').type('10001');
    cy.contains('button', 'Connect Bybit').click();
    cy.wait('@saveIntegrationCredentials');
    cy.get('#bybit-api-key').should('have.value', '');
    cy.get('#bybit-api-secret').should('have.value', '');
    cy.get('#bybit-master-uid').should('have.value', '');
    cy.wait('@discoverAccounts');
    cy.wait('@updatedGroups');
    cy.wait('@updatedStatuses');
    cy.intercept('GET', `${api}/101`, accountDetail()).as('accountDetail');
    cy.intercept('GET', `${api}/101/transactions?limit=20`, transactions).as('transactions');
    cy.intercept('GET', `${api}/bybit/sync-logs/101`, logs).as('syncLogs');

    cy.contains('Trading account').click();
    cy.wait('@accountDetail');
    cy.location('hash').should('eq', '#/exchanges/bybit/101');
  });

  it('surfaces integration save and discovery errors without retaining secrets', () => {
    cy.intercept('GET', `${api}/bybit/connection-groups`, response([])).as('connectionGroups');
    cy.intercept('GET', `${api}/bybit/sync-status`, response([])).as('syncStatus');
    cy.intercept('GET', `${api}/bybit/sub-members`, response([])).as('subMembers');
    cy.intercept('POST', `${api}/bybit/integration-credentials`, {
      statusCode: 503,
      body: { message: 'Key Vault is temporarily unavailable', isSuccess: false, data: 503 },
    }).as('saveIntegrationCredentials');
    authenticate();
    cy.visit('/#/exchanges/bybit');

    cy.get('#bybit-api-key').type('api-key');
    cy.get('#bybit-api-secret').type('api-secret');
    cy.get('#bybit-master-uid').type('10001');
    cy.contains('button', 'Connect Bybit').click();
    cy.wait('@saveIntegrationCredentials');
    cy.get('#bybit-api-key').should('have.value', '');
    cy.get('#bybit-api-secret').should('have.value', '');
    cy.get('#bybit-master-uid').should('have.value', '');
    cy.contains('Key Vault is temporarily unavailable').should('be.visible');
  });

  it('surfaces Bybit discovery errors after credentials are saved', () => {
    cy.intercept('GET', `${api}/bybit/connection-groups`, response([])).as('connectionGroups');
    cy.intercept('GET', `${api}/bybit/sync-status`, response([])).as('syncStatus');
    cy.intercept('GET', `${api}/bybit/sub-members`, response([])).as('subMembers');
    cy.intercept('POST', `${api}/bybit/integration-credentials`, response(null, 'Integration credentials saved successfully')).as('saveIntegrationCredentials');
    cy.intercept('POST', `${api}/bybit/sync-accounts`, {
      statusCode: 400,
      body: { message: 'Bybit rejected the integration credentials: 10003 - API key is invalid', isSuccess: false, data: 400 },
    }).as('discoverAccounts');
    authenticate();
    cy.visit('/#/exchanges/bybit');
    cy.wait('@connectionGroups');
    cy.wait('@syncStatus');
    cy.wait('@subMembers');

    cy.get('#bybit-api-key').type('bad-key');
    cy.get('#bybit-api-secret').type('bad-secret');
    cy.get('#bybit-master-uid').type('10001');
    cy.contains('button', 'Connect Bybit').click();
    cy.wait('@saveIntegrationCredentials');
    cy.wait('@discoverAccounts');

    cy.get('#bybit-api-key').should('have.value', '');
    cy.get('#bybit-api-secret').should('have.value', '');
    cy.contains('Bybit rejected the integration credentials: 10003 - API key is invalid').should('be.visible');
    cy.contains('Trading account').should('not.exist');
  });

  it('disconnects Bybit through the API with confirmation and reloads the setup state', () => {
    cy.intercept('POST', `${api}/bybit/disconnect`, response(null, 'Bybit integration disconnected')).as('disconnect');
    visitBybit();
    cy.window().then(win => cy.stub(win, 'confirm').returns(true));
    cy.intercept('GET', `${api}/bybit/connection-groups`, groups([])).as('disconnectedGroups');
    cy.intercept('GET', `${api}/bybit/sync-status`, response([])).as('disconnectedStatuses');
    cy.intercept('GET', `${api}/bybit/sub-members`, response([])).as('disconnectedSubMembers');

    cy.contains('button', 'Disconnect Bybit').click();
    cy.wait('@disconnect');
    cy.wait('@disconnectedGroups');
    cy.wait('@disconnectedStatuses');
    cy.wait('@disconnectedSubMembers');
    cy.contains('Bybit integration disconnected').should('be.visible');
    cy.contains('button', 'Disconnect Bybit').should('not.exist');
    cy.contains('Setup required').should('be.visible');
    cy.contains('Trading account').should('not.exist');
  });

  it('reconnects with different credentials and replaces discovered accounts', () => {
    cy.intercept('POST', `${api}/bybit/disconnect`, response(null, 'Bybit integration disconnected')).as('disconnect');
    visitBybit();
    cy.window().then(win => cy.stub(win, 'confirm').returns(true));
    cy.intercept('GET', `${api}/bybit/connection-groups`, groups([])).as('disconnectedGroups');
    cy.intercept('GET', `${api}/bybit/sync-status`, response([])).as('disconnectedStatuses');
    cy.intercept('GET', `${api}/bybit/sub-members`, response([])).as('disconnectedSubMembers');

    cy.contains('button', 'Disconnect Bybit').click();
    cy.wait('@disconnect');
    cy.wait('@disconnectedGroups');
    cy.wait('@disconnectedStatuses');
    cy.wait('@disconnectedSubMembers');
    cy.contains('Trading account').should('not.exist');

    const newAccount = () => ({
      accountId: 202,
      name: 'New trading account',
      externalId: '999999',
      status: 'ok',
      hasApiKey: true,
      hasApiSecret: true,
      hasWebhookSecret: true,
      maskedApiKey: '....9999',
      webhookUrl: '/api/tradewebhook/bybit/1/202',
      lastVerifiedAt: 'Just now',
      isEnabled: true,
    });
    const newGroups = (accounts = [newAccount()]) => response([
      {
        id: 'bybit-main',
        name: 'Main account (Bybit login)',
        subaccountCount: accounts.length,
        maxSubaccounts: 10,
        subaccounts: accounts,
      },
    ]);

    cy.intercept('POST', `${api}/bybit/integration-credentials`, request => {
      expect(request.body).to.deep.equal({ apiKey: 'new-key', apiSecret: 'new-secret', masterUid: '10001', region: 'Global' });
      request.reply(response(null, 'Integration credentials saved successfully'));
    }).as('saveIntegrationCredentials');
    cy.intercept('POST', `${api}/bybit/sync-accounts`, response(null, 'Account discovery complete. 1 found, 1 created.')).as('discoverAccounts');
    cy.intercept('GET', `${api}/bybit/connection-groups`, newGroups()).as('reconnectedGroups');
    cy.intercept('GET', `${api}/bybit/sync-status`, statuses([newAccount()])).as('reconnectedStatuses');
    cy.intercept('GET', `${api}/bybit/sub-members`, response([{
      uid: '999999',
      username: 'new-trading-account',
      remark: 'New trading account',
      mappedAccountName: 'New trading account',
      accountId: 202,
    }])).as('reconnectedSubMembers');

    cy.get('#bybit-api-key').type('new-key');
    cy.get('#bybit-api-secret').type('new-secret');
    cy.get('#bybit-master-uid').type('10001');
    cy.contains('button', 'Connect Bybit').click();
    cy.wait('@saveIntegrationCredentials');
    cy.wait('@discoverAccounts');
    cy.wait('@reconnectedGroups');
    cy.wait('@reconnectedStatuses');
    cy.wait('@reconnectedSubMembers');

    cy.contains('New trading account').should('be.visible');
    cy.contains('Trading account').should('not.exist');
  });

  it('rotates account credentials, clears secrets, and refreshes account state', () => {
    cy.intercept('POST', `${api}/bybit/credentials`, request => {
      expect(request.body).to.deep.equal({
        accountId: 101,
        apiKey: 'new-key',
        apiSecret: 'new-secret',
        webhookSecret: 'new-webhook',
        region: 'Global',
      });
      request.reply(response(null, 'Credentials saved successfully'));
    }).as('saveCredentials');
    visitAccount();

    cy.get('#account-api-key').type('new-key');
    cy.get('#account-api-secret').type('new-secret');
    cy.get('#account-webhook-secret').type('new-webhook');
    cy.contains('button', 'Save credentials').click();
    cy.wait('@saveCredentials');
    cy.get('#account-api-key').should('have.value', '');
    cy.get('#account-api-secret').should('have.value', '');
    cy.get('#account-webhook-secret').should('have.value', '');
  });

  it('surfaces Bybit account test connection errors', () => {
    cy.intercept('POST', `${api}/bybit/test-connection/101`, {
      statusCode: 400,
      body: { message: 'Bybit rejected the account credentials: 10003 - API key is invalid', isSuccess: false, data: 400 },
    }).as('testConnection');
    visitAccount();

    cy.contains('button', 'Test connection').click();
    cy.wait('@testConnection');

    cy.contains('Bybit rejected the account credentials: 10003 - API key is invalid').should('be.visible');
  });

  it('supports account test, pause, mapping, logs, transactions, and removal', () => {
    cy.intercept('POST', `${api}/bybit/test-connection/101`, response(null, 'Connection successful')).as('testConnection');
    cy.intercept('POST', `${api}/bybit/toggle/101`, response({ isEnabled: false }, 'Sync paused')).as('toggleAccount');
    cy.intercept('POST', `${api}/bybit/map-account`, request => {
      expect(request.body).to.deep.equal({ accountId: 101, externalId: '123456' });
      request.reply(response(null, 'Account mapped successfully'));
    }).as('mapAccount');
    cy.intercept('DELETE', `${api}/bybit/credentials/101`, response(null, 'Subaccount removed')).as('removeAccount');
    visitAccount();
    cy.window().then(win => cy.stub(win, 'confirm').returns(true));

    cy.contains('Recent exchange transactions').should('be.visible');
    cy.contains('BTC').should('be.visible');
    cy.contains('Sync logs').should('be.visible');
    cy.contains('ORD-1').should('be.visible');

    cy.contains('button', 'Test connection').click();
    cy.wait('@testConnection');
    cy.contains('button', 'Pause sync').click();
    cy.wait('@toggleAccount');
    cy.get('select[name="externalId"]').select('123456');
    cy.contains('button', 'Map account').click();
    cy.wait('@mapAccount');
    cy.contains('button', 'Remove exchange account').click();
    cy.wait('@removeAccount');
    cy.contains('Subaccount removed').should('be.visible');
  });
});
