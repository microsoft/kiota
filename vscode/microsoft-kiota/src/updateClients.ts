import { connectToKiota, KiotaLogEntry } from "./kiotaInterop";
import * as rpc from "vscode-jsonrpc/node";
import * as vscode from "vscode";

export function updateClients(context: vscode.ExtensionContext, cleanOutput: boolean, clearCache: boolean): Promise<KiotaLogEntry[] | undefined> {
    return connectToKiota(context, async (connection) => {
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