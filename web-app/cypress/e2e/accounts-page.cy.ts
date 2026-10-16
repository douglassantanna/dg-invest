describe('Accounts page in memory preview mode', () => {
  const accountApi = '**/api/Account/**';
  const tokenKey = 'dg-invest-token';

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
    cy.intercept(accountApi).as('accountApi');
    authenticate();
    cy.visit('/#/account');
    cy.contains('h1', 'Accounts').should('be.visible');
  };

  beforeEach(() => cy.clearLocalStorage());

  it('groups manual and exchange accounts and shows the active account passively in the header', () => {
    visitAccounts();

    cy.contains('h2', 'Manual portfolios').should('be.visible');
    cy.contains('h2', 'Exchange accounts').should('be.visible');
    cy.contains('a', 'Connect an exchange').should('be.visible');
    cy.contains('a', 'Exchange Hub').should('be.visible');
    cy.contains('h3', 'Bybit').should('be.visible');
    cy.contains('h3', 'Kraken').should('exist');
    cy.contains('button', 'Needs attention').should('exist');
    cy.contains('button', 'Paused').should('exist');
    cy.get('#selected-account-name').should('have.text', 'Personal portfolio');
    cy.get('[aria-label="Current account"]').should('contain.text', 'Personal portfolio');
    cy.get('[aria-label="Current account"]').should('not.have.attr', 'href');
    cy.get('a[aria-label^="Change account"]').should('not.exist');
    cy.get('@accountApi.all').should('have.length', 0);
  });

  it('keeps filters and pagination when switching accounts, resetting to page one', () => {
    visitAccounts();

    cy.get('app-crypto-filter input').type('BTC');
    cy.get('input[type="date"]').eq(0).type('2026-08-01').blur();
    cy.get('input[type="date"]').eq(1).type('2026-10-15').blur();
    cy.get('select').select('completed');
    cy.contains('button', 'Next').click();
    cy.contains('Page 2 of').scrollIntoView().should('be.visible');

    cy.contains('button', 'Bybit Trading').click();

    cy.contains('Page 1 of').scrollIntoView().should('be.visible');
    cy.get('app-crypto-filter input').should('have.value', 'BTC');
    cy.get('input[type="date"]').eq(0).should('have.value', '2026-08-01');
    cy.get('input[type="date"]').eq(1).should('have.value', '2026-10-15');
    cy.get('select').should('have.value', 'completed');
    cy.get('[aria-label="Current account"]').should('contain.text', 'Bybit Trading');
    cy.get('#selected-account-name').should('have.text', 'Bybit Trading');
    cy.contains('h3', 'Recent Transactions').scrollIntoView().should('be.visible');
    cy.contains('BTC exchange activity').scrollIntoView().should('be.visible');
    cy.contains('Manage this account').should('exist');
    cy.contains('button', 'Deposit').should('not.exist');
    cy.get('@accountApi.all').should('have.length', 0);
  });

  it('adds a manual portfolio in memory without writing it to the API', () => {
    visitAccounts();

    cy.contains('button', 'Add portfolio').click();
    cy.get('#new-manual-account-name').type('Long-term savings');
    cy.contains('button', 'Create').click();

    cy.contains('button', 'Long-term savings').should('be.visible');
    cy.get('[aria-label="Current account"]').should('contain.text', 'Personal portfolio');
    cy.get('@accountApi.all').should('have.length', 0);

    cy.reload();
    cy.contains('button', 'Long-term savings').should('not.exist');
  });

  it('keeps the grouped selector and activity usable on a narrow screen', () => {
    cy.viewport(390, 844);
    visitAccounts();

    cy.contains('button', 'Kraken Spot').click();
    cy.contains('h3', 'Recent Transactions').scrollIntoView().should('be.visible');
    cy.get('[aria-label="Current account"]').should('contain.text', 'Kraken Spot');
    cy.document().then(document => {
      expect(document.documentElement.scrollWidth).to.be.at.most(document.documentElement.clientWidth);
    });
  });
});
