describe('Auth Guard', () => {
  const localStorageTokenKey = 'dg-invest-token';
  const fakeValidJwt = "fake.valid.jwt"; 
  const fakeExpiredJwt = "fake.expired.jwt";

  const setAuthToken = (jwt: string) => {
    cy.window().then(win => {
      win.localStorage.setItem(
        localStorageTokenKey,
        JSON.stringify({ jwtToken: jwt })
      );
    });
  };

  beforeEach(() => {
    cy.clearLocalStorage();
  });

  it('should redirect unauthenticated users to the login page', () => {
    cy.visit('/#/cryptos');

    cy.url().should('include', '/login');

    cy.get('form').should('exist');
  });

  it('should redirect users with an invalid JWT to the login page', () => {
    setAuthToken(fakeExpiredJwt);

    cy.intercept(
      {
        method: 'GET',
        url: '**/api/Crypto/list-assets*'
      },
      {
        statusCode: 401,
        body: { message: 'Unauthorized' }
      })
      .as('list-assets');

    cy.visit('/#/cryptos');

    cy.wait('@list-assets');

    cy.url().should('include', '/login');

    cy.get('app-login').should('exist');
  });

  it('should allow authenticated users to access the cryptos page', () => {
    setAuthToken(fakeValidJwt);

    cy.intercept(
      {
        method: 'GET',
        url: '**/api/Crypto/list-assets*'
      },
      {
        statusCode: 200,
        body: []
      })
      .as('list-assets');

    cy.visit('/#/cryptos');

    cy.wait('@list-assets');
    cy.url().should('include', '/cryptos');
    cy.get('app-view-cryptos').should('exist');
  });
});
