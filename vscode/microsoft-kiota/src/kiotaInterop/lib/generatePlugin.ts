import * as rpc from "vscode-jsonrpc/node";

import { ConsumerOperation, GenerationConfiguration, KiotaLogEntry, PluginAuthType } from "..";
import connectToKiota from "../connect";
import { KiotaPluginType } from "../types";

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

export async function generatePlugin(pluginGenerationOptions: PluginGenerationOptions
): Promise<KiotaLogEntry[] | undefined> {
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

  return result;
};
