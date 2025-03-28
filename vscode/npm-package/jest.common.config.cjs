module.exports = {
  preset: 'ts-jest',
  testEnvironment: 'node',
  testMatch: [
    '**/?(*.)+(spec).ts?(x)'
  ],
  transform: {
    '^.+\\.ts?$': ['ts-jest', {
      tsconfig: 'tsconfig.json',
      diagnostics: false
    }],
  },
  transformIgnorePatterns: [
    '/node_modules/',
    '/dist/',
  ],
  moduleNameMapper: {
    '^(\\.{1,2}/.*)\\.js$': '$1',
    '^dist/(.*)$': '<rootDir>/src/$1'
  },
  globalSetup: "<rootDir>/tests/integration/setup.ts",
  globalTeardown: "<rootDir>/tests/integration/teardown.ts",
};


