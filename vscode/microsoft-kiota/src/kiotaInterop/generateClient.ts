import * as rpc from "vscode-jsonrpc/node";

import { ConsumerOperation, GenerationConfiguration, KiotaLogEntry } from ".";
import { KiotaGenerationLanguage } from "../types/enums";
import { getWorkspaceJsonDirectory } from "../util";
import connectToKiota from "./connect";

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

export function generateClient(
  {
    openAPIFilePath,
    outputPath,
    language,
    includePatterns,
    excludePatterns,
    clientClassName,
    clientNamespaceName,
    usesBackingStore,
    clearCache,
    cleanOutput,
    excludeBackwardCompatible,
    disabledValidationRules,
    serializers,
    deserializers,
    structuredMimeTypes,
    includeAdditionalData,
    operation,
    workingDirectory
  }: ClientGenerationOptions): Promise<KiotaLogEntry[] | undefined> {
  return connectToKiota<KiotaLogEntry[]>(async (connection) => {
    const request = new rpc.RequestType1<GenerationConfiguration, KiotaLogEntry[], void>(
      "Generate"
    );

    return await connection.sendRequest(
      request,
      {
        cleanOutput,
        clearCache,
        clientClassName,
        clientNamespaceName,
        deserializers,
        disabledValidationRules,
        excludeBackwardCompatible,
        excludePatterns,
        includeAdditionalData,
        includePatterns,
        language,
        openAPIFilePath,
        outputPath,
        serializers,
        structuredMimeTypes,
        usesBackingStore,
        operation: operation
      } as GenerationConfiguration,
    );
  }, workingDirectory);

};

