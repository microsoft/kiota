import * as rpc from "vscode-jsonrpc/node";

import { KiotaLogEntry } from "..";
import connectToKiota from "../connect";

interface UpdateClientsConfiguration {
  cleanOutput: boolean;
  clearCache: boolean;
  workspacePath: string;
}

export async function updateClients({ cleanOutput, clearCache, workspacePath }: UpdateClientsConfiguration): Promise<KiotaLogEntry[] | undefined> {
  const result = await connectToKiota(async (connection) => {
    const request = new rpc.RequestType3<string, boolean, boolean, KiotaLogEntry[], void>(
      "Update"
    );
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
};