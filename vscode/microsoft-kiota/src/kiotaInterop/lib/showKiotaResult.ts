import * as rpc from "vscode-jsonrpc/node";

import { KiotaShowConfiguration, KiotaShowResult } from "..";
import connectToKiota from "../connect";

interface KiotaResultOptions {
  includeFilters: string[];
  descriptionPath: string;
  excludeFilters: string[];
  clearCache: boolean;
}

/**
 * Asynchronously shows the Kiota result based on the provided options.
 *
 * @param {KiotaResultOptions} options - The options to configure the Kiota result.
 * @param {string[]} options.includeFilters - Filters to include in the result.
 * @param {string} options.descriptionPath - The path to the description file.
 * @param {string[]} options.excludeFilters - Filters to exclude from the result.
 * @param {boolean} options.clearCache - Whether to clear the cache before showing the result.
 * @returns {Promise<KiotaShowResult | undefined>} A promise that resolves to the Kiota show result or undefined if an error occurs.
 * @throws {Error} Throws an error if the result is an instance of Error.
 */
export async function showKiotaResult({ includeFilters, descriptionPath, excludeFilters, clearCache }: KiotaResultOptions): Promise<KiotaShowResult | undefined> {
  const result = await connectToKiota(async (connection) => {
    const request = new rpc.RequestType<KiotaShowConfiguration, KiotaShowResult, void>('Show');

    return await connection.sendRequest(request, {
      includeFilters,
      excludeFilters,
      descriptionPath,
      clearCache
    });
  });

  if (result instanceof Error) {
    throw result;
  }

  return result;
};