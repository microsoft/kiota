import { KiotaTreeResult, KiotaOpenApiNode, LogLevel, OAuth2SecurityScheme, HttpSecurityScheme, ApiKeySecurityScheme, OpenIdSecurityScheme, OpenApiSpecVersion } from "../../types";
import { getKiotaTree } from "../../lib/getKiotaTree";
import { existsEqualOrGreaterThanLevelLogs } from "../assertUtils";

function findOperationByPath(actual: KiotaTreeResult | undefined, path: string): KiotaOpenApiNode | undefined {
  if (!actual?.rootNode) return undefined;
  const flattenedTree = flattenKiotaTree(actual.rootNode);
  const operation = flattenedTree.find((child) => child.isOperation === true && child.path === path);
  return operation;
}

function flattenKiotaTree(tree: KiotaOpenApiNode): KiotaOpenApiNode[] {
  const result: KiotaOpenApiNode[] = [tree];
  for (const child of tree.children) {
    result.push(...flattenKiotaTree(child));
  }
  return result;
}

describe("getKiotaTree", () => {
  test('testGetKiotaTree_from_valid_File', async () => {
    const descriptionUrl = '../../tests/Kiota.Builder.IntegrationTests/DiscriminatorSample.yaml';

    const actual = await getKiotaTree({ includeFilters: [], descriptionPath: descriptionUrl, excludeFilters: [], clearCache: false });

    expect(actual).toBeDefined();
    expect(existsEqualOrGreaterThanLevelLogs(actual?.logs, LogLevel.warning)).toBeFalsy();
    expect(existsEqualOrGreaterThanLevelLogs(actual?.logs, LogLevel.information)).toBeTruthy();
    const actualOperationNode = findOperationByPath(actual, '\\discriminateme#POST');
    expect(actualOperationNode).toBeDefined();
    expect(actualOperationNode?.operationId).toBeUndefined();
  });

  test('testGetKiotaTree_withSecurity', async () => {
    const descriptionUrl = '../../tests/Kiota.Builder.IntegrationTests/ModelWithSecurity.yaml';

    const actual = await getKiotaTree({ includeFilters: [], descriptionPath: descriptionUrl, excludeFilters: [], clearCache: false });

    expect(actual).toBeDefined();
    expect(existsEqualOrGreaterThanLevelLogs(actual?.logs, LogLevel.warning)).toBeFalsy();
    expect(existsEqualOrGreaterThanLevelLogs(actual?.logs, LogLevel.information)).toBeTruthy();

    // Check if the security requirements are defined correctly for get operation
    // It should have one security requirement: oAuth2AuthCode
    const actualListOperationNode = findOperationByPath(actual, '\\repairs#GET');
    expect(actualListOperationNode).toBeDefined();
    expect(actualListOperationNode?.operationId).toEqual('listRepairs');
    expect(actualListOperationNode?.security).toBeDefined();
    expect(actualListOperationNode?.security?.length).toEqual(1);
    const firstSecurityRequirementInGet = actualListOperationNode?.security?.[0];
    const oAuth2AuthCodeSecurityRequirementInGet = firstSecurityRequirementInGet?.['oAuth2AuthCode'];
    expect(oAuth2AuthCodeSecurityRequirementInGet).toBeDefined();
    expect(oAuth2AuthCodeSecurityRequirementInGet?.length).toEqual(1);
    expect(oAuth2AuthCodeSecurityRequirementInGet?.[0]).toEqual('api://sample/repairs_read');

    // Check if the security requirements are defined correctly for post operation
    // It should have two security requirements: oAuth2AuthCode and httpAuth
    const actualPostOperationNode = findOperationByPath(actual, '\\repairs#POST');
    expect(actualPostOperationNode).toBeDefined();
    expect(actualPostOperationNode?.operationId).toBeUndefined();
    expect(actualPostOperationNode?.security).toBeDefined();
    expect(actualPostOperationNode?.security?.length).toEqual(1);
    const firstSecurityRequirementInPost = actualPostOperationNode?.security?.[0];
    const oAuth2AuthCodeSecurityRequirementInPost = firstSecurityRequirementInPost?.['oAuth2AuthCode'];
    expect(oAuth2AuthCodeSecurityRequirementInPost).toBeDefined();
    expect(oAuth2AuthCodeSecurityRequirementInPost?.length).toEqual(1);
    expect(oAuth2AuthCodeSecurityRequirementInPost?.[0]).toEqual('api://sample/repairs_write');

    // Check if the security schemes are defined correctly for post operation
    const actualOAuthSecuritySchema = actual?.securitySchemes?.["oAuth2AuthCode"] as OAuth2SecurityScheme;
    expect(actualOAuthSecuritySchema).toBeDefined();
    expect(actualOAuthSecuritySchema.flows).toBeDefined();
    const actualAuthorizationCodeFlow = actualOAuthSecuritySchema.flows.authorizationCode;
    expect(actualAuthorizationCodeFlow).toBeDefined();
    expect(actualAuthorizationCodeFlow?.authorizationUrl).toEqual('https://login.microsoftonline.com/common/oauth2/v2.0/authorize');
    expect(actualAuthorizationCodeFlow?.tokenUrl).toEqual('https://login.microsoftonline.com/common/oauth2/v2.0/token');
    expect(actualAuthorizationCodeFlow?.refreshUrl).toEqual('https://login.microsoftonline.com/common/oauth2/v2.0/refresh');
    expect(actualAuthorizationCodeFlow?.scopes).toBeDefined();
    expect(actualAuthorizationCodeFlow?.scopes['api://sample/repairs_read']).toEqual('Read repair records');
    expect(actualAuthorizationCodeFlow?.scopes['api://sample/repairs_write']).toEqual('Write repair records');
    const actualImplicitFlow = actualOAuthSecuritySchema.flows.implicit;
    expect(actualImplicitFlow).toBeDefined();
    expect(actualImplicitFlow?.authorizationUrl).toEqual('https://login.microsoftonline.com/common/oauth2/v2.0/authorize');
    expect(actualImplicitFlow?.refreshUrl).toEqual('https://login.microsoftonline.com/common/oauth2/v2.0/refresh');
    expect(actualImplicitFlow?.scopes).toBeDefined();
    expect(actualImplicitFlow?.scopes['api://sample/repairs_read']).toEqual('Read repair records');
    expect(actualImplicitFlow?.scopes['api://sample/repairs_write']).toEqual('Write repair records');
    const actualClientCredentialsFlow = actualOAuthSecuritySchema.flows.clientCredentials;
    expect(actualClientCredentialsFlow).toBeDefined();
    expect(actualClientCredentialsFlow?.tokenUrl).toEqual('https://login.microsoftonline.com/common/oauth2/v2.0/token');
    expect(actualClientCredentialsFlow?.refreshUrl).toEqual('https://login.microsoftonline.com/common/oauth2/v2.0/refresh');
    expect(actualClientCredentialsFlow?.scopes).toBeDefined();
    expect(actualClientCredentialsFlow?.scopes['api://sample/repairs_read']).toEqual('Read repair records');
    expect(actualClientCredentialsFlow?.scopes['api://sample/repairs_write']).toEqual('Write repair records');
    const actualPasswordFlow = actualOAuthSecuritySchema.flows.password;
    expect(actualPasswordFlow).toBeDefined();
    expect(actualPasswordFlow?.tokenUrl).toEqual('https://login.microsoftonline.com/common/oauth2/v2.0/token');
    expect(actualPasswordFlow?.refreshUrl).toEqual('https://login.microsoftonline.com/common/oauth2/v2.0/refresh');
    expect(actualPasswordFlow?.scopes).toBeDefined();
    expect(actualPasswordFlow?.scopes['api://sample/repairs_read']).toEqual('Read repair records');
    expect(actualPasswordFlow?.scopes['api://sample/repairs_write']).toEqual('Write repair records');

    const actualHttpSecuritySchema = actual?.securitySchemes?.["httpAuth"] as HttpSecurityScheme;
    expect(actualHttpSecuritySchema).toBeDefined();
    expect(actualHttpSecuritySchema.type).toEqual('http');
    expect(actualHttpSecuritySchema.scheme).toEqual('basic');
    expect(actualHttpSecuritySchema.description).toEqual('HTTP basic authentication');

    const actualApiKeySecuritySchema = actual?.securitySchemes?.["apiKeyAuth"] as ApiKeySecurityScheme;
    expect(actualApiKeySecuritySchema).toBeDefined();
    expect(actualApiKeySecuritySchema.type).toEqual('apiKey');
    expect(actualApiKeySecuritySchema.name).toEqual('X-API-Key');
    expect(actualApiKeySecuritySchema.in).toEqual('header');
    expect(actualApiKeySecuritySchema.description).toEqual('API key authentication');

    const actualOpenIdConnectSecuritySchema = actual?.securitySchemes?.["openIdConnectAuth"] as OpenIdSecurityScheme;
    expect(actualOpenIdConnectSecuritySchema).toBeDefined();
    expect(actualOpenIdConnectSecuritySchema.type).toEqual('openIdConnect');
    expect(actualOpenIdConnectSecuritySchema.description).toEqual('OpenID Connect authentication');
    expect(actualOpenIdConnectSecuritySchema.openIdConnectUrl).toEqual('https://login.microsoftonline.com/common/.well-known/openid-configuration');

  });

  test('testGetKiotaTree_withMultipleSecurity', async () => {
    const descriptionUrl = '../../tests/Kiota.Builder.IntegrationTests/ModelWithMultipleSecurity.yaml';

    const actual = await getKiotaTree({ includeFilters: [], descriptionPath: descriptionUrl, excludeFilters: [], clearCache: false });

    expect(actual).toBeDefined();
    expect(existsEqualOrGreaterThanLevelLogs(actual?.logs, LogLevel.warning)).toBeFalsy();
    expect(existsEqualOrGreaterThanLevelLogs(actual?.logs, LogLevel.information)).toBeTruthy();

    // Check if the security requirements are defined correctly for get operation
    // It should have one security requirement: oAuth2AuthCode
    const actualListOperationNode = findOperationByPath(actual, '\\repairs#GET');
    expect(actualListOperationNode).toBeDefined();
    expect(actualListOperationNode?.operationId).toEqual('listRepairs');
    expect(actualListOperationNode?.security).toBeDefined();
    expect(actualListOperationNode?.security?.length).toEqual(1);
    const firstSecurityRequirementInGet = actualListOperationNode?.security?.[0];
    const oAuth2AuthCodeSecurityRequirementInGet = firstSecurityRequirementInGet?.['oAuth2AuthCode'];
    expect(oAuth2AuthCodeSecurityRequirementInGet).toBeDefined();
    expect(oAuth2AuthCodeSecurityRequirementInGet?.length).toEqual(1);
    expect(oAuth2AuthCodeSecurityRequirementInGet?.[0]).toEqual('api://sample/repairs_read');

    // Check if the security requirements are defined correctly for post operation
    // It should have two security requirements: oAuth2AuthCode and httpAuth
    const actualPostOperationNode = findOperationByPath(actual, '\\repairs#POST');
    expect(actualPostOperationNode).toBeDefined();
    expect(actualPostOperationNode?.operationId).toBeUndefined();
    expect(actualPostOperationNode?.security).toBeDefined();
    expect(actualPostOperationNode?.security?.length).toEqual(2);
    const firstSecurityRequirementInPost = actualPostOperationNode?.security?.[0];
    const oAuth2AuthCodeSecurityRequirementInPost = firstSecurityRequirementInPost?.['oAuth2AuthCode'];
    expect(oAuth2AuthCodeSecurityRequirementInPost).toBeDefined();
    expect(oAuth2AuthCodeSecurityRequirementInPost?.length).toEqual(1);
    expect(oAuth2AuthCodeSecurityRequirementInPost?.[0]).toEqual('api://sample/repairs_write');
    const secondSecurityRequirementInPost = actualPostOperationNode?.security?.[1];
    expect(secondSecurityRequirementInPost).toBeDefined();
    const httpAuthSecurityRequirementInPost = secondSecurityRequirementInPost?.['httpAuth'];
    expect(httpAuthSecurityRequirementInPost).toBeDefined();
    expect(httpAuthSecurityRequirementInPost?.length).toEqual(0);

    // Check if the security requirements are defined correctly for post operation
    // It should have two security requirements: oAuth2AuthCode and httpAuth
    const actualPutOperationNode = findOperationByPath(actual, '\\repairs#PUT');
    expect(actualPutOperationNode).toBeDefined();
    expect(actualPutOperationNode?.operationId).toBeUndefined();
    expect(actualPutOperationNode?.security).toBeDefined();
    expect(actualPutOperationNode?.security?.length).toEqual(1);
    const firstSecurityRequirementInPut = actualPutOperationNode?.security?.[0];
    const oAuth2AuthCodeSecurityRequirementInPut = firstSecurityRequirementInPut?.['oAuth2AuthCode'];
    expect(oAuth2AuthCodeSecurityRequirementInPut).toBeDefined();
    expect(oAuth2AuthCodeSecurityRequirementInPut?.length).toEqual(1);
    expect(oAuth2AuthCodeSecurityRequirementInPut?.[0]).toEqual('api://sample/repairs_write');
    const httpAuthSecurityRequirementInPut = firstSecurityRequirementInPut?.['httpAuth'];
    expect(httpAuthSecurityRequirementInPut).toBeDefined();
    expect(httpAuthSecurityRequirementInPut?.length).toEqual(0);

    // Check if the security schemes are defined correctly for post operation
    const actualOAuthSecuritySchema = actual?.securitySchemes?.["oAuth2AuthCode"] as OAuth2SecurityScheme;
    expect(actualOAuthSecuritySchema).toBeDefined();
    expect(actualOAuthSecuritySchema.flows).toBeDefined();
    const actualAuthorizationCodeFlow = actualOAuthSecuritySchema.flows.authorizationCode;
    expect(actualAuthorizationCodeFlow).toBeDefined();
    expect(actualAuthorizationCodeFlow?.authorizationUrl).toEqual('https://login.microsoftonline.com/common/oauth2/v2.0/authorize');
    expect(actualAuthorizationCodeFlow?.tokenUrl).toEqual('https://login.microsoftonline.com/common/oauth2/v2.0/token');
    expect(actualAuthorizationCodeFlow?.refreshUrl).toEqual('https://login.microsoftonline.com/common/oauth2/v2.0/refresh');
    expect(actualAuthorizationCodeFlow?.scopes).toBeDefined();
    expect(actualAuthorizationCodeFlow?.scopes['api://sample/repairs_read']).toEqual('Read repair records');
    expect(actualAuthorizationCodeFlow?.scopes['api://sample/repairs_write']).toEqual('Write repair records');
    const actualImplicitFlow = actualOAuthSecuritySchema.flows.implicit;
    expect(actualImplicitFlow).toBeDefined();
    expect(actualImplicitFlow?.authorizationUrl).toEqual('https://login.microsoftonline.com/common/oauth2/v2.0/authorize');
    expect(actualImplicitFlow?.refreshUrl).toEqual('https://login.microsoftonline.com/common/oauth2/v2.0/refresh');
    expect(actualImplicitFlow?.scopes).toBeDefined();
    expect(actualImplicitFlow?.scopes['api://sample/repairs_read']).toEqual('Read repair records');
    expect(actualImplicitFlow?.scopes['api://sample/repairs_write']).toEqual('Write repair records');
    const actualClientCredentialsFlow = actualOAuthSecuritySchema.flows.clientCredentials;
    expect(actualClientCredentialsFlow).toBeDefined();
    expect(actualClientCredentialsFlow?.tokenUrl).toEqual('https://login.microsoftonline.com/common/oauth2/v2.0/token');
    expect(actualClientCredentialsFlow?.refreshUrl).toEqual('https://login.microsoftonline.com/common/oauth2/v2.0/refresh');
    expect(actualClientCredentialsFlow?.scopes).toBeDefined();
    expect(actualClientCredentialsFlow?.scopes['api://sample/repairs_read']).toEqual('Read repair records');
    expect(actualClientCredentialsFlow?.scopes['api://sample/repairs_write']).toEqual('Write repair records');
    const actualPasswordFlow = actualOAuthSecuritySchema.flows.password;
    expect(actualPasswordFlow).toBeDefined();
    expect(actualPasswordFlow?.tokenUrl).toEqual('https://login.microsoftonline.com/common/oauth2/v2.0/token');
    expect(actualPasswordFlow?.refreshUrl).toEqual('https://login.microsoftonline.com/common/oauth2/v2.0/refresh');
    expect(actualPasswordFlow?.scopes).toBeDefined();
    expect(actualPasswordFlow?.scopes['api://sample/repairs_read']).toEqual('Read repair records');
    expect(actualPasswordFlow?.scopes['api://sample/repairs_write']).toEqual('Write repair records');

    const actualHttpSecuritySchema = actual?.securitySchemes?.["httpAuth"] as HttpSecurityScheme;
    expect(actualHttpSecuritySchema).toBeDefined();
    expect(actualHttpSecuritySchema.type).toEqual('http');
    expect(actualHttpSecuritySchema.scheme).toEqual('basic');
    expect(actualHttpSecuritySchema.description).toEqual('HTTP basic authentication');

    const actualApiKeySecuritySchema = actual?.securitySchemes?.["apiKeyAuth"] as ApiKeySecurityScheme;
    expect(actualApiKeySecuritySchema).toBeDefined();
    expect(actualApiKeySecuritySchema.type).toEqual('apiKey');
    expect(actualApiKeySecuritySchema.name).toEqual('X-API-Key');
    expect(actualApiKeySecuritySchema.in).toEqual('header');
    expect(actualApiKeySecuritySchema.description).toEqual('API key authentication');

    const actualOpenIdConnectSecuritySchema = actual?.securitySchemes?.["openIdConnectAuth"] as OpenIdSecurityScheme;
    expect(actualOpenIdConnectSecuritySchema).toBeDefined();
    expect(actualOpenIdConnectSecuritySchema.type).toEqual('openIdConnect');
    expect(actualOpenIdConnectSecuritySchema.description).toEqual('OpenID Connect authentication');
    expect(actualOpenIdConnectSecuritySchema.openIdConnectUrl).toEqual('https://login.microsoftonline.com/common/.well-known/openid-configuration');
  });

  test('testGetKiotaTree_withMultipleSecurityAndVariables', async () => {
    const descriptionUrl = '../../tests/Kiota.Builder.IntegrationTests/ModelWithMultipleSecurityAndVariables.yaml';

    const actual = await getKiotaTree({ includeFilters: [], descriptionPath: descriptionUrl, excludeFilters: [], clearCache: false });

    expect(actual).toBeDefined();
    expect(existsEqualOrGreaterThanLevelLogs(actual?.logs, LogLevel.warning)).toBeFalsy();
    expect(existsEqualOrGreaterThanLevelLogs(actual?.logs, LogLevel.information)).toBeTruthy();

    // Check if the security requirements are defined correctly for get operation
    // It should have one security requirement: oAuth2AuthCode
    const actualListOperationNode = findOperationByPath(actual, '\\repairs#GET');
    expect(actualListOperationNode).toBeDefined();
    expect(actualListOperationNode?.operationId).toEqual('listRepairs');
    expect(actualListOperationNode?.security).toBeDefined();
    expect(actualListOperationNode?.security?.length).toEqual(1);
    const firstSecurityRequirementInGet = actualListOperationNode?.security?.[0];
    const oAuth2AuthCodeSecurityRequirementInGet = firstSecurityRequirementInGet?.['oAuth2AuthCode'];
    expect(oAuth2AuthCodeSecurityRequirementInGet).toBeDefined();
    expect(oAuth2AuthCodeSecurityRequirementInGet?.length).toEqual(1);
    expect(oAuth2AuthCodeSecurityRequirementInGet?.[0]).toEqual('api://sample/repairs_read');

    // Check if the security requirements are defined correctly for post operation
    // It should have two security requirements: oAuth2AuthCode and httpAuth
    const actualPostOperationNode = findOperationByPath(actual, '\\repairs#POST');
    expect(actualPostOperationNode).toBeDefined();
    expect(actualPostOperationNode?.operationId).toBeUndefined();
    expect(actualPostOperationNode?.security).toBeDefined();
    expect(actualPostOperationNode?.security?.length).toEqual(2);
    const firstSecurityRequirementInPost = actualPostOperationNode?.security?.[0];
    const oAuth2AuthCodeSecurityRequirementInPost = firstSecurityRequirementInPost?.['oAuth2AuthCode'];
    expect(oAuth2AuthCodeSecurityRequirementInPost).toBeDefined();
    expect(oAuth2AuthCodeSecurityRequirementInPost?.length).toEqual(1);
    expect(oAuth2AuthCodeSecurityRequirementInPost?.[0]).toEqual('api://sample/repairs_write');
    const secondSecurityRequirementInPost = actualPostOperationNode?.security?.[1];
    expect(secondSecurityRequirementInPost).toBeDefined();
    const httpAuthSecurityRequirementInPost = secondSecurityRequirementInPost?.['httpAuth'];
    expect(httpAuthSecurityRequirementInPost).toBeDefined();
    expect(httpAuthSecurityRequirementInPost?.length).toEqual(0);

    // Check if the security requirements are defined correctly for post operation
    // It should have two security requirements: oAuth2AuthCode and httpAuth
    const actualPutOperationNode = findOperationByPath(actual, '\\repairs#PUT');
    expect(actualPutOperationNode).toBeDefined();
    expect(actualPutOperationNode?.operationId).toBeUndefined();
    expect(actualPutOperationNode?.security).toBeDefined();
    expect(actualPutOperationNode?.security?.length).toEqual(1);
    const firstSecurityRequirementInPut = actualPutOperationNode?.security?.[0];
    const oAuth2AuthCodeSecurityRequirementInPut = firstSecurityRequirementInPut?.['oAuth2AuthCode'];
    expect(oAuth2AuthCodeSecurityRequirementInPut).toBeDefined();
    expect(oAuth2AuthCodeSecurityRequirementInPut?.length).toEqual(1);
    expect(oAuth2AuthCodeSecurityRequirementInPut?.[0]).toEqual('api://sample/repairs_write');
    const httpAuthSecurityRequirementInPut = firstSecurityRequirementInPut?.['httpAuth'];
    expect(httpAuthSecurityRequirementInPut).toBeDefined();
    expect(httpAuthSecurityRequirementInPut?.length).toEqual(0);

    // Check if the security schemes are defined correctly for post operation
    const actualOAuthSecuritySchema = actual?.securitySchemes?.["oAuth2AuthCode"] as OAuth2SecurityScheme;
    expect(actualOAuthSecuritySchema).toBeDefined();
    expect(actualOAuthSecuritySchema.flows).toBeDefined();
    const actualAuthorizationCodeFlow = actualOAuthSecuritySchema.flows.authorizationCode;
    expect(actualAuthorizationCodeFlow).toBeDefined();
    expect(actualAuthorizationCodeFlow?.authorizationUrl).toEqual('https://login.microsoftonline.com/${{AAD_APP_TENANT_ID}}/oauth2/v2.0/authorize');
    expect(actualAuthorizationCodeFlow?.tokenUrl).toEqual('https://login.microsoftonline.com/${{AAD_APP_TENANT_ID}}/oauth2/v2.0/token');
    expect(actualAuthorizationCodeFlow?.refreshUrl).toEqual('https://login.microsoftonline.com/${{AAD_APP_TENANT_ID}}/oauth2/v2.0/refresh');
    expect(actualAuthorizationCodeFlow?.scopes).toBeDefined();
    expect(actualAuthorizationCodeFlow?.scopes['api://sample/repairs_read']).toEqual('Read repair records');
    expect(actualAuthorizationCodeFlow?.scopes['api://sample/repairs_write']).toEqual('Write repair records');
    const actualImplicitFlow = actualOAuthSecuritySchema.flows.implicit;
    expect(actualImplicitFlow).toBeDefined();
    expect(actualImplicitFlow?.authorizationUrl).toEqual('https://login.microsoftonline.com/${{AAD_APP_TENANT_ID}}/oauth2/v2.0/authorize');
    expect(actualImplicitFlow?.refreshUrl).toEqual('https://login.microsoftonline.com/${{AAD_APP_TENANT_ID}}/oauth2/v2.0/refresh');
    expect(actualImplicitFlow?.scopes).toBeDefined();
    expect(actualImplicitFlow?.scopes['api://sample/repairs_read']).toEqual('Read repair records');
    expect(actualImplicitFlow?.scopes['api://sample/repairs_write']).toEqual('Write repair records');
    const actualClientCredentialsFlow = actualOAuthSecuritySchema.flows.clientCredentials;
    expect(actualClientCredentialsFlow).toBeDefined();
    expect(actualClientCredentialsFlow?.tokenUrl).toEqual('https://login.microsoftonline.com/${{AAD_APP_TENANT_ID}}/oauth2/v2.0/token');
    expect(actualClientCredentialsFlow?.refreshUrl).toEqual('https://login.microsoftonline.com/${{AAD_APP_TENANT_ID}}/oauth2/v2.0/refresh');
    expect(actualClientCredentialsFlow?.scopes).toBeDefined();
    expect(actualClientCredentialsFlow?.scopes['api://sample/repairs_read']).toEqual('Read repair records');
    expect(actualClientCredentialsFlow?.scopes['api://sample/repairs_write']).toEqual('Write repair records');
    const actualPasswordFlow = actualOAuthSecuritySchema.flows.password;
    expect(actualPasswordFlow).toBeDefined();
    expect(actualPasswordFlow?.tokenUrl).toEqual('https://login.microsoftonline.com/${{AAD_APP_TENANT_ID}}/oauth2/v2.0/token');
    expect(actualPasswordFlow?.refreshUrl).toEqual('https://login.microsoftonline.com/${{AAD_APP_TENANT_ID}}/oauth2/v2.0/refresh');
    expect(actualPasswordFlow?.scopes).toBeDefined();
    expect(actualPasswordFlow?.scopes['api://sample/repairs_read']).toEqual('Read repair records');
    expect(actualPasswordFlow?.scopes['api://sample/repairs_write']).toEqual('Write repair records');

    const actualHttpSecuritySchema = actual?.securitySchemes?.["httpAuth"] as HttpSecurityScheme;
    expect(actualHttpSecuritySchema).toBeDefined();
    expect(actualHttpSecuritySchema.type).toEqual('http');
    expect(actualHttpSecuritySchema.scheme).toEqual('basic');
    expect(actualHttpSecuritySchema.description).toEqual('HTTP basic authentication');

    const actualApiKeySecuritySchema = actual?.securitySchemes?.["apiKeyAuth"] as ApiKeySecurityScheme;
    expect(actualApiKeySecuritySchema).toBeDefined();
    expect(actualApiKeySecuritySchema.type).toEqual('apiKey');
    expect(actualApiKeySecuritySchema.name).toEqual('X-API-Key');
    expect(actualApiKeySecuritySchema.in).toEqual('header');
    expect(actualApiKeySecuritySchema.description).toEqual('API key authentication');

    const actualOpenIdConnectSecuritySchema = actual?.securitySchemes?.["openIdConnectAuth"] as OpenIdSecurityScheme;
    expect(actualOpenIdConnectSecuritySchema).toBeDefined();
    expect(actualOpenIdConnectSecuritySchema.type).toEqual('openIdConnect');
    expect(actualOpenIdConnectSecuritySchema.description).toEqual('OpenID Connect authentication');
    expect(actualOpenIdConnectSecuritySchema.openIdConnectUrl).toEqual('https://login.microsoftonline.com/${{AAD_APP_TENANT_ID}}/.well-known/openid-configuration');
  });

  test('testGetKiotaTree_withReferenceIdExtension', async () => {
    const descriptionUrl = '../../tests/Kiota.Builder.IntegrationTests/ModelWithRefIdExtension.yaml';
    const actual = await getKiotaTree({ includeFilters: [], descriptionPath: descriptionUrl, excludeFilters: [], clearCache: false });
    expect(actual).toBeDefined();
    expect(existsEqualOrGreaterThanLevelLogs(actual?.logs, LogLevel.warning)).toBeFalsy();
    expect(existsEqualOrGreaterThanLevelLogs(actual?.logs, LogLevel.information)).toBeTruthy();

    const actualSecuritySchema = actual?.securitySchemes?.["oAuth2AuthCode"];
    expect(actualSecuritySchema).toBeDefined();
    expect(actualSecuritySchema?.type).toEqual('oauth2');
    expect(actualSecuritySchema?.description).toEqual('OAuth configuration for the repair service');
    expect(actualSecuritySchema?.referenceId).toEqual('otherValue123');

    const securitySchemeAsOauthScheme = actualSecuritySchema as OAuth2SecurityScheme;
    expect(securitySchemeAsOauthScheme.flows).toBeDefined();
    expect(securitySchemeAsOauthScheme.flows.authorizationCode).toBeDefined();
    expect(securitySchemeAsOauthScheme.flows.authorizationCode?.authorizationUrl).toEqual('https://login.microsoftonline.com/common/oauth2/v2.0/authorize');
    expect(securitySchemeAsOauthScheme.flows.authorizationCode?.tokenUrl).toEqual('https://login.microsoftonline.com/common/oauth2/v2.0/token');
    expect(securitySchemeAsOauthScheme.flows.authorizationCode?.scopes).toBeDefined();
    expect(securitySchemeAsOauthScheme.flows.authorizationCode?.scopes['api://sample/repairs_read']).toEqual('Read repair records');
    expect(securitySchemeAsOauthScheme.flows.authorizationCode?.scopes['api://sample/repairs_write']).toEqual('Write repair records');
  });

  test('testGetKiotaTree_withoutBasicInfoInOneOperation', async () => {
    const descriptionUrl = '../../tests/Kiota.Builder.IntegrationTests/ModelWithoutBasicInfoInOneOperation.yaml';
    const actual = await getKiotaTree({ includeFilters: [], descriptionPath: descriptionUrl, excludeFilters: [], clearCache: false, includeKiotaValidationRules: true });

    expect(actual).toBeDefined();
    expect(existsEqualOrGreaterThanLevelLogs(actual?.logs, LogLevel.warning)).toBeFalsy();
    expect(existsEqualOrGreaterThanLevelLogs(actual?.logs, LogLevel.information)).toBeTruthy();

    const actualListOperationNode = findOperationByPath(actual, '\\repairs#GET');
    expect(actualListOperationNode).toBeDefined();
    expect(actualListOperationNode?.operationId).toEqual('listRepairs');
    expect(actualListOperationNode?.summary).toEqual('List all repairs with oauth');
    expect(actualListOperationNode?.description).toEqual('Returns a list of repairs with their details and images');
    expect(actualListOperationNode?.isOperation).toBeTruthy();
    expect(actualListOperationNode?.documentationUrl).toEqual('https://sample.server/api/docs');

    const actualPostOperationNode = findOperationByPath(actual, '\\repairs#POST');
    expect(actualPostOperationNode).toBeDefined();
    expect(actualPostOperationNode?.operationId).toBeUndefined();
    expect(actualPostOperationNode?.summary).toBeUndefined();
    expect(actualPostOperationNode?.description).toBeUndefined();
    expect(actualPostOperationNode?.isOperation).toBeTruthy();
    expect(actualPostOperationNode?.documentationUrl).toBeUndefined();
  });

  test('testGetKiotaTree_withOverriddenServer', async () => {
    const descriptionUrl = '../../tests/Kiota.Builder.IntegrationTests/ModelWithOverriddenServer.yaml';

    const actual = await getKiotaTree({ includeFilters: [], descriptionPath: descriptionUrl, excludeFilters: [], clearCache: false });

    expect(actual).toBeDefined();
    expect(existsEqualOrGreaterThanLevelLogs(actual?.logs, LogLevel.warning)).toBeFalsy();
    expect(existsEqualOrGreaterThanLevelLogs(actual?.logs, LogLevel.information)).toBeTruthy();
    const actualGlobalServer = actual?.servers?.[0];
    expect(actualGlobalServer).toBeDefined();
    expect(actualGlobalServer).toEqual('https://sample.server/api');

    const actualOperationNode = findOperationByPath(actual, '\\repairs#GET');
    expect(actualOperationNode).toBeDefined();
    expect(actualOperationNode?.operationId).toBeUndefined();
    expect(actualOperationNode?.servers).toBeDefined();
    expect(actualOperationNode?.servers?.[0]).toEqual('https://sample.server.overridden/api');
  });

  test('testGetKiotaTree_from_valid_external', async () => {
    const descriptionUrl = 'https://raw.githubusercontent.com/microsoftgraph/msgraph-sdk-powershell/dev/openApiDocs/v1.0/Mail.yml';

    const actual = await getKiotaTree({ includeFilters: [], descriptionPath: descriptionUrl, excludeFilters: [], clearCache: false });

    expect(actual).toBeDefined();
    expect(existsEqualOrGreaterThanLevelLogs(actual?.logs, LogLevel.warning)).toBeFalsy();
    expect(existsEqualOrGreaterThanLevelLogs(actual?.logs, LogLevel.information)).toBeTruthy();
    expect(actual?.servers?.[0]).toEqual('https://graph.microsoft.com/v1.0/');
  });

  test('testGetKiotaTree_withAdaptiveCard', async () => {
    const descriptionUrl = '../../tests/Kiota.Builder.IntegrationTests/ModelWithAdaptiveCardExtension.yaml';

    const actual = await getKiotaTree({ includeFilters: [], descriptionPath: descriptionUrl, excludeFilters: [], clearCache: false });

    expect(actual).toBeDefined();
    expect(existsEqualOrGreaterThanLevelLogs(actual?.logs, LogLevel.warning)).toBeFalsy();
    expect(existsEqualOrGreaterThanLevelLogs(actual?.logs, LogLevel.information)).toBeTruthy();

    const actualOperationNode = findOperationByPath(actual, '\\repairs#GET');
    expect(actualOperationNode).toBeDefined();
    expect(actualOperationNode?.operationId).toEqual('listRepairs');
    expect(actualOperationNode?.adaptiveCard).toBeDefined();
    expect(actualOperationNode?.adaptiveCard?.file).toEqual('path_to_adaptive_card_file');
    expect(actualOperationNode?.adaptiveCard?.dataPath).toEqual('$.test');
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

  test('testGetKiotaTree_fromV2_0', async () => {
    const descriptionUrl = '../../tests/Kiota.Builder.IntegrationTests/SwaggerPetStore.json';

    const actual = await getKiotaTree({ includeFilters: [], descriptionPath: descriptionUrl, excludeFilters: [], clearCache: false });

    expect(actual).toBeDefined();
    const actualSpecVersion = actual?.specVersion;
    expect(actualSpecVersion).toEqual(OpenApiSpecVersion.V2_0);
  });

  test('testGetKiotaTree_fromV3_0', async () => {
    const descriptionUrl = '../../tests/Kiota.Builder.IntegrationTests/DiscriminatorSample.yaml';

    const actual = await getKiotaTree({ includeFilters: [], descriptionPath: descriptionUrl, excludeFilters: [], clearCache: false });

    expect(actual).toBeDefined();
    const actualSpecVersion = actual?.specVersion;
    expect(actualSpecVersion).toEqual(OpenApiSpecVersion.V3_0);
  });

  test('testGetKiotaTree_fromV3_1', async () => {
    const descriptionUrl = '../../tests/Kiota.Builder.IntegrationTests/SimpleModelOpenApi3_1.yaml';

    const actual = await getKiotaTree({ includeFilters: [], descriptionPath: descriptionUrl, excludeFilters: [], clearCache: false });

    expect(actual).toBeDefined();
    const actualSpecVersion = actual?.specVersion;
    expect(actualSpecVersion).toEqual(OpenApiSpecVersion.V3_1);
  });


});