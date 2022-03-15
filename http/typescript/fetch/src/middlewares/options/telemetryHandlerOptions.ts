import { RequestOption } from "@microsoft/kiota-abstractions";

export interface TelemetryHandlerOptions extends RequestOption {
	telemetryConfigurator: (url: string, requestInit: RequestInit, requestOptions?: Record<string, RequestOption>, telemetryInfomation?: unknown) => void;
	telemetryInfomation: unknown;
}
