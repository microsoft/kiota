import { RequestOption } from "@microsoft/kiota-abstractions";
import { TelemetryHandlerOptions } from "./options/telemetryHandlerOptions";
import { Middleware } from "./middleware";

export const TelemetryHandlerOptionsKey = "TelemetryHandlerOptionsKey";
export class TelemetryHandler implements Middleware {
    constructor(private telemetryHandlerOptions: TelemetryHandlerOptions) { };
    next: Middleware;
    execute(url: string, requestInit: RequestInit, requestOptions?: Record<string, RequestOption>): Promise<Response> {

        if (this.telemetryHandlerOptions && this.telemetryHandlerOptions.telemetryConfigurator) {
            console.log("tell tell");
            this.telemetryHandlerOptions.telemetryConfigurator(url, requestInit, requestOptions, this.telemetryHandlerOptions.telemetryInfomation);
        }
        else if (requestOptions[TelemetryHandlerOptionsKey]){
            (requestOptions[TelemetryHandlerOptionsKey] as TelemetryHandlerOptions).telemetryConfigurator(url, requestInit, requestOptions);
        }
        return this.next.execute(url, requestInit, requestOptions);
    }
};