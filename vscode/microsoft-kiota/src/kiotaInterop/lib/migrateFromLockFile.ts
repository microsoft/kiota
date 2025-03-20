import * as rpc from "vscode-jsonrpc/node";

import { KiotaLogEntry } from "..";
import connectToKiota from "../connect";

/**
 * Migrates data from a lock file located in the specified directory.
 *
 * This function connects to the Kiota service and sends a request to migrate data from the lock file.
 * If the migration is successful, it returns an array of `KiotaLogEntry` objects.
 * If an error occurs during the migration, the error is thrown.
 *
 * @param {string} lockFileDirectory - The directory where the lock file is located.
 * @returns {Promise<KiotaLogEntry[] | undefined>} A promise that resolves to an array of `KiotaLogEntry` objects if the migration is successful, or `undefined` if no data is migrated.
 * @throws {Error} If an error occurs during the migration process.
 */
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