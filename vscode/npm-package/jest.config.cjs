const common = require('./jest.common.config.cjs')

/** @returns {Promise<import('jest').Config>} */
module.exports = async () => {
  // When debugging, we want to have a longer timeout
  let testTimeout = 5000; 
  if (process.env.VSCODE_INSPECTOR_OPTIONS) {
      testTimeout = 999999;
  }

  return {
    ...common,
    globalSetup: "<rootDir>/integration_tests/setup.ts",
    globalTeardown: "<rootDir>/integration_tests/teardown.ts",
    testTimeout: testTimeout,
  };
};