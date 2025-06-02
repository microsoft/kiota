import * as rpc from "vscode-jsonrpc/node";

import { checkForSuccess, ConsumerOperation, GenerationConfiguration, KiotaLogEntry, PluginAuthType } from "..";
import connectToKiota from "../connect";
import { KiotaPluginType, GeneratePluginResult } from "../types";
import * as path from "path";

export interface PluginGenerationOptions {
  descriptionPath: string;
  outputPath: string;
  pluginName: string;
  operation: ConsumerOperation;
  workingDirectory: string;

  pluginType?: KiotaPluginType;
  includePatterns?: string[];
  excludePatterns?: string[];
  clearCache?: boolean;
  cleanOutput?: boolean;
  disabledValidationRules?: string[];
  noWorkspace?: boolean;
  pluginAuthType?: PluginAuthType | null;
  pluginAuthRefid?: string;
}

/**
 * Generates a plugin based on the provided options.
 *
 * @param {PluginGenerationOptions} pluginGenerationOptions - The options for generating the plugin.
 * @param {string} pluginGenerationOptions.openAPIFilePath - The file path to the OpenAPI specification.
 * @param {string} pluginGenerationOptions.pluginName - The name of the plugin to generate.
 * @param {string} pluginGenerationOptions.outputPath - The output path where the generated plugin will be saved.
 * @param {ConsumerOperation} pluginGenerationOptions.operation - The operation to perform during generation.
 * @param {string} pluginGenerationOptions.workingDirectory - The working directory for the generation process.
 * @param {KiotaPluginType} [pluginGenerationOptions.pluginType] - The type of the plugin to generate.
 * @param {string[]} [pluginGenerationOptions.includePatterns] - The patterns to include in the generation process.
 * @param {string[]} [pluginGenerationOptions.excludePatterns] - The patterns to exclude from the generation process.
 * @param {boolean} [pluginGenerationOptions.clearCache] - Whether to clear the cache before generation.
 * @param {boolean} [pluginGenerationOptions.cleanOutput] - Whether to clean the output directory before generation.
 * @param {string[]} [pluginGenerationOptions.disabledValidationRules] - The validation rules to disable during generation.
 * @param {boolean} [pluginGenerationOptions.noWorkspace] - Whether to generate without a workspace.
 * @param {PluginAuthType | null} [pluginGenerationOptions.pluginAuthType] - The authentication type for the plugin, if any.
 * @param {string} [pluginGenerationOptions.pluginAuthRefid] - The reference ID for the plugin authentication, if any.
 * @returns {Promise<KiotaResult | undefined>} A promise that resolves to a KiotaResult if successful, or undefined if not.
 * @throws {Error} If an error occurs during the generation process.
 *
 * The function connects to Kiota and sends a request to generate a plugin using the provided options.
 * It handles the response and checks for success, returning the result or throwing an error if one occurs.
 */
export async function generatePlugin(pluginGenerationOptions: PluginGenerationOptions
): Promise<GeneratePluginResult | undefined> {
  const pluginType = pluginGenerationOptions.pluginType ?? KiotaPluginType.ApiPlugin;
  const result = await connectToKiota<KiotaLogEntry[]>(async (connection) => {
    const request = new rpc.RequestType1<GenerationConfiguration, KiotaLogEntry[], void>(
      "GeneratePlugin"
    );
    return await connection.sendRequest(
      request,
      {
        openAPIFilePath: pluginGenerationOptions.descriptionPath,
        outputPath: pluginGenerationOptions.outputPath,
        operation: pluginGenerationOptions.operation,
        clientClassName: pluginGenerationOptions.pluginName,

        pluginTypes: [pluginType],
        cleanOutput: pluginGenerationOptions.cleanOutput ?? false,
        clearCache: pluginGenerationOptions.clearCache ?? false,
        disabledValidationRules: pluginGenerationOptions.disabledValidationRules ?? [],
        excludePatterns: pluginGenerationOptions.excludePatterns ?? [],
        includePatterns: pluginGenerationOptions.includePatterns ?? [],
        noWorkspace: pluginGenerationOptions.noWorkspace ?? null,
        pluginAuthType: pluginGenerationOptions.pluginAuthType ?? null,
        pluginAuthRefid: pluginGenerationOptions.pluginAuthRefid ?? '',
      } as GenerationConfiguration,
    );
  }, pluginGenerationOptions.workingDirectory);

  if (result instanceof Error) {
    throw result;
  }

  if (result) {
    const outputPath = pluginGenerationOptions.outputPath;
    const pluginName = pluginGenerationOptions.pluginName;
    const pathOfSpec = path.join(outputPath, `${pluginName.toLowerCase()}-openapi.yml`);
    const plugingTypeName = KiotaPluginType[pluginType];
    const pathPluginManifest = path.join(outputPath, `${pluginName.toLowerCase()}-${plugingTypeName.toLowerCase()}.json`);
    return {
      aiPlugin: pathPluginManifest,
      openAPISpec: pathOfSpec, 
      isSuccess: checkForSuccess(result as KiotaLogEntry[]),
      logs: result
    };
  }

  return undefined;

};
