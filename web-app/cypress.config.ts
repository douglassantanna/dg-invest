import { defineConfig } from 'cypress'
import * as dotenv from 'dotenv';

dotenv.config();

export default defineConfig({
  projectId: 'jwbwyd',
  e2e: {
    'baseUrl': 'http://localhost:4200',
    env: {
      FAKE_VALID_JWT: process.env['FAKE_VALID_JWT'],
      FAKE_EXPIRED_JWT: process.env['FAKE_EXPIRED_JWT'],
      FAKE_VALID_USER_JWT: process.env['FAKE_VALID_USER_JWT'],
    }
  },


  component: {
    devServer: {
      framework: 'angular',
      bundler: 'webpack',
    },
    specPattern: '**/*.cy.ts'
  }

})
