import { Middleware } from "./middleware";

/**
 * Gets the default middlewares in use for the client.
 * @returns the default middlewares.
 */
export function getDefaultMiddlewares(): Middleware[] {
	return []; //TODO add default middlewares
}
/**
 * Gets the default request settings to be used for the client.
 * @returns the default request settings.
 */
export function getDefaultRequestSettings(): RequestInit {
	return {}; //TODO add default request settings
}
