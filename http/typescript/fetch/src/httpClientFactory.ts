/**
 * @module HTTPClientFactory
 */

 import { HttpClient } from "./httpClient";
 import { AuthenticationProvider } from "@microsoft/kiota-abstractions";
 import { Middleware } from "./middlewares/middleware";
import { MiddlewareFactory } from "./middlewares/middlewareFactory";
 
 
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
     public static createWithAuthenticationProvider(authProvider: AuthenticationProvider): HttpClient {
         return HTTPClientFactory.createWithMiddleware(MiddlewareFactory.getDefaultMiddlewareChain(authProvider));
     }
 
     /**
      * @public
      * @static
      * Creates a middleware chain with the given one
      * @property {...Middleware} middleware - The first middleware of the middleware chain or a sequence of all the Middleware handlers
      * @returns A HTTPClient instance
      */
     public static createWithMiddleware(middleware: Middleware[]): HttpClient {
         // Middleware should not empty or undefined. This is check is present in the HTTPClient constructor.
         return new HttpClient(undefined,...middleware);
     }

     public static createWithoutMiddleware(customFetch:()=> Promise<Response>): HttpClient {
        // Middleware should not empty or undefined. This is check is present in the HTTPClient constructor.
        return new HttpClient(customFetch);
    }
 }
 