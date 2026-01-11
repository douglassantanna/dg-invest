describe('Header Component', () => {
  const localStorageTokenKey = 'dg-invest-token';
  const AUTH_TOKEN = 'auth-token';

  beforeEach(() => {
    cy.clearLocalStorage();
  });

  const setAuthToken = () => {
    cy.window().then(win => {
      win.localStorage.setItem(
        localStorageTokenKey,
        JSON.stringify({ jwtToken: AUTH_TOKEN })
      );
    });
  };

  const interceptListAssets = () => {
    cy.intercept('GET', '**/api/Crypto/list-assets*', {
      statusCode: 200,
      fixture: 'list-assets.json'
    }).as('listAssets');
  };

  const visitCryptosAsAuthenticatedUser = () => {
    interceptListAssets();
    setAuthToken();

    cy.visit('/#/cryptos');

    cy.wait('@listAssets');
    cy.get('app-view-cryptos').should('exist');
  };

  it('should display hamburger icon in header component', () => {
    visitCryptosAsAuthenticatedUser();

    cy.get('app-header header button .material-icons')
      .should('contain', 'menu');
  });

  it('should display username initial letter in header component', () => {
    visitCryptosAsAuthenticatedUser();

    cy.get('#username-initial-letter')
      .should('be.visible')
      .and('not.be.empty');
  });
});
