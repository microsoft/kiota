import { TokenCredential } from "@azure/core-auth";
import { BaseBearerTokenAuthenticationProvider, RequestInfo  } from '@microsoft/kiota-abstractions';

export class AzureIdentityAuthenticationProvider extends BaseBearerTokenAuthenticationProvider {
    /**
     *
     */
    public constructor(private readonly credentials: TokenCredential, private readonly scopes: string[] = ['https://graph.microsoft.com/.default']) {
        super();
        if(!credentials) {
            throw new Error('parameter credentials cannot be null');
        }
        if(!scopes || scopes.length === 0) {
            throw new Error('scopes cannot be null or empty');
        }
    }
    public getAuthorizationToken = async (_: RequestInfo) : Promise<string> => {
        const result = await this.credentials.getToken(this.scopes);
        return result?.token ?? '';
    }
}