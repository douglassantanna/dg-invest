describe('Exchange Management', () => {
  const localStorageTokenKey = 'dg-invest-token';
  const fakeValidJwt = 'fake.valid.jwt';

  const setAuthToken = (jwt: string) => {
    cy.window().then((win) => {
      win.localStorage.setItem(localStorageTokenKey, JSON.stringify({ jwtToken: jwt }));
    });
  };

  const mockAccounts = [
    { id: 1, subaccountTag: 'main', isSelected: true, balance: 5000, userId: 1 },
    { id: 2, subaccountTag: 'rent', isSelected: false, balance: 2000, userId: 1 },
  ];

  const mockSubMembers = [
    { uid: '10001', username: 'rentAccount', remark: '', mappedAccountTag: null, accountId: null },
    { uid: '10002', username: 'dadAccount', remark: '', mappedAccountTag: null, accountId: null },
  ];

  const interceptStatic = () => {
    cy.intercept('GET', '**/Account', { statusCode: 200, body: mockAccounts }).as('listAccounts');
    cy.intercept('GET', '**/credentials-status', { statusCode: 200, body: { isSuccess: true, data: [] } }).as('credentialsStatus');
    cy.intercept('GET', '**/sync-status', { statusCode: 200, body: { isSuccess: true, data: [] } }).as('syncStatuses');
  };

  beforeEach(() => {
    cy.clearLocalStorage();
    setAuthToken(fakeValidJwt);
    interceptStatic();
  });

  it('should navigate to exchange page', () => {
    cy.visit('/#/exchanges');
    cy.get('app-exchange-management', { timeout: 10000 }).should('exist');
    cy.contains('Exchange Connections').should('exist');
    cy.contains('Bybit API Credentials').should('exist');
  });

  it('should sync sub-accounts and display them', () => {
    cy.intercept('POST', '**/sync-accounts', {
      statusCode: 200,
      body: { isSuccess: true, message: 'Sync complete. 0 matched, 2 created.' },
    }).as('syncAccounts');

    cy.intercept('GET', '**/sub-members', {
      statusCode: 200,
      body: { isSuccess: true, data: mockSubMembers },
    }).as('subMembers');

    cy.visit('/#/exchanges');
    cy.get('app-exchange-management', { timeout: 10000 }).should('exist');

    cy.contains('Sync from Bybit').click();
    cy.wait('@syncAccounts');
    cy.wait('@subMembers');

    cy.contains('rentAccount').should('exist');
    cy.contains('dadAccount').should('exist');
    cy.contains('Sync complete').should('exist');
  });

  it('should show Map button for unmapped sub-accounts', () => {
    cy.intercept('GET', '**/sub-members', {
      statusCode: 200,
      body: { isSuccess: true, data: mockSubMembers },
    }).as('subMembers');

    cy.visit('/#/exchanges');
    cy.get('app-exchange-management', { timeout: 10000 }).should('exist');

    cy.contains('Refresh List').click();
    cy.wait('@subMembers');

    cy.contains('Map to main').should('exist');
  });

  it('should map a sub-account to the selected account', () => {
    let callCount = 0;
    cy.intercept('GET', '**/sub-members', (req) => {
      callCount++;
      if (callCount === 1) {
        req.reply({ statusCode: 200, body: { isSuccess: true, data: mockSubMembers } });
      } else {
        req.reply({
          statusCode: 200,
          body: {
            isSuccess: true,
            data: [
              { uid: '10001', username: 'rentAccount', remark: '', mappedAccountTag: 'main', accountId: 1 },
              { uid: '10002', username: 'dadAccount', remark: '', mappedAccountTag: null, accountId: null },
            ],
          },
        });
      }
    });

    cy.intercept('POST', '**/map-account', {
      statusCode: 200,
      body: { isSuccess: true, message: "Account 'main' linked to Bybit UID 10001" },
    }).as('mapAccount');

    cy.visit('/#/exchanges');
    cy.get('app-exchange-management', { timeout: 10000 }).should('exist');

    cy.contains('Refresh List').click();
    cy.contains('Map to main', { timeout: 5000 }).should('exist');

    cy.contains('Map to main').click();
    cy.wait('@mapAccount');

    // After mapping, the button should be replaced by a badge with the tag name
    cy.contains('main').should('exist');
    cy.contains('Map to main').should('not.exist');
  });

  it('should show mapped account tag badge for already-mapped sub-accounts', () => {
    const alreadyMapped = [
      { uid: '10001', username: 'rentAccount', remark: '', mappedAccountTag: 'rent', accountId: 2 },
      { uid: '10002', username: 'dadAccount', remark: '', mappedAccountTag: null, accountId: null },
    ];

    cy.intercept('GET', '**/sub-members', {
      statusCode: 200,
      body: { isSuccess: true, data: alreadyMapped },
    }).as('subMembers');

    cy.visit('/#/exchanges');
    cy.get('app-exchange-management', { timeout: 10000 }).should('exist');

    cy.contains('Refresh List').click();
    cy.wait('@subMembers');

    cy.contains('rent').should('exist');
    cy.contains('Map to main').should('exist');
  });
});
