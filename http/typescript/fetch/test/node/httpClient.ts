/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

import { assert } from "chai";

import { CustomFetchHandler, DefaultFetchHandler, HttpClient, RedirectHandler, RetryHandler } from "../../src";
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
			assert.isTrue(next instanceof RedirectHandler);
			assert.isTrue(next.next instanceof DefaultFetchHandler);
		});

		it("Should set default middleware array with customFetchHandler if middleware parameter is undefined && customFetch is defined", () => {
			const client = new HttpClient(dummyCustomFetch);

			assert.isNotNull(client["middleware"]);
			assert.isNotNull(client[""]);

			const next = client["middleware"].next;

			assert.isTrue(client["middleware"] instanceof RetryHandler);
			assert.isTrue(next instanceof RedirectHandler);
			assert.isTrue(next.next instanceof CustomFetchHandler);
		});

		it("Should set to default fetch handler middleware array if middleware parameter is null && customFetch is undefined", () => {
			const client = new HttpClient(undefined, null);

			assert.isNotNull(client["middleware"]);

			assert.isTrue(client["middleware"] instanceof DefaultFetchHandler);
		});

		it("Should only set custom fetch if middleware parameter is null && customFetch is defined", () => {
			const client = new HttpClient(dummyCustomFetch, null);

			assert.isUndefined(client["middleware"]);
			assert.equal(client["customFetch"], dummyCustomFetch);
		});
	});
});
