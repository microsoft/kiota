/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

/**
 * @module FetchHandler
 */

import fetch from "cross-fetch";
import { Context } from "./Context";
import { Middleware } from "./middlewares/middleware";

/**
 * @class
 * @implements Middleware
 * Class for HTTPMessageHandler
 */
export class FetchHandler implements Middleware {
    next = null;
    constructor(private customFetch: (context) => Promise<Response>) {

    }
    /**
     * @public
     * @async
     * To execute the current middleware
     * @param {Context} context - The request context object
     * @returns A promise that resolves to nothing
     */
    public async execute(context: Context): Promise<void> {
        if (this.customFetch) {
            context.response = await this.customFetch(context);
        }
        else {
            context.response = await fetch(context.request);
        }
        return;
    }


    // Consider case where no middleware and just http called?
    
}
