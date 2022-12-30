require 'microsoft_kiota_abstractions'
require 'faraday'
require 'net/http'
require_relative 'kiota_client_factory'
require_relative 'middleware/response_handler_option'

module MicrosoftKiotaFaraday
  class FaradayRequestAdapter
    include MicrosoftKiotaAbstractions::RequestAdapter

    attr_accessor :authentication_provider, :content_type_header_key, :parse_node_factory, :serialization_writer_factory, :client
    
    def initialize(authentication_provider, parse_node_factory=MicrosoftKiotaAbstractions::ParseNodeFactoryRegistry.default_instance, serialization_writer_factory=MicrosoftKiotaAbstractions::SerializationWriterFactoryRegistry.default_instance, client = KiotaClientFactory::get_default_http_client)

      if !authentication_provider
        raise StandardError , 'authentication provider cannot be null'
      end
      @authentication_provider = authentication_provider
      @content_type_header_key = 'Content-Type'
      @parse_node_factory = parse_node_factory
      if @parse_node_factory.nil?
        @parse_node_factory = MicrosoftKiotaAbstractions::ParseNodeFactoryRegistry.default_instance
      end
      @serialization_writer_factory = serialization_writer_factory 
      if @serialization_writer_factory.nil?
        @serialization_writer_factory = MicrosoftKiotaAbstractions::SerializationWriterFactoryRegistry.default_instance
      end
      @client = client
      if @client.nil?
        @client = KiotaClientFactory::get_default_http_client
      end
      @base_url = ''
    end

    def set_base_url(base_url)
      @base_url = base_url
    end

    def get_base_url()
      @base_url
    end

    def get_serialization_writer_factory()
      @serialization_writer_factory
    end

    def send_async(request_info, factory, errors_mapping)
      raise StandardError, 'request_info cannot be null' unless request_info
      raise StandardError, 'factory cannot be null' unless factory

      Fiber.new do
        @authentication_provider.authenticate_request(request_info).resume
        request = self.get_request_from_request_info(request_info)
        response = @client.run_request(request.http_method, request.path, request.body, request.headers)

        response_handler = self.get_response_handler(request_info)
        response_handler.call(response).resume unless response_handler.nil?
        self.throw_if_failed_reponse(response, errors_mapping)
        root_node = self.get_root_parse_node(response)
        root_node.get_object_value(factory)
      end
    end

    def get_response_handler(request_info)
      option = request_info.get_request_option(MicrosoftKiotaFaraday::Middleware::ResponseHandlerOption::RESPONSE_HANDLER_KEY) unless request_info.nil?
      return option.async_callback unless !option || option.nil?
    end

    def get_root_parse_node(response)
      raise StandardError, 'response cannot be null' unless response
      response_content_type = self.get_response_content_type(response);
      raise StandardError, 'no response content type found for deserialization' unless response_content_type
      return @parse_node_factory.get_parse_node(response_content_type, response.body)
    end

    def throw_if_failed_reponse(response, errors_mapping)
      raise StandardError, 'response cannot be null' unless response

      status_code = response.status;
      if status_code < 400 then
        return
      end
      error_factory = errors_mapping[status_code] unless errors_mapping.nil?
      error_factory = errors_mapping['4XX'] unless !error_factory.nil? || errors_mapping.nil? || status_code > 500
      error_factory = errors_mapping['5XX'] unless !error_factory.nil? || errors_mapping.nil? || status_code < 500 || status_code > 600
      raise MicrosoftKiotaAbstractions::ApiError, 'The server returned an unexpected status code and no error factory is registered for this code:' + status_code.to_s if error_factory.nil?
      root_node = self.get_root_parse_node(response)
      error = root_node.get_object_value(error_factory) unless root_node.nil?
      raise error unless error.nil?
      raise MicrosoftKiotaAbstractions::ApiError, 'The server returned an unexpected status code:' + status_code.to_s
    end

    def get_request_from_request_info(request_info)
      request_info.path_parameters['baseurl'] = @base_url
      case request_info.http_method
        when :GET
          request = @client.build_request(:get)
        when :POST
          request = @client.build_request(:post)
        when :PATCH
          request = @client.build_request(:patch)
        when :DELETE
          request = @client.build_request(:delete)
        when :OPTIONS
          request = @client.build_request(:options)
        when :CONNECT
          request = @client.build_request(:connect)
        when :PUT
          request = @client.build_request(:put)
        when :TRACE
          request = @client.build_request(:trace)
        when :HEAD
          request = @client.build_request(:head)
        else
          raise StandardError, 'unsupported http method'
      end
      request.path = request_info.uri
      unless request_info.headers.nil? then
        request.headers = Faraday::Utils::Headers.new
        request_info.headers.get_all.select{|k,v| 
          if v.kind_of? Array then
            request.headers[k] = v.join(',')
          elsif v.kind_of? String then
            request.headers[k] = v
          else
            request.headers[k] = v.to_s
          end
        }
      end
      request.body = request_info.content unless request_info.content.nil? || request_info.content.empty?
      # TODO the json serialization writer returns a string at the moment, change to body_stream when this is fixed
      request_options = request_info.get_request_options
      if !request_options.nil? && !request_options.empty? then
        request.options = Faraday::RequestOptions.new if request.options.nil?
        request_options.each do |value|
          request.options.context[value.get_key] = value
        end
      end
      request
    end

    def get_response_content_type(response)
      begin
        response.headers['content-type'].split(';')[0].downcase()
      rescue
        return nil
      end
    end

  end
end
