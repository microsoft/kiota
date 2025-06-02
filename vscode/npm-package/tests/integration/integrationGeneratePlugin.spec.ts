import fs from 'fs';
import path from 'path';
import { generatePlugin } from '../../lib/generatePlugin';
import { getKiotaTree } from '../../lib/getKiotaTree';
import { getPluginManifest } from '../../lib/getPluginManifest';
import { KiotaPluginType, ConsumerOperation, LogLevel } from '../../types';
import { PluginAuthType } from '../../types';
import { existsEqualOrGreaterThanLevelLogs } from '../assertUtils';

async function loadJSON(filename: string) {
  const json = await import(filename, {
    with: { type: 'json' },
  });

  return json.default;
}

describe("GeneratePlugin", () => {

  test('generatePlugin_withoutWorkspaceAddOperationForExisting', async () => {
    const descriptionUrl = '../../tests/Kiota.Builder.IntegrationTests/DiscriminatorSample.yaml';
    const outputPath = './.tests_output';
    const pluginName = 'withoutWorkspaceAddOperationForExisting';

    const pluginType = KiotaPluginType.ApiPlugin;
    const actual = await generatePlugin({
      descriptionPath: descriptionUrl,
      outputPath: outputPath,
      clearCache: false,
      pluginType: pluginType,
      pluginName: pluginName,
      operation: ConsumerOperation.Generate,
      workingDirectory: '',
      noWorkspace: true,
    });
    expect(actual).toBeDefined();
    expect(existsEqualOrGreaterThanLevelLogs(actual?.logs, LogLevel.warning)).toBeFalsy();
    expect(existsEqualOrGreaterThanLevelLogs(actual?.logs, LogLevel.information)).toBeTruthy();

    // Second call to generatePlugin with the same parameters should raise an error
    const actual2 = await generatePlugin({
      descriptionPath: descriptionUrl,
      outputPath: outputPath,
      clearCache: false,
      pluginType: pluginType,
      pluginName: pluginName,
      operation: ConsumerOperation.Add,
      workingDirectory: '',
      noWorkspace: true,
    });
    expect(actual2).toBeDefined();
    expect(existsEqualOrGreaterThanLevelLogs(actual2?.logs, LogLevel.warning)).toBeFalsy();
    expect(existsEqualOrGreaterThanLevelLogs(actual2?.logs, LogLevel.information)).toBeTruthy();

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

  test('generatePlugin_withWorkspaceAddOperationForExisting', async () => {
    const descriptionUrl = '../../tests/Kiota.Builder.IntegrationTests/DiscriminatorSample.yaml';
    const outputPath = './.tests_output';
    const pluginName = 'withWorkspaceAddOperationForExisting';

    const pluginType = KiotaPluginType.ApiPlugin;
    const actual = await generatePlugin({
      descriptionPath: descriptionUrl,
      outputPath: outputPath,
      clearCache: false,
      pluginType: pluginType,
      pluginName: pluginName,
      operation: ConsumerOperation.Generate,
      workingDirectory: '',
      noWorkspace: false,
    });
    expect(actual).toBeDefined();
    expect(existsEqualOrGreaterThanLevelLogs(actual?.logs, LogLevel.warning)).toBeFalsy();
    expect(existsEqualOrGreaterThanLevelLogs(actual?.logs, LogLevel.information)).toBeTruthy();

    // Second call to generatePlugin with the same parameters should raise an error
    const actual2 = await generatePlugin({
      descriptionPath: descriptionUrl,
      outputPath: outputPath,
      clearCache: false,
      pluginType: pluginType,
      pluginName: pluginName,
      operation: ConsumerOperation.Add,
      workingDirectory: '',
      noWorkspace: false,
    });
    expect(actual2).toBeDefined();
    expect(existsEqualOrGreaterThanLevelLogs(actual2?.logs, LogLevel.error)).toBeTruthy();

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
      workingDirectory: '',
      noWorkspace: true,
    });
    expect(actual).toBeDefined();
    expect(existsEqualOrGreaterThanLevelLogs(actual?.logs, LogLevel.warning)).toBeFalsy();
    expect(existsEqualOrGreaterThanLevelLogs(actual?.logs, LogLevel.information)).toBeTruthy();

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

  test('generatePlugin_withAdaptiveCardExtension', async () => {
    const descriptionUrl = '../../tests/Kiota.Builder.IntegrationTests/ModelWithAdaptiveCardExtension.yaml';
    const outputPath = './.tests_output';

    const pluginType = KiotaPluginType.ApiPlugin;
    const actual = await generatePlugin({
      descriptionPath: descriptionUrl,
      outputPath: outputPath,
      clearCache: false,
      pluginType: pluginType,
      pluginName: 'withAdaptiveCardExtension',
      operation: ConsumerOperation.Generate,
      workingDirectory: '',
      noWorkspace: true,
    });
    expect(actual).toBeDefined();
    expect(existsEqualOrGreaterThanLevelLogs(actual?.logs, LogLevel.warning)).toBeFalsy();
    expect(existsEqualOrGreaterThanLevelLogs(actual?.logs, LogLevel.information)).toBeTruthy();

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

    // Load json from file path
    const pluginJsonPath = actual?.aiPlugin;
    if (!pluginJsonPath) {
      throw new Error('aiPlugin should be defined');
    }

    // add ./ to the path to make it relative
    const relativePath = path.relative(__dirname, pluginJsonPath);

    const pluginJson = await loadJSON(relativePath);
    expect(pluginJson).toBeDefined();

    // listRepairs should have capabilities defined using content from adaptive card extension information
    const firstFunction = pluginJson.functions.find((f: { name: string }) => f.name === 'listRepairs');
    expect(firstFunction).toBeDefined();
    expect(firstFunction.capabilities).toBeDefined();
    expect(firstFunction.capabilities.response_semantics).toBeDefined();
    expect(firstFunction.capabilities.response_semantics.data_path).toEqual('$.test');
    expect(firstFunction.capabilities.response_semantics.static_template).toBeDefined();
    expect(firstFunction.capabilities.response_semantics.static_template.file).toEqual('path_to_adaptive_card_file');

    // Check that this file does not exist since it is sample path coming from adaptive card extension, so template is not generated
    const firstFunctionStaticTemplateRelativePath = path.relative(__dirname, '.tests_output/path_to_adaptive_card_file');
    expect(firstFunctionStaticTemplateRelativePath).toBeDefined();
    const resolvedFirstFunctionPath = path.resolve(__dirname, firstFunctionStaticTemplateRelativePath);
    const firstFunctionFileExists = fs.existsSync(resolvedFirstFunctionPath);
    expect(firstFunctionFileExists).toBeFalsy();

    // post should have capabilities defined using template
    const secondFunction = pluginJson.functions.find((f: { name: string }) => f.name === 'repairs_post');
    expect(secondFunction).toBeDefined();
    expect(secondFunction.capabilities).toBeDefined();
    expect(secondFunction.capabilities.response_semantics).toBeDefined();
    expect(secondFunction.capabilities.response_semantics.data_path).toEqual('$');
    expect(secondFunction.capabilities.response_semantics.static_template).toBeDefined();
    expect(secondFunction.capabilities.response_semantics.static_template.file).toEqual('./adaptiveCards/repairs_post.json');
    
    // Check that this file exists
    const secondFunctionStaticTemplateRelativePath = path.relative(__dirname, '.tests_output/adaptiveCards/repairs_post.json');
    expect(secondFunctionStaticTemplateRelativePath).toBeDefined();
    const resolvedSecondFunctionPath = path.resolve(__dirname, secondFunctionStaticTemplateRelativePath);
    const secondFunctionFileExists = fs.existsSync(resolvedSecondFunctionPath);
    expect(secondFunctionFileExists).toBeTruthy();

    // put should not have capabilities defined since has empty adaptive card extension content to support typespec scenarios
    const thirdFunction = pluginJson.functions.find((f: { name: string }) => f.name === 'repairs_put');
    expect(thirdFunction).toBeDefined();
    expect(thirdFunction.capabilities).toBeUndefined();

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
    expect(actualSecurityScheme.referenceId).toBeUndefined();
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