using '../main.bicep'

param environmentName = 'stage'
param location = 'westeurope'
param sqlLocation = 'francecentral'
param frontendCustomDomain = 'stage.sanfi.xyz'
param bindCustomDomain = false
// Reads the GitHub Environment secret through the workflow process environment; no value is stored here.
param sqlAdministratorPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD')
// Proportional since 2026-11-10, after reviewing GET /api/Recalculate/preview against STAGE data (README "Cost basis").
param transferCostPolicy = 'Proportional'
// Recalculation stays off. The one-off recalculation after switching to Proportional ran on 2026-11-10.
param enableRecalculation = false
