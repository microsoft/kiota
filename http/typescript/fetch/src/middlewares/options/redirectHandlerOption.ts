/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

/**
 * @module RedirectHandlerOptions
 */

import { RequestOption } from "@microsoft/kiota-abstractions";

/**
 * @type
 * A type declaration for shouldRetry callback
 */
export type ShouldRedirect = (response: Response) => boolean;

/**
 * @class
 * @implements MiddlewareOptions
 * A class representing RedirectHandlerOptions
 */
export class RedirectHandlerOptions implements RequestOption {
    /**
     * @private
     * @static
     * A member holding default max redirects value
     */
    private static DEFAULT_MAX_REDIRECTS = 5;

    /**
     * @private
     * @static
     * A member holding maximum max redirects value
     */
    private static MAX_MAX_REDIRECTS = 20;

    /**
     * @public
     * A member holding max redirects value
     */
    public maxRedirects: number;

    /**
     * @public
     * A member holding shouldRedirect callback
     */
    public shouldRedirect: ShouldRedirect;

    /**
     * @private
     * A member holding default shouldRedirect callback
     */
    private static defaultShouldRetry: ShouldRedirect = () => true;

    /**
     * @public
     * @constructor
     * To create an instance of RedirectHandlerOptions
     * @param {number} [maxRedirects = RedirectHandlerOptions.DEFAULT_MAX_REDIRECTS] - The max redirects value
     * @param {ShouldRedirect} [shouldRedirect = RedirectHandlerOptions.DEFAULT_SHOULD_RETRY] - The should redirect callback
     * @returns An instance of RedirectHandlerOptions
     */
    public constructor(maxRedirects: number = RedirectHandlerOptions.DEFAULT_MAX_REDIRECTS, shouldRedirect: ShouldRedirect = RedirectHandlerOptions.defaultShouldRetry) {
        if (maxRedirects > RedirectHandlerOptions.MAX_MAX_REDIRECTS) {
            const error = new Error(`MaxRedirects should not be more than ${RedirectHandlerOptions.MAX_MAX_REDIRECTS}`);
            error.name = "MaxLimitExceeded";
            throw error;
        }
        if (maxRedirects < 0) {
            const error = new Error(`MaxRedirects should not be negative`);
            error.name = "MinExpectationNotMet";
            throw error;
        }
        this.maxRedirects = maxRedirects;
        this.shouldRedirect = shouldRedirect;
    }

    public getKey():string {
    // TODO
    return "";
    }
}

