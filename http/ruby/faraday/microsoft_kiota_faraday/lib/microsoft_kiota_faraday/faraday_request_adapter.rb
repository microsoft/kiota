require 'microsoft_kiota_abstractions'
require 'faraday'
require 'net/http'
require_relative 'kiota_client_factory'

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

    def send_async(request_info, factory, response_handler)
      raise StandardError, 'request_info cannot be null' unless request_info
      raise StandardError, 'factory cannot be null' unless factory

      Fiber.new do
        @authentication_provider.authenticate_request(request_info).resume
        request = self.get_request_from_request_info(request_info)
        response = @client.run_request(request.http_method, request.path, request.body, request.headers)

        if response_handler
          response_handler.handle_response_async(response).resume;
        else
          payload = response.body
          response_content_type = self.get_response_content_type(response);
          raise StandardError, 'no response content type found for deserialization' unless response_content_type
          root_node = @parse_node_factory.get_parse_node(response_content_type, payload)
          root_node.get_object_value(factory)
        end
      end
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
      if request_info.headers.instance_of? Hash
        request.headers = Faraday::Utils::Headers.new
        request_info.headers.select{|k,v| request.headers[k] = v }
      end
      request.body = request_info.content unless request_info.content.nil? || request_info.content.empty?
      # TODO the json serialization writer returns a string at the moment, change to body_stream when this is fixed
      request_options = request_info.get_request_options
      if !request_options.nil? && !request_options.empty? then
        request.options = Faraday::RequestOptions.new
        request_options.each do |value|
          request.options.context[value.key] = value
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
