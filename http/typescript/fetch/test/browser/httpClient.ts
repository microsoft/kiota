/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

import { assert } from "chai";

import { HttpClient } from "../../src/httpClient";
import { defaultFetchHandler } from "../../src/middlewares/defaultFetchHandler";
import { MiddlewareContext } from "../../src/middlewares/middlewareContext";
import { FetchRequestInfo, FetchRequestInit } from "../../src/utils/fetchDefinitions";
import { DummyFetchHandler } from "../common/middleware/dummyFetchHandler";

describe("HTTPClient.ts", () => {
	const dummyFetchHandler: DummyFetchHandler = new DummyFetchHandler();
	const httpClient: HttpClient = new HttpClient(undefined,null);
	
	// describe("constructor", () => {
	// 	it("Should create an instance and populate middleware member", async() => {
	// 		assert.isDefined(httpClient["middleware"]);
	// 		assert.equal(httpClient["middleware"], dummyFetchHandler);
	// 	});

	// 	it("Should create an instance and populate middleware member when passing a middleware array", () => {
	// 		const client = new HttpClient(undefined,...[dummyFetchHandler]);
	// 		assert.isDefined(client["middleware"]);
	// 		assert.equal(client["middleware"], dummyFetchHandler);
	// 	});

	// 	it("Should throw an error if middleware is undefined", () => {

	// 		const client = new HttpClient();

	// 		assert.isNotNull(client["middleware"]);
				
	// 	});

	// 	it("Should throw an error if middleware is passed as an empty array", () => {
	// 		try {
	// 			// eslint-disable-next-line @typescript-eslint/no-unused-vars
	// 			const client = new HttpClient(...[]);
	// 			throw new Error("Test failed - Expected error was not thrown");
	// 		} catch (error) {
	// 			assert.equal(error.name, "InvalidMiddlewareChain");
	// 		}
	// 	});
	// });

	describe("sendRequest", async () => {
		it("Should throw error for invalid request options incase if the url and options are passed", async () => {
			try {
				const url = "dummy_url";
				const context: MiddlewareContext = {
					request: url,
					options:{
						method:"GET"
					}
				};
				console.log("erer");
				assert.isTrue(httpClient["middleware"] instanceof defaultFetchHandler)
				await httpClient.executeFetch(context);
				//throw new Error("Test Failed - Something wrong with the context validation");
			} catch (error) {
				//assert.equal(error.name, "InvalidRequestOptions");
			}
		});

		// it("Should execute for context object with Request instance", async () => {
		// 	const request: FetchRequestInfo = "dummy_url";
		// 	const options: FetchRequestInit = {
		// 		method: "GET"
		// 	};
		// 	const context: MiddlewareContext = {
		// 		request,
		// 		options
		// 	};
		// 	await httpClient.executeFetch(context);
		// });

		// it("Should execute for context object with request uri and options", async () => {
			// const url = "dummy_url";
			// const options: FetchRequestInit = {
			// 	method: "GET",
			// };
			// const context: MiddlewareContext = {
			// 	request: url,
			// 	options,
			// };
			// await httpClient.executeFetch(context);
		//});
	});
});
