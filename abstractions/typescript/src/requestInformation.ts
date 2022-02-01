import * as urlTpl from "uri-template-lite";

import { HttpMethod } from "./httpMethod";
import { RequestAdapter } from "./requestAdapter";
import { RequestOption } from "./requestOption";
import { Parsable } from "./serialization";

/** This class represents an abstract HTTP request. */
export class RequestInformation {
  /** The URI of the request. */
  private uri?: string;
  /** The path parameters for the request. */
  public pathParameters: Record<string, unknown> = {};
  /** The URL template for the request */
  public urlTemplate?: string;
  /** Gets the URL of the request  */
  public get URL(): string {
    const rawUrl = this.pathParameters[
      RequestInformation.raw_url_key
    ] as string;
    if (this.uri) {
      return this.uri;
    } else if (rawUrl) {
      this.URL = rawUrl;
      return rawUrl;
    } else if (!this.queryParameters) {
      throw new Error("queryParameters cannot be undefined");
    } else if (!this.pathParameters) {
      throw new Error("pathParameters cannot be undefined");
    } else if (!this.urlTemplate) {
      throw new Error("urlTemplate cannot be undefined");
    } else {
      const template = new urlTpl.URI.Template(this.urlTemplate);
      const data = {} as { [key: string]: unknown };
      for (const key in this.queryParameters) {
        if (this.queryParameters[key]) {
          data[key] = this.queryParameters[key];
        }
      }
      for (const key in this.pathParameters) {
        if (this.pathParameters[key]) {
          data[key] = this.pathParameters[key];
        }
      }
      return template.expand(data);
    }
  }
  /** Sets the URL of the request */
  public set URL(url: string) {
    if (!url) throw new Error("URL cannot be undefined");
    this.uri = url;
    this.queryParameters = {};
    this.pathParameters = {};
  }
  public static raw_url_key = "request-raw-url";
  /** The HTTP method for the request */
  public httpMethod?: HttpMethod;
  /** The Request Body. */
  public content?: ArrayBuffer;
  /** The Query Parameters of the request. */
  public queryParameters: Record<
    string,
    string | number | boolean | undefined
  > = {}; //TODO: case insensitive
  /** The Request Headers. */
  public headers: Record<string, string> = {}; //TODO: case insensitive
  private _requestOptions: Record<string, RequestOption> = {}; //TODO: case insensitive
  /** Gets the request options for the request. */
  public getRequestOptions() {
    return this._requestOptions;
  }
  public addRequestOptions(...options: RequestOption[]) {
    if (!options || options.length === 0) return;
    options.forEach((option) => {
      this._requestOptions[option.getKey()] = option;
    });
  }
  /** Removes the request options for the request. */
  public removeRequestOptions(...options: RequestOption[]) {
    if (!options || options.length === 0) return;
    options.forEach((option) => {
      delete this._requestOptions[option.getKey()];
    });
  }
  private static binaryContentType = "application/octet-stream";
  private static contentTypeHeader = "Content-Type";
  /**
   * Sets the request body from a model with the specified content type.
   * @param values the models.
   * @param contentType the content type.
   * @param requestAdapter The adapter service to get the serialization writer from.
   * @typeParam T the model type.
   */
  public setContentFromParsable = <T extends Parsable>(
    requestAdapter?: RequestAdapter | undefined,
    contentType?: string | undefined,
    ...values: T[]
  ): void => {
    if (!requestAdapter) throw new Error("httpCore cannot be undefined");
    if (!contentType) throw new Error("contentType cannot be undefined");
    if (!values || values.length === 0) {
      throw new Error("values cannot be undefined or empty");
    }

    const writer = requestAdapter
      .getSerializationWriterFactory()
      .getSerializationWriter(contentType);
    if (!this.headers) {
      this.headers = {};
    }
    this.headers[RequestInformation.contentTypeHeader] = contentType;
    if (values.length === 1) {
      writer.writeObjectValue(undefined, values[0]);
    } else {
      writer.writeCollectionOfObjectValues(undefined, values);
    }
    this.content = writer.getSerializedContent();
  };
  /**
   * Sets the request body to be a binary stream.
   * @param value the binary stream
   */
  public setStreamContent = (value: ArrayBuffer): void => {
    this.headers[RequestInformation.contentTypeHeader] =
      RequestInformation.binaryContentType;
    this.content = value;
  };
  /**
   * Sets the query string parameters from a raw object.
   * @param parameters the parameters.
   */
  public setQueryStringParametersFromRawObject = (q: object): void => {
    Object.entries(q).forEach(([k, v]) => {
      this.queryParameters[k] = v;
    });
  };
}
