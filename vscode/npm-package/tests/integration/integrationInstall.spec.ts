import * as fs from 'fs';

import { setKiotaConfig } from '../../config';
import { ensureKiotaIsPresentInPath, getCurrentPlatform, getKiotaPath, Package } from '../../install';
import testRuntimeJson from '../test_runtime.json';

function getTestRuntimeDependenciesPackages(): Package[] {
  if (testRuntimeJson.runtimeDependencies) {
    return JSON.parse(JSON.stringify(<Package[]>testRuntimeJson.runtimeDependencies));
  }
  throw new Error("No test runtime dependencies found");
}

// Bigger timeout for the test to download the kiota binary
describe("integration install", () => {

  test('should install to specific location', async () => {
    const binaryVersion = '1.25.1';
    setKiotaConfig({
      binaryVersion
    })
    const unique_id = Math.random().toString(36).substring(7);
    const installLocation = `.kiotabin/test_install/${unique_id}`;
    const testRuntimeDependencies = getTestRuntimeDependenciesPackages();
    const currentPlatform = getCurrentPlatform();
    await ensureKiotaIsPresentInPath(installLocation, testRuntimeDependencies, currentPlatform);

    // check that the folder exists
    expect(fs.existsSync(installLocation)).toBe(true);

    // remove folder content after test
    fs.rmSync(installLocation, { recursive: true });
  }, 30000);

  test('should install specific version', async () => {
    const binaryVersion = '1.25.1';
    setKiotaConfig({
      binaryVersion
    })
    const currentPlatform = getCurrentPlatform();
    const kiotaPath = getKiotaPath().split(currentPlatform)[0] + currentPlatform;
    const testRuntimeDependencies = getTestRuntimeDependenciesPackages();
    await ensureKiotaIsPresentInPath(kiotaPath, testRuntimeDependencies, currentPlatform);
    // check that the folder exists
    expect(fs.existsSync(kiotaPath)).toBe(true);
    expect(kiotaPath.includes(binaryVersion)).toBe(true);

    // remove folder content after test
    fs.rmSync(kiotaPath, { recursive: true });
  }, 30000);

  test('should raise an error for bad hash', async () => {
    const binaryVersion = '1.25.1';
    setKiotaConfig({
      binaryVersion
    })
    const unique_id = Math.random().toString(36).substring(7);
    const installLocation = `.kiotabin/test_install/${unique_id}`;
    
    // Get the runtime dependencies from the test runtime.json file which has a bad hash for the test
    const runtimeDependencies = getTestRuntimeDependenciesPackages();
    const currentPlatform = getCurrentPlatform();

    // Set the hash to a bad value for the test
    for (const runtimeDependency of runtimeDependencies) {
      if (runtimeDependency.platformId === currentPlatform) {
        runtimeDependency.sha256 = "bad_hash_value";
      }
    }

    try {
      await ensureKiotaIsPresentInPath(installLocation, runtimeDependencies, currentPlatform);
    } catch (error) {
      expect(error.message).toEqual("Kiota download failed. Check the logs for more information.");
      expect(error.cause.message).toEqual("Hash validation of the downloaded file mismatch");
    }

    // check that the folder does not exist
    expect(fs.existsSync(installLocation)).toBe(false);
  }, 30000);
});

describe("sideloading install", () => {
  beforeAll(async () => {
    const binaryVersion = '1.25.1';
    setKiotaConfig({
      binaryVersion
    })
    const installLocation = getKiotaPath();
    const runtimeDependencies = getTestRuntimeDependenciesPackages();
    const currentPlatform = getCurrentPlatform();
    await ensureKiotaIsPresentInPath(installLocation, runtimeDependencies, currentPlatform);
    const zipFilePath = `${installLocation}.zip`;

    process.env.SIDELOADING_KIOTA_BINARY_ZIP_PATH = zipFilePath;
  });
  afterAll(() => {
    process.env.SIDELOADING_KIOTA_BINARY_ZIP_PATH = undefined;
  });

  test('ensureKiotaIsPresentInPath_sideloading', async () => {
    const unique_id = Math.random().toString(36).substring(7);
    const installLocation = `.kiotabin/test_install/${unique_id}`;
    const runtimeDependencies = getTestRuntimeDependenciesPackages();
    const currentPlatform = getCurrentPlatform();
    await ensureKiotaIsPresentInPath(installLocation, runtimeDependencies, currentPlatform);

    // check that the folder exists
    expect(fs.existsSync(installLocation)).toBe(true);

    // remove folder content after test
    fs.rmSync(installLocation, { recursive: true });
  }, 30000);

});
