import { AllowedHostsValidator } from "./allowedHostsValidator";

/**
 * @interface
 * An AccessTokenProvider implementation retrieves an access token
 * to be used by an AuthenticationProvider implementation.
 */
export interface AccessTokenProvider {
  /**
   * Retrieves an access token for the given target URL.
   * @param {string} url - The target URL.
   * @returns {Promise<string>} The access token.
   */
  getAuthorizationToken: (url?: string) => Promise<string>;
  /**
   * Retrieves the allowed hosts validator.
   * @returns {AllowedHostsValidator} The allowed hosts validator.
   */
  getAllowedHostsValidator: () => AllowedHostsValidator;
}
