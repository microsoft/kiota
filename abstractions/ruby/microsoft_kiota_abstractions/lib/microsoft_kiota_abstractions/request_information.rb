require 'uri'
require 'addressable/template'
require_relative 'http_method'
require_relative 'request_headers'

module MicrosoftKiotaAbstractions
  class RequestInformation
    attr_reader :content, :http_method, :headers
    attr_accessor :url_template, :path_parameters, :query_parameters
    @@binary_content_type = 'application/octet-stream'
    @@content_type_header = 'Content-Type'
    @@raw_url_key = 'request-raw-url'

    def initialize()
      @headers = RequestHeaders.new
      @query_parameters = Hash.new
      @path_parameters = Hash.new
    end
    
    def uri=(arg)
      if arg.nil? || arg.empty?
        raise ArgumentError, 'arg cannot be nil or empty'
      end
      self.path_parameters.clear()
      self.query_parameters.clear()
      @uri = URI(arg)
    end

    def uri
      if @uri != nil
        return @uri
      else
        if self.path_parameters[@@raw_url_key] != nil
          self.uri = self.path_parameters[@@raw_url_key]
          return @uri
        else
          template = Addressable::Template.new(@url_template)
          return URI(template.expand(self.path_parameters.merge(self.query_parameters)).to_s)
        end
      end
    end

    def add_request_options(request_options_to_add)
      unless request_options_to_add.nil? then
        @request_options ||= Hash.new
        unless request_options_to_add.kind_of?(Array) then
          request_options_to_add = [request_options_to_add]
        end
        request_options_to_add.each do |request_option|
          key = request_option.get_key
          @request_options[key] = request_option
        end
      end
    end

    def get_request_options()
      if @request_options.nil? then
        return []
      else
        return @request_options.values
      end
    end

    def get_request_option(key)
      if @request_options.nil? || key.nil? || key.empty? then
        return nil
      else
        return @request_options[key]
      end
    end

    def remove_request_options(keys)
      unless keys.nil? || @request_options.nil? then
        unless keys.kind_of?(Array) then
          keys = [keys]
        end
        keys.each do |key|
          @request_options.delete(key)
        end
      end
    end

    def http_method=(method)
      @http_method = HttpMethod::HTTP_METHOD[method]
    end

    def set_stream_content(value = $stdin)
      @content = value
      @headers.add(@@content_type_header, @@binary_content_type)
    end

    def set_content_from_parsable(request_adapter, content_type, values)
      begin
        writer  = request_adapter.get_serialization_writer_factory().get_serialization_writer(content_type)
        @headers.add(@@content_type_header, content_type)
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

    def add_headers_from_raw_object(h)
      h.select{|x,y| @headers.add(x.to_s, y)} unless !h
    end
    
    def set_query_string_parameters_from_raw_object(q)
      if !q || q.is_a?(Hash) || q.is_a?(Array)
        return
      end
      q.class.instance_methods(false).select{|x|
        method_name = x.to_s
        unless method_name == "compare_by_identity" || method_name == "get_query_parameter" || method_name.end_with?("=") || method_name.end_with?("?") || method_name.end_with?("!") then
          begin
            key = q.get_query_parameter(method_name)
          rescue => exception
            key = method_name
          end
          value = eval("q.#{method_name}")
          self.query_parameters[key] = value unless value.nil?
        end
      }
    end

  end
end
