import * as sinon from 'sinon';

import { removeClient, removePlugin } from '../removeItem';
import { setupKiotaStubs } from './stubs.util';
import { KiotaLogEntry } from '..';

describe("remove Client", () => {
  let connectionStub: sinon.SinonStub;

  beforeEach(() => {
    const stubs = setupKiotaStubs();
    connectionStub = stubs.connectionStub;
  });

  afterEach(() => {
    sinon.restore();
  });


  test('should return success when successful', async () => {
    const mockResults: KiotaLogEntry[] = [
      {
        level: 0,
        message: 'removed successfully'
      }
    ];

    connectionStub.resolves(mockResults);
    const results = await removeClient({ clientName: 'test', cleanOutput: false, workingDirectory: process.cwd() });
    expect(results).toBeDefined();
  });

});

describe("remove Plugin", () => {
  let connectionStub: sinon.SinonStub;

  beforeEach(() => {
    const stubs = setupKiotaStubs();
    connectionStub = stubs.connectionStub;
  });

  afterEach(() => {
    sinon.restore();
  });


  test('should return success when successful', async () => {
    const mockResults: KiotaLogEntry[] = [
      {
        level: 0,
        message: 'removed successfully'
      }
    ];

    connectionStub.resolves(mockResults);
    const results = await removePlugin({ pluginName: 'test', cleanOutput: false, workingDirectory: process.cwd() });
    expect(results).toBeDefined();
  });

});