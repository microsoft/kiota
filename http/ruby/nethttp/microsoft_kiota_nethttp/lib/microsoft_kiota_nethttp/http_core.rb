require 'microsoft_kiota_abstractions'
require 'net/https'
require 'net/http'
require 'concurrent'

module MicrosoftKiotaNethttp
  class HttpCore
    include MicrosoftKiotaAbstractions::HttpCore
    include Concurrent::Async

    attr_accessor :authorization_header_key, :content_type_header_key, :parse_node_factory, :serialization_writer_factory, :client

    def initialize(authentication_provider, parse_node_factory, serialization_writer_factory, client = Net::HTTP)
      if !authentication_provider
        raise StandardError , 'authentication provider cannot be null'
      end
      @authorization_header_key = 'Authorization'
      @content_type_header_key = 'Content-Type'
      @parse_node_factory = parse_node_factory
      @serialization_writer_factory = serialization_writer_factory 
      @client = client
    end

    def sendAsync(request_info, type, response_handler)
      if !request_info
        raise StandardError, 'requestInfo cannot be null'
      end
      self.await.add_bearer_if_not_present(request_info)
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

    def add_bearer_if_not_present(request_info)
      if !request_info.uri
        raise StandardError, 'uri cannot be null'
      end
      if !request_info.headers.has_key?(@authorization_header_key) 
        return self.authentication_provider.await.get_authorization_token(request_info.URI)
        token = self.authentication_provider.await.get_authorization_token(request_info.URI)
        if !token
          raise StandardError, 'Could not get an authorization token'
        end
        if !request_info.headers
          request_info.headers Hash.new()
        end
        request_info.headers[@authorization_header_key] = `Bearer #{token}`
      end
    end

    def get_request_from_request_info(request_info)
      #TODO Add swtich using reequest_info.http_method for the different types of requests
      request = @client::Get.new(uri.request_uri)
      if request_info.headers.instance_of? Hash
        request_info.headers.select{|k,v| request[k] = v }
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
