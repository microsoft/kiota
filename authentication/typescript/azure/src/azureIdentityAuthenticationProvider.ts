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
        options?: GetTokenOptions,
        allowedHosts: Set<string> = new Set<string>(['graph.microsoft.com', 'graph.microsoft.us', 'dod-graph.microsoft.us', 'graph.microsoft.de', 'microsoftgraph.chinacloudapi.cn', 'canary.graph.microsoft.com'])) {
        super(new AzureIdentityAccessTokenProvider(credentials, scopes, options, allowedHosts));
    }
  }
  public getAuthorizationToken = async (
    _: RequestInformation
  ): Promise<string> => {
    const result = await this.credentials.getToken(this.scopes);
    return result?.token ?? "";
  };
}
