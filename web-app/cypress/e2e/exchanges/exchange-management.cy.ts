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

  const interceptApi = () => {
    cy.intercept('GET', '**/api/Account/list', { statusCode: 200, body: mockAccounts }).as('listAccounts');
    cy.intercept('GET', '**/api/Exchange/bybit/credentials-status', { statusCode: 200, body: { isSuccess: true, data: [] } }).as('credentialsStatus');
    cy.intercept('GET', '**/api/Exchange/bybit/sync-status', { statusCode: 200, body: { isSuccess: true, data: [] } }).as('syncStatuses');
  };

  beforeEach(() => {
    cy.clearLocalStorage();
    setAuthToken(fakeValidJwt);
    interceptApi();
  });

  it('should navigate to exchange page', () => {
    cy.visit('/#/exchanges');
    cy.wait(['@listAccounts', '@credentialsStatus', '@syncStatuses']);
    cy.get('app-exchange-management').should('exist');
    cy.contains('Exchange Connections').should('exist');
    cy.contains('Bybit API Credentials').should('exist');
  });

  it('should sync sub-accounts and display them', () => {
    cy.intercept('POST', '**/api/Exchange/bybit/sync-accounts', {
      statusCode: 200,
      body: { isSuccess: true, message: 'Sync complete. 0 matched, 2 created.' },
    }).as('syncAccounts');

    cy.intercept('GET', '**/api/Exchange/bybit/sub-members', {
      statusCode: 200,
      body: { isSuccess: true, data: mockSubMembers },
    }).as('subMembers');

    cy.visit('/#/exchanges');
    cy.wait(['@listAccounts', '@credentialsStatus', '@syncStatuses']);

    cy.contains('Sync from Bybit').click();
    cy.wait('@syncAccounts');
    cy.wait('@subMembers');

    cy.contains('rentAccount').should('exist');
    cy.contains('dadAccount').should('exist');
    cy.contains('Sync complete').should('exist');
  });

  it('should show Map button for unmapped sub-accounts', () => {
    cy.intercept('GET', '**/api/Exchange/bybit/sub-members', {
      statusCode: 200,
      body: { isSuccess: true, data: mockSubMembers },
    }).as('subMembers');

    cy.visit('/#/exchanges');
    cy.wait(['@listAccounts', '@credentialsStatus', '@syncStatuses']);

    cy.contains('Refresh List').click();
    cy.wait('@subMembers');

    // First account is selected by default ('main'), so button should say "Map to main"
    cy.contains('Map to main').should('exist');
  });

  it('should map a sub-account to the selected account', () => {
    cy.intercept('GET', '**/api/Exchange/bybit/sub-members', {
      statusCode: 200,
      body: { isSuccess: true, data: mockSubMembers },
    }).as('subMembers');

    cy.intercept('POST', '**/api/Exchange/bybit/map-account', {
      statusCode: 200,
      body: { isSuccess: true, message: "Account 'main' linked to Bybit UID 10001" },
    }).as('mapAccount');

    // After mapping, the sub-member shows mappedAccountTag
    const mappedSubMembers = [
      { uid: '10001', username: 'rentAccount', remark: '', mappedAccountTag: 'main', accountId: 1 },
      { uid: '10002', username: 'dadAccount', remark: '', mappedAccountTag: null, accountId: null },
    ];

    cy.intercept('GET', '**/api/Exchange/bybit/sub-members', {
      statusCode: 200,
      body: { isSuccess: true, data: mappedSubMembers },
    }).as('subMembersAfterMap');

    cy.visit('/#/exchanges');
    cy.wait(['@listAccounts', '@credentialsStatus', '@syncStatuses']);

    cy.contains('Refresh List').click();
    cy.wait('@subMembers');

    cy.contains('Map to main').click();
    cy.wait('@mapAccount');
    cy.wait('@subMembersAfterMap');

    // After mapping, the button should be replaced with the mapped account tag badge
    cy.contains('main').should('exist');
    cy.contains('Map to main').should('not.exist');
  });

  it('should show mapped account tag badge for already-mapped sub-accounts', () => {
    const alreadyMapped = [
      { uid: '10001', username: 'rentAccount', remark: '', mappedAccountTag: 'rent', accountId: 2 },
      { uid: '10002', username: 'dadAccount', remark: '', mappedAccountTag: null, accountId: null },
    ];

    cy.intercept('GET', '**/api/Exchange/bybit/sub-members', {
      statusCode: 200,
      body: { isSuccess: true, data: alreadyMapped },
    }).as('subMembers');

    cy.visit('/#/exchanges');
    cy.wait(['@listAccounts', '@credentialsStatus', '@syncStatuses']);

    cy.contains('Refresh List').click();
    cy.wait('@subMembers');

    // Mapped account shows tag badge
    cy.contains('rent').should('exist');
    // Unmapped still shows Map button
    cy.contains('Map to main').should('exist');
  });
});
