/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

import { RequestOption } from "@microsoft/kiota-abstractions";

import { FetchRequestInfo, FetchRequestInit } from "../utils/fetchDefinitions";

/**
 * @interface
 * @property {RequestInfo} request - The request url string or the Request instance
 * @property {RequestInit} [options] - The options for the request
 * @property {Response} [response] - The response content
 *
 */

export interface MiddlewareContext {
	requestUrl: FetchRequestInfo;
	fetchRequestInit?: FetchRequestInit;
	requestInformationOptions?: Record<string, RequestOption>; // this can get updated depending on the use of request options
}
