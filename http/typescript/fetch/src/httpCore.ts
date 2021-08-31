import { AuthenticationProvider, BackingStoreFactory, BackingStoreFactorySingleton, HttpCore as IHttpCore, Parsable, ParseNodeFactory, RequestInfo, ResponseHandler, ParseNodeFactoryRegistry, enableBackingStoreForParseNodeFactory, SerializationWriterFactoryRegistry, enableBackingStoreForSerializationWriterFactory, SerializationWriterFactory } from '@microsoft/kiota-abstractions';
import { Headers as FetchHeadersCtor } from 'cross-fetch';
import { ReadableStream } from 'web-streams-polyfill';
import { URLSearchParams } from 'url';
import { HttpClient } from './httpClient';
import { Middleware } from "./middleware";
import { FetchOptions } from './fetchOptions';

export interface RequestBuilderConfig {
    parseNodeFactory: ParseNodeFactory,
    serializationWriterFactory: SerializationWriterFactory
}

export interface CoreConfig {
    authenticationProvider: AuthenticationProvider,
    httpClient?: HttpClient,
    middleware?: Middleware | Middleware[],
    fetchOptions?: FetchOptions
}

interface CoreOptions {
    authenticationProvider: AuthenticationProvider,
    httpClient: HttpClient,
    fetchOptions?: FetchOptions
}

export class HttpCore implements IHttpCore {
    public getSerializationWriterFactory(): SerializationWriterFactory {
        return this.requestBuilderConfig.serializationWriterFactory;
    }
    /**
     * @private
     * @constructor
     * Instantiates a new http core service
     */
    private constructor(private requestBuilderConfig: RequestBuilderConfig = { parseNodeFactory: ParseNodeFactoryRegistry.defaultInstance, serializationWriterFactory: SerializationWriterFactoryRegistry.defaultInstance }, private coreOptions: CoreOptions) {
        if (!coreOptions.authenticationProvider) {
            throw new Error('authentication provider cannot be null');
        }
        if (!requestBuilderConfig.parseNodeFactory) {
            throw new Error('parse node factory cannot be null');
        }
        if (!requestBuilderConfig.serializationWriterFactory) {
            throw new Error('serialization writer factory cannot be null');
        }
        if (!coreOptions.httpClient) {
            throw new Error('http client cannot be null');
        }

    }
    private getResponseContentType = (response: Response): string | undefined => {
        const header = response.headers.get("content-type")?.toLowerCase();
        if (!header) return undefined;
        const segments = header.split(';');
        if (segments.length === 0) return undefined;
        else return segments[0];
    }
    public sendCollectionAsync = async <ModelType extends Parsable>(requestInfo: RequestInfo, type: new () => ModelType, responseHandler: ResponseHandler | undefined): Promise<ModelType[]> => {
        if (!requestInfo) {
            throw new Error('requestInfo cannot be null');
        }
        await this.coreOptions.authenticationProvider.authenticateRequest(requestInfo);

        const request = this.getRequestFromRequestInfo(requestInfo);
        const response = await this.coreOptions.httpClient.fetch(this.getRequestUrl(requestInfo), request);
        if (responseHandler) {
            return await responseHandler.handleResponseAsync(response);
        } else {
            const payload = await response.arrayBuffer();
            const responseContentType = this.getResponseContentType(response);
            if (!responseContentType)
                throw new Error("no response content type found for deserialization");

            const rootNode = this.requestBuilderConfig.parseNodeFactory.getRootParseNode(responseContentType, payload);
            const result = rootNode.getCollectionOfObjectValues(type);
            return result as unknown as ModelType[];
        }
    }
    public sendAsync = async <ModelType extends Parsable>(requestInfo: RequestInfo, type: new () => ModelType, responseHandler: ResponseHandler | undefined): Promise<ModelType> => {
        if (!requestInfo) {
            throw new Error('requestInfo cannot be null');
        }
        await this.coreOptions.authenticationProvider.authenticateRequest(requestInfo);

        const request = this.getRequestFromRequestInfo(requestInfo);
        const response = await this.coreOptions.httpClient.fetch(this.getRequestUrl(requestInfo), request);
        if (responseHandler) {
            return await responseHandler.handleResponseAsync(response);
        } else {
            const payload = await response.arrayBuffer();
            const responseContentType = this.getResponseContentType(response);
            if (!responseContentType)
                throw new Error("no response content type found for deserialization");

            const rootNode = this.requestBuilderConfig.parseNodeFactory.getRootParseNode(responseContentType, payload);
            const result = rootNode.getObjectValue(type);
            return result as unknown as ModelType;
        }
    }
    public sendPrimitiveAsync = async <ResponseType>(requestInfo: RequestInfo, responseType: "string" | "number" | "boolean" | "Date" | "ReadableStream", responseHandler: ResponseHandler | undefined): Promise<ResponseType> => {
        if (!requestInfo) {
            throw new Error('requestInfo cannot be null');
        }
        await this.coreOptions.authenticationProvider.authenticateRequest(requestInfo);

        const request = this.getRequestFromRequestInfo(requestInfo);
        const response = await this.coreOptions.httpClient.fetch(this.getRequestUrl(requestInfo), request);
        if (responseHandler) {
            return await responseHandler.handleResponseAsync(response);
        } else {
            switch (responseType) {
                case "ReadableStream":
                    const buffer = await response.arrayBuffer();
                    let bufferPulled = false;
                    const stream = new ReadableStream({
                        pull: (controller) => {
                            if (!bufferPulled) {
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
                    if (!responseContentType)
                        throw new Error("no response content type found for deserialization");

                    const rootNode = this.requestBuilderConfig.parseNodeFactory.getRootParseNode(responseContentType, payload);
                    if (responseType === 'string') {
                        return rootNode.getStringValue() as unknown as ResponseType;
                    } else if (responseType === 'number') {
                        return rootNode.getNumberValue() as unknown as ResponseType;
                    } else if (responseType === 'boolean') {
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
        if (!requestInfo) {
            throw new Error('requestInfo cannot be null');
        }
        await this.coreOptions.authenticationProvider.authenticateRequest(requestInfo);

        const request = this.getRequestFromRequestInfo(requestInfo);
        const response = await this.coreOptions.httpClient.fetch(this.getRequestUrl(requestInfo), request);
        if (responseHandler) {
            return await responseHandler.handleResponseAsync(response);
        }
    }
    public enableBackingStore = (backingStoreFactory?: BackingStoreFactory | undefined): void => {
        this.requestBuilderConfig.parseNodeFactory = enableBackingStoreForParseNodeFactory(this.requestBuilderConfig.parseNodeFactory);
        this.requestBuilderConfig.serializationWriterFactory = enableBackingStoreForSerializationWriterFactory(this.requestBuilderConfig.serializationWriterFactory);
        if (!this.requestBuilderConfig.serializationWriterFactory || !this.requestBuilderConfig.parseNodeFactory)
            throw new Error("unable to enable backing store");
        if (backingStoreFactory) {
            BackingStoreFactorySingleton.instance = backingStoreFactory;
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
    private getRequestUrl = (requestInfo: RequestInfo): string => {
        let url = requestInfo.URI ?? '';
        if (requestInfo.queryParameters?.size ?? -1 > 0) {
            const queryParametersBuilder = new URLSearchParams();
            requestInfo.queryParameters?.forEach((v, k) => {
                queryParametersBuilder.append(k, `${v}`);
            });
            url = url + '?' + queryParametersBuilder.toString();
        }
        return url;
    }

    /**
     * Factory method to create HttpCore instance for url requests not using RequestBuilders
     * @param coreConfig the configuration used to set authprovider, httpClient or middleware, fetchOptions and more.
     */
    public static createCoreForPrimitiveRequests(coreConfig: CoreConfig): HttpCore {
        const coreOptions: CoreOptions = {
            httpClient: HttpCore.getHttpClient(coreConfig.httpClient, coreConfig.middleware),
            authenticationProvider: coreConfig.authenticationProvider,
            fetchOptions: coreConfig.fetchOptions
        }
        return new HttpCore({ parseNodeFactory: ParseNodeFactoryRegistry.defaultInstance, serializationWriterFactory: SerializationWriterFactoryRegistry.defaultInstance }, coreOptions);
    }

    /**
     * Factory method to create HttpCore instance to support Kiota Request Builders
     * @param requestBuilderConfig the configuration used to set ParseNodeFactory and serializationWriterFactory
     * @param coreConfig the configuration used to set authprovider, httpClient or middleware, fetchOptions and more.
     */
    public static createCoreForRequestBuilders(requestBuilderConfig: RequestBuilderConfig, coreConfig: CoreConfig): HttpCore {
        const coreOptions: CoreOptions = {
            httpClient: HttpCore.getHttpClient(coreConfig.httpClient, coreConfig.middleware),
            authenticationProvider: coreConfig.authenticationProvider,
            fetchOptions: coreConfig.fetchOptions
        }
        return new HttpCore(requestBuilderConfig, coreOptions);
    }

    // TODO : add factory method  return instance with default values.


    /**
     * @private
     * Static method returning an HttpClient instance which will be used by the HttpCore while making request.
     * @param httpClient the an HttpClient instance passed as a configuration of the HttpCore
     * @param middleware a single middleware or an array of middleware used to create an HttpClient
     */
    private static getHttpClient(httpClient?: HttpClient, middleware?: Middleware | Middleware[]): HttpClient {
        if (httpClient && middleware) {
            throw Error("Please provide either the HttpClient instance or the middleware configurations.");
        }
        if (httpClient) {
            return httpClient;
        }
        if (middleware) {
            // TODO: return with new middleware.
            return new HttpClient();
        }
        return new HttpClient();
    }
}