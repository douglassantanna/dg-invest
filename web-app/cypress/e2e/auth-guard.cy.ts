describe('Auth Guard', () => {
  const localStorageTokenKey = 'dg-invest-token';
  const fakeValidJwt = Cypress.env('FAKE_VALID_JWT');
  const fakeExpiredJwt = Cypress.env('FAKE_EXPIRED_JWT');
  beforeEach(() => {
    cy.clearLocalStorage();
  });

  it('should redirect unauthenticated users to the login page', () => {
    cy.visit('http://localhost:4200/#/cryptos');

    cy.url().should('include', '/login');

    cy.get('form').should('exist');
  });

  it('should redirect users with an invalid JWT to the login page', () => {
    cy.window().then((window) => {
      window.localStorage.setItem(localStorageTokenKey, JSON.stringify({ jwtToken: fakeExpiredJwt }));
      expect(window.localStorage.getItem(localStorageTokenKey)).to.exist;
    });

    cy.intercept('GET', 'https://localhost:7204/api/Crypto/list-assets?page=1&pageSize=50&assetName=&sortBy=symbol&sortOrder=asc&hideZeroBalance=false&userId=2003', (req) => {
      req.reply({
        statusCode: 401,
        body: {
          message: 'Unauthorized'
        }
      });
    }).as('list-assets');

    cy.visit('http://localhost:4200/#/cryptos');

    cy.wait('@list-assets');

    cy.url().should('include', '/login');

    cy.get('app-login').should('exist');
  });

  it('should allow authenticated users to access the cryptos page', () => {
    cy.window().then((window) => {
      window.localStorage.setItem(localStorageTokenKey, JSON.stringify({ jwtToken: fakeValidJwt }));
    });

    cy.visit('http://localhost:4200/#/cryptos');

    cy.url().should('include', '/cryptos');

    cy.get('app-view-cryptos').should('exist');
  });
});
