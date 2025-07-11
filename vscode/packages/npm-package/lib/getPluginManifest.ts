import * as rpc from "vscode-jsonrpc/node";

import connectToKiota from "../connect";
import { PluginManifestResult } from "../types";

export interface GetPluginManifestOptions {
  descriptionPath: string;
}

/**
 * Shows the Kiota result based on the provided options.
 *
 * @param {KiotaResultOptions} options - The options to configure the Kiota result.
 * @param {string} options.descriptionPath - The path to the manifest file.
 * @returns {Promise<PluginManifestResult | undefined>} A promise that resolves to the result or undefined if an error occurs.
 * @throws {Error} Throws an error if the result is an instance of Error.
 */
export async function getPluginManifest({ descriptionPath }: GetPluginManifestOptions): Promise<PluginManifestResult | undefined> {
  const result = await connectToKiota(async (connection) => {
    const request = new rpc.RequestType<GetPluginManifestOptions, PluginManifestResult, void>('ShowPlugin');

    const response = await connection.sendRequest(request, {
      descriptionPath,
    });

    // Mapping
    return response;

  });

  if (result instanceof Error) {
    throw result;
  }

  return result;
};