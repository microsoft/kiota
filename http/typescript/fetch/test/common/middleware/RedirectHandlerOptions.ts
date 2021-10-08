/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

import { assert } from "chai";

import { RedirectHandlerOptions } from "../../../src/middlewares/options/redirectHandlerOption";

describe("RedirectHandlerOptions.ts", () => {
	describe("constructor", () => {
		it("Should initialize the instance with given options", () => {
			const shouldRedirect = (response: Response) => {
				if (response.status === 301) {
					return true;
				}
				return false;
			};
			const maxRedirects = 5;
			const options = new RedirectHandlerOptions(maxRedirects, shouldRedirect);
			assert.equal(options.maxRedirects, maxRedirects);
			assert.equal(options.shouldRedirect, shouldRedirect);
		});

		it("Should throw error for setting max redirects more than allowed", () => {
			try {
				// eslint-disable-next-line @typescript-eslint/no-unused-vars
				const options = new RedirectHandlerOptions(100);
				throw new Error("Test Failed - Something wrong with the max redirects value redirection");
			} catch (error) {
				assert.equal(error.name, "MaxLimitExceeded");
			}
		});
		it("Should throw error for setting max redirects to negative", () => {
			try {
				// eslint-disable-next-line @typescript-eslint/no-unused-vars
				const options = new RedirectHandlerOptions(-10);
				throw new Error(" Test Failed - Something wrong with the max redirects value redirection");
			} catch (error) {
				assert.equal(error.name, "MinExpectationNotMet");
			}
		});

		it("Should initialize instance with default options", () => {
			const options = new RedirectHandlerOptions();
			assert.equal(options.maxRedirects, RedirectHandlerOptions["DEFAULT_MAX_REDIRECTS"]);
			assert.equal(options.shouldRedirect, RedirectHandlerOptions["defaultShouldRetry"]);
		});
	});
});
