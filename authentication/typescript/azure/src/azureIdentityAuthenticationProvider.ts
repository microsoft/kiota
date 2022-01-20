import { GetTokenOptions, TokenCredential } from "@azure/core-auth";
import { BaseBearerTokenAuthenticationProvider } from '@microsoft/kiota-abstractions';
import { AzureIdentityAccessTokenProvider } from "./azureIdentityAccessTokenProvider";

export class AzureIdentityAuthenticationProvider extends BaseBearerTokenAuthenticationProvider {
    /**
     *@constructor
    *@param credentials The tokenCredential implementation to use for authentication.
    *@param scopes The scopes to use for authentication.
    *@param options The options to use for authentication.
    */
    public constructor(readonly credentials: TokenCredential, readonly scopes: string[] = ['https://graph.microsoft.com/.default'], options?: GetTokenOptions) {
        super(new AzureIdentityAccessTokenProvider(credentials, scopes, options));
    }
}