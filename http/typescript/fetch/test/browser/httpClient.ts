/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

import { assert } from "chai";

import { CustomFetchHandler, defaultFetchHandler, HttpClient, RetryHandler } from "../../src";
import { DummyFetchHandler } from "../common/middleware/dummyFetchHandler";

describe("HTTPClient.ts", () => {
	describe("constructor", () => {
		const dummyFetchHandler: DummyFetchHandler = new DummyFetchHandler();

		const dummyCustomFetch = (): Promise<Response> => {
			return null;
		};
		it("Should create an instance and populate middleware member", async () => {
			const httpClient: HttpClient = new HttpClient(undefined, dummyFetchHandler);
			assert.isDefined(httpClient["middleware"]);
			assert.equal(httpClient["middleware"], dummyFetchHandler);
		});

		it("Should create an instance and populate middleware member when passing a middleware array", () => {
			const client = new HttpClient(undefined, ...[dummyFetchHandler]);
			assert.isDefined(client["middleware"]);
			assert.equal(client["middleware"], dummyFetchHandler);
		});

		it("Should set default middleware array if middleware parameter is undefined && customFetch is undefined", () => {
			const client = new HttpClient();

			assert.isNotNull(client["middleware"]);
			const next = client["middleware"].next;

			assert.isTrue(client["middleware"] instanceof RetryHandler);
			assert.isTrue(next instanceof defaultFetchHandler);
		});

		it("Should set default middleware array with customFetchHandler if middleware parameter is undefined && customFetch is defined", () => {
			const client = new HttpClient(dummyCustomFetch);

			assert.isNotNull(client["middleware"]);
			assert.isNotNull(client[""]);

			const next = client["middleware"].next;

			assert.isTrue(client["middleware"] instanceof RetryHandler);
			assert.isTrue(next instanceof CustomFetchHandler);
		});

		it("Should set to default fetch handler middleware array if middleware parameter is null && customFetch is undefined", () => {
			const client = new HttpClient(undefined, null);

			assert.isNotNull(client["middleware"]);

			assert.isTrue(client["middleware"] instanceof defaultFetchHandler);
		});

		it("Should only set custom fetch if middleware parameter is null && customFetch is defined", () => {
			const client = new HttpClient(dummyCustomFetch, null);

			assert.isUndefined(client["middleware"]);
			assert.equal(client["customFetch"], dummyCustomFetch);
		});
	});

	// describe("sendRequest", async () => {
	// 	it("Should throw error for invalid request options incase if the url and options are passed", async () => {
	// 		try {
	// 			const url = "dummy_url";
	// 			const context: MiddlewareContext = {
	// 				request: url,
	// 				options: {
	// 					method: "GET"
	// 				}
	// 			};

	// 			assert.isTrue(httpClient["middleware"] instanceof defaultFetchHandler)
	// 			await httpClient.executeFetch(context);
	// 			throw new Error("Test Failed - Something wrong with the context validation");
	// 		} catch (error) {
	// 			assert.equal(error.name, "InvalidRequestOptions");
	// 		}
	// 	});

	// 	it("Should execute for context object with Request instance", async () => {
	// 		const request: FetchRequestInfo = "dummy_url";
	// 		const options: FetchRequestInit = {
	// 			method: "GET"
	// 		};
	// 		const context: MiddlewareContext = {
	// 			request,
	// 			options
	// 		};
	// 		await httpClient.executeFetch(context);
	// 	});

	// 	it("Should execute for context object with request uri and options", async () => {
	// 		const url = "dummy_url";
	// 		const options: FetchRequestInit = {
	// 			method: "GET",
	// 		};
	// 		const context: MiddlewareContext = {
	// 			request: url,
	// 			options,
	// 		};
	// 		await httpClient.executeFetch(context);
	// 	});
	// });
});
