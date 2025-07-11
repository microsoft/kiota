import * as rpc from "vscode-jsonrpc/node";

import { KiotaLogEntry, KiotaResult } from "..";
import connectToKiota from "../connect";

export interface RemoveItemConfiguration {
  cleanOutput: boolean;
  workingDirectory: string;
}

export interface RemovePluginConfiguration extends RemoveItemConfiguration {
  pluginName: string;
}

export interface RemoveClientConfiguration extends RemoveItemConfiguration {
  clientName: string;
}

/**
 * Removes a plugin from the Kiota environment.
 *
 * @param {RemovePluginConfiguration} config - The configuration for removing the plugin.
 * @param {string} config.pluginName - The name of the plugin to remove.
 * @param {boolean} config.cleanOutput - Whether to clean the output directory after removal.
 * @param {string} config.workingDirectory - The working directory where the operation should be performed.
 * @returns {Promise<KiotaResult | undefined>} A promise that resolves to a KiotaResult if the operation is successful, or undefined if no result is returned.
 * @throws {Error} Throws an error if the operation fails.
 */
export async function removePlugin({ pluginName, cleanOutput, workingDirectory }: RemovePluginConfiguration): Promise<KiotaResult | undefined> {
  const result = await connectToKiota(async (connection) => {
    const request = new rpc.RequestType2<string, boolean, KiotaLogEntry[], void>(
      "RemovePlugin"
    );
    return await connection.sendRequest(
      request,
      pluginName,
      cleanOutput
    );
  }, workingDirectory);

  if (result instanceof Error) {
    throw result;
  }

  if (result) {
    return {
      isSuccess: result.some(k => k.message.includes('removed successfully')),
      logs: result
    };
  }

  return undefined;
};

/**
 * Removes a client using the provided configuration.
 *
 * @param {RemoveClientConfiguration} config - The configuration for removing the client.
 * @param {string} config.clientName - The name of the client to be removed.
 * @param {boolean} config.cleanOutput - A flag indicating whether to clean the output.
 * @param {string} config.workingDirectory - The working directory for the operation.
 * @returns {Promise<KiotaResult | undefined>} A promise that resolves to a KiotaResult if the client was removed successfully, or undefined if no result is returned.
 * @throws {Error} Throws an error if the result is an instance of Error.
 */
export async function removeClient({ clientName, cleanOutput, workingDirectory }: RemoveClientConfiguration): Promise<KiotaResult | undefined> {
  const result = await connectToKiota(async (connection) => {
    const request = new rpc.RequestType2<string, boolean, KiotaLogEntry[], void>(
      "RemoveClient"
    );
    return await connection.sendRequest(
      request,
      clientName,
      cleanOutput
    );
  }, workingDirectory);

  if (result instanceof Error) {
    throw result;
  }

  if (result) {
    return {
      isSuccess: result.some(k => k.message.includes('removed successfully')),
      logs: result
    };
  }

  return undefined;
};