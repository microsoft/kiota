#!/usr/bin/env node
const { execFileSync } = require('child_process');
const { getKiotaPath } = require('../dist/cjs/install.js');

const binaryPath = getKiotaPath();

try {
  execFileSync(binaryPath, process.argv.slice(2), { stdio: 'inherit' });
} catch (err) {
  if (err.code === 'ENOENT') {
    console.error(`Error: Unable to find the specified executable ${binaryPath}.`);
  } else {
    console.error(`Error: ${err.message}`);
  }
  process.exit(1); // Exit with a failure code
}
