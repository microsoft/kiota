require_relative "request_info"
require_relative "response_handler"

module MicrosoftKiotaAbstractions
    module HttpCore
        include RequestInfo
        include ResponseHandler

        def sendAsync(requestInfo=RequestInfo, ResponseHandler=ResponseHandler)
            raise NotImplementedError.new
        end

        def sendPrimitiveAsync(requestInfo=RequestInfo, ResponseHandler=ResponseHandler)
            raise NotImplementedError.new
        end

        def sendAsync(requestInfo=RequestInfo, ResponseHandler=ResponseHandler)
            raise NotImplementedError.new
        end

    end
end
