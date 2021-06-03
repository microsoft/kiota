import { TokenCredential } from "@azure/core-auth";
import { AuthenticationProvider  } from '@microsoft/kiota-abstractions';

export class AzureIdentityAuthenticationProvider implements AuthenticationProvider {
    /**
     *
     */
    public constructor(private readonly credentials: TokenCredential, private readonly scopes: string[] = ['https://graph.microsoft.com/.default']) {
        if(!credentials) {
            throw new Error('parameter credentials cannot be null');
        }
        if(!scopes || scopes.length === 0) {
            throw new Error('scopes cannot be null or empty');
        }
    }
    public getAuthorizationToken = async (_requestUrl: string) : Promise<string> => {
        const result = await this.credentials.getToken(this.scopes);
        return result?.token ?? '';
    }
}