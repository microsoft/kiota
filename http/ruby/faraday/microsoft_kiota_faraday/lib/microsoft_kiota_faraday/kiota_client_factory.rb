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

        def self.get_default_http_client(middleware=nil)
            if middleware.nil? #empty is fine in case the user doesn't want to use any middleware
                middleware = self.get_default_middleware()
            end
            conn = Faraday::Connection.new do |builder|
                builder.adapter Faraday.default_adapter
                builder.ssl.verify = true
                builder.ssl.verify_mode = OpenSSL::SSL::VERIFY_PEER
                middleware.each do |middleware|
                    builder.use middleware
                end
            end
            conn
        end
    end
end