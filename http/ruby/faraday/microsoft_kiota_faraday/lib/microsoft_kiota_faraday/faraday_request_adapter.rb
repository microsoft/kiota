require 'microsoft_kiota_abstractions'
require 'net/https'
require 'net/http'

module MicrosoftKiotaFaraday
  class FaradayRequestAdapter
    include MicrosoftKiotaAbstractions::RequestAdapter

    attr_accessor :authentication_provider, :content_type_header_key, :parse_node_factory, :serialization_writer_factory, :client
    
    def initialize(authentication_provider, parse_node_factory=MicrosoftKiotaAbstractions::ParseNodeFactoryRegistry.default_instance, serialization_writer_factory=MicrosoftKiotaAbstractions::SerializationWriterFactoryRegistry.default_instance, client = Net::HTTP)

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

    def send_async(request_info, type, response_handler)
      if !request_info
        raise StandardError, 'requestInfo cannot be null'
      end

      Fiber.new do
        @authentication_provider.authenticate_request(request_info).resume
        request = self.get_request_from_request_info(request_info)
        uri = request_info.uri

        http = @client.new(uri.host, uri.port)
        http.use_ssl = true
        http.verify_mode = OpenSSL::SSL::VERIFY_PEER
        response = http.request(request)

        if response_handler
          response_handler.handle_response_async(response).resume;
        else
          payload = response.body
          response_content_type = self.get_response_content_type(response);

          unless response_content_type
            raise StandardError, 'no response content type found for deserialization'
          end
          root_node = @parse_node_factory.get_parse_node(response_content_type, payload)
          root_node.get_object_value(type)
        end
      end
    end

    def get_request_from_request_info(request_info)
      request_info.path_parameters['baseurl'] = @base_url
      case request_info.http_method
        when :GET
          request = @client::Get.new(request_info.uri.request_uri)
        when :POST
          request = @client::Post.new(request_info.uri.request_uri)
        when :PATCH
          request = @client::Patch.new(request_info.uri.request_uri)
        when :DELETE
          request = @client::Delete.new(request_info.uri.request_uri)
        when :OPTIONS
          request = @client::Options.new(request_info.uri.request_uri)
        when :CONNECT
          request = @client::Connect.new(request_info.uri.request_uri)
        when :PUT
          request = @client::Put.new(request_info.uri.request_uri)
        when :TRACE
          request = @client::Trace.new(request_info.uri.request_uri)
        when :HEAD
          request = @client::Head.new(request_info.uri.request_uri)
        else
          raise StandardError, 'unsupported http method'
      end
      if request_info.headers.instance_of? Hash
        request_info.headers.select{|k,v| request[k] = v }
      end
      if request_info.content != nil
        request.body = request_info.content # the json serialization writer returns a string at the moment, change to body_stream when this is fixed
      end
      request
    end

    def get_response_content_type(response)
      begin
        response['content-type'].split(';')[0].downcase()
      rescue
        return nil
      end
    end

  end
end
