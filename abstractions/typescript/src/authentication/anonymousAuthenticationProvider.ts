import { RequestInfo } from "../requestInfo";
import { AuthenticationProvider } from "./authenticationProvider";

/** This authentication provider does not perform any authentication.   */
export class AnonymousAuthenticationProvider implements AuthenticationProvider {
    public authenticateRequest = (_: RequestInfo) : Promise<void> => {
        return Promise.resolve();   
    };
}