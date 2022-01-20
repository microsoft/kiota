import { GetTokenOptions, TokenCredential } from "@azure/core-auth";
import { AccessTokenProvider } from '@microsoft/kiota-abstractions';

export class AzureIdentityAccessTokenProvider implements AccessTokenProvider {
    /**
     *@constructor
     *@param credentials The tokenCredential implementation to use for authentication.
     *@param scopes The scopes to use for authentication.
     *@param options The options to use for authentication.
     */
    public constructor(private readonly credentials: TokenCredential, private readonly scopes: string[] = ['https://graph.microsoft.com/.default'], private readonly options?: GetTokenOptions) {
        if (!credentials) {
            throw new Error('parameter credentials cannot be null');
        }
        if (!scopes || scopes.length === 0) {
            throw new Error('scopes cannot be null or empty');
        }
    }
    public getAuthorizationToken = async (): Promise<string> => {
        const result = await this.credentials.getToken(this.scopes, this.options);
        return result?.token ?? '';
    }
}