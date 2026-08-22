describe('Bybit Connect Mock', () => {
  const tokenKey = 'dg-invest-token';

  const visitConnectPage = () => {
    cy.visit('/');
    cy.window().then(win => {
      win.localStorage.setItem(tokenKey, JSON.stringify({ jwtToken: 'auth-token' }));
    });
    cy.visit('/#/exchanges/bybit/connect');
  };

  beforeEach(() => cy.clearLocalStorage());

  it('validates credentials and completes the mock onboarding sequence without submitting secrets', () => {
    cy.intercept('POST', '**/api/Exchange/bybit/credentials').as('saveCredentials');
    visitConnectPage();

    cy.contains('Connect your Bybit account').should('be.visible');
    cy.contains('Prototype').should('be.visible');
    cy.contains('Save securely and continue').click();
    cy.contains('Enter both an API key and API secret to continue.').should('be.visible');

    cy.get('#api-key').type('mock-api-key');
    cy.get('#api-secret').type('mock-api-secret');
    cy.contains('Save securely and continue').click();
    cy.contains('Credentials saved').should('be.visible');
    cy.contains('Test connection').click();
    cy.contains('Connection verified. You can now discover your Bybit accounts.').should('be.visible');
    cy.contains('Discover Bybit accounts').click();
    cy.contains('4 Bybit accounts found.').should('be.visible');
    cy.get('@saveCredentials.all').should('have.length', 0);
  });

  it('keeps secrets masked by default and returns to Exchange management', () => {
    cy.intercept('GET', '**/api/Exchange/bybit/connection-groups', {
      statusCode: 200,
      body: { message: 'ok', isSuccess: true, data: [] },
    });
    cy.intercept('GET', '**/api/Exchange/bybit/sync-status', {
      statusCode: 200,
      body: { message: 'ok', isSuccess: true, data: [] },
    });
    visitConnectPage();

    cy.get('#api-secret').should('have.attr', 'type', 'password');
    cy.contains('Show').click();
    cy.get('#api-secret').should('have.attr', 'type', 'text');
    cy.contains('Hide').click();
    cy.get('#api-secret').should('have.attr', 'type', 'password');

    cy.contains('Back to Bybit integration').click();
    cy.url().should('include', '/exchanges');
    cy.contains('Bybit integration').should('be.visible');
  });
});
