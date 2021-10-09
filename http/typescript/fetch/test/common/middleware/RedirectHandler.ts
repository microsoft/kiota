/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

import { assert } from "chai";

import { MiddlewareContext } from "../../../src/middlewares/middlewareContext";
import { MiddlewareControl } from "../../../src/middlewares/middlewareControl";
import { RedirectHandlerOptions } from "../../../src/middlewares/options/redirectHandlerOption";
import { RedirectHandler } from "../../../src/middlewares/redirectHandler";
import { DummyFetchHandler } from "./dummyFetchHandler";
import { getResponse } from "../testUtils"

var Response = Response;
if (typeof Response != "object") {
	Response = getResponse();

}

const redirectHandlerOptions = new RedirectHandlerOptions();
const redirectHandler = new RedirectHandler();
describe("RedirectHandler.ts", () => {
	describe("constructor", () => {
		it("Should create an instance with given options", () => {
			const handler = new RedirectHandler(redirectHandlerOptions);
			assert.isDefined(handler["options"]);
		});

		it("Should create an instance with default set of options", () => {
			const handler = new RedirectHandler();
			assert.isDefined(handler["options"]);
		});
	});

	describe("isRedirect", () => {
		it("Should return true for response having 301 status code", () => {
			const response = new Response("Dummy", {
				status: 301,
			});
			assert.isTrue(redirectHandler["isRedirect"](response));
		});

		it("Should return true for response having 302 status code", () => {
			const response = new Response("Dummy", {
				status: 302,
			});
			assert.isTrue(redirectHandler["isRedirect"](response));
		});

		it("Should return true for response having 303 status code", () => {
			const response = new Response("Dummy", {
				status: 303,
			});
			assert.isTrue(redirectHandler["isRedirect"](response));
		});

		it("Should return true for response having 307 status code", () => {
			const response = new Response("Dummy", {
				status: 307,
			});
			assert.isTrue(redirectHandler["isRedirect"](response));
		});

		it("Should return true for response having 308 status code", () => {
			const response = new Response("Dummy", {
				status: 308,
			});
			assert.isTrue(redirectHandler["isRedirect"](response));
		});

		it("Should return false for non redirect status codes", () => {
			const response = new Response("Dummy", {
				status: 200,
			});
			assert.isFalse(redirectHandler["isRedirect"](response));
		});
	});

	describe("hasLocationHeader", () => {
		it("Should return true for response with location header", () => {
			const res = new Response("Dummy", {
				status: 301,
				headers: {
					location: "https://dummylocation.microsoft.com",
				},
			});
			assert.isTrue(redirectHandler["hasLocationHeader"](res));
		});

		it("Should return false for response without location header", () => {
			const res = new Response("Dummy", {
				status: 301,
			});
			assert.isFalse(redirectHandler["hasLocationHeader"](res));
		});
	});

	describe("getLocationHeader", () => {
		it("Should return location from response", () => {
			const location = "https://dummylocation.microsoft.com";
			const res = new Response("Dummy", {
				status: 301,
				headers: {
					location,
				},
			});
			assert.equal(redirectHandler["getLocationHeader"](res), location);
		});

		it("Should return null for response without location header", () => {
			const res = new Response("Dummy", {
				status: 301,
			});
			assert.equal(redirectHandler["getLocationHeader"](res), null);
		});
	});

	describe("isRelativeURL", () => {
		it("Should return true for a relative url", () => {
			const url = "/graphproxy/me";
			assert.isTrue(redirectHandler["isRelativeURL"](url));
		});

		it("Should return false for a absolute url", () => {
			const url = "https://graph.microsoft.com/v1.0/graphproxy/me";
			assert.isFalse(redirectHandler["isRelativeURL"](url));
		});
	});

	describe("shouldDropAuthorizationHeader", () => {
		it("Should return true for urls with different domain", () => {
			const requestUrl = "https://graph.microsoft.com/v1.0/me";
			const redirectedUrl = "https://graphredirection.microsoft.com/v1.0/me";
			assert.isTrue(redirectHandler["shouldDropAuthorizationHeader"](requestUrl, redirectedUrl));
		});

		it("Should return true for urls with different domain and one without path", () => {
			const requestUrl = "https://graph.microsoft.com/v1.0/me";
			const redirectedUrl = "https://graphredirection.microsoft.com/";
			assert.isTrue(redirectHandler["shouldDropAuthorizationHeader"](requestUrl, redirectedUrl));
		});

		it("Should return true for urls with different domain without path", () => {
			const requestUrl = "https://graph.microsoft.com/";
			const redirectedUrl = "https://graphredirection.microsoft.com";
			assert.isTrue(redirectHandler["shouldDropAuthorizationHeader"](requestUrl, redirectedUrl));
		});

		it("Should return false relative urls", () => {
			const requestUrl = "/graph/me/";
			const redirectedUrl = "/graphRedirection/me";
			assert.isFalse(redirectHandler["shouldDropAuthorizationHeader"](requestUrl, redirectedUrl));
		});

		it("Should return false redirect url is relative", () => {
			const requestUrl = "https://graph.microsoft.com/v1.0/me";
			const redirectedUrl = "/graphRedirection";
			assert.isFalse(redirectHandler["shouldDropAuthorizationHeader"](requestUrl, redirectedUrl));
		});

		it("Should return false for urls with same domain", () => {
			const requestUrl = "https://graph.microsoft.com/v1.0/me";
			const redirectedUrl = "https://graph.microsoft.com/v2.0/me";
			assert.isFalse(redirectHandler["shouldDropAuthorizationHeader"](requestUrl, redirectedUrl));
		});
	});

	describe("getOptions", () => {
		it("Should return the options in the context object", () => {
			const maxRedirects = 10;
			const shouldRedirect = () => false;
			const options = new RedirectHandlerOptions(maxRedirects, shouldRedirect);
			const cxt: MiddlewareContext = {
				request: "url",
				middlewareControl: new MiddlewareControl([options]),
			};
			const o = redirectHandler["getOptions"](cxt);
			assert.equal(o.maxRedirects, maxRedirects);
			assert.equal(o.shouldRedirect, shouldRedirect);
		});

		it("Should return the default set of options in the middleware", () => {
			const cxt: MiddlewareContext = {
				request: "url",
			};
			const o = redirectHandler["getOptions"](cxt);
			assert.equal(o.maxRedirects, redirectHandler["options"].maxRedirects);
			assert.equal(o.shouldRedirect, redirectHandler["options"].shouldRedirect);
		});
	});

	describe("executeWithRedirect", async () => {
		const context: MiddlewareContext = {
			request: "/me",
			options: {
				method: "GET",
			},
		};
		const dummyFetchHandler = new DummyFetchHandler();
		const handler = new RedirectHandler();
		handler.next = dummyFetchHandler;
		it("Should not redirect for the redirect count equal to maxRedirects", async () => {
			const maxRedirect = 1;
			const options = new RedirectHandlerOptions(maxRedirect);
			dummyFetchHandler.setResponses([new Response("", { status: 301 }), new Response("ok", { status: 200 })]);
			await handler["executeWithRedirect"](context, maxRedirect, options);
			assert.equal(context.response.status, 301);
		});

		it("Should not redirect for the non redirect response", async () => {
			const options = new RedirectHandlerOptions();
			dummyFetchHandler.setResponses([new Response("", { status: 200 })]);
			await handler["executeWithRedirect"](context, 0, options);
			assert.equal(context.response.status, 200);
		});

		it("Should not redirect for the redirect response without location header", async () => {
			const options = new RedirectHandlerOptions();
			dummyFetchHandler.setResponses([new Response("", { status: 301 }), new Response("ok", { status: 200 })]);
			await handler["executeWithRedirect"](context, 0, options);
			assert.equal(context.response.status, 301);
		});

		it("Should not redirect for shouldRedirect callback returning false", async () => {
			const options = new RedirectHandlerOptions(undefined, () => false);
			dummyFetchHandler.setResponses([new Response("", { status: 301 }), new Response("ok", { status: 200 })]);
			await handler["executeWithRedirect"](context, 0, options);
			assert.equal(context.response.status, 301);
		});

		it("Should drop body and change method to get for SEE_OTHER status code", async () => {
			const options = new RedirectHandlerOptions();
			dummyFetchHandler.setResponses([
				new Response("", {
					status: 303,
					headers: {
						[RedirectHandler["LOCATION_HEADER"]]: "/location",
					},
				}),
				new Response("ok", { status: 200 }),
			]);
			await handler["executeWithRedirect"](context, 0, options);
			assert.isUndefined(context.options.body);
			assert.equal(context.options.method, "GET");
			assert.equal(context.response.status, 200);
		});

		it("Should not drop Authorization header for relative url redirect", async () => {
			const options = new RedirectHandlerOptions();
			const cxt: MiddlewareContext = {
				request: "/me",
				options: {
					method: "POST",
					body: "dummy body",
					headers: {
						[RedirectHandler["AUTHORIZATION_HEADER"]]: "Bearer TEST",
					},
				},
			};
			dummyFetchHandler.setResponses([
				new Response("", {
					status: 301,
					headers: {
						[RedirectHandler["LOCATION_HEADER"]]: "/location",
					},
				}),
				new Response("ok", { status: 200 }),
			]);
			await handler["executeWithRedirect"](cxt, 0, options);
			assert.isDefined(cxt.options.headers[RedirectHandler["AUTHORIZATION_HEADER"]]);
			assert.equal(cxt.response.status, 200);
		});

		it("Should not drop Authorization header for same authority redirect url", async () => {
			const options = new RedirectHandlerOptions();
			const cxt: MiddlewareContext = {
				request: "https://graph.microsoft.com/v1.0/me",
				options: {
					method: "POST",
					body: "dummy body",
					headers: {
						[RedirectHandler["AUTHORIZATION_HEADER"]]: "Bearer TEST",
					},
				},
			};
			dummyFetchHandler.setResponses([
				new Response("", {
					status: 301,
					headers: {
						[RedirectHandler["LOCATION_HEADER"]]: "https://graph.microsoft.com/v2.0/me",
					},
				}),
				new Response("ok", { status: 200 }),
			]);
			await handler["executeWithRedirect"](cxt, 0, options);
			assert.isDefined(cxt.options.headers[RedirectHandler["AUTHORIZATION_HEADER"]]);
			assert.equal(cxt.response.status, 200);
		});

		it("Should return success response after successful redirect", async () => {
			const options = new RedirectHandlerOptions();
			const cxt: MiddlewareContext = {
				request: "https://graph.microsoft.com/v1.0/me",
				options: {
					method: "POST",
					body: "dummy body",
				},
			};
			dummyFetchHandler.setResponses([
				new Response(null, {
					status: 301,
					headers: {
						[RedirectHandler["LOCATION_HEADER"]]: "https://graphredirect.microsoft.com/v1.0/me",
					},
				}),
				new Response("ok", { status: 200 }),
			]);
			await handler["executeWithRedirect"](cxt, 0, options);
			assert.equal(cxt.response.status, 200);
		});
	});

	describe("execute", async () => {
		it("Should set the redirect value in options to manual", async () => {
			const context: MiddlewareContext = {
				request: "/me",
				options: {
					method: "GET",
				},
			};
			const dummyFetchHandler = new DummyFetchHandler();
			const handler = new RedirectHandler();
			handler.next = dummyFetchHandler;
			dummyFetchHandler.setResponses([new Response("", { status: 200 })]);
			await handler.execute(context);
			assert.equal(context.options.redirect, RedirectHandler["MANUAL_REDIRECT"]);
		});
	});
});
