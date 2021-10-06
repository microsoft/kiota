require 'uri'
require_relative 'http_method'

module MicrosoftKiotaAbstractions
  class RequestInformation
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

    def set_uri(current_path, path_segment, is_raw_url)
      if is_raw_url
        if current_path.nil? || current_path.empty?
          raise ArgumentError, 'current_path cannot be nil or empty'
        end
        question_mark_splat = current_path.split(/\?/)
        scheme_host_and_path = question_mark_splat[0]
        if question_mark_splat.length > 1
          query_parameters = question_mark_splat[1]
          query_parameters.split(/&/).each do |query_parameter|
            key_value_pair = query_parameter.split(/=/)
            if key_value_pair.length > 1
              query_parameters[key_value_pair[0]] = key_value_pair[1]
            elsif key_value_pair.length == 1
              query_parameters[key_value_pair[0]] = nil
            end
          end
        end          
        @uri = URI(current_path)
      else
        @uri = URI(current_path + path_segment)
      end
    end

    def set_stream_content(value = $stdin)
      @content = value
      @headers[@@content_type_header] = @@binary_content_type
    end

    def set_content_from_parsable(request_adapter, content_type, values)
      begin
        writer  = request_adapter.get_serialization_writer_factory().get_serialization_writer(content_type)
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

    def set_headers_from_raw_object(h)
      if !h
        return
      end
      h.select{|x,y| @headers[x.to_s] = y.to_s}
    end
    
    def set_query_string_parameters_from_raw_object(q)
      if !q
        return
      end
      q.select{|x,y| @query_parameters[x.to_s] = y.to_s}
    end

  end
end
