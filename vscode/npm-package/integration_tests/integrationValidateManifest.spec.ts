import { validateManifest } from '../lib/validateManifest';

describe("validateManifestIntegration", () => {
  test('should return log entries when successful', async () => {
    const manifestPath = "manifest.json";

    const actual = await validateManifest({ manifestPath: manifestPath });
    expect(actual).toBeDefined();
  });
});
