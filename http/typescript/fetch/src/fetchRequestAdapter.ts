import { ApiError, AuthenticationProvider, BackingStoreFactory, BackingStoreFactorySingleton, RequestAdapter, Parsable, ParseNodeFactory, RequestInformation, ResponseHandler, ParseNodeFactoryRegistry, enableBackingStoreForParseNodeFactory, SerializationWriterFactoryRegistry, enableBackingStoreForSerializationWriterFactory, SerializationWriterFactory, ParseNode } from '@microsoft/kiota-abstractions';
import { HttpClient } from './httpClient';

export class FetchRequestAdapter implements RequestAdapter {
    /** The base url for every request. */
    public baseUrl: string = '';
    public getSerializationWriterFactory(): SerializationWriterFactory {
        return this.serializationWriterFactory;
    }
    /**
     * Instantiates a new http core service
     * @param authenticationProvider the authentication provider to use.
     * @param parseNodeFactory the parse node factory to deserialize responses.
     * @param serializationWriterFactory the serialization writer factory to use to serialize request bodies.
     * @param httpClient the http client to use to execute requests.
     */
    public constructor(public readonly authenticationProvider: AuthenticationProvider, private parseNodeFactory: ParseNodeFactory = ParseNodeFactoryRegistry.defaultInstance, private serializationWriterFactory: SerializationWriterFactory = SerializationWriterFactoryRegistry.defaultInstance, private readonly httpClient: HttpClient = new HttpClient()) {
        if (!authenticationProvider) {
            throw new Error('authentication provider cannot be null');
        }
        if (!parseNodeFactory) {
            throw new Error('parse node factory cannot be null');
        }
        if (!serializationWriterFactory) {
            throw new Error('serialization writer factory cannot be null');
        }
        if (!httpClient) {
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
    public sendCollectionOfPrimitiveAsync = async <ResponseType>(requestInfo: RequestInformation, responseType: "string" | "number" | "boolean" | "Date", responseHandler: ResponseHandler | undefined, errorMappings: Record<string, new () => Parsable> | undefined): Promise<ResponseType[] | undefined> => {
        if (!requestInfo) {
            throw new Error('requestInfo cannot be null');
        }
        const response = await this.getHttpResponseMessage(requestInfo);
        if (responseHandler) {
            return await responseHandler.handleResponseAsync(response, errorMappings);
        } else {
            await this.throwFailedResponses(response, errorMappings);
            switch (responseType) {
                case 'string':
                case 'number':
                case 'boolean':
                case 'Date':
                    const rootNode = await this.getRootParseNode(response);
                    if (responseType === 'string') {
                        return rootNode.getCollectionOfPrimitiveValues<string>() as unknown as ResponseType[];
                    } else if (responseType === 'number') {
                        return rootNode.getCollectionOfPrimitiveValues<number>() as unknown as ResponseType[];
                    } else if (responseType === 'boolean') {
                        return rootNode.getCollectionOfPrimitiveValues<boolean>() as unknown as ResponseType[];
                    } else if (responseType === 'Date') {
                        return rootNode.getCollectionOfPrimitiveValues<Date>() as unknown as ResponseType[];
                    } else {
                        throw new Error("unexpected type to deserialize");
                    }
            }
        }
    }
    public sendCollectionAsync = async <ModelType extends Parsable>(requestInfo: RequestInformation, type: new () => ModelType, responseHandler: ResponseHandler | undefined, errorMappings: Record<string, new () => Parsable> | undefined): Promise<ModelType[]> => {
        if (!requestInfo) {
            throw new Error('requestInfo cannot be null');
        }
        const response = await this.getHttpResponseMessage(requestInfo);
        if (responseHandler) {
            return await responseHandler.handleResponseAsync(response, errorMappings);
        } else {
            await this.throwFailedResponses(response, errorMappings);
            const rootNode = await this.getRootParseNode(response);
            const result = rootNode.getCollectionOfObjectValues(type);
            return result as unknown as ModelType[];
        }
    }
    public sendAsync = async <ModelType extends Parsable>(requestInfo: RequestInformation, type: new () => ModelType, responseHandler: ResponseHandler | undefined, errorMappings: Record<string, new () => Parsable> | undefined): Promise<ModelType> => {
        if (!requestInfo) {
            throw new Error('requestInfo cannot be null');
        }
        const response = await this.getHttpResponseMessage(requestInfo);
        if (responseHandler) {
            return await responseHandler.handleResponseAsync(response, errorMappings);
        } else {
            await this.throwFailedResponses(response, errorMappings);
            const rootNode = await this.getRootParseNode(response);
            const result = rootNode.getObjectValue(type);
            return result as unknown as ModelType;
        }
    }
    public sendPrimitiveAsync = async <ResponseType>(requestInfo: RequestInformation, responseType: "string" | "number" | "boolean" | "Date" | "ArrayBuffer", responseHandler: ResponseHandler | undefined, errorMappings: Record<string, new () => Parsable> | undefined): Promise<ResponseType> => {
        if (!requestInfo) {
            throw new Error('requestInfo cannot be null');
        }
        const response = await this.getHttpResponseMessage(requestInfo);
        if (responseHandler) {
            return await responseHandler.handleResponseAsync(response, errorMappings);
        } else {
            await this.throwFailedResponses(response, errorMappings);
            switch (responseType) {
                case "ArrayBuffer":
                    return await response.arrayBuffer() as unknown as ResponseType;
                case 'string':
                case 'number':
                case 'boolean':
                case 'Date':
                    const rootNode = await this.getRootParseNode(response);
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
    public sendNoResponseContentAsync = async (requestInfo: RequestInformation, responseHandler: ResponseHandler | undefined, errorMappings: Record<string, new () => Parsable> | undefined): Promise<void> => {
        if (!requestInfo) {
            throw new Error('requestInfo cannot be null');
        }
        const response = await this.getHttpResponseMessage(requestInfo);
        if (responseHandler) {
            return await responseHandler.handleResponseAsync(response, errorMappings);
        }
        await this.throwFailedResponses(response, errorMappings);
    }
    public enableBackingStore = (backingStoreFactory?: BackingStoreFactory | undefined): void => {
        this.parseNodeFactory = enableBackingStoreForParseNodeFactory(this.parseNodeFactory);
        this.serializationWriterFactory = enableBackingStoreForSerializationWriterFactory(this.serializationWriterFactory);
        if (!this.serializationWriterFactory || !this.parseNodeFactory)
            throw new Error("unable to enable backing store");
        if (backingStoreFactory) {
            BackingStoreFactorySingleton.instance = backingStoreFactory;
        }
    }
    private getRootParseNode = async (response: Response) : Promise<ParseNode> => {
        const payload = await response.arrayBuffer();
        const responseContentType = this.getResponseContentType(response);
        if (!responseContentType)
            throw new Error("no response content type found for deserialization");

        return this.parseNodeFactory.getRootParseNode(responseContentType, payload);
    }
    private throwFailedResponses = async (response: Response, errorMappings: Record<string, new () => Parsable> | undefined): Promise<void> => {
        if(response.ok) return;

        const statusCode = response.status;
        const statusCodeAsString = statusCode.toString();
        if(!errorMappings ||
            !errorMappings[statusCodeAsString] &&
            !(statusCode >= 400 && statusCode < 500 && errorMappings['4XX']) &&
            !(statusCode >= 500 && statusCode < 600 && errorMappings['5XX']))
            throw new ApiError("the server returned an unexpected status code and no error class is registered for this code " + statusCode);
        
        const factory = errorMappings[statusCodeAsString] ?? 
                        (statusCode >= 400 && statusCode < 500 ? errorMappings['4XX'] : undefined) ??
                        (statusCode >= 500 && statusCode < 600 ? errorMappings['5XX'] : undefined);
        
        const rootNode = await this.getRootParseNode(response);
        const error = rootNode.getObjectValue(factory);
        
        if(error instanceof Error) throw error;
        else throw new ApiError("unexpected error type" + typeof(error))
    }
    private getHttpResponseMessage = async (requestInfo: RequestInformation): Promise<Response> => {
        if (!requestInfo) {
            throw new Error('requestInfo cannot be null');
        }
        this.setBaseUrlForRequestInformation(requestInfo);
        await this.authenticationProvider.authenticateRequest(requestInfo);

        const request = this.getRequestFromRequestInformation(requestInfo);
        return await this.httpClient.fetch(requestInfo.URL, request);
    }
    private setBaseUrlForRequestInformation = (requestInfo: RequestInformation): void => {
        requestInfo.pathParameters["baseurl"] = this.baseUrl;
    }
    private getRequestFromRequestInformation = (requestInfo: RequestInformation): RequestInit => {
        const request = {
            method: requestInfo.httpMethod?.toString(),
            headers: requestInfo.headers,
            body: requestInfo.content,
        } as RequestInit;
        return request;
    }
}