require 'net/https'
require 'faraday'
module MicrosoftKiotaFaraday
    class KiotaClientFactory
        def self.get_default_middleware()
            #TODO
        end

        def self.get_default_http_client(middleware=nil)
            if middleware.nil? #empty is fine in case the user doesn't want to use any middleware
                middleware = self.get_default_middleware()
            end
            conn = Faraday::Connection.new do |builder|
                builder.adapter Faraday.default_adapter
                builder.ssl.verify = true
                builder.ssl.verify_mode = OpenSSL::SSL::VERIFY_PEER
            end
            
            #TODO iterate over the middleware
            # conn.use Faraday::Response::RaiseError
            # https://lostisland.github.io/faraday/middleware/
            # https://stackoverflow.com/questions/31075517/adding-params-in-faraday-middleware
            conn
        end
    end
end