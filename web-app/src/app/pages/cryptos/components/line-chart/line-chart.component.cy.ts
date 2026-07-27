import { LineChartComponent } from './line-chart.component'

const mockMarketData = {
  value: [
    { time: 1753614000, value: 10000 },
    { time: 1753617600, value: 10100 },
    { time: 1753621200, value: 10050 },
  ],
}

const timeframeLabels = ['24H', '7D', '1M', '1Y', 'All']

describe('LineChartComponent', () => {
  beforeEach(() => {
    cy.intercept('GET', '**/get-marketData-by-timeframe*', {
      statusCode: 200,
      body: mockMarketData,
    }).as('marketData')
  })

  it('should mount', () => {
    cy.mount(LineChartComponent)
    cy.wait('@marketData')
    cy.get('button').should('exist')
  })

  it('should render all 5 timeframe buttons', () => {
    cy.mount(LineChartComponent)
    cy.wait('@marketData')
    timeframeLabels.forEach((label) => {
      cy.contains('button', label).should('be.visible')
    })
  })

  it('should default to 24H selected', () => {
    cy.mount(LineChartComponent)
    cy.wait('@marketData')
    cy.contains('button', '24H').should('have.class', 'bg-gray-200')
    cy.get('@marketData.all').should('have.length', 1)
  })

  timeframeLabels.forEach((label, index) => {
    it(`should fetch market data with correct timeframe when ${label} is clicked`, () => {
      cy.mount(LineChartComponent)
      cy.wait('@marketData')

      cy.contains('button', label).click()

      if (index > 0) {
        cy.wait('@marketData').its('request.url').should('contain', `timeframe=${index + 1}`)
      } else {
        cy.get('@marketData.all').should('have.length.at.least', 1)
      }

      cy.contains('button', label).should('have.class', 'bg-gray-200')
    })
  })

  it('should display correct chart title after selecting timeframe', () => {
    cy.mount(LineChartComponent)
    cy.wait('@marketData')

    cy.contains('button', '1Y').click()
    cy.wait('@marketData')
    cy.contains('h1', '1Y Market Value').should('be.visible')

    cy.contains('button', 'All').click()
    cy.wait('@marketData')
    cy.contains('h1', 'All Market Value').should('be.visible')
  })
})
