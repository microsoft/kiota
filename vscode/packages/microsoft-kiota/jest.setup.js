const path = require('path');
const { runTests } = require('vscode-test');

module.exports = async () => {
  const extensionDevelopmentPath = path.resolve(__dirname);
  const extensionTestsPath = path.resolve(__dirname, 'out', 'test');

  await runTests({
    extensionDevelopmentPath,
    extensionTestsPath,
    launchArgs: ['--disable-extensions'],
  });
};