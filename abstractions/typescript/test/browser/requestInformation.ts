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

//     it("Should set request information uri", () => {
// 		const requestInformation = new RequestInformation();
// 		const currentPath = "CURRENT_PATH";
// 		const pathSegment = "PATH_SEGMENT";
// 		requestInformation.URL = new URL(currentPath, pathSegment) as unknown;
// 		assert.isNotNull(URL);
// 		console.log(requestInformation.URL);
// 		assert.equal(requestInformation.URL, new URL(currentPath, pathSegment));
// 	});
});
