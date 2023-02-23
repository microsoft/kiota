import { connectToKiota } from "./kiotaInterop";
import * as rpc from "vscode-jsonrpc/node";
import * as vscode from "vscode";

export function getKiotaVersion(kiotaOutputChannel: vscode.LogOutputChannel): Promise<string | undefined> {
    return connectToKiota<string>(async (connection) => {
      const request = new rpc.RequestType0<string, void>("GetVersion");
      const result = await connection.sendRequest(request);
      if (result) {
        const version = result.split("+")[0];
        if (version) {
          kiotaOutputChannel.info(`kiota version: ${version}`);
          return version;
        }
      }
      kiotaOutputChannel.error(`kiota version: not found`);
      kiotaOutputChannel.show();
      return undefined;
    });
};