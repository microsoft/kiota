/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */
import { RequestOption } from "@microsoft/kiota-abstractions";

import { Middleware } from "./middleware";
import { TelemetryHandlerOptions } from "./options/telemetryHandlerOptions";

export const TelemetryHandlerOptionsKey = "TelemetryHandlerOptionsKey";
export class TelemetryHandler implements Middleware {
	constructor(private telemetryHandlerOptions: TelemetryHandlerOptions) {}
	next: Middleware | undefined;
	execute(url: string, requestInit: RequestInit, requestOptions?: Record<string, RequestOption>): Promise<Response> {
		if (this.telemetryHandlerOptions && this.telemetryHandlerOptions.telemetryConfigurator) {
			this.telemetryHandlerOptions.telemetryConfigurator(url, requestInit, requestOptions, this.telemetryHandlerOptions.telemetryInfomation);
		} else if (requestOptions && requestOptions[TelemetryHandlerOptionsKey]) {
			(requestOptions[TelemetryHandlerOptionsKey] as TelemetryHandlerOptions).telemetryConfigurator(url, requestInit, requestOptions);
		}
		if (!this.next) {
			throw new Error("Please set the next middleware to continue the request");
		}
		return this.next.execute(url, requestInit, requestOptions);
	}
}
