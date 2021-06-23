import { AuthenticationProvider, HttpCore as IHttpCore, Parsable, ParseNodeFactory, RequestInfo, ResponseHandler, ParseNodeFactoryRegistry } from '@microsoft/kiota-abstractions';
import { fetch, Headers as FetchHeadersCtor } from 'cross-fetch';
import { ReadableStream } from 'web-streams-polyfill';
import { URLSearchParams } from 'url';
export class HttpCore implements IHttpCore {
    private static readonly authorizationHeaderKey = "Authorization";
    /**
     *
     */
    public constructor(public readonly authenticationProvider: AuthenticationProvider, public readonly parseNodeFactory: ParseNodeFactory = ParseNodeFactoryRegistry.defaultInstance) {
        if(!authenticationProvider) {
            throw new Error('authentication provider cannot be null');
        }
    }
    private getResponseContentType = (response: Response): string | undefined => {
        const header = response.headers.get("content-type")?.toLowerCase();
        if(!header) return undefined;
        const segments = header.split(';');
        if(segments.length === 0) return undefined;
        else return segments[0];
    }
    public sendAsync = async <ModelType extends Parsable>(requestInfo: RequestInfo, type: new() => ModelType, responseHandler: ResponseHandler | undefined): Promise<ModelType> => {
        if(!requestInfo) {
            throw new Error('requestInfo cannot be null');
        }
        await this.addBearerIfNotPresent(requestInfo);
        
        const request = this.getRequestFromRequestInfo(requestInfo);
        const response = await fetch(this.getRequestUrl(requestInfo), request);
        if(responseHandler) {
            return await responseHandler.handleResponseAsync(response);
        } else {
            const payload = await response.arrayBuffer();
            const responseContentType = this.getResponseContentType(response);
            if(!responseContentType)
                throw new Error("no response content type found for deserialization");
            
            const rootNode = this.parseNodeFactory.getRootParseNode(responseContentType, payload);
            const result = rootNode.getObjectValue(type);
            return result as unknown as ModelType;
        }
    }
    public sendPrimitiveAsync = async <ResponseType>(requestInfo: RequestInfo, responseType: "string" | "number" | "boolean" | "Date" | "ReadableStream", responseHandler: ResponseHandler | undefined): Promise<ResponseType> => {
        if(!requestInfo) {
            throw new Error('requestInfo cannot be null');
        }
        await this.addBearerIfNotPresent(requestInfo);
        
        const request = this.getRequestFromRequestInfo(requestInfo);
        const response = await fetch(this.getRequestUrl(requestInfo), request);
        if(responseHandler) {
            return await responseHandler.handleResponseAsync(response);
        } else {
            switch(responseType) {
                case "ReadableStream":
                    const buffer =  await response.arrayBuffer();
                    let bufferPulled = false;
                    const stream = new ReadableStream({
                        pull: (controller) => {
                            if(!bufferPulled) {
                                controller.enqueue(buffer.slice(0))
                                bufferPulled = true;
                            }
                        },
                    });
                    return stream as unknown as ResponseType;
                case 'string':
                case 'number':
                case 'boolean':
                case 'Date':
                    const payload = await response.arrayBuffer();
                    const responseContentType = this.getResponseContentType(response);
                    if(!responseContentType)
                        throw new Error("no response content type found for deserialization");
                    
                    const rootNode = this.parseNodeFactory.getRootParseNode(responseContentType, payload);
                    if(responseType === 'string') {
                        return rootNode.getStringValue() as unknown as ResponseType;
                    } else if (responseType === 'number') {
                        return rootNode.getNumberValue() as unknown as ResponseType;
                    } else if(responseType === 'boolean') {
                        return rootNode.getBooleanValue() as unknown as ResponseType;
                    } else if (responseType === 'Date') {
                        return rootNode.getDateValue() as unknown as ResponseType;
                    } else {
                        throw new Error("unexpected type to deserialize");
                    }
            }
        }
    }
    public sendNoResponseContentAsync = async (requestInfo: RequestInfo, responseHandler: ResponseHandler | undefined): Promise<void> => {
        if(!requestInfo) {
            throw new Error('requestInfo cannot be null');
        }
        await this.addBearerIfNotPresent(requestInfo);
        
        const request = this.getRequestFromRequestInfo(requestInfo);
        const response = await fetch(this.getRequestUrl(requestInfo), request);
        if(responseHandler) {
            return await responseHandler.handleResponseAsync(response);
        }
    }
    private addBearerIfNotPresent = async (requestInfo: RequestInfo): Promise<void> => {
        if(!requestInfo.URI) {
            throw new Error('uri cannot be null');
        }
        if(!requestInfo.headers?.has(HttpCore.authorizationHeaderKey)) {
            const token = await this.authenticationProvider.getAuthorizationToken(requestInfo.URI);
            if(!token) {
                throw new Error('Could not get an authorization token');
            }
            if(!requestInfo.headers) {
                requestInfo.headers = new Map<string, string>();
            }
            requestInfo.headers?.set(HttpCore.authorizationHeaderKey, `Bearer ${token}`);
        }
    }
    private getRequestFromRequestInfo = (requestInfo: RequestInfo): RequestInit => {
        const request = {
            method: requestInfo.httpMethod?.toString(),
            headers: new FetchHeadersCtor(),
            body: requestInfo.content,
        } as RequestInit;
        requestInfo.headers?.forEach((v, k) => (request.headers as Headers).set(k, v));
        return request;
    }
    private getRequestUrl = (requestInfo: RequestInfo) : string => {
        let url = requestInfo.URI ?? '';
        if(requestInfo.queryParameters?.size ?? -1 > 0) {
            const queryParametersBuilder = new URLSearchParams();
            requestInfo.queryParameters?.forEach((v, k) => {
                queryParametersBuilder.append(k, `${v}`);
            });
            url = url + '?' + queryParametersBuilder.toString();
        }
        return url;
    }

}