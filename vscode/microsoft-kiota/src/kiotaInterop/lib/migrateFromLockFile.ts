import * as rpc from "vscode-jsonrpc/node";

import { KiotaLogEntry } from "..";
import connectToKiota from "../connect";

export async function migrateFromLockFile(lockFileDirectory: string): Promise<KiotaLogEntry[] | undefined> {
  const result = await connectToKiota(async (connection) => {
    const request = new rpc.RequestType1<string, KiotaLogEntry[], void>(
      "MigrateFromLockFile"
    );
    return await connection.sendRequest(
      request,
      lockFileDirectory
    );
  });

  if (result instanceof Error) {
    throw result;
  }

  return result;
};