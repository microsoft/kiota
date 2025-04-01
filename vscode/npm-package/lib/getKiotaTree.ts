import * as rpc from "vscode-jsonrpc/node";

import { KiotaShowConfiguration, KiotaTreeResult } from "..";
import connectToKiota from "../connect";

export interface KiotaResultOptions {
  descriptionPath: string;
  includeFilters?: string[];
  excludeFilters?: string[];
  clearCache?: boolean;
  includeKiotaValidationRules?: boolean;
}

/**
 * Shows the Kiota result based on the provided options.
 *
 * @param {KiotaResultOptions} options - The options to configure the Kiota result.
 * @param {string} options.descriptionPath - The path to the description file.
 * @param {string[]} [options.includeFilters] - Filters to include in the result.
 * @param {string[]} [options.excludeFilters] - Filters to exclude from the result.
 * @param {boolean} [options.clearCache] - Whether to clear the cache before showing the result.
 * @param {boolean} [options.includeKiotaValidationRules] - Whether to evaluate built-in kiota rules when parsing the description file.
 * @returns {Promise<KiotaTreeResult | undefined>} A promise that resolves to the Kiota show result or undefined if an error occurs.
 * @throws {Error} Throws an error if the result is an instance of Error.
 */
export async function getKiotaTree({ descriptionPath, includeFilters, excludeFilters, clearCache, includeKiotaValidationRules }: KiotaResultOptions): Promise<KiotaTreeResult | undefined> {
  const result = await connectToKiota(async (connection) => {
    const request = new rpc.RequestType<KiotaShowConfiguration, KiotaTreeResult, void>('Show');

    const result = await connection.sendRequest(request, {
      includeFilters: includeFilters ?? [],
      excludeFilters: excludeFilters ?? [],
      descriptionPath,
      clearCache: clearCache ?? false,
      includeKiotaValidationRules: includeKiotaValidationRules ?? false,
    });
    return result;
  });

  if (result instanceof Error) {
    throw result;
  }

  return result;
};