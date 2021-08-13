require 'uri'
require_relative 'http_method'

module MicrosoftKiotaAbstractions
  class RequestInfo
    attr_reader :uri, :content, :http_method
    @@binary_content_type = 'application/octet-stream'
    @@content_type_header = 'Content-Type'
    
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

    def set_stream_content(value = $stdin)
      @content = value
      @headers[@@content_type_header] = @@binary_content_type
    end

    def set_content_from_parsable(serializer_factory, content_type, values)
      begin
        writer  = serializer_factory.get_serialization_writer(content_type)
        headers[@@content_type_header] = content_type
        if values != nil && values.kind_of?(Array)
          writer.write_collection_of_object_values(nil, values)
        else
          writer.write_object_value(nil, values);
        end
        this.content = writer.get_serialized_content();
      rescue => exception
        raise Exception.new "could not serialize payload"
      end
    end

  end
end
