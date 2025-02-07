import * as rpc from "vscode-jsonrpc/node";

import { KiotaLogEntry } from "..";
import connectToKiota from "../connect";

interface UpdateClientsConfiguration {
  cleanOutput: boolean;
  clearCache: boolean;
  workspacePath: string;
}

export function updateClients({ cleanOutput, clearCache, workspacePath }: UpdateClientsConfiguration): Promise<KiotaLogEntry[] | undefined> {
  return connectToKiota(async (connection) => {
    const request = new rpc.RequestType3<string, boolean, boolean, KiotaLogEntry[], void>(
      "Update"
    );
    const result = await connection.sendRequest(
      request,
      workspacePath,
      cleanOutput,
      clearCache,
    );
    return result;
  });
};