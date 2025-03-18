import * as cp from 'child_process';
import * as net from 'node:net';
import * as os from 'node:os';
import * as path from 'node:path';
import { v4 as uuidv4 } from 'uuid';
import * as rpc from 'vscode-jsonrpc/node';

import { getKiotaPath, ensureKiotaIsPresent } from './install';

export default async function connectToKiota<T>(callback: (connection: rpc.MessageConnection) => Promise<T | undefined>, workingDirectory: string = process.cwd()): Promise<T | undefined | Error> {
  const kiotaPath = getKiotaPath();
  await ensureKiotaIsPresent();
  // Use a unique pipe for this extension.
  const suffix = uuidv4();
  const pipeName = `KiotaJsonRpc-${suffix}`;
  const childProcess = cp.spawn(kiotaPath, ["rpc", "--mode", "NamedPipe", "--pipe-name", pipeName], {
    cwd: workingDirectory,
    env: {
      ...process.env,
      // eslint-disable-next-line @typescript-eslint/naming-convention
      KIOTA_CONFIG_PREVIEW: "true",
    }
  });
  const prefix = os.platform() === 'win32' ? '\\\\.\\pipe\\' : path.join(os.tmpdir(), 'CoreFxPipe_');
  const name = `${prefix}${pipeName}`;
  const socket = net.createConnection(name);
  const reader = new rpc.SocketMessageReader(socket);
  const writer = new rpc.SocketMessageWriter(socket);
  let connection = rpc.createMessageConnection(reader, writer);
  connection.listen();
  try {
    return await callback(connection);
  } catch (error) {
    const errorMessage = (error as { data?: { message: string; }; })?.data?.message
      || 'An unknown error occurred';
    return new Error(errorMessage);
  } finally {
    connection.dispose();
    childProcess.kill();
  }
}
