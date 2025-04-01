import * as fs from 'fs';

import { setKiotaConfig } from '../../config';
import { ensureKiotaIsPresentInPath } from '../../install';

// Bigger timeout for the test to download the kiota binary
describe("integration install", () => {

  test('should install to specific location', async () => {
    const unique_id = Math.random().toString(36).substring(7);
    const installLocation = `.kiotabin/test_install/${unique_id}`;
    await ensureKiotaIsPresentInPath(installLocation);

    // check that the folder exists
    expect(fs.existsSync(installLocation)).toBe(true);

    // remove folder content after test
    fs.rmSync(installLocation, { recursive: true });
  }, 30000);

  test('should install specific version', async () => {
    const unique_id = Math.random().toString(36).substring(7);
    const installLocation = `.kiotabin/test_install/${unique_id}`;
    const binaryVersion = '1.22.2';
    setKiotaConfig({
      binaryVersion
    })
    await ensureKiotaIsPresentInPath(installLocation);

    // check that the folder exists
    expect(fs.existsSync(installLocation)).toBe(true);
    expect(installLocation.includes(binaryVersion)).toBe(true);

    // remove folder content after test
    fs.rmSync(installLocation, { recursive: true });
  }, 30000);

});
