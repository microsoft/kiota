const common = require('./jest.common.config.cjs')

/** @returns {Promise<import('jest').Config>} */
module.exports = {
  ...common,
  testMatch: [
    '**/tests/unit/?(*.)+(spec).ts?(x)'
  ]
};