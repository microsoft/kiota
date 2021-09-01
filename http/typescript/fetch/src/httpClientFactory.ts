/**
 * @module HTTPClientFactory
 */

 import { HTTPClient } from "./HTTPClient";
 import { AuthenticationProvider } from "./IAuthenticationProvider";
 import { AuthenticationHandler } from "./middleware/AuthenticationHandler";
 import { HTTPMessageHandler } from "./middleware/FetchHandler";
 import { Middleware } from "./middleware/IMiddleware";
 import { RedirectHandlerOptions } from "./middleware/options/RedirectHandlerOptions";
 import { RetryHandlerOptions } from "./middleware/options/RetryHandlerOptions";
 import { RedirectHandler } from "./middleware/RedirectHandler";
 import { RetryHandler } from "./middleware/RetryHandler";
 
 /**
  * @private
  * To check whether the environment is node or not
  * @returns A boolean representing the environment is node or not
  */
 const isNodeEnvironment = (): boolean => {
     return typeof process === "object" && typeof require === "function";
 };
 
 /**
  * @class
  * Class representing HTTPClientFactory
  */
 export class HTTPClientFactory {
     /**
      * @public
      * @static
      * Creates HTTPClient with default middleware chain
      * @param {AuthenticationProvider} authProvider - The authentication provider instance
      * @returns A HTTPClient instance
      *
      * NOTE: These are the things that we need to remember while doing modifications in the below default pipeline.
      * 		* HTTPMessageHander should be the last one in the middleware pipeline, because this makes the actual network call of the request
      * 		* The best place for AuthenticationHandler is in the starting of the pipeline, because every other handler might have to work for multiple times for a request but the auth token for
      * 		  them will remain same. For example, Retry and Redirect handlers might be working multiple times for a request based on the response but their auth token would remain same.
      */
     public static createWithAuthenticationProvider(authProvider: AuthenticationProvider): HTTPClient {
         const authenticationHandler = new AuthenticationHandler(authProvider);
         const retryHandler = new RetryHandler(new RetryHandlerOptions());
         const FetchHandler = new FetchHandler();
 
         authenticationHandler.setNext(retryHandler);
         if (isNodeEnvironment()) {
             const redirectHandler = new RedirectHandler(new RedirectHandlerOptions());
             retryHandler.setNext(redirectHandler);
             redirectHandler.setNext(FetchHandler);
         } else {
             retryHandler.setNext(FetchHandler);
         }
         return HTTPClientFactory.createWithMiddleware(authenticationHandler);
     }
 
     /**
      * @public
      * @static
      * Creates a middleware chain with the given one
      * @property {...Middleware} middleware - The first middleware of the middleware chain or a sequence of all the Middleware handlers
      * @returns A HTTPClient instance
      */
     public static createWithMiddleware(...middleware: Middleware[]): HTTPClient {
         // Middleware should not empty or undefined. This is check is present in the HTTPClient constructor.
         return new HTTPClient(...middleware);
     }
 }
 