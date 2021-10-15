/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

import { assert } from "chai";

import { FetchRequestInit } from "../../../src/utils/fetchDefinitions";
import { appendRequestHeader, getRequestHeader, setRequestHeader } from "../../../src/utils/headersUtil";

describe("HeaderUtil", async () => {
	describe("getRequestHeader", () => {
		const value = "application/json";
		const url = "dummy_url";

		it("Should get header from headers object", () => {
			const options: FetchRequestInit = {
				method: "test",
				headers: {
					version: "version",
					"Content-Type": value,
				},
			};
			const headerValue: string = getRequestHeader(options, "Content-Type");
			const headerVersion: string = getRequestHeader(options, "version");
			assert.equal(headerValue, value);
			assert.equal(headerVersion, "version");
		});

		it("Should get header from record of headers", () => {
			const options: FetchRequestInit = {
				method: "test",
				headers: {
					version: "version",
					"Content-Type": value,
				},
			};
			const headerValue: string = getRequestHeader(options, "Content-Type");
			const headerVersion: string = getRequestHeader(options, "version");
			assert.equal(headerValue, value);
			assert.equal(headerVersion, "version");
		});
	});

	describe("setRequestHeader", () => {
		const key = "Content-Type";
		const value = "application/json";
		const url = "dummy_url";

		it("Should set header for undefined headers", () => {
			const options: FetchRequestInit = {
				method: "test",
			};
			setRequestHeader(options, key, value);
			assert.isDefined(options.headers);
			assert.equal(options.headers[key], value);
		});

		it("Should set header for empty headers", () => {
			const options: FetchRequestInit = {
				method: "test",
				headers: {}
			};
			setRequestHeader(options, key, value);
			assert.isDefined(options.headers);
			assert.equal(options.headers[key], value);
		});

		it("Should set header in headers object", () => {
			const options: FetchRequestInit = {
				method: "test",
				headers: {
					version: "version",
				},
			};
			setRequestHeader(options, key, value);
			assert.equal(options.headers[key], value);
		});

		it("Should replace header in headers object if header is already present", () => {
			const options: FetchRequestInit = {
				method: "test",
				headers: {
					version: "version",
					[key]: value,
				},
			};
			setRequestHeader(options, key, value);
			assert.equal(options.headers[key], value);
		});
	});

	describe("appendRequestHeader", () => {
		const key = "Content-Type";
		const value = "application/json";
		const firstValue = "text/html";
		const url = "dummy_url";

		it("Should set header for empty headers", () => {
			const options: FetchRequestInit = {
				method: "test",
			};
			appendRequestHeader(options, key, value);
			assert.isDefined(options.headers);
			assert.equal(options.headers[key], value);
		});

		it("Should set header in headers object if header is not present", () => {
			const options: FetchRequestInit = {
				method: "test",
				headers: {
					version: "version",
				},
			};
			appendRequestHeader(options, key, value);
			assert.equal(options.headers[key], value);
		});

		it("Should append header in headers object", () => {
			const options: FetchRequestInit = {
				method: "test",
				headers: {
					version: "version",
					[key]: firstValue,
				},
			};
			appendRequestHeader(options, key, value);
			assert.equal(options.headers[key], `${firstValue}, ${value}`);
		});

		it("Should append header in headers object even if the value is duplicate", () => {
			const options: FetchRequestInit = {
				method: "test",
				headers: {
					version: "version",
					[key]: value,
				},
			};
			appendRequestHeader(options, key, value);
			assert.equal(options.headers[key], `${value}, ${value}`);
		});
	});
});
