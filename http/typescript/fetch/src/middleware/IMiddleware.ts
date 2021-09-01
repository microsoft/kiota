/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

import { Context } from "../IContext";

/**
 * @interface
 * @property {Function} execute - The method to execute the middleware
 * @property {Function} [setNext] - A method to set the next middleware in the chain
 */
export interface Middleware {
	execute: (context: Context) => Promise<void>;
	setNext?: (middleware: Middleware) => void;
}
