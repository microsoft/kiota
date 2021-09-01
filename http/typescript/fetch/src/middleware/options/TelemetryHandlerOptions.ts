/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

/**
 * @module TelemetryHandlerOptions
 */

import { Context } from "../../IContext";
import { MiddlewareControl } from "../MiddlewareControl";
import { MiddlewareOptions } from "./IMiddlewareOptions";

/**
 * @enum
 * @property {number} NONE - The hexadecimal flag value for nothing enabled
 * @property {number} REDIRECT_HANDLER_ENABLED - The hexadecimal flag value for redirect handler enabled
 * @property {number} RETRY_HANDLER_ENABLED - The hexadecimal flag value for retry handler enabled
 * @property {number} AUTHENTICATION_HANDLER_ENABLED - The hexadecimal flag value for the authentication handler enabled
 */

export enum FeatureUsageFlag {
	/* eslint-disable  @typescript-eslint/naming-convention */
	NONE = 0x0,
	REDIRECT_HANDLER_ENABLED = 0x1,
	RETRY_HANDLER_ENABLED = 0x2,
	AUTHENTICATION_HANDLER_ENABLED = 0x4,
	/* eslint-enable  @typescript-eslint/naming-convention */
}

/**
 * @class
 * @implements MiddlewareOptions
 * Class for TelemetryHandlerOptions
 */

export class TelemetryHandlerOptions implements MiddlewareOptions {
	/**
	 * @private
	 * A member to hold the OR of feature usage flags
	 */
	private featureUsage: FeatureUsageFlag = FeatureUsageFlag.NONE;

	/**
	 * @public
	 * @static
	 * To update the feature usage in the context object
	 * @param {Context} context - The request context object containing middleware options
	 * @param {FeatureUsageFlag} flag - The flag value
	 * @returns nothing
	 */
	public static updateFeatureUsageFlag(context: Context, flag: FeatureUsageFlag): void {
		let options: TelemetryHandlerOptions;
		if (context.middlewareControl instanceof MiddlewareControl) {
			options = context.middlewareControl.getMiddlewareOptions(TelemetryHandlerOptions) as TelemetryHandlerOptions;
		} else {
			context.middlewareControl = new MiddlewareControl();
		}
		if (typeof options === "undefined") {
			options = new TelemetryHandlerOptions();
			context.middlewareControl.setMiddlewareOptions(TelemetryHandlerOptions, options);
		}
		options.setFeatureUsage(flag);
	}

	/**
	 * @private
	 * To set the feature usage flag
	 * @param {FeatureUsageFlag} flag - The flag value
	 * @returns nothing
	 */
	private setFeatureUsage(flag: FeatureUsageFlag): void {
		this.featureUsage = this.featureUsage | flag;
	}

	/**
	 * @public
	 * To get the feature usage
	 * @returns A feature usage flag as hexadecimal string
	 */
	public getFeatureUsage(): string {
		return this.featureUsage.toString(16);
	}
}
