const common = require('./jest.common.config.cjs')

/** @returns {Promise<import('jest').Config>} */
module.exports = {
  ...common,
  // Remove global setup/teardown for unit tests
  globalSetup: undefined,
  globalTeardown: undefined,
  testMatch: [
    '**/tests/unit/?(*.)+(spec).ts?(x)'
  ]
};