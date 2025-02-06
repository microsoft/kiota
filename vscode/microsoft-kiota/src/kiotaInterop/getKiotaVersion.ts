import * as rpc from "vscode-jsonrpc/node";

import connectToKiota from './connect';

export function getKiotaVersion(): Promise<string | undefined> {

  return connectToKiota<string>(async (connection) => {
    const request = new rpc.RequestType0<string, void>("GetVersion");
    const result = await connection.sendRequest(request);
    if (result) {
      const version = result.split("+")[0];
      if (version) {
        return version;
      }
    }
    return undefined;
  });
};