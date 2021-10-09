/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */
import { assert } from "chai";

import { MiddlewareContext } from "../../../src/middlewares/middlewareContext";
import { FetchRequestInit } from "../../../src/utils/fetchDefinitions";
import { MiddlewareControl } from "../../../src/middlewares/middlewareControl";
import { RetryHandlerOptions, ShouldRetry } from "../../../src/middlewares/options/retryHandlerOptions";
import { RetryHandler } from "../../../src/middlewares/RetryHandler";
import { DummyFetchHandler } from "./dummyFetchHandler";
import { getResponse } from "../testUtils"

var Response = Response;
if (typeof Response != "object") {
	Response = getResponse();

}
describe("RetryHandler.ts", function () {
	this.timeout(20 * 1000);
	const retryHandler = new RetryHandler();
	const retryHandlerOptions = new RetryHandlerOptions();
	const tooManyRequestsResponseWithRetryAfterDelay = new Response("", {
		status: 429,
		statusText: "TooManyRequests",
		headers: {
			"Retry-After": "10",
		},
	});
	const tooManyRequestsResponseWithRetyAfterDate = new Response("", {
		status: 429,
		statusText: "TooManyRequests",
		headers: {
			"Retry-After": new Date(Date.now() + 10000).toUTCString(),
		},
	});
	const serviceUnavailableResponse = new Response("", {
		status: 503,
		statusText: "ServiceUnavailable",
	});
	const gatewayTimeoutResponse = new Response("", {
		status: 504,
		statusText: "GatewayTimeout",
	});
	const nonRetryResponse = new Response("", {
		status: 200,
		statusText: "OK",
	});

	describe("constructor", () => {
		it("Should set the option member with retryHanderOptions", () => {
			const handler = new RetryHandler(retryHandlerOptions);
			assert.isDefined(handler["options"]);
		});

		it("Should create retryHandler instance with default retryHandlerOptions", () => {
			const handler = new RetryHandler();
			assert.isDefined(handler["options"]);
		});
	});

	describe("isRetry", () => {
		it("Should return true for 429 response", () => {
			assert.isTrue(retryHandler["isRetry"](tooManyRequestsResponseWithRetryAfterDelay));
		});

		it("Should return true for 503 response", () => {
			assert.isTrue(retryHandler["isRetry"](serviceUnavailableResponse));
		});

		it("Should return true for 504 response", () => {
			assert.isTrue(retryHandler["isRetry"](gatewayTimeoutResponse));
		});

		it("Should return false for non retry response", () => {
			assert.isFalse(retryHandler["isRetry"](nonRetryResponse));
		});
	});

	describe("isBuffered", () => {
		const url = "dummy_url";
		it("Should succeed for non post, patch, put requests", () => {
			const options: FetchRequestInit = {
				method: "GET",
			};
			assert.isTrue(retryHandler["isBuffered"](url, options));
		});

		it("Should succeed for post request with non stream request", () => {
			const options: FetchRequestInit = {
				method: "POST",
				body: "test",
			};
			assert.isTrue(retryHandler["isBuffered"](url, options));
		});

		it("Should fail for stream request", () => {
			const options: FetchRequestInit = {
				method: "PUT",
				headers: {
					"Content-Type": "application/octet-stream",
				},
			};
			assert.isFalse(retryHandler["isBuffered"](url, options));
		});
	});

	describe("getDelay", () => {
		it("Should return retry delay from the response header", () => {
			const delay = retryHandler["getDelay"](tooManyRequestsResponseWithRetryAfterDelay, 1, 5);
			assert.equal(delay, 10);
		});

		it("Should return retry delay from the response header mentioning delay time", () => {
			const delay = retryHandler["getDelay"](tooManyRequestsResponseWithRetyAfterDate, 1, 5);
			assert.isDefined(delay);
		});

		it("Should return delay without exponential backoff", () => {
			const delay = retryHandler["getDelay"](gatewayTimeoutResponse, 1, 10);
			assert.isAbove(delay, 10);
			assert.isBelow(delay, 11);
		});

		it("Should return delay with exponential backoff", () => {
			const delay = retryHandler["getDelay"](gatewayTimeoutResponse, 2, 10);
			assert.isAbove(delay, 12);
			assert.isBelow(delay, 13);
		});

		it("Should return max delay for if the calculated delay is more", () => {
			const delay = retryHandler["getDelay"](gatewayTimeoutResponse, 10, 100);
			assert.isAbove(delay, 180);
			assert.isBelow(delay, 181);
		});
	});

	describe("getExponentialBackOffTime", () => {
		it("Should return 0 delay for 0th attempt i.e for a fresh request", () => {
			const time = retryHandler["getExponentialBackOffTime"](0);
			assert.equal(time, 0);
		});

		it("Should return attempt time", () => {
			const time = retryHandler["getExponentialBackOffTime"](1);
			assert.equal(time, 1);
		});
	});

	describe("sleep", async () => {
		it("Should run the sleep method for 1 second", async () => {
			await retryHandler["sleep"](1);
		});
	});

	describe("getOptions", () => {
		it("Should return the options in the context object", () => {
			const delay = 10;
			const maxRetries = 8;
			const shouldRetry: ShouldRetry = () => false;
			const options = new RetryHandlerOptions(delay, maxRetries, shouldRetry);
			const cxt: MiddlewareContext = {
				request: "url",
				middlewareControl: new MiddlewareControl([options]),
			};
			const o = retryHandler["getOptions"](cxt);
			assert.equal(o.delay, delay);
			assert.equal(o.maxRetries, maxRetries);
			assert.equal(o.shouldRetry, shouldRetry);
		});

		it("Should return the default set of options in the middleware", () => {
			const cxt: MiddlewareContext = {
				request: "url",
			};
			const o = retryHandler["getOptions"](cxt);
			assert.equal(o.delay, retryHandler["options"].delay);
			assert.equal(o.maxRetries, retryHandler["options"].maxRetries);
			assert.equal(o.shouldRetry, retryHandler["options"].shouldRetry);
		});
	});

	describe("executeWithRetry", async () => {
		const options = new RetryHandlerOptions();
		const handler = new RetryHandler(options);
		const dummyFetchHandler = new DummyFetchHandler();
		handler.next = dummyFetchHandler;
		const cxt: MiddlewareContext = {
			request: "url",
			options: {
				method: "GET",
			},
		};
		it("Should return non retried response incase of maxRetries busted out", async () => {
			dummyFetchHandler.setResponses([new Response(null, { status: 429 }), new Response("ok", { status: 200 })]);
			await handler["executeWithRetry"](cxt, RetryHandlerOptions["MAX_MAX_RETRIES"], options);
			assert.equal(cxt.response.status, 429);
		});

		it("Should return succeeded response for non retry response", async () => {
			dummyFetchHandler.setResponses([new Response("ok", { status: 200 })]);
			await handler["executeWithRetry"](cxt, 0, options);
			assert.equal(cxt.response.status, 200);
		});

		it("Should return non retried response for streaming request", async () => {
			const c: MiddlewareContext = {
				request: "url",
				options: {
					method: "POST",
					headers: {
						"Content-Type": "application/octet-stream",
					},
				},
			};
			dummyFetchHandler.setResponses([new Response(null, { status: 429 }), new Response("ok", { status: 200 })]);
			await handler["executeWithRetry"](c, 0, options);
			assert.equal(c.response.status, 429);
		});

		it("Should successfully retry and return ok response", async () => {
			const opts = new RetryHandlerOptions(1);
			dummyFetchHandler.setResponses([new Response(null, { status: 429 }), new Response(null, { status: 429 }), new Response("ok", { status: 200 })]);
			await handler["executeWithRetry"](cxt, 0, opts);
			assert.equal(cxt.response.status, 200);
		});

		it("Should fail by exceeding max retries", async () => {
			const opts = new RetryHandlerOptions(1, 2);
			dummyFetchHandler.setResponses([new Response(null, { status: 429 }), new Response(null, { status: 429 }), new Response(null, { status: 429 }), new Response("ok", { status: 200 })]);
			await handler["executeWithRetry"](cxt, 0, opts);
			assert.equal(cxt.response.status, 429);
		});
	});
});
