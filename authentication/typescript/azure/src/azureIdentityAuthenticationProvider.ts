import { GetTokenOptions, TokenCredential } from "@azure/core-auth";
import { BaseBearerTokenAuthenticationProvider } from '@microsoft/kiota-abstractions';
import { AzureIdentityAccessTokenProvider } from "./azureIdentityAccessTokenProvider";

export class AzureIdentityAuthenticationProvider extends BaseBearerTokenAuthenticationProvider {
    /**
     *@constructor
    *@param credentials The tokenCredential implementation to use for authentication.
    *@param scopes The scopes to use for authentication.
    *@param options The options to use for authentication.
    *@param allowedHosts The allowed hosts to use for authentication.
    */
    public constructor(credentials: TokenCredential,
        scopes: string[] = ['https://graph.microsoft.com/.default'],
        allowedHosts: Set<string> = new Set<string>(),
        options?: GetTokenOptions) {
        super(new AzureIdentityAccessTokenProvider(credentials, scopes, allowedHosts, options));
    }
}