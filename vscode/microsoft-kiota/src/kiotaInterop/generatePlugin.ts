import * as rpc from "vscode-jsonrpc/node";

import { ConsumerOperation, GenerationConfiguration, KiotaLogEntry, PluginAuthType } from ".";
import { KiotaPluginType } from "../types/enums";
import { getWorkspaceJsonDirectory } from "../util";
import connectToKiota from "./connect";

export function generatePlugin(
  descriptionPath: string,
  output: string,
  pluginTypes: KiotaPluginType[],
  includeFilters: string[],
  excludeFilters: string[],
  clientClassName: string,
  clearCache: boolean,
  cleanOutput: boolean,
  disableValidationRules: string[],
  operation: ConsumerOperation,
  pluginAuthType?: PluginAuthType | null,
  pluginAuthRefid?: string,
  workingDirectory: string = getWorkspaceJsonDirectory(),
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
        disabledValidationRules: disableValidationRules,
        excludePatterns: excludeFilters,
        includePatterns: includeFilters,
        openAPIFilePath: descriptionPath,
        outputPath: output,
        pluginAuthType,
        pluginAuthRefid,
        operation,
      } as GenerationConfiguration,
    );
  }, workingDirectory);
};
