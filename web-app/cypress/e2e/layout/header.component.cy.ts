describe('Header Component', () => {
  const localStorageTokenKey = 'dg-invest-token';
  const AUTH_TOKEN = 'eyJhbGciOiJub25lIn0.eyJ1bmlxdWVfbmFtZSI6IkRvdWdsYXMiLCJyb2xlIjoiQWRtaW4iLCJuYW1laWQiOiIxIn0.';

  beforeEach(() => {
    cy.clearLocalStorage();
  });

  const interceptListAssets = () => {
    cy.intercept('GET', '**/api/Crypto/list-assets*', {
      statusCode: 200,
      fixture: 'list-assets.json'
    }).as('listAssets');
  };

  const interceptAccountSelectorOptions = () => {
    cy.intercept('GET', '**/api/Account/selector-options', {
      body: [{
        accountId: 1,
        accountName: 'Main portfolio',
        balance: 0,
        isSelected: true,
        accountType: 'Manual',
        exchangeName: null,
        isBybitCombinedWallet: false,
        healthStatus: null,
        lastSyncAt: null,
        errorCount: 0,
        lastErrorMessage: null,
      }],
    }).as('selectorOptions');
  };

  const visitCryptosAsAuthenticatedUser = () => {
    interceptAccountSelectorOptions();
    interceptListAssets();
    cy.visit('/#/cryptos', {
      onBeforeLoad: win => {
        win.localStorage.setItem(
          localStorageTokenKey,
          JSON.stringify({ jwtToken: AUTH_TOKEN })
        );
      },
    });

    cy.wait('@selectorOptions');
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
