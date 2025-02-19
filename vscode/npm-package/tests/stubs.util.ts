import * as rpc from 'vscode-jsonrpc';
import * as connectToKiota from '../connect';

export function setupKiotaStubs() {
  const connectionStub = jest.fn();

  const mockConnection = {
    sendRequest: connectionStub,
    listen: jest.fn(),
    dispose: jest.fn(),
  } as unknown as rpc.MessageConnection;

  const connectToKiotaStub = jest.spyOn(connectToKiota, 'default').mockImplementation(
    async (callback: (connection: rpc.MessageConnection) => Promise<any>) => {
      return callback(mockConnection);
    }
  );

  return { connectToKiotaStub, connectionStub, mockConnection };
}