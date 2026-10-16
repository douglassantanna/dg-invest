describe('Accounts page', () => {
  const tokenKey = 'dg-invest-token';
  const api = '**/api/Account';
  let selectedAccountId: number;
  let createdManualAccountName: string | null;

  const accountOptions = () => [
    {
      accountId: 1,
      accountName: 'Personal portfolio',
      balance: 18265.43,
      isSelected: selectedAccountId === 1,
      accountType: 'Manual',
      exchangeName: null,
      isBybitCombinedWallet: false,
      healthStatus: null,
    },
    {
      accountId: 2,
      accountName: 'Tax reserve',
      balance: 7200,
      isSelected: selectedAccountId === 2,
      accountType: 'Manual',
      exchangeName: null,
      isBybitCombinedWallet: false,
      healthStatus: null,
    },
    {
      accountId: 101,
      accountName: 'Bybit Main',
      balance: 25842.1,
      isSelected: selectedAccountId === 101,
      accountType: 'Exchange',
      exchangeName: 'Bybit',
      isBybitCombinedWallet: true,
      healthStatus: 'Connected',
      lastSyncAt: '2026-10-15T13:58:00Z',
      errorCount: 0,
      lastErrorMessage: null,
    },
    {
      accountId: 102,
      accountName: 'Bybit Trading',
      balance: 8430.5,
      isSelected: selectedAccountId === 102,
      accountType: 'Exchange',
      exchangeName: 'Bybit',
      isBybitCombinedWallet: false,
      healthStatus: 'NeedsAttention',
      lastSyncAt: '2026-10-15T13:45:00Z',
      errorCount: 2,
      lastErrorMessage: 'Temporary connection error',
    },
    {
      accountId: 201,
      accountName: 'Kraken Spot',
      balance: 4305.75,
      isSelected: selectedAccountId === 201,
      accountType: 'Exchange',
      exchangeName: 'Kraken',
      isBybitCombinedWallet: false,
      healthStatus: 'Paused',
      lastSyncAt: '2026-10-14T13:00:00Z',
      errorCount: 0,
      lastErrorMessage: null,
    },
    ...(createdManualAccountName ? [{
      accountId: 3,
      accountName: createdManualAccountName,
      balance: 0,
      isSelected: selectedAccountId === 3,
      accountType: 'Manual',
      exchangeName: null,
      isBybitCombinedWallet: false,
      healthStatus: null,
    }] : []),
  ];

  const accountDetails = (accountId: number, page: number) => ({
    id: accountId,
    balance: accountId === 102 ? 8430.5 : 18265.43,
    groupedAccountTransactions: [{
      date: '2026-10-15T13:25:00Z',
      transactions: [{
        date: '2026-10-15T13:25:00Z',
        transactionType: 2,
        amount: 0.002,
        exchangeName: accountId === 102 ? 'Bybit' : '',
        currency: 'USD',
        destination: '',
        notes: accountId === 102 ? 'Bybit trade fill' : 'Monthly contribution',
        cryptoCurrentPrice: 122000,
        cryptoSymbol: 'BTC',
        fee: 0.5,
        exchangeStatus: 'Filled',
        feeCurrency: 'USDT',
        feeQuoteValue: 0.5,
      }],
    }],
    totalCount: 25,
    page,
    pageSize: 20,
    hasNextPage: page === 1,
    hasPreviousPage: page > 1,
    isBybitCombinedWallet: accountId === 101,
  });

  const authenticate = () => {
    cy.visit('/');
    cy.window().then(win => {
      const payload = btoa(JSON.stringify({ role: 'User', unique_name: 'Demo user', nameid: '1', exp: 4102444800 }))
        .replace(/\+/g, '-')
        .replace(/\//g, '_')
        .replace(/=+$/, '');
      win.localStorage.setItem(tokenKey, JSON.stringify({ jwtToken: `eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.${payload}.demo` }));
    });
    cy.reload();
  };

  const visitAccounts = () => {
    selectedAccountId = 1;
    createdManualAccountName = null;
    cy.intercept('GET', `${api}/selector-options`, request => request.reply(accountOptions())).as('selectorOptions');
    cy.intercept('GET', `${api}/account-details*`, request => {
      const page = Number(new URL(request.url).searchParams.get('page') ?? 1);
      request.reply(accountDetails(selectedAccountId, page));
    }).as('accountDetails');
    cy.intercept('POST', `${api}/select-account`, request => {
      selectedAccountId = request.body.accountId;
      request.reply({ body: { message: 'Account selected successfully', isSuccess: true, data: null } });
    }).as('selectAccount');

    authenticate();
    cy.visit('/#/account');
    cy.wait('@selectorOptions');
    cy.wait('@accountDetails');
  };

  beforeEach(() => cy.clearLocalStorage());

  it('groups by source and shows the active account passively in the header', () => {
    visitAccounts();

    cy.contains('h1', 'Accounts').should('be.visible');
    cy.contains('h2', 'Manual portfolios').should('be.visible');
    cy.contains('h2', 'Exchange accounts').should('be.visible');
    cy.contains('h3', 'Bybit').should('be.visible');
    cy.contains('h3', 'Kraken').should('exist');
    cy.contains('button', 'Needs attention').should('exist');
    cy.contains('button', 'Paused').should('exist');
    cy.get('#selected-account-name').should('have.text', 'Personal portfolio');
    cy.get('[aria-label="Current account"]').should('contain.text', 'Personal portfolio');
    cy.get('[aria-label="Current account"]').should('not.have.attr', 'href');
    cy.get('a[aria-label^="Change account"]').should('not.exist');
    cy.contains('a', 'Exchange Hub').should('be.visible');
  });

  it('keeps filters and pagination when switching accounts, resetting to page one', () => {
    visitAccounts();

    cy.get('app-crypto-filter input').type('BTC');
    cy.wait('@accountDetails').its('request.url').should('contain', 'filter=BTC');
    cy.get('input[type="date"]').eq(0).type('2026-10-01').blur();
    cy.wait('@accountDetails').its('request.url').should('contain', 'startDate=2026-10-01');
    cy.get('input[type="date"]').eq(1).type('2026-10-15').blur();
    cy.wait('@accountDetails').its('request.url').should('contain', 'endDate=2026-10-15');
    cy.get('select').select('completed');
    cy.wait('@accountDetails').its('request.url').should('contain', 'status=completed');
    cy.contains('button', 'Next').click();
    cy.wait('@accountDetails').then(({ request }) => {
      const url = new URL(request.url);
      expect(url.searchParams.get('page')).to.equal('2');
      expect(url.searchParams.get('status')).to.equal('completed');
    });

    cy.contains('button', 'Bybit Trading').click();
    cy.wait('@selectAccount');
    cy.wait('@accountDetails').then(({ request }) => {
      const url = new URL(request.url);
      expect(url.searchParams.get('filter')).to.equal('BTC');
      expect(url.searchParams.get('page')).to.equal('1');
      expect(url.searchParams.get('startDate')).to.equal('2026-10-01');
      expect(url.searchParams.get('endDate')).to.equal('2026-10-15');
      expect(url.searchParams.get('status')).to.equal('completed');
    });

    cy.get('[aria-label="Current account"]').should('contain.text', 'Bybit Trading');
    cy.get('#selected-account-name').should('have.text', 'Bybit Trading');
    cy.contains('h3', 'Recent Transactions').scrollIntoView().should('be.visible');
    cy.contains('Bybit trade fill').scrollIntoView().should('be.visible');
    cy.contains('Manage this account').should('exist');
    cy.contains('button', 'Deposit').should('not.exist');
  });

  it('creates a manual portfolio through the existing Account API', () => {
    visitAccounts();
    cy.intercept('POST', `${api}/create`, request => {
      expect(request.body).to.deep.equal({ name: 'Long-term savings' });
      createdManualAccountName = request.body.name;
      request.reply({ body: '' });
    }).as('createManualAccount');

    cy.contains('button', 'Add portfolio').click();
    cy.get('#new-manual-account-name').type('Long-term savings');
    cy.contains('button', 'Create').click();
    cy.wait('@createManualAccount');
    cy.wait('@selectorOptions');

    cy.contains('button', 'Long-term savings').should('be.visible');
    cy.get('[aria-label="Current account"]').should('contain.text', 'Personal portfolio');
  });

  it('keeps the grouped selector and activity usable on a narrow screen', () => {
    cy.viewport(390, 844);
    visitAccounts();

    cy.contains('button', 'Kraken Spot').click();
    cy.wait('@selectAccount');
    cy.contains('h3', 'Recent Transactions').scrollIntoView().should('be.visible');
    cy.get('[aria-label="Current account"]').should('contain.text', 'Kraken Spot');
    cy.document().then(document => {
      expect(document.documentElement.scrollWidth).to.be.at.most(document.documentElement.clientWidth);
    });
  });
});
