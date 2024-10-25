describe('Auth Guard', () => {
  const localStorageTokenKey = 'dg-invest-token';
  const fakeValidJwt = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJlbWFpbCI6ImpvaG45QGVtYWlsLmNvbSIsIm5hbWVpZCI6IjIwMDgiLCJ1bmlxdWVfbmFtZSI6IkpvaG4gRG9lIiwicm9sZSI6IkFkbWluIiwibmJmIjoxNzI5ODU0MjU2LCJleHAiOjE3MzA0NTkwNTYsImlhdCI6MTcyOTg1NDI1NiwiaXNzIjoiaHR0cDovL215LWxvY2FsLWhvc3QuY29tIn0.eqzUT6DYqXcftzsIY-XDToI578_biUWE1DrVbp2PhYQ';
  const fakeExpiredJwt = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.QuY29tIn0';
  beforeEach(() => {
    cy.clearLocalStorage();
  });

  it('should redirect unauthenticated users to the login page', () => {
    // Visit the cryptos page directly without setting a JWT
    cy.visit('http://localhost:4200/#/cryptos');

    // Check if the URL includes 'login'
    cy.url().should('include', '/login');

    // Check if the login page contains the login form
    cy.get('form').should('exist');
  });

  it('should redirect users with an invalid JWT to the login page', () => {
    // Set an invalid JWT using the LocalStorageService's setToken method
    cy.window().then((window) => {
      window.localStorage.setItem(localStorageTokenKey, JSON.stringify({ jwtToken: fakeExpiredJwt }));
    });

    // Visit the cryptos page
    cy.visit('http://localhost:4200/#/cryptos');

    // Check if the URL includes 'login'
    cy.url({ timeout: 20000 }).should('include', '/#/login');

    // Check if the login page contains the login form
    cy.get('form').should('exist');
  });

  it('should allow authenticated users to access the cryptos page', () => {
    // Set a valid JWT using the LocalStorageService's setToken method
    cy.window().then((window) => {
      window.localStorage.setItem(localStorageTokenKey, JSON.stringify({ jwtToken: fakeValidJwt }));
    });

    // Visit the cryptos page
    cy.visit('http://localhost:4200/#/cryptos');

    // Check if the URL includes 'cryptos'
    cy.url().should('include', '/cryptos');

    // Check if the cryptos page contains the cryptos list
    cy.get('app-view-cryptos').should('exist');
  });
});
