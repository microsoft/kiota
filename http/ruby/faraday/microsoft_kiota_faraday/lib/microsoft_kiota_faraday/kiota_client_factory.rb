require 'net/https'
require 'faraday'
require_relative 'middleware/parameters_name_decoding_handler'
module MicrosoftKiotaFaraday
    class KiotaClientFactory
        def self.get_default_middleware()
            return [
                MicrosoftKiotaFaraday::Middleware::ParametersNameDecodingHandler
            ]
        end

        def self.get_default_http_client(middleware=nil, default_middleware_options=Array.new)
            if middleware.nil? #empty is fine in case the user doesn't want to use any middleware
                middleware = self.get_default_middleware()
            end
            connection_options = Hash.new
            connection_options[:request] = Hash.new
            connection_options[:request][:context] = Hash.new
            unless default_middleware_options.nil? || default_middleware_options.empty? then
                default_middleware_options.each do |value|
                    connection_options[:request][:context][value.get_key] = value
                end
            end
            conn = Faraday::Connection.new(nil, connection_options) do |builder|
                builder.adapter Faraday.default_adapter
                builder.ssl.verify = true
                builder.ssl.verify_mode = OpenSSL::SSL::VERIFY_PEER
                middleware.each do |handler|
                    builder.use handler
                end
            end
            conn
        end
    end
end