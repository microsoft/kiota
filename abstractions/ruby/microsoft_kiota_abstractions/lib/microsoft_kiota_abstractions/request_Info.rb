require 'uri'
require_relative "http_method"

module MicrosoftKiotaAbstractions
  class RequestInfo
    attr_reader :uri, :content, :http_method
    @@binaryContentType = "application/octet-stream"
    @@contentTypeHeader = "Content-Type"
    
    def uri=(arg)
      @uri = URI(arg)
    end

    def http_method=(method)
      @http_method = HttpMethod::HTTP_METHOD[method]
    end

    def query_parameters
      @query_parameters ||= Hash.new
    end

    def headers
      @headers ||= Hash.new
    end

    def setStreamContent(value = $stdin)
      @content = value
      @headers[@@contentTypeHeader] = @@binaryContentType
    end

    def setContentFromParsable(value, serializerFactory, contentType)
      begin
        writer  = serializerFactory.getSerializationWriter(contentType)
        headers[@@contentTypeHeader] = contentType
        writer.writeObjectValue(null, value);
        this.content = writer.getSerializedContent();
      rescue => exception
        raise Exception.new "could not serialize payload"
      end
    end

  end
end