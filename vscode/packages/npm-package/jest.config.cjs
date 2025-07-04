const common = require('./jest.common.config.cjs')

/** @returns {Promise<import('jest').Config>} */
module.exports = async () => {
  // When debugging, we want to have a longer timeout
  let testTimeout = 100000;
  if (process.env.VSCODE_INSPECTOR_OPTIONS) {
    testTimeout = 999999;
  }

  return {
    ...common,
    globalSetup: "<rootDir>/tests/integration/setup.ts",
    globalTeardown: "<rootDir>/tests/integration/teardown.ts",
    testTimeout: testTimeout,
  };
};