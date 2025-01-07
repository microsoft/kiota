import * as vscode from "vscode";
import * as rpc from "vscode-jsonrpc/node";

import { connectToKiota, ConsumerOperation, GenerationConfiguration, KiotaLogEntry, PluginAuthType } from "../../kiotaInterop";
import { KiotaPluginType } from "../../types/enums";
import { getWorkspaceJsonDirectory } from "../../util";

export function generatePlugin(context: vscode.ExtensionContext,
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
  return connectToKiota<KiotaLogEntry[]>(context, async (connection) => {
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
