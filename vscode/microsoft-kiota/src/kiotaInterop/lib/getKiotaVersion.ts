import * as rpc from "vscode-jsonrpc/node";

import connectToKiota from '../connect';

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
};