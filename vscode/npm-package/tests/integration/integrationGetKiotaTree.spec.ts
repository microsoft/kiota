import { getKiotaTree } from "../../lib/getKiotaTree";
import { LogLevel } from "../../types";

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
    const actual = await getKiotaTree({ includeFilters: [], descriptionPath: descriptionUrl, excludeFilters: [], clearCache: false, includeKiotaValidationRules: true });

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
    const descriptionUrl = '../../tests/Kiota.Builder.IntegrationTests/ModelWithAdaptiveCardExtension.yaml';
    const actual = await getKiotaTree({ includeFilters: [], descriptionPath: descriptionUrl, excludeFilters: [], clearCache: false });
    expect(actual?.rootNode?.children[0].children[0].adaptiveCard?.card).toBeTruthy();
    expect(actual).toBeDefined();
  });
  
  test('testGetKiotaTree_withNoServer_withKiotaValidationRules', async () => {
    const descriptionUrl = '../../tests/Kiota.Builder.IntegrationTests/ModelWithNoServer.yml';
    const actual = await getKiotaTree({ includeFilters: [], descriptionPath: descriptionUrl, excludeFilters: [], clearCache: false, includeKiotaValidationRules: true });

    expect(actual).toBeDefined();
    expect(actual?.rootNode?.children[0].children[0].isOperation).toBeTruthy();
    expect(actual?.rootNode?.children[0].children[0].operationId).toEqual('listRepairs');

    // Maximum log level is warning, so we should not have any logs with level greater than warning
    const actualLogsGreaterThanWarning = actual?.logs.filter((log) => log.level > LogLevel.warning);
    expect(actualLogsGreaterThanWarning?.length).toEqual(0);

    // We should find a log regarding the missing server entry
    const actualLogNoServerEntry = actual?.logs?.find((log) => log.message.startsWith('OpenAPI warning: #/ - A servers entry (v3) or host + basePath + schemes properties (v2) was not present in the OpenAPI description.'));
    expect(actualLogNoServerEntry).toBeDefined();
    expect(actualLogNoServerEntry?.level).toEqual(LogLevel.warning);
  });

  test('testGetKiotaTree_withNoServer_withoutKiotaValidationRules', async () => {
    const descriptionUrl = '../../tests/Kiota.Builder.IntegrationTests/ModelWithNoServer.yml';
    const actual = await getKiotaTree({ includeFilters: [], descriptionPath: descriptionUrl, excludeFilters: [], clearCache: false });

    expect(actual).toBeDefined();
    expect(actual?.rootNode?.children[0].children[0].isOperation).toBeTruthy();
    expect(actual?.rootNode?.children[0].children[0].operationId).toEqual('listRepairs');

    // Maximum log level is warning, so we should not have any logs with level greater than warning
    const actualLogsGreaterThanWarning = actual?.logs.filter((log) => log.level > LogLevel.warning);
    expect(actualLogsGreaterThanWarning?.length).toEqual(0);

    // We should find a log regarding the missing server entry
    const actualLogNoServerEntry = actual?.logs?.find((log) => log.message.startsWith('OpenAPI warning: #/ - A servers entry (v3) or host + basePath + schemes properties (v2) was not present in the OpenAPI description.'));
    expect(actualLogNoServerEntry).toBeUndefined();
  });
});