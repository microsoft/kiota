import path from 'path';
import { generatePlugin } from '../../lib/generatePlugin';
import { getKiotaTree } from '../../lib/getKiotaTree';
import { getPluginManifest } from '../../lib/getPluginManifest';
import { KiotaPluginType, ConsumerOperation } from '../../types';
import { PluginAuthType } from '../../types';

describe("GeneratePlugin", () => {
  test('generatePlugin_Simple', async () => {
    const descriptionUrl = '../../tests/Kiota.Builder.IntegrationTests/DiscriminatorSample.yaml';
    const outputPath = './.tests_output';

    const pluginType = KiotaPluginType.ApiPlugin;
    const actual = await generatePlugin({
      descriptionPath: descriptionUrl,
      outputPath: outputPath,
      clearCache: false,
      pluginType: pluginType,
      pluginName: 'test3',
      operation: ConsumerOperation.Generate,
      workingDirectory: ''
    });
    expect(actual).toBeDefined();

    if (!actual?.aiPlugin) {
      throw new Error('aiPlugin should be defined');
    }
    const actualPluginManifest = await getPluginManifest({
      descriptionPath: actual?.aiPlugin
    });
    expect(actualPluginManifest).toBeDefined();

    if (!actual?.openAPISpec) {
      throw new Error('descriptionPath should be defined');
    }
    const actualApiManifest = await getKiotaTree({
      descriptionPath: actual?.openAPISpec,
    });
    expect(actualApiManifest).toBeDefined();
  });

  test('generatePlugin_withSecurity', async () => {
    const descriptionUrl = '../../tests/Kiota.Builder.IntegrationTests/ModelWithSecurity.yaml';
    const outputPath = './.tests_output';
    const pluginName = 'withsecurity';

    const pluginType = KiotaPluginType.ApiPlugin;
    const actual = await generatePlugin({
      descriptionPath: descriptionUrl,
      outputPath: outputPath,
      clearCache: false,
      pluginType: pluginType,
      pluginName: pluginName,
      operation: ConsumerOperation.Generate,
      workingDirectory: ''
    });
    expect(actual).toBeDefined();
    expect(actual?.aiPlugin).toEqual(path.join(outputPath, `${pluginName.toLowerCase()}-apiplugin.json`));
    expect(actual?.openAPISpec).toEqual(path.join(outputPath, `${pluginName.toLowerCase()}-openapi.yml`));

    if (!actual?.aiPlugin) {
      throw new Error('aiPlugin should be defined');
    }
    const actualPluginManifest = await getPluginManifest({
      descriptionPath: actual?.aiPlugin
    });
    expect(actualPluginManifest).toBeDefined();
    expect(actualPluginManifest?.runtime[0].auth.type).toEqual('None');
    expect(actualPluginManifest?.runtime[0].run_for_functions[0]).toEqual('listRepairs');
    expect(actualPluginManifest?.runtime[0].run_for_functions[1]).toEqual('repairs_post');
    expect(actualPluginManifest?.functions[0].name).toEqual('listRepairs');
    expect(actualPluginManifest?.functions[1].name).toEqual('repairs_post');

    if (!actual?.openAPISpec) {
      throw new Error('descriptionPath should be defined');
    }
    const actualApiManifest = await getKiotaTree({
      descriptionPath: actual?.openAPISpec,
    });
    expect(actualApiManifest).toBeDefined();
    const actualSecuritySchemes = actualApiManifest?.securitySchemes;
    expect(actualSecuritySchemes).toBeDefined();
    if (!actualSecuritySchemes) {
      throw new Error('securitySchemes should be defined');
    }
    const actualSecurityScheme = actualSecuritySchemes['oAuth2AuthCode'];
    expect(actualSecurityScheme).toBeDefined();
    expect(actualSecurityScheme.referenceId).toEqual('{oAuth2AuthCode_REGISTRATION_ID}');
  });

  
  test('generatePlugin_withAuthAndExistingRefId', async () => {
    const descriptionUrl = '../../tests/Kiota.Builder.IntegrationTests/ModelWithRefIdExtension.yaml';
    const outputPath = './.tests_output';
    const pluginName = 'withrefidandsecurity';

    const pluginType = KiotaPluginType.ApiPlugin;
    const actual = await generatePlugin({
      descriptionPath: descriptionUrl,
      outputPath: outputPath,
      clearCache: false,
      pluginType: pluginType,
      pluginName: pluginName,
      operation: ConsumerOperation.Generate,
      workingDirectory: ''
    });
    expect(actual).toBeDefined();

    expect(actual).toBeDefined();
    expect(actual?.aiPlugin).toEqual(path.join(outputPath, `${pluginName.toLowerCase()}-apiplugin.json`));
    expect(actual?.openAPISpec).toEqual(path.join(outputPath, `${pluginName.toLowerCase()}-openapi.yml`));

    if (!actual?.aiPlugin) {
      throw new Error('aiPlugin should be defined');
    }
    const actualPluginManifest = await getPluginManifest({
      descriptionPath: actual?.aiPlugin
    });
    expect(actualPluginManifest).toBeDefined();
    expect(actualPluginManifest?.runtime[0].auth.type).toEqual('OAuthPluginVault');
    expect(actualPluginManifest?.runtime[0].auth.reference_id).toEqual('otherValue123');
    expect(actualPluginManifest?.runtime[0].run_for_functions[0]).toEqual('listRepairs');
    expect(actualPluginManifest?.runtime[0].run_for_functions[1]).toEqual('repairs_post');
    expect(actualPluginManifest?.functions[0].name).toEqual('listRepairs');
    expect(actualPluginManifest?.functions[1].name).toEqual('repairs_post');

    if (!actual?.openAPISpec) {
      throw new Error('descriptionPath should be defined');
    }
    const actualApiManifest = await getKiotaTree({
      descriptionPath: actual?.openAPISpec,
    });
    expect(actualApiManifest).toBeDefined();
    const actualSecuritySchemes = actualApiManifest?.securitySchemes;
    expect(actualSecuritySchemes).toBeDefined();
    if (!actualSecuritySchemes) {
      throw new Error('securitySchemes should be defined');
    }
    const actualSecurityScheme = actualSecuritySchemes['oAuth2AuthCode'];
    expect(actualSecurityScheme).toBeDefined();
    expect(actualSecurityScheme.referenceId).toEqual('otherValue123');
  });

  test('generatePlugin_withExplicitAuthTypeAndRefId', async () => {
    const descriptionUrl = '../../tests/Kiota.Builder.IntegrationTests/ModelWithRefIdExtension.yaml';
    const outputPath = './.tests_output';
    const pluginName = 'withrefidandsecurity2';

    const pluginType = KiotaPluginType.ApiPlugin;
    const actual = await generatePlugin({
      descriptionPath: descriptionUrl,
      outputPath: outputPath,
      clearCache: false,
      pluginType: pluginType,
      pluginName: pluginName,
      operation: ConsumerOperation.Generate,
      workingDirectory: '',
      pluginAuthType: PluginAuthType.apiKeyPluginVault,
      pluginAuthRefid: 'explicitRefId'
    });
    expect(actual).toBeDefined();

    expect(actual).toBeDefined();
    expect(actual?.aiPlugin).toEqual(path.join(outputPath, `${pluginName.toLowerCase()}-apiplugin.json`));
    expect(actual?.openAPISpec).toEqual(path.join(outputPath, `${pluginName.toLowerCase()}-openapi.yml`));

    if (!actual?.aiPlugin) {
      throw new Error('aiPlugin should be defined');
    }
    const actualPluginManifest = await getPluginManifest({
      descriptionPath: actual?.aiPlugin
    });
    expect(actualPluginManifest).toBeDefined();
    expect(actualPluginManifest?.runtime[0].auth.type).toEqual('ApiKeyPluginVault');
    expect(actualPluginManifest?.runtime[0].auth.reference_id).toEqual('explicitRefId');
    expect(actualPluginManifest?.runtime[0].run_for_functions[0]).toEqual('listRepairs');
    expect(actualPluginManifest?.runtime[0].run_for_functions[1]).toEqual('repairs_post');
    expect(actualPluginManifest?.functions[0].name).toEqual('listRepairs');
    expect(actualPluginManifest?.functions[1].name).toEqual('repairs_post');

    if (!actual?.openAPISpec) {
      throw new Error('descriptionPath should be defined');
    }
    const actualApiManifest = await getKiotaTree({
      descriptionPath: actual?.openAPISpec,
    });
    expect(actualApiManifest).toBeDefined();
    const actualSecuritySchemes = actualApiManifest?.securitySchemes;
    expect(actualSecuritySchemes).toBeDefined();
    if (!actualSecuritySchemes) {
      throw new Error('securitySchemes should be defined');
    }
    const actualSecurityScheme = actualSecuritySchemes['oAuth2AuthCode'];
    expect(actualSecurityScheme).toBeDefined();
    expect(actualSecurityScheme.referenceId).toEqual('otherValue123');
  });

});