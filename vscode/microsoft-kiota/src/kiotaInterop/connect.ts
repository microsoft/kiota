import * as cp from 'child_process';
import * as rpc from 'vscode-jsonrpc/node';

import { getKiotaPath, ensureKiotaIsPresent } from './install';

export default async function connectToKiota<T>(callback: (connection: rpc.MessageConnection) => Promise<T | undefined>, workingDirectory: string = process.cwd()): Promise<T | undefined | Error> {
  const kiotaPath = getKiotaPath();
  await ensureKiotaIsPresent();
  const childProcess = cp.spawn(kiotaPath, ["rpc"], {
      cwd: workingDirectory,
      env: {
          ...process.env,
          // eslint-disable-next-line @typescript-eslint/naming-convention
          KIOTA_CONFIG_PREVIEW: "true",
      }
  });
  let connection = rpc.createMessageConnection(
      new rpc.StreamMessageReader(childProcess.stdout),
      new rpc.StreamMessageWriter(childProcess.stdin));
  connection.listen();
  try {
      return await callback(connection);
  } catch (error) {
      console.warn(error);
      const errorMessage = (error as { data?: { message: string } })?.data?.message
          || 'An unknown error occurred';
      return new Error(errorMessage);
  } finally {
      connection.dispose();
      childProcess.stdin?.end();
      childProcess.kill();
  }
}
