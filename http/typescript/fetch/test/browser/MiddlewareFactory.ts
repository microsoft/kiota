/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

import { assert } from "chai";

import { RetryHandler, defaultFetchHandler, MiddlewareFactory } from "../../src/browser";

describe("MiddlewareFactory", () => {
	it("Should return the default pipeline", () => {

		const defaultMiddleWareArray = MiddlewareFactory.getDefaultMiddlewareChain();

		assert.equal(defaultMiddleWareArray.length,2);
		assert.isTrue(defaultMiddleWareArray[0] instanceof RetryHandler);
		assert.isTrue(defaultMiddleWareArray[1] instanceof defaultFetchHandler);
	});
});
