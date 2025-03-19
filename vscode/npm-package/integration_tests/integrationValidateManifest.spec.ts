import { validateManifest } from '../lib/validateManifest';

describe("validateManifestIntegration", () => {
  test('should return log entries when successful', async () => {
    const manifestPath = '../../tests/Kiota.Builder.IntegrationTests/DiscriminatorSample.yaml';

    const actual = await validateManifest({ manifestPath: manifestPath });
    expect(actual).toBeDefined();
  });

  test('testGetKiotaTree_from_invalid_File', async () => {
    const manifestPath = '../../tests/Kiota.Builder.IntegrationTests/DiscriminatorSampleWithErrors.yaml';
    const actual = await validateManifest({ manifestPath: manifestPath });
    expect(actual).toBeDefined();
  });

  test('external location with no errors', async () => {
    const manifestPath = 'https://raw.githubusercontent.com/microsoftgraph/msgraph-sdk-powershell/dev/openApiDocs/v1.0/Mail.yml';

    const actual = await validateManifest({ manifestPath: manifestPath });
    expect(actual).toBeDefined();
  });
});
