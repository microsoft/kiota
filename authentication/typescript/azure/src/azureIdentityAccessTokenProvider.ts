import { GetTokenOptions, TokenCredential } from "@azure/core-auth";
import {
  AccessTokenProvider,
  AllowedHostsValidator,
  validateProtocol,
} from "@microsoft/kiota-abstractions";

/** Access token provider that leverages the Azure Identity library to retrieve an access token. */
export class AzureIdentityAccessTokenProvider implements AccessTokenProvider {
  /**
   *@constructor
   *@param credentials The tokenCredential implementation to use for authentication.
   *@param scopes The scopes to use for authentication.
   *@param options The options to use for authentication.
   *@param allowedHosts The allowed hosts to use for authentication.
   */
  public constructor(
    private readonly credentials: TokenCredential,
    private readonly scopes: string[] = [
      "https://graph.microsoft.com/.default",
    ],
    private readonly options?: GetTokenOptions,
    allowedHosts: Set<string> = new Set<string>([
      "graph.microsoft.com",
      "graph.microsoft.us",
      "dod-graph.microsoft.us",
      "graph.microsoft.de",
      "microsoftgraph.chinacloudapi.cn",
      "canary.graph.microsoft.com",
    ])
  ) {
    if (!credentials) {
      throw new Error("parameter credentials cannot be null");
    }
    if (!scopes || scopes.length === 0) {
      throw new Error("scopes cannot be null or empty");
    }
    this.allowedHostsValidator = new AllowedHostsValidator(allowedHosts);
  }
  private readonly allowedHostsValidator: AllowedHostsValidator;
  /**
   * @inheritdoc
   */
  public getAuthorizationToken = async (url?: string): Promise<string> => {
    if (!url || !this.allowedHostsValidator.isUrlHostValid(url)) {
      return "";
    }
    validateProtocol(url);
    const result = await this.credentials.getToken(this.scopes, this.options);
    return result?.token ?? "";
  };
  /**
   * @inheritdoc
   */
  public getAllowedHostsValidator = () => this.allowedHostsValidator;
}
