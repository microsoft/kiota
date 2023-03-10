import { config } from '../config/config';
import { Logger } from './common/logger';
import { DeviceCodeCredential } from '@azure/identity';
import { FetchRequestAdapter } from '@microsoft/kiota-http-fetchlibrary';
import { AzureIdentityAuthenticationProvider } from '@microsoft/kiota-authentication-azure';
import { ApiClient } from './client/apiClient';

export class App {
  static run(): App {
    const app = new App();
    app.start();
    const cred = new DeviceCodeCredential({
      tenantId: '7607FBB5-D5FF-4E09-8E32-1C7CE79C5529',
      clientId: '836A24EA-B7E5-47C8-836A-901261168FB7',
      userPromptCallback: (deviceCodeInfo) => {
        // eslint-disable-next-line no-console
        console.log(deviceCodeInfo.message);
      },
    });
    const authProvider = new AzureIdentityAuthenticationProvider(cred, ['Mail.Read']);
    const requestAdapter = new FetchRequestAdapter(authProvider);
    const client = new ApiClient(requestAdapter);
    Logger.log(`${client}`);
    return app;
  }

  private start(): void {
    this.logAppInfo();
  }

  private logAppInfo(): void {
    Logger.logTask('APP', {
      develop: DEVELOP,
      version: VERSION,
      config: config,
    });
  }
}
