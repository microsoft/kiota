import * as sinon from 'sinon';

import { removeClient, removePlugin } from '../removeItem';
import { setupKiotaStubs } from './stubs.util';

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
    const mockResults = [
      {
        level: 0,
        message: 'removed successfully'
      }
    ];

    connectionStub.resolves(mockResults);
    const results = await removeClient('test', false, process.cwd());
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
    const mockResults = [
      {
        level: 0,
        message: 'removed successfully'
      }
    ];

    connectionStub.resolves(mockResults);
    const results = await removePlugin('test', false, process.cwd());
    expect(results).toBeDefined();
  });

});