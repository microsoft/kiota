import * as vscode from "vscode";
import * as rpc from "vscode-jsonrpc/node";

import { KiotaLogEntry } from ".";
import connectToKiota from "./connect";

interface UpdateClientsConfiguration {
  cleanOutput: boolean;
  clearCache: boolean;
}

export function updateClients({ cleanOutput, clearCache }: UpdateClientsConfiguration): Promise<KiotaLogEntry[] | undefined> {
  return connectToKiota(async (connection) => {
    const request = new rpc.RequestType3<string, boolean, boolean, KiotaLogEntry[], void>(
      "Update"
    );
    const result = await connection.sendRequest(
      request,
      vscode.workspace.workspaceFolders![0].uri.fsPath,
      cleanOutput,
      clearCache,
    );
    return result;
  });
};