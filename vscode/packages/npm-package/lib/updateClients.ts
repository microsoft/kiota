import * as rpc from "vscode-jsonrpc/node";

import { KiotaLogEntry } from "../index.js";
import connectToKiota from "../connect.js";

export interface UpdateClientsConfiguration {
  cleanOutput: boolean;
  clearCache: boolean;
  workspacePath: string;
}

/**
 * Updates the clients by connecting to Kiota and sending a request to update.
 *
 * @param {UpdateClientsConfiguration} config - The configuration object containing the following properties:
 * @param {boolean} config.cleanOutput - Whether to clean the output directory before updating.
 * @param {boolean} config.clearCache - Whether to clear the cache before updating.
 * @param {string} config.workspacePath - The path to the workspace where the clients are located.
 *
 * @returns {Promise<KiotaLogEntry[] | undefined>} A promise that resolves to an array of Kiota log entries if the update is successful, or undefined if there is an error.
 *
 * @throws {Error} Throws an error if the result of the update is an instance of Error.
 */
export async function updateClients({
  cleanOutput,
  clearCache,
  workspacePath,
}: UpdateClientsConfiguration): Promise<KiotaLogEntry[] | undefined> {
  const result = await connectToKiota(async (connection) => {
    const request = new rpc.RequestType3<
      string,
      boolean,
      boolean,
      KiotaLogEntry[],
      void
    >("Update");
    return await connection.sendRequest(
      request,
      workspacePath,
      cleanOutput,
      clearCache,
    );
  });

  if (result instanceof Error) {
    throw result;
  }

  return result;
}
