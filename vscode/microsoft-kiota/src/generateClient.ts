import { connectToKiota, ConsumerOperation, GenerationConfiguration, KiotaGenerationLanguage, KiotaLogEntry } from "./kiotaInterop";
import * as rpc from "vscode-jsonrpc/node";
import * as vscode from "vscode";
import { getWorkspaceJsonDirectory } from "./util";

export function generateClient(context: vscode.ExtensionContext, 
  descriptionPath: string,
  output: string,
  language: KiotaGenerationLanguage,
  includeFilters: string[],
  excludeFilters: string[],
  clientClassName: string,
  clientNamespaceName: string,
  usesBackingStore: boolean,
  clearCache: boolean,
  cleanOutput: boolean,
  excludeBackwardCompatible: boolean,
  disableValidationRules: string[],
  serializers: string[],
  deserializers: string[],
  structuredMimeTypes: string[],
  includeAdditionalData: boolean,
  operation: ConsumerOperation,
  workingDirectory: string = getWorkspaceJsonDirectory()
): Promise<KiotaLogEntry[] | undefined> {
    return connectToKiota<KiotaLogEntry[]>(context, async (connection) => {
      const request = new rpc.RequestType1<GenerationConfiguration, KiotaLogEntry[], void>(
        "Generate"
      );
      return await connection.sendRequest(
        request,
        {
          cleanOutput: cleanOutput,
          clearCache: clearCache,
          clientClassName: clientClassName,
          clientNamespaceName: clientNamespaceName,
          deserializers: deserializers,
          disabledValidationRules: disableValidationRules,
          excludeBackwardCompatible: excludeBackwardCompatible,
          excludePatterns: excludeFilters,
          includeAdditionalData: includeAdditionalData,
          includePatterns: includeFilters,
          language: language,
          openAPIFilePath: descriptionPath,
          outputPath: output,
          serializers: serializers,
          structuredMimeTypes: structuredMimeTypes,
          usesBackingStore: usesBackingStore,
          operation: operation
        } as GenerationConfiguration,
      );
    }, workingDirectory);
};