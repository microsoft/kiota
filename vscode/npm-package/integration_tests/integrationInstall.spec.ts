import { ensureKiotaIsPresentInPath } from '../install';
import * as fs from 'fs';

describe("getKiotaVersionIntegration", () => {

  // Bigger timeout for the test to download the kiota binary
  test('should install to specific location', async () => {
    const installLocation = ".kiotabin/test_install";
    await ensureKiotaIsPresentInPath(installLocation);

    // check that the folder exists
    expect(fs.existsSync(installLocation)).toBe(true);

    // remove folder content after test
    fs.rmSync(installLocation, { recursive: true });
  }, 30000);

});
