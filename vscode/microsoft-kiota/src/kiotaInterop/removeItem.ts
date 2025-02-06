import * as rpc from "vscode-jsonrpc/node";
import { KiotaLogEntry } from ".";
import connectToKiota from "./connect";

export function removePlugin(pluginName: string, cleanOutput: boolean, workingDirectory: string): Promise<KiotaLogEntry[] | undefined> {
  return connectToKiota(async (connection) => {
    const request = new rpc.RequestType2<string, boolean, KiotaLogEntry[], void>(
      "RemovePlugin"
    );
    const result = await connection.sendRequest(
      request,
      pluginName,
      cleanOutput
    );
    return result;
  }, workingDirectory);
};

export function removeClient(clientName: string, cleanOutput: boolean, workingDirectory: string): Promise<KiotaLogEntry[] | undefined> {
  return connectToKiota(async (connection) => {
    const request = new rpc.RequestType2<string, boolean, KiotaLogEntry[], void>(
      "RemoveClient"
    );
    const result = await connection.sendRequest(
      request,
      clientName,
      cleanOutput
    );
    return result;
  }, workingDirectory);
};