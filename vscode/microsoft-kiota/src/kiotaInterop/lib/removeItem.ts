import * as rpc from "vscode-jsonrpc/node";

import { KiotaLogEntry, KiotaResult } from "..";
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

export async function removePlugin({ pluginName, cleanOutput, workingDirectory }: RemovePluginConfiguration): Promise<KiotaResult | undefined> {
  const result = await connectToKiota(async (connection) => {
    const request = new rpc.RequestType2<string, boolean, KiotaLogEntry[], void>(
      "RemovePlugin"
    );
    return await connection.sendRequest(
      request,
      pluginName,
      cleanOutput
    );
  }, workingDirectory);

  if (result instanceof Error) {
    throw result;
  }

  if (result) {
    return {
      isSuccess: result.some(k => k.message.includes('removed successfully')),
      logs: result
    };
  }

  return undefined;
};





export async function removeClient({ clientName, cleanOutput, workingDirectory }: RemoveClientConfiguration): Promise<KiotaResult | undefined> {
  const result = await connectToKiota(async (connection) => {
    const request = new rpc.RequestType2<string, boolean, KiotaLogEntry[], void>(
      "RemoveClient"
    );
    return await connection.sendRequest(
      request,
      clientName,
      cleanOutput
    );
  }, workingDirectory);

  if (result instanceof Error) {
    throw result;
  }

  if (result) {
    return {
      isSuccess: result.some(k => k.message.includes('removed successfully')),
      logs: result
    };
  }

  return undefined;
};