import { connectToKiota } from "./kiotaInterop";
import * as rpc from "vscode-jsonrpc/node";
import * as vscode from "vscode";

export function getKiotaVersion(context: vscode.ExtensionContext, kiotaOutputChannel: vscode.LogOutputChannel): Promise<string | undefined> {
  try {
    return connectToKiota<string>(context, async (connection) => {
      const request = new rpc.RequestType0<string, void>("GetVersion");
      const result = await connection.sendRequest(request);
      if (result) {
        const version = result.split("+")[0];
        if (version) {
          kiotaOutputChannel.info(`kiota: ${version}`);
          return version;
        }
      }
      kiotaOutputChannel.error(`kiota: ${vscode.l10n.t('not found')}`);
      kiotaOutputChannel.show();
      return undefined;
    });
  } catch (error) {
    return Promise.resolve(undefined);
  }
};