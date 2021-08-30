require 'microsoft_kiota_abstractions'
require 'net/https'
require 'net/http'
require 'concurrent'

module MicrosoftKiotaNethttp
  class HttpCore
    include MicrosoftKiotaAbstractions::HttpCore
    include Concurrent::Async

    attr_accessor :authentication_provider, :content_type_header_key, :parse_node_factory, :serialization_writer_factory, :client
    
    # TODO: When #478 is implemented then parse_node_factory and serialization_writer_factory should default to nil
    def initialize(authentication_provider, parse_node_factory, serialization_writer_factory, client = Net::HTTP)
      if !authentication_provider
        raise StandardError , 'authentication provider cannot be null'
      end
      @authentication_provider = authentication_provider
      @content_type_header_key = 'Content-Type'
      # TODO: When #478 is implemented get the static factories if @parse_node_factory and @serialization_writer_factory are nil
      @parse_node_factory = parse_node_factory 
      @serialization_writer_factory = serialization_writer_factory 
      @client = client
    end

    def send_async(request_info, type, response_handler)
      if !request_info
        raise StandardError, 'requestInfo cannot be null'
      end

      self.authentication_provider.await.authenticate_request(request_info)
      uri = request_info.uri
      http = @client.new(uri.host, uri.port)
      http.use_ssl = true
      http.verify_mode = OpenSSL::SSL::VERIFY_PEER
      request = self.get_request_from_request_info(request_info)
      response = http.request(request)

      if response_handler
        return response_handler.await.handle_response_async(response);
      else
        payload = response.body
        response_content_type = self.get_response_content_type(response);
        if !response_content_type
          raise StandardError, 'no response content type found for deserialization'
        end
        root_node = @parse_node_factory.get_parse_node(response_content_type, payload)
        root_node.get_object_value(type)
      end
    end

    def get_request_from_request_info(request_info)
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
