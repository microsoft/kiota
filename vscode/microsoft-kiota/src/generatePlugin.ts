import { connectToKiota, GenerationConfiguration, KiotaGenerationLanguage, KiotaLogEntry, KiotaPluginType } from "./kiotaInterop";
import * as rpc from "vscode-jsonrpc/node";
import * as vscode from "vscode";

export function generatePlugin(context: vscode.ExtensionContext, 
  descriptionPath: string,
  output: string,
  pluginTypes: KiotaPluginType[],
  includeFilters: string[],
  excludeFilters: string[],
  clientClassName: string,
  clearCache: boolean,
  cleanOutput: boolean,
  disableValidationRules: string[]): Promise<KiotaLogEntry[] | undefined> {
    return connectToKiota<KiotaLogEntry[]>(context, async (connection) => {
      const request = new rpc.RequestType1<GenerationConfiguration, KiotaLogEntry[], void>(
        "GeneratePlugin"
      );
      return await connection.sendRequest(
        request,
        {
          pluginTypes: pluginTypes,
          cleanOutput: cleanOutput,
          clearCache: clearCache,
          clientClassName: clientClassName,
          disabledValidationRules: disableValidationRules,
          excludePatterns: excludeFilters,
          includePatterns: includeFilters,
          openAPIFilePath: descriptionPath,
          outputPath: output,
        } as GenerationConfiguration,
      );
    });
};