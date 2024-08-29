import { connectToKiota, HttpGenerationConfiguration, KiotaLogEntry } from "./kiotaInterop";
import * as rpc from "vscode-jsonrpc/node";
import * as vscode from "vscode";

export function generateHttpSnippet(context: vscode.ExtensionContext, 
  descriptionPath: string,
  output: string,
  includeFilters: string[],
  excludeFilters: string[],
  clearCache: boolean,
  cleanOutput: boolean,
  excludeBackwardCompatible: boolean,
  disableValidationRules: string[],
  structuredMimeTypes: string[]): Promise<KiotaLogEntry[] | undefined> {
    return connectToKiota<KiotaLogEntry[]>(context, async (connection) => {
      const request = new rpc.RequestType1<HttpGenerationConfiguration, KiotaLogEntry[], void>(
        "GenerateHttpSnippet"
      );
      return await connection.sendRequest(
        request,
        {
          cleanOutput: cleanOutput,
          clearCache: clearCache,
          disabledValidationRules: disableValidationRules,
          excludeBackwardCompatible: excludeBackwardCompatible,
          excludePatterns: excludeFilters,
          includePatterns: includeFilters,
          openAPIFilePath: descriptionPath,
          outputPath: output,
          structuredMimeTypes: structuredMimeTypes,
        } as HttpGenerationConfiguration,
      );
    });
};
