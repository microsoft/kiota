/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

import { assert } from "chai";

import { RequestInformation } from "../../src/";

describe("RequestInformation", () => {
	it("Should set request information uri", () => {
		const requestInformation = new RequestInformation();
		const browserReadableStream = new ReadableStream();
		requestInformation.setStreamContent(browserReadableStream);

		assert.equal(requestInformation["content"], browserReadableStream);
	});
});
