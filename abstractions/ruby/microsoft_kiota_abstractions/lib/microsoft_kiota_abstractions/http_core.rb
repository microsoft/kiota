require_relative 'request_info'
require_relative 'response_handler'

module MicrosoftKiotaAbstractions
  module HttpCore
    include RequestInfo
    include ResponseHandler

    def sendAsync(request_info=RequestInfo, response_handler=ResponseHandler)
      raise NotImplementedError.new
    end

    def sendPrimitiveAsync(request_info=RequestInfo, response_handler=ResponseHandler)
      raise NotImplementedError.new
    end

    def sendAsync(request_info=RequestInfo, response_handler=ResponseHandler)
      raise NotImplementedError.new
    end

  end
end
