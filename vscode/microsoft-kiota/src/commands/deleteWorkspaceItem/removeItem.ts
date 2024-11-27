import * as vscode from "vscode";
import * as rpc from "vscode-jsonrpc/node";

import { connectToKiota, KiotaLogEntry } from "../../kiotaInterop";

export function removePlugin(context: vscode.ExtensionContext, pluginName: string, cleanOutput: boolean): Promise<KiotaLogEntry[] | undefined> {
  return connectToKiota(context, async (connection) => {
    const request = new rpc.RequestType2<string, boolean, KiotaLogEntry[], void>(
      "RemovePlugin"
    );
    const result = await connection.sendRequest(
      request,
      pluginName,
      cleanOutput
    );
    return result;
  });
};

export function removeClient(context: vscode.ExtensionContext, clientName: string, cleanOutput: boolean): Promise<KiotaLogEntry[] | undefined> {
  return connectToKiota(context, async (connection) => {
    const request = new rpc.RequestType2<string, boolean, KiotaLogEntry[], void>(
      "RemoveClient"
    );
    const result = await connection.sendRequest(
      request,
      clientName,
      cleanOutput
    );
    return result;
  });
};