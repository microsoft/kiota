import { RequestInformation } from "../requestInformation";
import { AuthenticationProvider } from "./authenticationProvider";

/** This authentication provider does not perform any authentication.   */
export class AnonymousAuthenticationProvider implements AuthenticationProvider {
	public authenticateRequest = (_: RequestInformation): Promise<void> => {
		return Promise.resolve();
	};
}
