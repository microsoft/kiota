/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */
import {RequestOption} from "@microsoft/kiota-abstractions"
import { FetchRequestInit, FetchResponse } from "../utils/fetchDefinitions";
// use import types
/** Defines the contract for a middleware in the request execution pipeline. */
export interface Middleware {
	/** Next middleware to be executed. The current middleware must execute it in its implementation. */
	next: Middleware | undefined;

	/**
	 * Main method of the middleware.
	 * @param requestInit The Fetch RequestInit object.
	 * @param url The URL of the request.
	 * @return A promise that resolves to the response object.
	 */
	execute(url: string, requestInit: FetchRequestInit, requestOptions?: Record<string, RequestOption>): Promise<FetchResponse>;
}
