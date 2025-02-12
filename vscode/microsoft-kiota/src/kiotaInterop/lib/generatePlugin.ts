import * as rpc from "vscode-jsonrpc/node";

import { checkForSuccess, ConsumerOperation, GenerationConfiguration, KiotaLogEntry, PluginAuthType } from "..";
import connectToKiota from "../connect";
import { KiotaPluginType, KiotaResult } from "../types";

interface PluginGenerationOptions {
  openAPIFilePath: string;
  outputPath: string;
  pluginTypes: KiotaPluginType[];
  includePatterns: string[];
  excludePatterns: string[];
  clientClassName: string;
  clearCache: boolean;
  cleanOutput: boolean;
  disabledValidationRules: string[];
  operation: ConsumerOperation;
  pluginAuthType?: PluginAuthType | null;
  pluginAuthRefid?: string;

  workingDirectory: string;
}

/**
 * Generates a plugin based on the provided options.
 *
 * @param {PluginGenerationOptions} pluginGenerationOptions - The options for generating the plugin.
 * @returns {Promise<KiotaResult | undefined>} A promise that resolves to a KiotaResult if successful, or undefined if not.
 * @throws {Error} If an error occurs during the generation process.
 *
 * The function connects to Kiota and sends a request to generate a plugin using the provided options.
 * It handles the response and checks for success, returning the result or throwing an error if one occurs.
 */
export async function generatePlugin(pluginGenerationOptions: PluginGenerationOptions
): Promise<KiotaResult | undefined> {
  const result = await connectToKiota<KiotaLogEntry[]>(async (connection) => {
    const request = new rpc.RequestType1<GenerationConfiguration, KiotaLogEntry[], void>(
      "GeneratePlugin"
    );
    return await connection.sendRequest(
      request,
      {
        pluginTypes: pluginGenerationOptions.pluginTypes,
        cleanOutput: pluginGenerationOptions.cleanOutput,
        clearCache: pluginGenerationOptions.clearCache,
        clientClassName: pluginGenerationOptions.clientClassName,
        disabledValidationRules: pluginGenerationOptions.disabledValidationRules,
        excludePatterns: pluginGenerationOptions.excludePatterns,
        includePatterns: pluginGenerationOptions.includePatterns,
        openAPIFilePath: pluginGenerationOptions.openAPIFilePath,
        outputPath: pluginGenerationOptions.outputPath,
        pluginAuthType: pluginGenerationOptions.pluginAuthType,
        pluginAuthRefid: pluginGenerationOptions.pluginAuthRefid,
        operation: pluginGenerationOptions.operation,
      } as GenerationConfiguration,
    );
  }, pluginGenerationOptions.workingDirectory);

  if (result instanceof Error) {
    throw result;
  }

  if (result) {
    return {
      isSuccess: checkForSuccess(result as KiotaLogEntry[]),
      logs: result
    };
  }

  return undefined;

};
