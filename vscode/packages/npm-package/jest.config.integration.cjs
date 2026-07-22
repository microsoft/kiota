const common = require('./jest.common.config.cjs')

module.exports = {
  ...common,
  globalSetup: "<rootDir>/tests/integration/setup.cjs",
  globalTeardown: "<rootDir>/tests/integration/teardown.ts",
  testMatch: [
    '**/tests/integration/?(*.)+(spec).ts?(x)'
  ]
};
