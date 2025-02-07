import * as rpc from "vscode-jsonrpc/node";

import { KiotaLogEntry } from ".";
import connectToKiota from "./connect";

export function migrateFromLockFile(lockFileDirectory: string): Promise<KiotaLogEntry[] | undefined> {
  return connectToKiota(async (connection) => {
    const request = new rpc.RequestType1<string, KiotaLogEntry[], void>(
      "MigrateFromLockFile"
    );
    const result = await connection.sendRequest(
      request,
      lockFileDirectory
    );
    return result;
  });
};