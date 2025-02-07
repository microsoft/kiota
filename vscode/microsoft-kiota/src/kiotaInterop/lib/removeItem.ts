import * as rpc from "vscode-jsonrpc/node";
import { KiotaLogEntry } from "..";
import connectToKiota from "../connect";

interface RemoveItemConfiguration {
  cleanOutput: boolean;
  workingDirectory: string;
}

interface RemovePluginConfiguration extends RemoveItemConfiguration {
  pluginName: string;
}

interface RemoveClientConfiguration extends RemoveItemConfiguration {
  clientName: string;
}

export function removePlugin({ pluginName, cleanOutput, workingDirectory }: RemovePluginConfiguration): Promise<KiotaLogEntry[] | undefined> {
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



export function removeClient({ clientName, cleanOutput, workingDirectory }: RemoveClientConfiguration): Promise<KiotaLogEntry[] | undefined> {
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