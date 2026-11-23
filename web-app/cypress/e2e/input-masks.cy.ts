// Number inputs format while typing (en-US) and still send plain numbers to the API (issue #328).
describe('Input masks: add transaction', () => {
  const tokenKey = 'dg-invest-token';
  const encodeJwtSegment = (value: object) => btoa(JSON.stringify(value))
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/, '');

  const authenticate = () => {
    cy.visit('/');
    cy.window().then(win => {
      const header = encodeJwtSegment({ alg: 'none', typ: 'JWT' });
      const payload = encodeJwtSegment({ role: 'User', unique_name: 'Demo user', nameid: '1', exp: 4102444800 });
      win.localStorage.setItem(tokenKey, JSON.stringify({ jwtToken: `${header}.${payload}.demo` }));
    });
    cy.reload();
  };

  beforeEach(() => {
    cy.clearLocalStorage();
    cy.intercept('GET', '**/api/Account/selector-options', []);
    cy.intercept('GET', '**/api/Crypto/get-crypto-asset-by-id/*', {
      isSuccess: true,
      data: { id: 79, cryptoAssetData: [], cryptoInformation: { symbol: 'BTC' }, transactions: [] },
    }).as('cryptoAsset');
  });

  it('formats quantity, price and fee while typing and sends plain numbers', () => {
    cy.intercept('POST', '**/api/Crypto/add-transaction', { isSuccess: true, message: 'ok' }).as('addTransaction');
    authenticate();
    cy.visit('/#/crypto-dashboard/79');
    cy.wait('@cryptoAsset');

    cy.get('app-add-transaction #transactionType').select(1);
    // Coin quantity: up to 8 decimals, letters dropped, no rounding.
    cy.get('app-add-transaction #amount').type('0.123456789x').should('have.value', '0.12345678');
    cy.get('app-add-transaction #pricePerUnit').type('65000.5').should('have.value', '65,000.5');
    cy.get('app-add-transaction #fee').type('a1.25').should('have.value', '1.25');
    cy.get('app-add-transaction #pricePerUnit').clear().type('1234567.89').should('have.value', '1,234,567.89');

    cy.get('app-add-transaction').contains('button', 'Save').click();
    cy.contains('button', 'Confirm').click();

    cy.wait('@addTransaction').its('request.body').then(body => {
      expect(body.amount).to.equal(0.12345678);
      expect(body.price).to.equal(1234567.89);
      expect(body.fee).to.equal(1.25);
    });
  });
});
