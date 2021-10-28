/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */
/**
 * Interface accepting request options
 * to be passed in RequestInformaton object
 */
export interface RequestOption {
	/** Gets the option key for when adding it to a request. Must be unique. */
	getKey(): string;
}
