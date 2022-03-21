/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */
import { RequestOption } from "@microsoft/kiota-abstractions";

export interface TelemetryHandlerOptions extends RequestOption {
	telemetryConfigurator: (url: string, requestInit: RequestInit, requestOptions?: Record<string, RequestOption>, telemetryInfomation?: unknown) => void;
	telemetryInfomation: unknown;
}
