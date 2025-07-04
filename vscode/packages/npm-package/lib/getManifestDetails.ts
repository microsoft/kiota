import * as rpc from "vscode-jsonrpc/node";

import { KiotaGetManifestDetailsConfiguration, KiotaManifestResult } from "..";
import connectToKiota from "../connect";

export interface ManifestOptions {
  manifestPath: string;
  clearCache?: boolean;
  apiIdentifier?: string;
}

/**
 * Retrieves the manifest details for a given API.
 *
 * @param {ManifestOptions} options - The options for retrieving the manifest details.
 * @param {string} options.manifestPath - The path to the manifest file.
 * @param {boolean} [options.clearCache] - Whether to clear the cache before retrieving the manifest details.
 * @param {string} [options.apiIdentifier] - The identifier of the API.
 * @returns {Promise<KiotaManifestResult | undefined>} A promise that resolves to the manifest details or undefined if not found.
 * @throws {Error} Throws an error if the request fails.
 */
export async function getManifestDetails({ manifestPath, clearCache, apiIdentifier }: ManifestOptions): Promise<KiotaManifestResult | undefined> {
  const result = await connectToKiota(async (connection) => {
    const request = new rpc.RequestType<KiotaGetManifestDetailsConfiguration, KiotaManifestResult, void>('GetManifestDetails');

    return await connection.sendRequest(
      request,
      {
        manifestPath,
        apiIdentifier: apiIdentifier ?? '',
        clearCache: clearCache ?? false,
      }
    );
  });

  if (result instanceof Error) {
    throw result;
  }

  return result;
};