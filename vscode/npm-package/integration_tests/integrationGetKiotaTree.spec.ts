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

  test('testGetKiotaTree_withoutBasicInfoInOneOperation', async () => {
    const descriptionUrl = '../../tests/Kiota.Builder.IntegrationTests/ModelWithoutBasicInfoInOneOperation.yaml';
    const actual = await getKiotaTree({ includeFilters: [], descriptionPath: descriptionUrl, excludeFilters: [], clearCache: false });

    expect(actual).toBeDefined();
    expect(actual?.rootNode?.children[0].children[0].isOperation).toBeTruthy();
    expect(actual?.rootNode?.children[0].children[0].operationId).toEqual('listRepairs');
    expect(actual?.rootNode?.children[0].children[0].summary).toEqual('List all repairs with oauth');
    expect(actual?.rootNode?.children[0].children[0].description).toEqual('Returns a list of repairs with their details and images');
    expect(actual?.rootNode?.children[0].children[1].isOperation).toBeTruthy();
    expect(actual?.rootNode?.children[0].children[1].operationId).toEqual('repairs_post');
    expect(actual?.rootNode?.children[0].children[1].summary).toBeUndefined();
    expect(actual?.rootNode?.children[0].children[1].description).toBeUndefined();
  });

  test('testGetKiotaTree_from_valid_external', async () => {
    const descriptionUrl = 'https://raw.githubusercontent.com/microsoftgraph/msgraph-sdk-powershell/dev/openApiDocs/v1.0/Mail.yml';
    const actual = await getKiotaTree({ includeFilters: [], descriptionPath: descriptionUrl, excludeFilters: [], clearCache: false });
    expect(actual).toBeDefined();
  });

  test('testGetKiotaTree_withAdaptiveCard', async () => {
    const descriptionUrl = '../../tests/Kiota.Builder.IntegrationTests/ModelWithSecurity.yaml';
    const actual = await getKiotaTree({ includeFilters: [], descriptionPath: descriptionUrl, excludeFilters: [], clearCache: false });
    expect(actual?.rootNode?.children[0].children[0].adaptiveCard?.card).toBeTruthy();
    expect(actual).toBeDefined();
  });

});