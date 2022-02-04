import { RequestInformation } from "../requestInformation";

/**
 * @interface
 * Interface to be implementated to provide authentication information for a request.
 * @property {Function} authenticateRequest - The function to authenticate the request.
 */
export interface AuthenticationProvider {
  /**
   * Authenticates the application and returns a token base on the provided Uri.
   * @param request the request to authenticate.
   * @return a Promise to await for the authentication to be completed.
   */
  authenticateRequest: (request: RequestInformation) => Promise<void>;
}
