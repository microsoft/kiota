import * as fs from 'fs';

import { setKiotaConfig } from '../../config';
import { ensureKiotaIsPresentInPath, getKiotaPath } from '../../install';

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
    const binaryVersion = '1.22.2';
    setKiotaConfig({
      binaryVersion
    })
    const kiotaPath = getKiotaPath();
    await ensureKiotaIsPresentInPath(kiotaPath);
    // check that the folder exists
    expect(fs.existsSync(kiotaPath)).toBe(true);
    expect(kiotaPath.includes(binaryVersion)).toBe(true);

    // remove folder content after test
    fs.rmSync(kiotaPath, { recursive: true });
  }, 30000);

});
