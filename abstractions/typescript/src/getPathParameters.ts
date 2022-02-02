import { RequestInformation } from "./requestInformation";

export function getPathParameters(parameters: Record<string, unknown> | string | undefined) : Record<string, unknown> {
    const result:  Record<string, unknown> = {};
    if(typeof parameters === "string") {
        result[RequestInformation.raw_url_key] = parameters;
    } else if(parameters) {
        for(const key in parameters){
            result[key]= parameters[key];
        };
    }
    return result;
}
