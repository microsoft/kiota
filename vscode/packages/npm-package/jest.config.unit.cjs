const common = require('./jest.common.config.cjs')

/** @returns {Promise<import('jest').Config>} */
module.exports = {
  ...common,
  globalSetup: undefined,
  globalTeardown: undefined,
  testMatch: [
    '**/tests/unit/?(*.)+(spec).ts?(x)'
  ]
};