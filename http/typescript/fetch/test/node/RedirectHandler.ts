/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

import { assert } from "chai";
import { Response } from "node-fetch";

import { RedirectHandlerOptionKey, RedirectHandlerOptions } from "../../src/middlewares/options/redirectHandlerOptions";
import { RedirectHandler } from "../../src/middlewares/redirectHandler";
import { DummyFetchHandler } from "../common/middleware/dummyFetchHandler";

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
			assert.isTrue(redirectHandler["isRedirect"](response as any));
		});

		it("Should return true for response having 302 status code", () => {
			const response = new Response("Dummy", {
				status: 302,
			});
			assert.isTrue(redirectHandler["isRedirect"](response as any));
		});

		it("Should return true for response having 303 status code", () => {
			const response = new Response("Dummy", {
				status: 303,
			});
			assert.isTrue(redirectHandler["isRedirect"](response as any));
		});

		it("Should return true for response having 307 status code", () => {
			const response = new Response("Dummy", {
				status: 307,
			});
			assert.isTrue(redirectHandler["isRedirect"](response as any));
		});

		it("Should return true for response having 308 status code", () => {
			const response = new Response("Dummy", {
				status: 308,
			});
			assert.isTrue(redirectHandler["isRedirect"](response as any));
		});

		it("Should return false for non redirect status codes", () => {
			const response = new Response("Dummy", {
				status: 200,
			});
			assert.isFalse(redirectHandler["isRedirect"](response as any));
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
			assert.isFalse(redirectHandler["hasLocationHeader"](res as any));
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
			assert.equal(redirectHandler["getLocationHeader"](res as any), location);
		});

		it("Should return null for response without location header", () => {
			const res = new Response("Dummy", {
				status: 301,
			});
			assert.equal(redirectHandler["getLocationHeader"](res as any), null);
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

	describe("set RedirectOptions in RequestInformation", () => {
		it("Should set the RedirectOptions from the context object", async () => {
			const defaultOptions = new RedirectHandlerOptions();
			const handler = new RedirectHandler(defaultOptions);
			const dummyFetchHandler = new DummyFetchHandler();
			handler.next = dummyFetchHandler;
			const maxRedirects = 2;
			const shouldRedirect = () => true;
			const options = new RedirectHandlerOptions(maxRedirects, shouldRedirect);

			const requestUrl = "url";
			const fetchRequestInit = {
				method: "PUT",
				headers: {
					"Content-Type": "application/octet-stream",
				},
			};
			const requestInformationOptions = {
				[RedirectHandlerOptionKey]: options,
			};

			dummyFetchHandler.setResponses([
				new Response(null, {
					status: 301,
					headers: {
						[RedirectHandler["LOCATION_HEADER"]]: "/location",
					},
				}),
				new Response(null, {
					status: 301,
					headers: {
						[RedirectHandler["LOCATION_HEADER"]]: "/location",
					},
				}),
				new Response("RETURNED", { status: 301 }),
				new Response("ok", { status: 200 }),
			] as any);
			const response = await handler["execute"](requestUrl, fetchRequestInit, requestInformationOptions);
			assert.equal(response.status, 301);
			assert.isDefined(response.body);
		});

		it("Should use default options", async () => {
			const handler = new RedirectHandler();
			const dummyFetchHandler = new DummyFetchHandler();
			handler.next = dummyFetchHandler;

			const requestUrl = "url";
			const fetchRequestInit = {
				method: "PUT",
				headers: {
					"Content-Type": "application/octet-stream",
				},
			};

			const responseBody = {
				test: "TEST",
			};
			const arr = [];
			for (let i = 0; i < 5; i++) {
				arr[i] = new Response(null, {
					status: 301,
					headers: {
						[RedirectHandler["LOCATION_HEADER"]]: "/location",
					},
				});
			}
			dummyFetchHandler.setResponses([...arr, new Response(responseBody as any, { status: 301 })]);

			const response = await handler["execute"](requestUrl, fetchRequestInit);
			assert.equal(response.status, 301);
			assert.isNotNull(response.body);
		});
	});

	describe("executeWithRedirect", async () => {
		const requestUrl = "/me";
		const fetchRequestInit = {
			method: "GET",
		};

		const handler = new RedirectHandler();
		const dummyFetchHandler = new DummyFetchHandler();
		handler.next = dummyFetchHandler;

		it("Should not redirect for the redirect count equal to maxRedirects", async () => {
			const maxRedirect = 1;
			const options = new RedirectHandlerOptions(maxRedirect);
			const handler = new RedirectHandler(options);
			handler.next = dummyFetchHandler;
			dummyFetchHandler.setResponses([new Response("", { status: 301 }), new Response("ok", { status: 200 }) as any]);
			const response = await handler["executeWithRedirect"](requestUrl, fetchRequestInit, maxRedirect);
			assert.equal(response.status, 301);
		});

		it("Should not redirect for the non redirect response", async () => {
			const options = new RedirectHandlerOptions();
			const handler = new RedirectHandler(options);
			handler.next = dummyFetchHandler;
			dummyFetchHandler.setResponses([new Response("", { status: 200 })] as any);
			const response = await handler["executeWithRedirect"](requestUrl, fetchRequestInit, 0);
			assert.equal(response.status, 200);
		});

		it("Should not redirect for the redirect response without location header", async () => {
			dummyFetchHandler.setResponses([new Response("", { status: 301 }), new Response("ok", { status: 200 }) as any]);
			const response = await handler["executeWithRedirect"](requestUrl, fetchRequestInit, 0);
			assert.equal(response.status, 301);
		});

		it("Should not redirect for shouldRedirect callback returning false", async () => {
			const options = new RedirectHandlerOptions(undefined, () => false);
			const handler = new RedirectHandler(options);
			handler.next = dummyFetchHandler;
			dummyFetchHandler.setResponses([new Response("", { status: 301 }), new Response("ok", { status: 200 }) as any]);
			const response = await handler["executeWithRedirect"](requestUrl, fetchRequestInit, 0);
			assert.equal(response.status, 301);
		});

		it("Should drop body and change method to get for SEE_OTHER status code", async () => {
			dummyFetchHandler.setResponses([
				new Response("", {
					status: 303,
					headers: {
						[RedirectHandler["LOCATION_HEADER"]]: "/location",
					},
				}),
				new Response("ok", { status: 200 }),
			] as any);
			const response = await handler["executeWithRedirect"](requestUrl, fetchRequestInit, 0);
			assert.isUndefined(fetchRequestInit["body"]);
			assert.equal(fetchRequestInit.method, "GET");
			assert.equal(response.status, 200);
		});

		it("Should not drop Authorization header for relative url redirect", async () => {
			const requestUrl = "/me";
			const fetchRequestInit = {
				method: "POST",
				body: "dummy body",
				headers: {
					[RedirectHandler["AUTHORIZATION_HEADER"]]: "Bearer TEST",
				},
			};

			dummyFetchHandler.setResponses([
				new Response("", {
					status: 301,
					headers: {
						[RedirectHandler["LOCATION_HEADER"]]: "/location",
					},
				}),
				new Response("ok", { status: 200 }) as any,
			]);
			const response = await handler["executeWithRedirect"](requestUrl, fetchRequestInit, 0);
			assert.isDefined(fetchRequestInit.headers[RedirectHandler["AUTHORIZATION_HEADER"]]);
			assert.equal(fetchRequestInit.headers[RedirectHandler["AUTHORIZATION_HEADER"]], "Bearer TEST");

			assert.equal(response.status, 200);
		});

		it("Should not drop Authorization header for same authority redirect url", async () => {
			const requestUrl = "https://graph.microsoft.com/v1.0/me";
			const fetchRequestInit = {
				method: "POST",
				body: "dummy body",
				headers: {
					[RedirectHandler["AUTHORIZATION_HEADER"]]: "Bearer TEST",
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
			] as any);
			const response = await handler["executeWithRedirect"](requestUrl, fetchRequestInit, 0);
			assert.isDefined(fetchRequestInit.headers[RedirectHandler["AUTHORIZATION_HEADER"]]);
			assert.equal(response.status, 200);
		});

		it("Should return success response after successful redirect", async () => {
			const requestUrl = "https://graph.microsoft.com/v1.0/me";
			const fetchRequestInit = {
				method: "POST",
				body: "dummy body",
			};

			dummyFetchHandler.setResponses([
				new Response(null, {
					status: 301,
					headers: {
						[RedirectHandler["LOCATION_HEADER"]]: "https://graphredirect.microsoft.com/v1.0/me",
					},
				}),
				new Response("ok", { status: 200 }),
			] as any);
			const response = await handler["executeWithRedirect"](requestUrl, fetchRequestInit, 0);
			assert.equal(response.status, 200);
		});
	});

	describe("execute", async () => {
		it("Should set the redirect value in options to manual", async () => {
			const requestUrl = "/me";
			const fetchRequestInit: RequestInit = {
				method: "GET",
			};

			const dummyFetchHandler = new DummyFetchHandler();
			const handler = new RedirectHandler();
			handler.next = dummyFetchHandler;
			dummyFetchHandler.setResponses([new Response("", { status: 200 })] as any);
			await handler.execute(requestUrl, fetchRequestInit);
			assert.equal(fetchRequestInit.redirect, RedirectHandler["MANUAL_REDIRECT"]);
		});
	});
});
