describe('Header Component', () => {
  const localStorageTokenKey = 'dg-invest-token';
  const fakeValidUserJwt = Cypress.env('FAKE_VALID_USER_JWT');
  const fakeValidAdminJwt = Cypress.env('FAKE_VALID_JWT');

  beforeEach(() => {
    cy.clearLocalStorage();
  });

  const loginAndSetToken = (token: string) => {
    cy.intercept('POST', 'https://localhost:7204/api/Authentication/login', {
      statusCode: 200,
      body: {
        data: {
          token: token
        }
      }
    }).as('loginRequest');

    cy.visit('http://localhost:4200/#/login');

    cy.get('input[formControlName="email"]').type('testuser@example.com');
    cy.get('input[formControlName="password"]').type('password');
    cy.get('button[type="submit"]').click();

    cy.wait('@loginRequest');

    cy.window().then((window) => {
      window.localStorage.setItem(localStorageTokenKey, JSON.stringify({ jwtToken: token }));
    });
  };

  const interceptListAssets = () => {
    cy.intercept('GET', 'https://localhost:7204/api/Crypto/list-assets?page=1&pageSize=50&assetName=&sortBy=symbol&sortOrder=asc&hideZeroBalance=false&userId=2003', {
      statusCode: 200,
      fixture: 'list-assets.json'
    }).as('listAssets');
  };

  const verifyHeaderLinks = (expectedLinks: string[]) => {
    cy.get('app-header .navbar-nav .nav-link').should('have.length', expectedLinks.length);
    expectedLinks.forEach(linkText => {
      cy.get('app-header header').should('contain.text', linkText);
    });
  };

  it('should display hamburguer icon in header component', () => {
    loginAndSetToken(fakeValidUserJwt);
    interceptListAssets();

    cy.visit('http://localhost:4200/#/cryptos');
    cy.wait('@listAssets');
    cy.url().should('include', '/cryptos');
    cy.get('app-view-cryptos').should('exist');

    cy.get('app-header header button .material-icons').should('contain', 'menu');
  });

  it('should display username initial letter in header component', () => {
    loginAndSetToken(fakeValidAdminJwt);
    interceptListAssets();

    cy.visit('http://localhost:4200/#/cryptos');
    cy.wait('@listAssets');
    cy.url().should('include', '/cryptos');
    cy.get('app-view-cryptos').should('exist');

    cy.get('app-header header #username-initial-letter').should('contain', 'J');
  });
});
