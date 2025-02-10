import * as rpc from "vscode-jsonrpc/node";

import { checkForSuccess, ConsumerOperation, GenerationConfiguration, KiotaLogEntry } from "..";
import connectToKiota from "../connect";
import { KiotaGenerationLanguage, KiotaResult } from "../types";

interface ClientGenerationOptions {
  clearCache: boolean;
  cleanOutput: boolean;
  clientClassName: string;
  clientNamespaceName: string;
  deserializers: string[];
  disabledValidationRules: string[];
  excludeBackwardCompatible: boolean;
  excludePatterns: string[];
  includeAdditionalData: boolean;
  includePatterns: string[];
  language: KiotaGenerationLanguage;
  openAPIFilePath: string;
  outputPath: string;
  serializers: string[];
  structuredMimeTypes: string[];
  usesBackingStore: boolean;
  operation: ConsumerOperation;

  workingDirectory: string;
}

export async function generateClient(clientGenerationOptions: ClientGenerationOptions): Promise<KiotaResult | undefined> {
  const result = await connectToKiota<KiotaLogEntry[]>(async (connection) => {
    const request = new rpc.RequestType1<GenerationConfiguration, KiotaLogEntry[], void>(
      "Generate"
    );

    return await connection.sendRequest(
      request,
      {
        cleanOutput: clientGenerationOptions.cleanOutput,
        clearCache: clientGenerationOptions.clearCache,
        clientClassName: clientGenerationOptions.clientClassName,
        clientNamespaceName: clientGenerationOptions.clientNamespaceName,
        deserializers: clientGenerationOptions.deserializers,
        disabledValidationRules: clientGenerationOptions.disabledValidationRules,
        excludeBackwardCompatible: clientGenerationOptions.excludeBackwardCompatible,
        excludePatterns: clientGenerationOptions.excludePatterns,
        includeAdditionalData: clientGenerationOptions.includeAdditionalData,
        includePatterns: clientGenerationOptions.includePatterns,
        language: clientGenerationOptions.language,
        openAPIFilePath: clientGenerationOptions.openAPIFilePath,
        outputPath: clientGenerationOptions.outputPath,
        serializers: clientGenerationOptions.serializers,
        structuredMimeTypes: clientGenerationOptions.structuredMimeTypes,
        usesBackingStore: clientGenerationOptions.usesBackingStore,
        operation: clientGenerationOptions.operation
      } as GenerationConfiguration,
    );
  }, clientGenerationOptions.workingDirectory);

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

