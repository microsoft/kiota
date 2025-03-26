import { getKiotaTree } from '../lib/getKiotaTree';

describe("getKiotaTree", () => {
  test('testGetKiotaTree_from_valid_File', async () => {
    const descriptionUrl = '../../tests/Kiota.Builder.IntegrationTests/DiscriminatorSample.yaml';
    const actual = await getKiotaTree({ includeFilters: [], descriptionPath: descriptionUrl, excludeFilters: [], clearCache: false });
    expect(actual).toBeDefined();
  });

  test('testGetKiotaTree_withSecurity', async () => {
    const descriptionUrl = '../../tests/Kiota.Builder.IntegrationTests/ModelWithSecurity.yaml';
    const actual = await getKiotaTree({ includeFilters: [], descriptionPath: descriptionUrl, excludeFilters: [], clearCache: false });
    expect(actual).toBeDefined();
  });

  test('testGetKiotaTree_withReferencIdExtension', async () => {
    const descriptionUrl = '../../tests/Kiota.Builder.IntegrationTests/ModelWithRefIdExtension.yaml';
    const actual = await getKiotaTree({ includeFilters: [], descriptionPath: descriptionUrl, excludeFilters: [], clearCache: false });
    expect(actual).toBeDefined();
  });

  test('testGetKiotaTree_from_valid_external', async () => {
    const descriptionUrl = 'https://raw.githubusercontent.com/microsoftgraph/msgraph-sdk-powershell/dev/openApiDocs/v1.0/Mail.yml';
    const actual = await getKiotaTree({ includeFilters: [], descriptionPath: descriptionUrl, excludeFilters: [], clearCache: false });
    expect(actual).toBeDefined();
  });

});