const common = require('./jest.common.config.cjs')

module.exports = {
  ...common,
  globalSetup: "<rootDir>/integration_tests/setup.ts",
  globalTeardown: "<rootDir>/integration_tests/teardown.ts",
  testMatch: [
    '**/integration_tests/?(*.)+(spec).ts?(x)'
  ]
};
