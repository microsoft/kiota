import * as sinon from 'sinon';


import { setupKiotaStubs } from './stubs.util';
import { updateClients } from '../updateClients';

describe("update Clients", () => {
  let connectionStub: sinon.SinonStub;

  beforeEach(() => {
    const stubs = setupKiotaStubs();
    connectionStub = stubs.connectionStub;
  });

  afterEach(() => {
    sinon.restore();
  });


  test('should return results when successful', async () => {
    const mockResults = [
      {
        level: 0,
        message: 'updated successfully'
      }
    ];

    connectionStub.resolves(mockResults);
    const results = await updateClients({ clearCache: false, cleanOutput: false, workspacePath: 'test' });
    expect(results).toBeDefined();
  });

});

