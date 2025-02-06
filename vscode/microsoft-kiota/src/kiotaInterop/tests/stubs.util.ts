import * as sinon from 'sinon';
import * as rpc from 'vscode-jsonrpc';
import * as connectToKiota from '../connect';

export function setupKiotaStubs() {
  const connectionStub = sinon.stub();

  const mockConnection = {
    sendRequest: connectionStub,
    listen: sinon.stub(),
    dispose: sinon.stub(),
  } as unknown as rpc.MessageConnection;

  const connectToKiotaStub = sinon.stub(connectToKiota, 'default').callsFake(
    async (callback: (connection: rpc.MessageConnection) => Promise<any>) => {
      return callback(mockConnection);
    }
  );

  return { connectToKiotaStub, connectionStub, mockConnection };
}