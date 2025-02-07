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

export function generatePlugin(
  {
    openAPIFilePath,
    outputPath,
    pluginTypes,
    includePatterns,
    excludePatterns,
    clientClassName,
    clearCache,
    cleanOutput,
    disabledValidationRules,
    operation,
    pluginAuthType,
    pluginAuthRefid,
    workingDirectory
  }: PluginGenerationOptions
): Promise<KiotaLogEntry[] | undefined> {
  return connectToKiota<KiotaLogEntry[]>(async (connection) => {
    const request = new rpc.RequestType1<GenerationConfiguration, KiotaLogEntry[], void>(
      "GeneratePlugin"
    );
    return await connection.sendRequest(
      request,
      {
        pluginTypes,
        cleanOutput,
        clearCache,
        clientClassName,
        disabledValidationRules,
        excludePatterns,
        includePatterns,
        openAPIFilePath,
        outputPath,
        pluginAuthType,
        pluginAuthRefid,
        operation,
      } as GenerationConfiguration,
    );
  }, workingDirectory);
};
