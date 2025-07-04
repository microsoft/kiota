import * as rpc from "vscode-jsonrpc/node";

import { checkForSuccess, ConsumerOperation, GenerationConfiguration, KiotaLogEntry } from "..";
import connectToKiota from "../connect";
import { KiotaGenerationLanguage, KiotaResult } from "../types";

export interface ClientGenerationOptions {
  openAPIFilePath: string;
  clientClassName: string;
  clientNamespaceName: string;
  language: KiotaGenerationLanguage;
  outputPath: string;
  operation: ConsumerOperation;
  workingDirectory: string;

  deserializers?: string[];
  disabledValidationRules?: string[];
  excludeBackwardCompatible?: boolean;
  excludePatterns?: string[];
  includeAdditionalData?: boolean;
  includePatterns?: string[];
  clearCache?: boolean;
  cleanOutput?: boolean;
  serializers?: string[];
  structuredMimeTypes?: string[];
  usesBackingStore?: boolean;
}

/**
 * Generates a client based on the provided client generation options.
 *
 * @param {ClientGenerationOptions} clientGenerationOptions - The options for generating the client.
 *  * Options for generating a client.
 *
 * @param {string} clientGenerationOptions.openAPIFilePath - The file path to the OpenAPI specification.
 * @param {string} clientGenerationOptions.clientClassName - The name of the client class to generate.
 * @param {string} clientGenerationOptions.clientNamespaceName - The namespace name for the generated client.
 * @param {KiotaGenerationLanguage} clientGenerationOptions.language - The programming language for the generated client.
 * @param {string} clientGenerationOptions.outputPath - The output path for the generated client.
 * @param {ConsumerOperation} clientGenerationOptions.operation - The consumer operation to perform.
 * @param {string} clientGenerationOptions.workingDirectory - The working directory for the generation process.
 * @param {boolean} [clientGenerationOptions.clearCache] - Whether to clear the cache before generating the client.
 * @param {boolean} [clientGenerationOptions.cleanOutput] - Whether to clean the output directory before generating the client.
 * @param {string[]} [clientGenerationOptions.deserializers] - The list of deserializers to use.
 * @param {string[]} [clientGenerationOptions.disabledValidationRules] - The list of validation rules to disable.
 * @param {boolean} [clientGenerationOptions.excludeBackwardCompatible] - Whether to exclude backward-compatible changes.
 * @param {string[]} [clientGenerationOptions.excludePatterns] - The list of patterns to exclude from generation.
 * @param {boolean} [clientGenerationOptions.includeAdditionalData] - Whether to include additional data in the generated client.
 * @param {string[]} [clientGenerationOptions.includePatterns] - The list of patterns to include in generation.
 * @param {string[]} [clientGenerationOptions.serializers] - The list of serializers to use.
 * @param {string[]} [clientGenerationOptions.structuredMimeTypes] - The list of structured MIME types to support.
 * @param {boolean} [clientGenerationOptions.usesBackingStore] - Whether the generated client uses a backing store.

 * @returns {Promise<KiotaResult | undefined>} A promise that resolves to a KiotaResult if successful, or undefined if not.
 * @throws {Error} If an error occurs during the client generation process.
 */
export async function generateClient(clientGenerationOptions: ClientGenerationOptions): Promise<KiotaResult | undefined> {
  const result = await connectToKiota<KiotaLogEntry[]>(async (connection) => {
    const request = new rpc.RequestType1<GenerationConfiguration, KiotaLogEntry[], void>(
      "Generate"
    );

    return await connection.sendRequest(
      request,
      {
        openAPIFilePath: clientGenerationOptions.openAPIFilePath,
        clientClassName: clientGenerationOptions.clientClassName,
        clientNamespaceName: clientGenerationOptions.clientNamespaceName,
        language: clientGenerationOptions.language,
        outputPath: clientGenerationOptions.outputPath,
        operation: clientGenerationOptions.operation,

        deserializers: clientGenerationOptions.deserializers ?? [],
        disabledValidationRules: clientGenerationOptions.disabledValidationRules ?? [],
        excludeBackwardCompatible: clientGenerationOptions.excludeBackwardCompatible ?? false,
        excludePatterns: clientGenerationOptions.excludePatterns ?? [],
        includeAdditionalData: clientGenerationOptions.includeAdditionalData ?? false,
        cleanOutput: clientGenerationOptions.cleanOutput ?? false,
        clearCache: clientGenerationOptions.clearCache ?? false,
        includePatterns: clientGenerationOptions.includePatterns ?? [],
        serializers: clientGenerationOptions.serializers ?? [],
        structuredMimeTypes: clientGenerationOptions.structuredMimeTypes ?? [],
        usesBackingStore: clientGenerationOptions.usesBackingStore ?? false,
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

