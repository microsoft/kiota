import * as rpc from "vscode-jsonrpc/node";

import connectToKiota from "../connect.js";

/**
 * Retrieves the version of Kiota by connecting to the Kiota service.
 *
 * @returns {Promise<string | undefined>} A promise that resolves to the Kiota version string if available, otherwise undefined.
 * @throws {Error} If an error occurs while connecting to the Kiota service or retrieving the version.
 */
export async function getKiotaVersion(): Promise<string | undefined> {
  const result = await connectToKiota<string>(async (connection) => {
    const request = new rpc.RequestType0<string, void>("GetVersion");
    return await connection.sendRequest(request);
  });

  if (result instanceof Error) {
    throw result;
  }

  if (result) {
    const version = result.split("+")[0];
    if (version) {
      return version;
    }
  }
  return undefined;
}
